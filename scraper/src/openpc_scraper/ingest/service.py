"""Ingestão com dedup em 3 níveis — espelho de IngestionService.cs.

1. part number do fabricante (match exato)
2. chave determinística marca+modelo (match por tokens)
3. fila de revisão (sem match confiável → produto próprio + candidato)
"""

from __future__ import annotations

import logging
import uuid
from dataclasses import dataclass, field

from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from ..collect.models import RawListing
from ..db.models import (
    Category,
    Listing,
    PriceHistory,
    Product,
    ProductAttribute,
    ProductMatchCandidate,
    Store,
)
from ..normalize import noise_filter, part_number, reference
from ..normalize.text import normalize as normalize_text
from .attributes import apply_specs

logger = logging.getLogger("openpc_scraper.ingest")

_BRANDS = (
    "amd", "intel", "nvidia", "gigabyte", "asus", "msi", "kingston", "corsair",
    "samsung", "wd", "seagate", "xpg", "teamgroup", "coolermaster", "deepcool",
)

_BRAND_NAMES = {"wd": "Western Digital", "teamgroup": "TeamGroup", "coolermaster": "Cooler Master"}

_TITLE_PREFIXES = ("Processador ", "Placa de vídeo ", "Memória ", "Gabinete ")


@dataclass
class IngestResult:
    items_found: int = 0
    new_products: int = 0
    new_listings: int = 0
    new_candidates: int = 0
    price_drop_product_ids: set[uuid.UUID] = field(default_factory=set)


def clean_title(title: str) -> str:
    t = title.strip()
    for prefix in _TITLE_PREFIXES:
        if t.lower().startswith(prefix.lower()):
            return t[len(prefix):]
    return t


def normalize_brand(manufacturer: str | None, title: str) -> str:
    t = normalize_text(manufacturer or title)
    for brand in _BRANDS:
        if brand in t:
            return _BRAND_NAMES.get(brand, brand)
    return "Outros"


def _normalize_model(match_key: str | None) -> str:
    return match_key if match_key else uuid.uuid4().hex[:12]


class IngestionService:
    """Persiste listings coletados; dedup por part number → match key → fila."""

    def __init__(self, db: AsyncSession) -> None:
        self._db = db

    async def ingest(
        self, store: Store, category_slug: str, items: list[RawListing]
    ) -> IngestResult:
        db = self._db

        category = (await db.scalars(select(Category).where(Category.slug == category_slug))).one()

        # descarta ruído de categoria antes de persistir
        relevant = [i for i in items if not noise_filter.is_noise(category_slug, i.title)]
        if len(relevant) < len(items):
            logger.info(
                "Ingestão %s/%s: %d itens descartados como ruído de categoria",
                store.slug, category_slug, len(items) - len(relevant),
            )
        items = relevant

        products = (await db.scalars(select(Product).where(Product.category_id == category.id))).all()
        by_part_number = {p.part_number: p for p in products if p.part_number}
        by_match_key = {p.model: p for p in products if p.model}

        attributes_rows = (
            await db.execute(
                select(ProductAttribute).where(ProductAttribute.product_id.in_([p.id for p in products]))
            )
        ).scalars().all()
        attributes_by_product: dict[uuid.UUID, dict[str, ProductAttribute]] = {}
        for a in attributes_rows:
            attributes_by_product.setdefault(a.product_id, {})[a.key] = a

        listings = (await db.scalars(select(Listing).where(Listing.store_id == store.id))).all()
        existing_listings = {l.store_sku: l for l in listings}

        products_by_id = {p.id: p for p in products if p.id in {l.product_id for l in listings}}

        # menor preço em estoque por produto (para detectar quedas → alertas)
        rows = (
            await db.execute(
                select(PriceHistory.listing_id, PriceHistory.price_cash)
                .where(PriceHistory.in_stock.is_(True), PriceHistory.price_cash.isnot(None))
            )
        ).all()
        listing_min: dict[uuid.UUID, float] = {}
        listing_product: dict[uuid.UUID, uuid.UUID] = {l.id: l.product_id for l in listings}
        for listing_id, price in rows:
            pid = listing_product.get(listing_id)
            if pid is not None:
                listing_min[pid] = min(listing_min.get(pid, float("inf")), price)

        expect_anchor = category_slug in ("cpu", "gpu")

        result = IngestResult(items_found=len(items))

        for item in items:
            # âncora primária: listing existente da loja (SKU estável entre scrapes)
            listing = existing_listings.get(item.store_sku)
            product = (
                products_by_id.get(listing.product_id)
                if listing is not None and listing.product_id in products_by_id
                else self._resolve_product(item, by_part_number, by_match_key)
            )

            if product is None:
                product = Product(
                    id=uuid.uuid4(),
                    category_id=category.id,
                    brand=normalize_brand(item.manufacturer, item.title),
                    model=_normalize_model(item.match_key),
                    name=clean_title(item.title),
                    part_number=part_number.normalize(item.part_number) if item.part_number else None,
                    image_url=item.thumbnail,
                    spec_source="scraper",
                )
                db.add(product)
                if product.part_number:
                    by_part_number[product.part_number] = product
                by_match_key[product.model] = product
                products_by_id[product.id] = product
                result.new_products += 1

                if expect_anchor and item.part_number is None and item.match_key is None:
                    db.add(ProductMatchCandidate(
                        id=uuid.uuid4(),
                        product_id=product.id,
                        store_id=store.id,
                        store_sku=item.store_sku,
                        title=item.title,
                        reason="no_anchor",
                    ))
                    result.new_candidates += 1
            else:
                product.name = clean_title(item.title)
                if product.image_url is None:
                    product.image_url = item.thumbnail
                if product.part_number is None and item.part_number is not None:
                    product.part_number = part_number.normalize(item.part_number)

            self._apply_attributes(product, item, attributes_by_product)

            if listing is None:
                listing = Listing(
                    id=uuid.uuid4(),
                    product_id=product.id,
                    store_id=store.id,
                    store_sku=item.store_sku,
                    url=item.product_url,
                    title=item.title,
                )
                db.add(listing)
                existing_listings[item.store_sku] = listing
                result.new_listings += 1

            listing.product_id = product.id
            listing.url = item.product_url
            listing.title = item.title
            listing.price_cash = item.price_cash
            listing.price_card = item.price_card
            listing.installments = item.installments
            listing.installment_text = item.installment_text
            listing.in_stock = item.in_stock
            if listing.thumbnail is None:
                listing.thumbnail = item.thumbnail

            # queda de preço → candidato a alerta (M6)
            if (
                item.in_stock
                and item.price_cash is not None
                and product.id in listing_min
                and item.price_cash < listing_min[product.id]
            ):
                result.price_drop_product_ids.add(product.id)

            # preço mudou? append ao histórico (append-only)
            last = (
                await db.scalars(
                    select(PriceHistory)
                    .where(PriceHistory.listing_id == listing.id)
                    .order_by(PriceHistory.collected_at.desc())
                    .limit(1)
                )
            ).first()
            if last is None or last.price_cash != item.price_cash or last.in_stock != item.in_stock:
                db.add(PriceHistory(
                    id=uuid.uuid4(),
                    listing_id=listing.id,
                    price_cash=item.price_cash or 0,
                    price_card=item.price_card,
                    in_stock=item.in_stock,
                ))

        await db.commit()

        logger.info(
            "Ingestão %s/%s: %d itens, %d produtos novos, %d listings novos, %d na fila",
            store.slug, category_slug, len(items), result.new_products,
            result.new_listings, result.new_candidates,
        )
        return result

    def _resolve_product(
        self,
        item: RawListing,
        by_part_number: dict[str, Product],
        by_match_key: dict[str, Product],
    ) -> Product | None:
        if item.part_number is not None:
            normalized = part_number.normalize(item.part_number)
            if normalized in by_part_number:
                return by_part_number[normalized]
        if item.match_key is not None and item.match_key in by_match_key:
            return by_match_key[item.match_key]
        return None

    def _apply_attributes(
        self,
        product: Product,
        item: RawListing,
        cache: dict[uuid.UUID, dict[str, ProductAttribute]],
    ) -> None:
        """Specs do produto em ordem de precedência: banco de referência por
        chip (preenche lacunas) e depois título da listagem (sobrescreve).
        A ficha da página de produto (source='page') é aplicada pelo job
        collect-details e vence ambas."""
        existing = cache.setdefault(product.id, {})

        ref = reference.lookup(item.category_slug, item.title)
        if ref is not None:
            apply_specs(self._db, product, ref[1], "reference", existing)

        apply_specs(self._db, product, item.specs, "title", existing)

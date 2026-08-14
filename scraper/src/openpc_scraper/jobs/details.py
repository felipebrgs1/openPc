"""Coleta da ficha técnica da página de produto (job collect-details).

Visita as páginas de produto de listings em estoque que ainda não tiveram a
ficha coletada (ou cuja coleta venceu), extrai as specs via o extrator da
loja (Kabum HTTP / Pichau-Terabyte browser) e aplica com source='page' —
precedência máxima entre as fontes automáticas (page > title > reference).

Rodar com limite pequeno (default 20): cada visita é uma página de produto
a mais por loja; o timestamp SpecsCollectedAt garante progresso incremental
entre execuções.
"""

from __future__ import annotations

import asyncio
import logging
import random
import uuid
from dataclasses import dataclass
from datetime import datetime, timedelta, timezone

import httpx
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession
from sqlalchemy.orm import selectinload

from ..collect.browser import BrowserPool
from ..collect.details import browser_stores
from ..collect.details.kabum import HEADERS, fetch_product_specs
from ..db.models import Category, Listing, ProductAttribute, Store
from ..ingest.attributes import apply_specs
from ..normalize.spec_map import map_specs

logger = logging.getLogger("openpc_scraper.details")

MAX_PER_PRODUCT_PAGES = 200  # teto de segurança por execução


@dataclass
class DetailsResult:
    visited: int = 0
    specs_found: int = 0
    attrs_changed: int = 0
    failed: int = 0


class DetailsCollectionService:
    def __init__(self, db: AsyncSession, pool: BrowserPool | None = None) -> None:
        self._db = db
        self._pool = pool or BrowserPool()

    async def collect(
        self,
        store_slug: str,
        category_slug: str,
        limit: int = 20,
        refresh_days: int = 30,
        concurrency: int = 3,
    ) -> DetailsResult:
        db = self._db
        limit = max(1, min(limit, MAX_PER_PRODUCT_PAGES))

        store = (await db.scalars(select(Store).where(Store.slug == store_slug))).one_or_none()
        if store is None:
            raise ValueError(f"Loja '{store_slug}' não encontrada")
        category = (
            await db.scalars(select(Category).where(Category.slug == category_slug))
        ).one_or_none()
        if category is None:
            raise ValueError(f"Categoria '{category_slug}' não encontrada")

        cutoff = datetime.now(timezone.utc) - timedelta(days=refresh_days)
        listings = (
            await db.scalars(
                select(Listing)
                .options(selectinload(Listing.product))
                .join(Listing.product)
                .where(
                    Listing.store_id == store.id,
                    Listing.in_stock.is_(True),
                    (Listing.specs_collected_at.is_(None)) | (Listing.specs_collected_at < cutoff),
                )
                .order_by(Listing.last_seen_at.desc())
                .limit(limit)
            )
        ).all()
        if not listings:
            logger.info("collect-details %s/%s: nada pendente", store_slug, category_slug)
            return DetailsResult()

        # cache de atributos por produto (aplicação com precedência)
        product_ids = {l.product_id for l in listings}
        attrs = (
            await db.scalars(
                select(ProductAttribute).where(ProductAttribute.product_id.in_(product_ids))
            )
        ).all()
        cache: dict[uuid.UUID, dict[str, ProductAttribute]] = {}
        for a in attrs:
            cache.setdefault(a.product_id, {})[a.key] = a

        result = DetailsResult()
        if store_slug == "kabum":
            await self._collect_kabum(listings, category_slug, concurrency, cache, result)
        else:
            await self._collect_browser(store_slug, listings, category_slug, cache, result)

        await db.commit()
        logger.info(
            "collect-details %s/%s: %d visitadas, %d com ficha, %d attrs, %d falhas",
            store_slug, category_slug, result.visited, result.specs_found,
            result.attrs_changed, result.failed,
        )
        return result

    async def _collect_kabum(
        self,
        listings: list[Listing],
        category_slug: str,
        concurrency: int,
        cache: dict[uuid.UUID, dict[str, ProductAttribute]],
        result: DetailsResult,
    ) -> None:
        sem = asyncio.Semaphore(max(1, concurrency))

        async def visit(listing: Listing) -> None:
            async with sem:
                result.visited += 1
                try:
                    async with httpx.AsyncClient(
                        headers=HEADERS, timeout=httpx.Timeout(30.0), follow_redirects=True
                    ) as client:
                        specs = await fetch_product_specs(client, listing.url, category_slug)
                    await self._apply(listing, specs, cache, result)
                    await asyncio.sleep(random.uniform(0.8, 1.6))  # rate limit gentil
                except Exception as exc:  # noqa: BLE001 — uma falha não derruba o lote
                    result.failed += 1
                    logger.warning("collect-details kabum %s: %s", listing.url, exc)

        await asyncio.gather(*(visit(l) for l in listings))

    async def _collect_browser(
        self,
        store_slug: str,
        listings: list[Listing],
        category_slug: str,
        cache: dict[uuid.UUID, dict[str, ProductAttribute]],
        result: DetailsResult,
    ) -> None:
        page = await self._pool.new_page()
        try:
            for listing in listings:
                result.visited += 1
                try:
                    await page.goto(listing.url, wait_until="domcontentloaded", timeout=45_000)
                    await page.wait_for_function(
                        "() => document.querySelectorAll('a[href]').length > 50", timeout=45_000
                    )
                    await asyncio.sleep(1.0)
                    pairs = await browser_stores.extract_pairs(page, store_slug)
                    specs = map_specs(category_slug, pairs)
                    await self._apply(listing, specs, cache, result)
                except Exception as exc:  # noqa: BLE001
                    result.failed += 1
                    logger.warning("collect-details %s %s: %s", store_slug, listing.url, exc)
                await asyncio.sleep(random.uniform(1.0, 2.0))
        finally:
            await page.close()

    async def _apply(
        self,
        listing: Listing,
        specs: dict[str, str],
        cache: dict[uuid.UUID, dict[str, ProductAttribute]],
        result: DetailsResult,
    ) -> None:
        listing.specs_collected_at = datetime.now(timezone.utc)
        if not specs:
            return
        result.specs_found += 1
        existing = cache.setdefault(listing.product_id, {})
        result.attrs_changed += apply_specs(self._db, listing.product, specs, "page", existing)

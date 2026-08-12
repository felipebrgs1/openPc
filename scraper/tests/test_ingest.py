"""Dedup de ingestão — port de IngestionDedupTests.cs.

O listing (loja+StoreSku) é a identidade estável do produto entre scrapes;
categorias sem part number/match key (motherboard, memory, case) não têm
outra âncora — sem isso cada re-scrape cria produto novo (bug real de
produção que o C# corrigiu).
"""

from __future__ import annotations

import uuid

import pytest
from sqlalchemy import select
from sqlalchemy.ext.asyncio import async_sessionmaker, create_async_engine

from openpc_scraper.collect.models import RawListing
from openpc_scraper.db.models import Base, Category, Listing, Product, Store
from openpc_scraper.ingest.service import IngestionService


@pytest.fixture
async def db():
    engine = create_async_engine("sqlite+aiosqlite:///:memory:")
    async with engine.begin() as conn:
        await conn.run_sync(Base.metadata.create_all)
    sm = async_sessionmaker(engine, expire_on_commit=False)
    async with sm() as session:
        yield session
    await engine.dispose()


def listing(sku: str, title: str, thumbnail: str | None = None) -> RawListing:
    return RawListing(
        store_slug="terabyte",
        store_name="Terabyte",
        product_url=f"https://store.example/{sku}",
        price_cash=199.90,
        price_card=None,
        installments=None,
        installment_text=None,
        in_stock=True,
        thumbnail=thumbnail,
        title=title,
        manufacturer=None,
        part_number=None,
        match_key=None,
        category_slug="",
        store_sku=sku,
    )


async def seed(db, category_slug: str, store_slug: str = "terabyte") -> tuple[Category, Store]:
    category = Category(id=uuid.uuid4(), slug=category_slug, name=category_slug, display_order=1)
    store = Store(id=uuid.uuid4(), slug=store_slug, name=store_slug.title(), base_url="https://store.example")
    db.add_all([category, store])
    await db.commit()
    return category, store


async def test_reingest_mesmo_store_sku_reusa_produto_sem_duplicar(db) -> None:
    category, store = await seed(db, "motherboard")
    product = Product(
        id=uuid.uuid4(),
        category_id=category.id,
        brand="asrock",
        model=uuid.uuid4().hex[:12],  # sem part number e sem match key
        name="Placa-Mãe ASRock B650M",
        spec_source="scraper",
    )
    db.add(product)
    db.add(Listing(
        id=uuid.uuid4(),
        product_id=product.id,
        store_id=store.id,
        store_sku="placa-mae-asrock-b650m",
        url="https://store.example/placa-mae-asrock-b650m",
        title="Placa-Mãe ASRock B650M",
        price_cash=199.90,
        in_stock=True,
    ))
    await db.commit()

    service = IngestionService(db)
    result = await service.ingest(
        store, category.slug, [listing("placa-mae-asrock-b650m", "Placa-Mãe ASRock B650M", "https://img/placa.jpg")]
    )

    assert result.new_products == 0
    assert result.new_listings == 0
    assert len((await db.scalars(select(Product))).all()) == 1
    assert len((await db.scalars(select(Listing))).all()) == 1

    reloaded = (await db.scalars(select(Product))).one()
    assert reloaded.id == product.id
    assert reloaded.image_url == "https://img/placa.jpg"


async def test_primeira_coleta_cria_produto_e_listing(db) -> None:
    category, store = await seed(db, "case")

    service = IngestionService(db)
    result = await service.ingest(store, category.slug, [listing("gabinete-abc", "Gabinete Gamer ABC")])

    assert result.new_products == 1
    assert result.new_listings == 1
    assert len((await db.scalars(select(Product))).all()) == 1
    assert len((await db.scalars(select(Listing))).all()) == 1


async def test_dedup_por_part_number(db) -> None:
    from openpc_scraper.collect.models import RawListing

    category, store = await seed(db, "cpu")

    item = RawListing(
        store_slug="kabum",
        store_name="KaBuM!",
        product_url="https://www.kabum.com.br/produto/1/ryzen",
        price_cash=999.0,
        price_card=None,
        installments=None,
        installment_text=None,
        in_stock=True,
        thumbnail=None,
        title="Processador AMD Ryzen 5 7600, 6 Núcleos, AM5 - 100-100000931WOF",
        manufacturer="AMD",
        part_number="100-100000931WOF",
        match_key="amd 7600",
        category_slug="cpu",
        store_sku="1",
    )

    service = IngestionService(db)
    first = await service.ingest(store, category.slug, [item])
    assert first.new_products == 1

    # mesma peça de outra loja: casa por part number, não cria produto novo
    item2 = RawListing(**{**item.to_dict(), "store_slug": "pichau", "store_sku": "2"})
    second = await service.ingest(store, category.slug, [item2])
    assert second.new_products == 0
    products = (await db.scalars(select(Product))).all()
    assert len(products) == 1

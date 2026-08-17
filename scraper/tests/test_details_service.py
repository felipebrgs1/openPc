"""Seleção de listings do collect-details — o filtro de categoria é
obrigatório: sem ele o serviço visita os listings mais recentes da loja
(independe da categoria pedida) e aplica o extrator errado.
"""

from __future__ import annotations

import uuid

import pytest
from sqlalchemy import select
from sqlalchemy.ext.asyncio import async_sessionmaker, create_async_engine

from openpc_scraper.db.models import Base, Category, Listing, Product, Store
from openpc_scraper.jobs import details


@pytest.fixture
async def db():
    engine = create_async_engine("sqlite+aiosqlite:///:memory:")
    async with engine.begin() as conn:
        await conn.run_sync(Base.metadata.create_all)
    sm = async_sessionmaker(engine, expire_on_commit=False)
    async with sm() as session:
        yield session
    await engine.dispose()


async def _seed(db):
    store = Store(id=uuid.uuid4(), slug="kabum", name="KaBuM!", base_url="https://kabum.example")
    gpu = Category(id=uuid.uuid4(), slug="gpu", name="Placas de vídeo", display_order=1)
    case = Category(id=uuid.uuid4(), slug="case", name="Gabinetes", display_order=2)
    db.add_all([store, gpu, case])
    await db.commit()

    for category, sku in ((gpu, "gpu-1"), (case, "case-1"), (case, "case-2")):
        product = Product(
            id=uuid.uuid4(),
            category_id=category.id,
            brand="x",
            model=uuid.uuid4().hex[:12],
            name=f"Produto {category.slug} {sku}",
            spec_source="scraper",
        )
        db.add(product)
        db.add(Listing(
            id=uuid.uuid4(),
            product_id=product.id,
            store_id=store.id,
            store_sku=sku,
            url=f"https://kabum.example/{sku}",
            title=f"Produto {category.slug} {sku}",
            price_cash=99.9,
            in_stock=True,
        ))
    await db.commit()
    return store, gpu, case


async def test_coleta_detalhes_filtra_pela_categoria(db, monkeypatch) -> None:
    store, gpu, case = await _seed(db)

    async def fake_fetch(client, url, category_slug):
        return {}  # sem ficha — mas SpecsCollectedAt é marcado mesmo assim

    monkeypatch.setattr(details, "fetch_product_specs", fake_fetch)
    # evita o sleep aleatório entre páginas
    monkeypatch.setattr(details.random, "uniform", lambda a, b: 0)

    service = details.DetailsCollectionService(db)
    result = await service.collect("kabum", "gpu", limit=10)

    assert result.visited == 1  # só a GPU — os gabinetes não podem entrar

    listings = (await db.scalars(select(Listing))).all()
    by_sku = {l.store_sku: l for l in listings}
    assert by_sku["gpu-1"].specs_collected_at is not None
    assert by_sku["case-1"].specs_collected_at is None
    assert by_sku["case-2"].specs_collected_at is None

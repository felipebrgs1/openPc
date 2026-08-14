"""Precedência de fontes nas specs: page > title > reference."""

from __future__ import annotations

import uuid

import pytest
from sqlalchemy import select
from sqlalchemy.ext.asyncio import async_sessionmaker, create_async_engine

from openpc_scraper.db.models import Base, Category, Product, ProductAttribute
from openpc_scraper.ingest.attributes import apply_specs


@pytest.fixture
async def db():
    engine = create_async_engine("sqlite+aiosqlite:///:memory:")
    async with engine.begin() as conn:
        await conn.run_sync(Base.metadata.create_all)
    sm = async_sessionmaker(engine, expire_on_commit=False)
    async with sm() as session:
        yield session
    await engine.dispose()


async def product(db) -> Product:
    category = Category(id=uuid.uuid4(), slug="gpu", name="gpu", display_order=1)
    db.add(category)
    await db.commit()
    p = Product(
        id=uuid.uuid4(),
        category_id=category.id,
        brand="gigabyte",
        model="nvidia 5070",
        name="Placa de Vídeo RTX 5070 Gigabyte Gaming OC",
        spec_source="scraper",
    )
    db.add(p)
    await db.commit()
    return p


async def attrs(db, product_id: uuid.UUID) -> dict[str, tuple[str, str]]:
    rows = (await db.execute(
        select(ProductAttribute).where(ProductAttribute.product_id == product_id)
    )).scalars().all()
    return {a.key: (a.value_text, a.source) for a in rows}


async def test_referencia_preenche_lacunas_e_title_sobrescreve(db) -> None:
    p = await product(db)
    cache: dict = {}
    apply_specs(db, p, {"cuda_cores": "6144", "boost_clock_mhz": "2512", "tdp_w": "250"}, "reference", cache)
    await db.commit()

    # título sobrescreve tdp (mesma prioridade maior) mas não mexe no resto
    apply_specs(db, p, {"tdp_w": "260"}, "title", cache)
    await db.commit()

    assert await attrs(db, p.id) == {
        "cuda_cores": ("6144", "reference"),
        "boost_clock_mhz": ("2512", "reference"),
        "tdp_w": ("260", "title"),
    }


async def test_title_nao_sobrescreve_page(db) -> None:
    p = await product(db)
    cache: dict = {}
    apply_specs(db, p, {"boost_clock_mhz": "2640"}, "page", cache)
    await db.commit()

    # re-scrape do título com valor de referência antigo não regride a spec
    apply_specs(db, p, {"boost_clock_mhz": "2512"}, "title", cache)
    apply_specs(db, p, {"boost_clock_mhz": "2512"}, "reference", cache)
    await db.commit()

    assert await attrs(db, p.id) == {"boost_clock_mhz": ("2640", "page")}


async def test_reference_nao_regride_page(db) -> None:
    p = await product(db)
    cache: dict = {}
    apply_specs(db, p, {"memory_gb": "12"}, "page", cache)
    apply_specs(db, p, {"memory_gb": "12", "memory_type": "gddr7"}, "reference", cache)
    await db.commit()

    assert await attrs(db, p.id) == {
        "memory_gb": ("12", "page"),
        "memory_type": ("gddr7", "reference"),
    }


async def test_reference_atualizada_pode_mudar_reference_antiga(db) -> None:
    p = await product(db)
    cache: dict = {}
    apply_specs(db, p, {"cuda_cores": "5888"}, "reference", cache)
    await db.commit()
    # dado curado revisado: mesma fonte, prioridade igual → atualiza
    apply_specs(db, p, {"cuda_cores": "6144"}, "reference", cache)
    await db.commit()
    assert await attrs(db, p.id) == {"cuda_cores": ("6144", "reference")}


async def test_valores_numericos_e_booleanos(db) -> None:
    p = await product(db)
    apply_specs(db, p, {"cuda_cores": "6144", "has_igpu": "false"}, "reference", {})
    await db.commit()
    rows = (await db.execute(
        select(ProductAttribute).where(ProductAttribute.product_id == p.id)
    )).scalars().all()
    by_key = {a.key: a for a in rows}
    assert by_key["cuda_cores"].value_num == 6144
    assert by_key["has_igpu"].value_bool is False

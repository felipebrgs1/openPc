"""Seed de categorias, lojas e jobs — espelho de DbSeeder.cs + SeedData.cs.

Migrações NÃO rodam aqui (a API aplica com advisory lock no startup).
"""

from __future__ import annotations

from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from .models import Category, ScrapeJob, Store

CRON_CATALOG_DAILY = "0 30 4 * * ?"   # 04:30 diário (formato Quartz — igual ao banco)
CRON_HOT_PRICES = "0 0 */6 * * ?"     # a cada 6h

CATEGORIES: list[tuple[str, str, int]] = [
    ("cpu", "Processadores", 1),
    ("motherboard", "Placas-mãe", 2),
    ("gpu", "Placas de vídeo", 3),
    ("memory", "Memórias RAM", 4),
    ("storage", "Armazenamento", 5),
    ("psu", "Fontes", 6),
    ("case", "Gabinetes", 7),
    ("cooler", "Coolers e watercoolers", 8),
]

STORES: list[tuple[str, str, str]] = [
    ("kabum", "KaBuM!", "https://www.kabum.com.br"),
    ("terabyte", "Terabyte Shop", "https://www.terabyteshop.com.br"),
    ("pichau", "Pichau", "https://www.pichau.com.br"),
]

HOT_CATEGORIES = {"cpu", "gpu"}


async def seed(db: AsyncSession) -> None:
    if not (await db.scalars(select(Category).limit(1))).first():
        db.add_all(
            Category(slug=s, name=n, display_order=o) for s, n, o in CATEGORIES
        )

    if not (await db.scalars(select(Store).limit(1))).first():
        db.add_all(Store(slug=s, name=n, base_url=u) for s, n, u in STORES)

    # Persiste categorias/lojas ANTES de buildar os jobs (os ids vêm do banco).
    await db.commit()

    if not (await db.scalars(select(ScrapeJob).limit(1))).first():
        categories = (await db.scalars(select(Category))).all()
        stores = (await db.scalars(select(Store))).all()
        for cat in categories:
            for store in stores:
                cron = CRON_HOT_PRICES if cat.slug in HOT_CATEGORIES else CRON_CATALOG_DAILY
                db.add(ScrapeJob(store_id=store.id, category_id=cat.id, schedule_cron=cron, enabled=True))
        await db.commit()

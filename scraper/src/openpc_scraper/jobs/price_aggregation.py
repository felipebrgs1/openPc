"""Agregação diária de preços — espelho de PriceAggregationService.cs (M6).

Consolida o raw de price_history na tabela price_daily (menor preço em
estoque por produto/dia) e aplica a retenção — raw 90 dias, price_daily
24 meses. Idempotente.
"""

from __future__ import annotations

import logging
import uuid
from datetime import datetime, timedelta

from sqlalchemy import delete, select
from sqlalchemy.ext.asyncio import AsyncSession

from ..db.models import Listing, PriceDaily, PriceHistory

logger = logging.getLogger("openpc_scraper.prices")

RAW_RETENTION_DAYS = 90
DAILY_RETENTION_MONTHS = 24


async def run_aggregation(db: AsyncSession, days: int = 1) -> tuple[int, int, int]:
    today = datetime.utcnow().date()
    since = today - timedelta(days=days - 1)
    raw_cutoff = today - timedelta(days=RAW_RETENTION_DAYS)
    daily_cutoff = today - timedelta(days=DAILY_RETENTION_MONTHS * 30)

    # 1) Upsert do price_daily: menor preço em estoque por (produto, dia).
    rows = (
        await db.execute(
            select(
                Listing.product_id,
                PriceHistory.collected_at,
                PriceHistory.price_cash,
                PriceHistory.listing_id,
            )
            .join(Listing, PriceHistory.listing_id == Listing.id)
            .where(
                PriceHistory.in_stock.is_(True),
                PriceHistory.price_cash > 0,
                PriceHistory.collected_at >= since,
                PriceHistory.collected_at < today + timedelta(days=1),
            )
        )
    ).all()

    by_day: dict[tuple[uuid.UUID, datetime], list[tuple[float, uuid.UUID]]] = {}
    for product_id, collected_at, price_cash, listing_id in rows:
        day = collected_at.date() if hasattr(collected_at, "date") else collected_at.date()
        by_day.setdefault((product_id, day), []).append((price_cash, listing_id))

    existing = (
        await db.execute(
            select(PriceDaily).where(PriceDaily.date >= since)
        )
    ).scalars().all()
    existing_by_key = {(d.product_id, d.date): d for d in existing}

    upserted = 0
    for (product_id, day), prices in by_day.items():
        min_price, listing_id = min(prices, key=lambda p: p[0])
        key = (product_id, day)
        current = existing_by_key.get(key)
        if current is None:
            db.add(PriceDaily(
                id=uuid.uuid4(),
                product_id=product_id,
                date=day,
                min_price=min_price,
                listing_id=listing_id,
            ))
            upserted += 1
        elif current.min_price != min_price or current.listing_id != listing_id:
            current.min_price = min_price
            current.listing_id = listing_id
            upserted += 1

    # 2) Retenção: raw 90 dias, price_daily 24 meses.
    raw_deleted = (
        await db.execute(
            delete(PriceHistory).where(PriceHistory.collected_at < raw_cutoff)
        )
    ).rowcount
    daily_deleted = (
        await db.execute(
            delete(PriceDaily).where(PriceDaily.date < daily_cutoff)
        )
    ).rowcount

    await db.commit()
    logger.info(
        "aggregate-prices: %d upserts, %d raws deletados, %d diários deletados",
        upserted, raw_deleted, daily_deleted,
    )
    return upserted, raw_deleted, daily_deleted

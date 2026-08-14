"""CLI do scraper — espelho de Program.cs.

Comandos:
  run-once [store] [category]   coleta imediata dos jobs habilitados
  cleanup-noise [category] [--dry-run]
  aggregate-prices [days]
  sync-images
  alerts-check <productId>
  backfill-attributes
  collect-details [store] [category] [--limit N] [--refresh-days D]
  scheduler                      roda o agendamento (APScheduler)
"""

from __future__ import annotations

import argparse
import asyncio
import logging
import os
import sys
import uuid

from sqlalchemy import delete, select
from sqlalchemy.ext.asyncio import async_sessionmaker, create_async_engine
from sqlalchemy.orm import selectinload

from .collect.browser import BrowserPool
from .db import models
from .db.seed import seed
from .ingest.service import clean_title
from .jobs.collection import CollectionService
from .jobs.price_aggregation import run_aggregation
from .normalize import gpu_series, noise_filter, spec_extractor

logger = logging.getLogger("openpc_scraper")


def _connection_string() -> str:
    conn = os.environ.get("DATABASE_URL") or os.environ.get("ConnectionStrings__Default")
    if not conn:
        sys.exit("Connection string não configurada (DATABASE_URL ou ConnectionStrings__Default).")
    # aceita o formato ADO.NET do .NET e converte para SQLAlchemy
    if conn.startswith("Host="):
        parts = dict(p.split("=", 1) for p in conn.split(";") if "=" in p)
        host = parts.get("Host", "localhost")
        port = parts.get("Port", "5432")
        database = parts.get("Database", "openpc")
        user = parts.get("Username", "openpc")
        password = parts.get("Password", "")
        conn = f"postgresql+asyncpg://{user}:{password}@{host}:{port}/{database}"
    elif conn.startswith("postgres://"):
        conn = conn.replace("postgres://", "postgresql+asyncpg://", 1)
    return conn


def _sessionmaker() -> async_sessionmaker:
    engine = create_async_engine(_connection_string(), pool_pre_ping=True)
    return async_sessionmaker(engine, expire_on_commit=False)


async def _cleanup_noise(db, category: str | None, dry_run: bool) -> None:
    stmt = select(models.Product).options(selectinload(models.Product.category))
    if category:
        stmt = stmt.join(models.Category).where(models.Category.slug == category)
    products = (await db.scalars(stmt)).all()

    to_delete = [
        p for p in products
        if (category is None or p.category.slug == category)
        and noise_filter.is_noise(p.category.slug, p.name)
    ]

    if dry_run:
        by_category: dict[str, list[models.Product]] = {}
        for p in to_delete:
            by_category.setdefault(p.category.slug, []).append(p)
        for slug, items in sorted(by_category.items()):
            logger.info("dry-run %s: %d produtos (ex: %s)", slug, len(items), items[0].name)
        return

    for p in to_delete:
        await db.delete(p)
    await db.commit()
    logger.info("cleanup-noise: %d produtos removidos (%s)", len(to_delete), category or "todas")


async def _backfill_attributes(db) -> None:
    products = (await db.scalars(
        select(models.Product)
        .options(selectinload(models.Product.category), selectinload(models.Product.attributes))
        .join(models.Category)
        .where(models.Category.slug.in_(["gpu", "memory"]))
    )).all()
    added = 0
    by_value: dict[str, int] = {}
    for product in products:
        attrs = {a.key: a for a in product.attributes}
        if product.category.slug == "gpu":
            key, value = "series", gpu_series.classify(product.name)
        else:
            value = spec_extractor.extract_memory(product.name).get("type")
            key = "type" if value else None
        if key is None or value is None or key in attrs:
            continue
        db.add(models.ProductAttribute(
            id=uuid.uuid4(), product_id=product.id, key=key, value_text=value,
        ))
        by_value[f"{key}={value}"] = by_value.get(f"{key}={value}", 0) + 1
        added += 1
    await db.commit()
    for k, v in sorted(by_value.items()):
        logger.info("backfill-attributes: %s: %d produtos", k, v)
    logger.info("backfill-attributes: %d atributos adicionados", added)


async def _alerts_check(db, product_id: uuid.UUID) -> None:
    from .jobs.alerts import PriceAlertService

    sent = await PriceAlertService(db).check_product(product_id)
    logger.info("alerts-check %s: %d alertas disparados", product_id, sent)


async def _main(argv: list[str] | None = None) -> int:
    logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(name)s: %(message)s")

    parser = argparse.ArgumentParser(prog="openpc-scraper", description="Scraper OpenPC (port Python do scraper C#).")
    sub = parser.add_subparsers(dest="command", required=True)

    p = sub.add_parser("run-once", help="coleta imediata dos jobs habilitados")
    p.add_argument("store", nargs="?", help="filtro por loja (kabum|terabyte|pichau)")
    p.add_argument("category", nargs="?", help="filtro por categoria (cpu|gpu|...)")
    p.add_argument(
        "--concurrency",
        type=int,
        default=int(os.environ.get("SCRAPE_CONCURRENCY", "4")),
        help="jobs coletados em paralelo (default 4; env SCRAPE_CONCURRENCY; 1 = sequencial)",
    )

    p = sub.add_parser("cleanup-noise", help="remove produtos que não pertencem à categoria")
    p.add_argument("category", nargs="?")
    p.add_argument("--dry-run", action="store_true")

    p = sub.add_parser("aggregate-prices", help="consolida price_daily + retenção")
    p.add_argument("days", nargs="?", type=int, default=1)

    sub.add_parser("sync-images", help="baixa fotos dos CDNs e sobe para o MinIO")

    p = sub.add_parser("alerts-check", help="dispara alertas de preço de um produto")
    p.add_argument("product_id")

    sub.add_parser("backfill-attributes", help="calcula atributos (série GPU, tipo de memória)")

    p = sub.add_parser(
        "collect-details",
        help="coleta a ficha técnica das páginas de produto (specs detalhadas)",
    )
    p.add_argument("store", nargs="?", default="kabum", help="loja (kabum|terabyte|pichau; default kabum)")
    p.add_argument("category", nargs="?", default="gpu", help="categoria (default gpu)")
    p.add_argument("--limit", type=int, default=20, help="páginas de produto por execução (default 20)")
    p.add_argument("--refresh-days", type=int, default=30, help="recoletar após N dias (default 30)")
    p.add_argument("--concurrency", type=int, default=3, help="fetches paralelos (kabum; default 3)")

    sub.add_parser("scheduler", help="roda o agendamento (APScheduler)")

    args = parser.parse_args(argv)

    sm = _sessionmaker()
    async with sm() as db:
        await seed(db)

        if args.command == "run-once":
            pool = BrowserPool()
            try:
                service = CollectionService(db, pool, session_factory=sm)
                await service.run_all_enabled(args.store, args.category, concurrency=args.concurrency)
            finally:
                await pool.close()
        elif args.command == "cleanup-noise":
            await _cleanup_noise(db, args.category, args.dry_run)
        elif args.command == "aggregate-prices":
            await run_aggregation(db, days=args.days)
        elif args.command == "sync-images":
            from .ingest.image_sync import ImageSyncService

            service = ImageSyncService(
                db,
                endpoint=os.environ.get("MINIO_ENDPOINT"),
                access_key=os.environ.get("MINIO_ACCESS_KEY"),
                secret_key=os.environ.get("MINIO_SECRET_KEY"),
                bucket=os.environ.get("MINIO_BUCKET"),
                public_path=os.environ.get("MINIO_PUBLIC_PATH"),
                use_ssl=(os.environ.get("MINIO_USE_SSL") == "true"),
            )
            await service.sync()
        elif args.command == "alerts-check":
            await _alerts_check(db, uuid.UUID(args.product_id))
        elif args.command == "backfill-attributes":
            await _backfill_attributes(db)
        elif args.command == "collect-details":
            from .jobs.details import DetailsCollectionService

            pool = BrowserPool()
            try:
                service = DetailsCollectionService(db, pool)
                await service.collect(
                    args.store,
                    args.category,
                    limit=args.limit,
                    refresh_days=args.refresh_days,
                    concurrency=args.concurrency,
                )
            finally:
                await pool.close()
        elif args.command == "scheduler":
            from apscheduler.schedulers.asyncio import AsyncIOScheduler

            from .jobs.scheduler import build_scheduler, schedule_jobs

            pool = BrowserPool()
            collection = CollectionService(db, pool)
            scheduler: AsyncIOScheduler = build_scheduler(db, collection)
            await schedule_jobs(scheduler, db, collection)
            scheduler.start()
            logger.info("Scheduler rodando. Ctrl+C para parar.")
            try:
                await asyncio.Event().wait()
            except (KeyboardInterrupt, asyncio.CancelledError):
                pass
            finally:
                scheduler.shutdown(wait=False)
                await pool.close()

    return 0


def main() -> int:
    return asyncio.run(_main())


if __name__ == "__main__":
    sys.exit(main())

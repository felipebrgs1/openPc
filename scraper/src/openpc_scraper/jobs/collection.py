"""Execução de ScrapeJob: coleta → ingestão → ScrapeRun — espelho de
CollectionService.cs. Em caso de falha, notifica o webhook (ScrapeAlert).
"""

from __future__ import annotations

import logging
import time
import uuid
from datetime import datetime

import httpx
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession
from sqlalchemy.orm import selectinload

from ..collect.browser import BrowserCollector, BrowserPool
from ..collect.kabum import collect as collect_kabum
from ..collect.models import RawListing
from ..collect.pichau import PichauCollector
from ..collect.terabyte import TerabyteCollector
from ..db.models import ScrapeJob, ScrapeRun, Store
from ..ingest.service import IngestionService

logger = logging.getLogger("openpc_scraper.collection")


class CollectionService:
    def __init__(self, db: AsyncSession, pool: BrowserPool | None = None) -> None:
        self._db = db
        self._pool = pool or BrowserPool()

    def _collectors(self) -> dict[str, BrowserCollector]:
        return {
            "pichau": PichauCollector(self._pool),
            "terabyte": TerabyteCollector(self._pool),
        }

    async def run_job(self, job_id: uuid.UUID) -> None:
        db = self._db
        job = (await db.scalars(
            select(ScrapeJob)
            .options(selectinload(ScrapeJob.store), selectinload(ScrapeJob.category))
            .where(ScrapeJob.id == job_id)
        )).one()

        run = ScrapeRun(id=uuid.uuid4(), job_id=job.id, status="running")
        db.add(run)
        await db.commit()

        started = time.monotonic()
        try:
            logger.info("Coleta %s/%s iniciada", job.store.slug, job.category.slug)
            items = await self._collect(job)
            ingestion = IngestionService(db)
            result = await ingestion.ingest(job.store, job.category.slug, items)

            run.status = "ok"
            run.items_found = result.items_found
            run.items_new = result.new_products
            run.duration_ms = int((time.monotonic() - started) * 1000)
            run.finished_at = datetime.utcnow()
            logger.info(
                "Coleta %s/%s ok em %d ms (%d itens)",
                job.store.slug, job.category.slug, run.duration_ms, result.items_found,
            )

            # alertas de preço: só produtos cujo menor preço caiu no run
            for product_id in result.price_drop_product_ids:
                await self._check_price_alerts(product_id)
        except Exception as exc:  # noqa: BLE001 — registra e notifica
            run.status = "failed"
            run.error = str(exc)[:2000]
            run.duration_ms = int((time.monotonic() - started) * 1000)
            run.finished_at = datetime.utcnow()
            logger.exception("Coleta %s/%s falhou", job.store.slug, job.category.slug)
            await self._notify_failed(run, job)

        await db.commit()

    async def run_all_enabled(self, store_slug: str | None = None, category_slug: str | None = None) -> None:
        stmt = select(ScrapeJob).options(selectinload(ScrapeJob.store), selectinload(ScrapeJob.category)).where(ScrapeJob.enabled.is_(True))
        if store_slug:
            stmt = stmt.join(Store).where(Store.slug == store_slug)
        if category_slug:
            stmt = stmt.join(ScrapeJob.category).where(ScrapeJob.category.has(slug=category_slug))
        jobs = (await self._db.scalars(stmt)).all()
        logger.info("run-once: %d jobs habilitados%s%s", len(jobs),
                    f" (loja={store_slug})" if store_slug else "",
                    f" (categoria={category_slug})" if category_slug else "")
        for job in jobs:
            await self.run_job(job.id)

    async def _collect(self, job: ScrapeJob) -> list[RawListing]:
        if job.store.slug == "kabum":
            return await collect_kabum(job.category.slug)
        collector = self._collectors().get(job.store.slug)
        if collector is None:
            raise ValueError(f"Sem collector para a loja '{job.store.slug}'")
        return await collector.collect(job.category.slug)

    async def _check_price_alerts(self, product_id: uuid.UUID) -> None:
        from .alerts import PriceAlertService

        await PriceAlertService(self._db).check_product(product_id)

    async def _notify_failed(self, run: ScrapeRun, job: ScrapeJob) -> None:
        from .alerts import notify_run_failed

        await notify_run_failed(run, job)

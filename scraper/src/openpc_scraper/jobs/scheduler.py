"""Agendamento — espelho de ScrapeScheduler.cs (Quartz).

Usa APScheduler. As crons do banco estão em formato Quartz (7 campos,
ex: "0 30 4 * * ?") — converte para o formato do APScheduler (6 campos)
descartando o campo de segundos.
"""

from __future__ import annotations

import logging
import uuid

from apscheduler.schedulers.asyncio import AsyncIOScheduler
from apscheduler.triggers.cron import CronTrigger
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession
from sqlalchemy.orm import selectinload

from ..db.models import ScrapeJob
from .collection import CollectionService
from .price_aggregation import run_aggregation

logger = logging.getLogger("openpc_scraper.scheduler")

# crons fixos (mesmos do C#): agregação 02:00, sync de imagens 05:00
AGGREGATION_CRON = "0 2 * * *"
IMAGE_SYNC_CRON = "0 5 * * *"


def quartz_to_apscheduler(cron: str) -> str:
    """"0 30 4 * * ?" (Quartz) → "30 4 * * *" (APScheduler, sem segundos)."""
    fields = cron.split()
    if len(fields) == 7:
        fields = fields[1:]
    mapped = ["*" if f == "?" else f for f in fields]
    return " ".join(mapped)


def build_scheduler(db: AsyncSession, collection: CollectionService) -> AsyncIOScheduler:
    scheduler = AsyncIOScheduler(timezone="UTC")

    scheduler.add_job(
        lambda: run_aggregation(db, days=1),
        CronTrigger.from_crontab(AGGREGATION_CRON),
        id="price-aggregation",
        replace_existing=True,
    )
    scheduler.add_job(
        lambda: _image_sync_job(db),
        CronTrigger.from_crontab(IMAGE_SYNC_CRON),
        id="image-sync",
        replace_existing=True,
    )

    return scheduler


async def schedule_jobs(scheduler: AsyncIOScheduler, db: AsyncSession, collection: CollectionService) -> None:
    jobs = (await db.scalars(
        select(ScrapeJob)
        .options(selectinload(ScrapeJob.store), selectinload(ScrapeJob.category))
        .where(ScrapeJob.enabled.is_(True))
    )).all()
    for job in jobs:
        cron = quartz_to_apscheduler(job.schedule_cron)
        scheduler.add_job(
            lambda jid=job.id: collection.run_job(jid),
            CronTrigger.from_crontab(cron),
            id=f"job-{job.id}",
            replace_existing=True,
        )
        logger.info("Agendado: %s/%s cron=%s", job.store.slug, job.category.slug, cron)
    logger.info("APScheduler iniciado com %d jobs", len(jobs))


async def _image_sync_job(db: AsyncSession) -> None:
    from ..ingest.image_sync import ImageSyncService
    import os

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

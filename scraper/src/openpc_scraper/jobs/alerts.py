"""Alertas — espelho de PriceAlertService.cs + ScrapeAlertService.cs.

PriceAlert: dispara e-mail quando o menor preço em estoque <= alvo
(cooldown de 24 h por alerta). ScrapeAlert: webhook quando um run falha.
"""

from __future__ import annotations

import logging
import os
import uuid
from datetime import datetime, timedelta

import httpx
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from ..db.models import (
    Listing,
    PriceAlert,
    PriceAlertEvent,
    Product,
    ScrapeJob,
    ScrapeRun,
)
from ..email import EmailSender

logger = logging.getLogger("openpc_scraper.alerts")

COOLDOWN = timedelta(hours=24)

_SITE_URL = os.environ.get("SITE_URL", "https://openpc.example")


class PriceAlertService:
    def __init__(self, db: AsyncSession, email: EmailSender | None = None) -> None:
        self._db = db
        self._email = email or EmailSender(
            host=os.environ.get("SMTP_HOST"),
            port=int(os.environ.get("SMTP_PORT") or 587),
            username=os.environ.get("SMTP_USERNAME"),
            password=os.environ.get("SMTP_PASSWORD"),
            from_=os.environ.get("SMTP_FROM", "OpenPC <no-reply@openpc.example>"),
        )

    async def check_product(self, product_id: uuid.UUID) -> int:
        """Verifica alertas confirmados do produto com o preço mais barato."""
        db = self._db

        current = (
            await db.execute(
                select(Listing.id, Listing.price_cash)
                .where(
                    Listing.product_id == product_id,
                    Listing.in_stock.is_(True),
                    Listing.price_cash.isnot(None),
                )
                .order_by(Listing.price_cash)
                .limit(1)
            )
        ).first()
        if current is None or current.price_cash is None:
            return 0  # sem preço em estoque — nada a fazer

        product = (await db.scalars(select(Product).where(Product.id == product_id))).first()
        if product is None:
            return 0

        cutoff = datetime.utcnow() - COOLDOWN
        alerts = (
            await db.scalars(
                select(PriceAlert).where(
                    PriceAlert.product_id == product_id,
                    PriceAlert.confirmed.is_(True),
                    PriceAlert.target_price >= current.price_cash,
                    (PriceAlert.last_triggered_at.is_(None))
                    | (PriceAlert.last_triggered_at < cutoff),
                )
            )
        ).all()
        if not alerts:
            return 0

        product_url = f"{_SITE_URL}/pecas/{product.id}"
        sent = 0
        for alert in alerts:
            cancel_url = f"{_SITE_URL}/api/v1/alerts/{alert.id}/cancel?token={alert.token}"
            body = f"""
                <p>O preço de <b>{product.brand} {product.model}</b> chegou ao seu alvo!</p>
                <p><b>Preço atual: R$ {current.price_cash:.2f}</b> (seu alvo: R$ {alert.target_price:.2f})</p>
                <p><a href="{product_url}">Ver produto</a></p>
                <p style="color:#888;font-size:12px">
                  <a href="{cancel_url}">Cancelar este alerta</a>
                </p>
                """
            try:
                self._email.send(alert.email, f"OpenPC: {product.name} atingiu seu preço alvo", body)
                alert.last_triggered_at = datetime.utcnow()
                alert.trigger_count += 1
                db.add(PriceAlertEvent(
                    id=uuid.uuid4(),
                    alert_id=alert.id,
                    listing_id=current.id,
                    price_at_trigger=current.price_cash,
                    email_sent=True,
                ))
                sent += 1
            except Exception:  # noqa: BLE001 — falha de e-mail não derruba o job
                logger.exception("Falha ao enviar alerta de preço para %s (%s)", alert.email, product.name)

        await db.commit()
        if sent:
            logger.info("Alertas de preço disparados: %d para %s", sent, product.name)
        return sent


async def notify_run_failed(run: ScrapeRun, job: ScrapeJob) -> None:
    """Alerta de scraper quebrado: POST JSON para o webhook (fire-and-forget)."""
    url = os.environ.get("ALERTS_WEBHOOK_URL")
    if not url:
        logger.warning(
            "Scrape falhou: %s/%s — %s (sem webhook configurado em ALERTS_WEBHOOK_URL)",
            job.store.slug, job.category.slug, run.error,
        )
        return

    payload = {
        "event": "scrape_run_failed",
        "store": job.store.slug,
        "category": job.category.slug,
        "status": run.status,
        "error": run.error,
        "startedAt": run.started_at.isoformat() if run.started_at else None,
        "finishedAt": run.finished_at.isoformat() if run.finished_at else None,
        "durationMs": run.duration_ms,
    }
    try:
        async with httpx.AsyncClient(timeout=5.0) as client:
            resp = await client.post(url, json=payload)
            if resp.status_code >= 400:
                logger.warning("Webhook de falha respondeu HTTP %d", resp.status_code)
    except Exception:  # noqa: BLE001 — o alerta nunca pode derrubar o job
        logger.exception("Falha ao notificar webhook de scrape")

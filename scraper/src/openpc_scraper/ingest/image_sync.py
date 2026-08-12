"""Sync de imagens CDN → MinIO — espelho de ImageSyncService.cs + ImageKeys.cs.

Chave = hash da URL de origem (dedup entre produtos) + extensão. Idempotente;
falha de um item não aborta o lote. Sem MinIO configurado, vira no-op.
"""

from __future__ import annotations

import asyncio
import hashlib
import logging
import os
import re
from pathlib import Path

import httpx
from minio import Minio
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from ..db.models import Product

logger = logging.getLogger("openpc_scraper.images")

MAX_PARALLEL = 8

_EXT_RE = re.compile(r"\.(jpg|jpeg|png|webp|gif|avif)$", re.IGNORECASE)


def key_for(url: str) -> str:
    """Chave do objeto no bucket: hash da URL + extensão real."""
    digest = hashlib.sha256(url.encode("utf-8")).hexdigest()[:40]
    path = url.split("?", 1)[0].split("#", 1)[0]
    ext = _EXT_RE.search(path)
    return digest + (ext.group(1).lower() if ext else ".img")


def public_url(public_path: str | None, key: str) -> str:
    if not public_path:
        return f"/images/{key}"
    return f"{public_path.rstrip('/')}/{key}"


class ImageSyncService:
    def __init__(
        self,
        db: AsyncSession,
        endpoint: str | None = None,
        access_key: str | None = None,
        secret_key: str | None = None,
        bucket: str | None = None,
        public_path: str | None = None,
        use_ssl: bool = False,
    ) -> None:
        self._db = db
        self._bucket = bucket
        self._public_path = public_path
        self._minio = (
            Minio(endpoint, access_key=access_key, secret_key=secret_key, secure=use_ssl)
            if endpoint
            else None
        )

    async def sync(self) -> int:
        if self._minio is None or not self._bucket:
            logger.warning("MinIO não configurado — sync de imagens pulado.")
            return 0

        products = (
            await self._db.scalars(
                select(Product).where(
                    Product.image_url.isnot(None),
                    Product.image_url.like("http%"),
                )
            )
        ).all()
        if not products:
            return 0
        logger.info("sync-images: %d produtos com imagem externa", len(products))

        if not await asyncio.to_thread(self._minio.bucket_exists, self._bucket):
            logger.warning("sync-images: bucket '%s' não existe — crie via minio-init (compose).", self._bucket)
            return 0

        sem = asyncio.Semaphore(MAX_PARALLEL)
        done = 0

        async def sync_one(product: Product) -> None:
            nonlocal done
            async with sem:
                if await self._sync_one(product):
                    done += 1

        await asyncio.gather(*(sync_one(p) for p in products))
        await self._db.commit()
        logger.info("sync-images: %d/%d fotos sincronizadas", done, len(products))
        return done

    async def _sync_one(self, product: Product) -> bool:
        url = product.image_url or ""
        key = key_for(url)
        try:
            if await asyncio.to_thread(self._object_exists, key):
                # outro produto já subiu a mesma foto — só aponta o caminho
                product.image_url = public_url(self._public_path, key)
                return True

            async with httpx.AsyncClient(
                timeout=30.0,
                headers={"User-Agent": (
                    "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 "
                    "(KHTML, like Gecko) Chrome/126.0 Safari/537.36"
                )},
            ) as client:
                resp = await client.get(url)
                resp.raise_for_status()

            content_type = resp.headers.get("content-type", "image/jpeg")
            await asyncio.to_thread(
                self._minio.put_object,
                self._bucket,
                key,
                resp.content,
                len(resp.content),
                content_type=content_type,
            )
            product.image_url = public_url(self._public_path, key)
            return True
        except Exception:  # noqa: BLE001 — falha de um item não aborta o lote
            logger.warning("sync-images: falha ao sincronizar %s", url, exc_info=True)
            return False

    def _object_exists(self, key: str) -> bool:
        try:
            self._minio.stat_object(self._bucket, key)  # type: ignore[arg-type]
            return True
        except Exception:
            return False

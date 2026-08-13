"""Terabyte (Cloudflare) — espelho de TerabyteCollector.cs.

Cards com "De: R$ X por: R$ Y" (PIX). Catálogo em uma página com
carregamento em lote: botão "CLIQUE PARA VER MAIS PRODUTOS" (+30 itens
por clique) até esgotar.
"""

from __future__ import annotations

import asyncio
import re

from playwright.async_api import Page

from .browser import BrowserCollector, BrowserPool

_PRODUCT_HREF_RE = re.compile(r"/produto/\d+/[a-z0-9-]+", re.IGNORECASE)
_PRICE_RE = re.compile(r"por:\s*\|?\s*R\$\s*([\d.]+,[\d]{2})", re.IGNORECASE)

_CATEGORY_PATHS = {
    "cpu": "hardware/processadores",
    "gpu": "hardware/placas-de-video",
    "motherboard": "hardware/placas-mae",
    "memory": "hardware/memorias",
    "storage": "hardware/hard-disk",
    "psu": "fontes",
    "case": "gabinetes",
    "cooler": "refrigeracao",
}

_COUNT_PRODUCTS_JS = (
    "() => new Set(Array.from(document.querySelectorAll('a[href]'))"
    ".map(a => a.href).filter(h => /\\/produto\\//.test(h))).size"
)


class TerabyteCollector(BrowserCollector):
    store_slug = "terabyte"
    store_name = "Terabyte Shop"
    store_domain = "www.terabyteshop.com.br"
    product_href_re = _PRODUCT_HREF_RE
    price_re = _PRICE_RE
    price_marker = "de:"

    def __init__(self, pool: BrowserPool) -> None:
        super().__init__(pool)

    def category_path(self, category_slug: str) -> str:
        path = _CATEGORY_PATHS.get(category_slug)
        if path is None:
            raise ValueError(f"Terabyte: categoria '{category_slug}' sem rota.")
        return path

    def extract_store_sku(self, href: str) -> str:
        m = re.search(r"/produto/(\d+)", href)
        return m.group(1) if m else href

    async def go_next_page(self, page: Page, next_page: int, prev_first_href: str | None) -> bool:
        more = page.locator(".btn-pdmore, .tfv2-more").first
        if not await more.is_visible():
            return False

        # Clique via JS (o Playwright não consegue clicar — elemento instável
        # por animação). Os primeiros cliques podem ser no-op (anti-bot/fila);
        # o 5º carrega o primeiro lote — tenta vários antes de declarar fim.
        no_progress = 0
        while no_progress < 6:
            before = await page.evaluate(_COUNT_PRODUCTS_JS)
            await more.evaluate("el => el.click()")
            await asyncio.sleep(2.0)
            after = await page.evaluate(_COUNT_PRODUCTS_JS)
            if after > before:
                return True  # lote carregado — a base extrai e chama de novo
            no_progress += 1

        return False

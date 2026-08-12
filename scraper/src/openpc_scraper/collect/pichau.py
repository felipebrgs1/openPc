"""Pichau (VTEX + Cloudflare) — espelho de PichauCollector.cs.

Cards com "de R$ X por R$ Y" (PIX à vista). Paginação SPA por clique em
"?page=N".
"""

from __future__ import annotations

import asyncio
import re

from playwright.async_api import Page

from .browser import BrowserCollector, BrowserPool

_PRODUCT_HREF_RE = re.compile(
    r"/(?:processador|placa-de-video|placa-mae|memoria|ssd|fonte|gabinete|water-cooler)-[a-z0-9-]+",
    re.IGNORECASE,
)
_PRICE_RE = re.compile(r"por\s*\|?\s*R\$\s*([\d.]+,[\d]{2})", re.IGNORECASE)

_CATEGORY_PATHS = {
    "cpu": "hardware/processadores",
    "gpu": "hardware/placa-de-video",
    "motherboard": "hardware/placa-mae",
    "memory": "hardware/memoria-ram",
    "storage": "hardware/ssd",
    "psu": "hardware/fonte",
    "case": "hardware/gabinete",
    "cooler": "hardware/water-cooler",
}


class PichauCollector(BrowserCollector):
    store_slug = "pichau"
    store_name = "Pichau"
    store_domain = "www.pichau.com.br"
    product_href_re = _PRODUCT_HREF_RE
    price_re = _PRICE_RE
    price_marker = "de r$"

    def __init__(self, pool: BrowserPool) -> None:
        super().__init__(pool)

    def category_path(self, category_slug: str) -> str:
        path = _CATEGORY_PATHS.get(category_slug)
        if path is None:
            raise ValueError(f"Pichau: categoria '{category_slug}' sem rota.")
        return path

    def extract_store_sku(self, href: str) -> str:
        sku = href.rstrip("/").split("/")[-1]
        return sku[:255] if len(sku) > 255 else sku

    async def go_next_page(self, page: Page, next_page: int, prev_first_href: str | None) -> bool:
        click_js = (
            "() => { const a = Array.from(document.querySelectorAll('a[href]'))"
            f".find(x => x.getAttribute('href')?.endsWith('?page={next_page}')); "
            "if (a) { a.click(); return true; } return false; }"
        )
        clicked = await page.evaluate(click_js)
        if not clicked:
            return False

        # aguarda o primeiro produto da página mudar (navegação SPA)
        try:
            wait_js = (
                "(prev) => { const a = document.querySelector("
                "'a[href*=\"/processador-\"], a[href*=\"/placa-de-video-\"], "
                "a[href*=\"/placa-mae-\"], a[href*=\"/memoria-\"], a[href*=\"/ssd-\"], "
                "a[href*=\"/fonte-\"], a[href*=\"/gabinete-\"], a[href*=\"/water-cooler-\"]'); "
                "return a && a.href !== prev; }"
            )
            await page.wait_for_function(wait_js, prev_first_href, timeout=15_000)
        except TimeoutError:
            return False
        await asyncio.sleep(1.5)
        return True

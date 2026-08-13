"""Base para lojas com Cloudflare (Pichau, Terabyte) — espelho de
BrowserCollectorBase.cs + BrowserPool.cs. Navega com Chromium completo
(não headless-shell — o Cloudflare detecta o shell), resolve o desafio,
scrolla para forçar lazy-load e extrai cards do DOM renderizado.
"""

from __future__ import annotations

import asyncio
import json
import re

from playwright.async_api import Browser, Page, Playwright, async_playwright

from .card import build_card_listing
from .models import RawListing

_USER_AGENT = (
    "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 "
    "(KHTML, like Gecko) Chrome/126.0 Safari/537.36"
)


class BrowserPool:
    """Browser Playwright único, reutilizado entre coletas (o desafio do
    Cloudflare é resolvido uma vez por sessão)."""

    def __init__(self) -> None:
        self._playwright: Playwright | None = None
        self._browser: Browser | None = None
        self._lock = asyncio.Lock()

    async def new_page(self) -> Page:
        async with self._lock:
            if self._browser is None:
                self._playwright = await async_playwright().start()
                self._browser = await self._playwright.chromium.launch(
                    headless=True,
                    channel="chromium",
                    args=["--disable-blink-features=AutomationControlled"],
                )
        return await self._browser.new_page(user_agent=_USER_AGENT)

    async def close(self) -> None:
        if self._browser is not None:
            await self._browser.close()
        if self._playwright is not None:
            await self._playwright.stop()


class BrowserCollector:
    """Base abstrata: coleta uma categoria navegando a listagem da loja."""

    store_slug: str = ""
    store_name: str = ""
    store_domain: str = ""
    product_href_re: re.Pattern[str] = re.compile(r"$^")  # sobrescrito
    price_re: re.Pattern[str] = re.compile(r"$^")         # sobrescrito
    price_marker: str = ""

    def __init__(self, pool: BrowserPool) -> None:
        self._pool = pool

    def category_path(self, category_slug: str) -> str:
        raise NotImplementedError

    def extract_store_sku(self, href: str) -> str:
        raise NotImplementedError

    async def go_next_page(self, page: Page, next_page: int, prev_first_href: str | None) -> bool:
        return False

    async def collect(self, category_slug: str) -> list[RawListing]:
        url = f"https://{self.store_domain}/{self.category_path(category_slug)}"
        all_cards: list[RawListing] = []
        page = await self._pool.new_page()
        try:
            await self._block_heavy_resources(page)
            await page.goto(url, wait_until="domcontentloaded", timeout=45_000)
            await self._wait_cloudflare(page)

            page_number = 1
            while True:
                await self._scroll_to_load(page)
                all_cards.extend(await self._extract_cards(page, category_slug))

                prev_first = all_cards[0].product_url if all_cards else None
                if not await self.go_next_page(page, page_number + 1, prev_first):
                    break
                page_number += 1
        finally:
            await page.close()

        # dedup por SKU da loja (cards repetidos entre scrolls/páginas)
        seen: set[str] = set()
        distinct: list[RawListing] = []
        for l in all_cards:
            if l.store_sku not in seen:
                seen.add(l.store_sku)
                distinct.append(l)
        return distinct

    @staticmethod
    async def _block_heavy_resources(page: Page) -> None:
        # Fontes/vídeo/favicon não afetam a extração de cards e pesam no
        # carregamento (imagens ficam — os thumbnails vêm dos atributos img).
        await page.route(
            "**/*",
            lambda route: (
                route.abort()
                if route.request.resource_type in {"font", "media"}
                else route.continue_()
            ),
        )

    async def _wait_cloudflare(self, page: Page) -> None:
        # aguarda o desafio passar (a página renderiza nav + cards)
        await page.wait_for_function(
            "() => document.querySelectorAll('a[href]').length > 50",
            timeout=45_000,
        )
        await asyncio.sleep(1.0)

    @staticmethod
    async def _scroll_to_load(page: Page) -> None:
        for _ in range(12):
            await page.evaluate("window.scrollBy(0, 900)")
            await asyncio.sleep(0.25)
        await asyncio.sleep(1.5)

    async def _extract_cards(self, page: Page, category_slug: str) -> list[RawListing]:
        pattern = self.product_href_re.pattern.replace("\\", "\\\\")
        marker = self.price_marker
        js = (
            "() => Array.from(document.querySelectorAll('a[href]'))"
            f".filter(a => new RegExp('{pattern}').test(a.href))"
            ".map(a => { let n = a; for (let i = 0; i < 5 && n.parentElement; i++) {"
            " n = n.parentElement; if (n.querySelectorAll('a[href]').length > 2) break;"
            " const t = n.innerText || '';"
            " if (t.includes('R$') && t.length > 80) break; }"
            " const t = n.innerText || '';"
            " const img = n.querySelector('img');"
            " const src = img ? (img.currentSrc || img.src || img.dataset.src || img.dataset.original || '') : '';"
            " return { href: a.href, text: n.querySelectorAll('a[href]').length > 2 ? '' : t, img: src }; })"
        )
        cards = await page.evaluate(js)

        result: list[RawListing] = []
        for el in cards:
            href = el.get("href") or ""
            text = el.get("text") or ""
            img = el.get("img")
            listing = build_card_listing(
                href, text, category_slug, self.price_re, marker, self.extract_store_sku, img
            )
            if listing is not None:
                listing.store_slug = self.store_slug
                listing.store_name = self.store_name
                result.append(listing)
        return result

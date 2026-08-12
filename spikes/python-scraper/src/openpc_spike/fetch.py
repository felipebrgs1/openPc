"""Coleta HTTP da Kabum — espelho de KabumCollector.cs (HTTP puro, sem Playwright).

Testa a hipótese central do spike: a Kabum entrega a listagem via
`__NEXT_DATA__` (JSON SSR) e não exige renderização de JS nem anti-bot.
"""

from __future__ import annotations

import asyncio
import random

import httpx

from .models import KabumListItem
from .parse import parse_next_data

# Rotas validadas no scraper C# (sitemap 2026-08-08).
CATEGORY_PATHS: dict[str, str] = {
    "cpu": "hardware/processadores",
    "motherboard": "hardware/placas-mae",
    "gpu": "hardware/placa-de-video-vga",
    "memory": "hardware/memoria-ram",
    "storage": "hardware/ssd-2-5",
    "psu": "hardware/fontes",
    "cooler": "hardware/coolers",
}

PAGE_SIZE = 60  # Kabum entrega 60 por página na listagem.

# Mesmo perfil do C# (BrowserCollectorBase/HttpClient): Chrome desktop.
HEADERS = {
    "User-Agent": (
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 "
        "(KHTML, like Gecko) Chrome/126.0 Safari/537.36"
    ),
    "Accept-Language": "pt-BR,pt;q=0.9",
    "Accept": "text/html,application/xhtml+xml",
}

# Delay conservador entre páginas — igual ao C# (1.5–2.5s).
DELAY_MIN, DELAY_MAX = 1.5, 2.5


def make_client() -> httpx.AsyncClient:
    return httpx.AsyncClient(
        headers=HEADERS,
        timeout=httpx.Timeout(30.0),
        follow_redirects=True,
    )


async def fetch_page(client: httpx.AsyncClient, path: str, page: int) -> list[KabumListItem]:
    """Uma página da listagem: GET + parse do __NEXT_DATA__."""
    url = f"https://www.kabum.com.br/{path}?page_number={page}"
    resp = await client.get(url)
    resp.raise_for_status()
    return parse_next_data(resp.text)


async def collect(
    category: str,
    max_pages: int | None = None,
    delay: tuple[float, float] = (DELAY_MIN, DELAY_MAX),
) -> list[KabumListItem]:
    """Coleta a categoria inteira (ou até max_pages), com rate limit."""
    path = CATEGORY_PATHS.get(category)
    if path is None:
        raise ValueError(f"Kabum: categoria '{category}' sem rota mapeada.")

    items: list[KabumListItem] = []
    page = 1
    async with make_client() as client:
        while max_pages is None or page <= max_pages:
            batch = await fetch_page(client, path, page)
            if not batch:
                break
            items.extend(batch)
            if len(batch) < PAGE_SIZE:
                break
            page += 1
            await asyncio.sleep(random.uniform(*delay))
    return items


def collect_sync(
    category: str,
    max_pages: int | None = None,
    delay: tuple[float, float] = (DELAY_MIN, DELAY_MAX),
) -> list[KabumListItem]:
    """Versão síncrona para CLI e testes (evita gerenciar o loop no main)."""
    return asyncio.run(collect(category, max_pages=max_pages, delay=delay))

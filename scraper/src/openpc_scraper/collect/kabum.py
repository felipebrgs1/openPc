"""Collector da Kabum via HTTP puro — espelho de KabumCollector.cs +
KabumPageParser.cs. Fonte: __NEXT_DATA__ SSR da listagem (sem Playwright).
"""

from __future__ import annotations

import asyncio
import json
import random
import re

import httpx

from .models import KabumListItem, RawListing
from ..normalize import match_key, part_number, price, spec_extractor

CATEGORY_PATHS: dict[str, str] = {
    "cpu": "hardware/processadores",
    "motherboard": "hardware/placas-mae",
    "gpu": "hardware/placa-de-video-vga",
    "memory": "hardware/memoria-ram",
    "storage": "hardware/ssd-2-5",
    "psu": "hardware/fontes",
    "cooler": "hardware/coolers",
}

PAGE_SIZE = 60
HEADERS = {
    "User-Agent": (
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 "
        "(KHTML, like Gecko) Chrome/126.0 Safari/537.36"
    ),
    "Accept-Language": "pt-BR,pt;q=0.9",
    "Accept": "text/html,application/xhtml+xml",
}

_NEXT_DATA_RE = re.compile(
    r'<script id="__NEXT_DATA__" type="application/json">(.*?)</script>',
    re.DOTALL,
)

STORE_NAME = "KaBuM!"


class KabumParseError(RuntimeError):
    """HTML sem __NEXT_DATA__ ou estrutura inesperada (bloqueio/mudança)."""


def parse_next_data(html: str) -> list[KabumListItem]:
    m = _NEXT_DATA_RE.search(html)
    if not m:
        raise KabumParseError("Kabum: __NEXT_DATA__ ausente na página (bloqueio?).")

    doc = json.loads(m.group(1))
    data = doc["props"]["pageProps"]["data"]
    if not isinstance(data, str):
        raise KabumParseError("Kabum: pageProps.data vazio.")

    catalog = json.loads(data)["catalogServer"]["data"]
    items: list[KabumListItem] = []
    for el in catalog:
        manufacturer = None
        if isinstance(el.get("manufacturer"), dict):
            manufacturer = el["manufacturer"].get("name")

        pwd = el.get("priceWithDiscount") or 0
        items.append(
            KabumListItem(
                code=int(el["code"]),
                title=str(el.get("name") or ""),
                friendly_name=str(el.get("friendlyName") or ""),
                manufacturer=manufacturer,
                price_with_discount=float(pwd) if pwd > 0 else None,
                price=float(el["price"]),
                max_installment=el.get("maxInstallment"),
                available=bool(el.get("available")),
                thumbnail=el.get("thumbnail"),
            )
        )
    return items


def build_listing(item: KabumListItem, category: str) -> RawListing:
    """Item bruto → listing normalizada (espelho do BuildListing do C#)."""
    url = f"https://www.kabum.com.br/produto/{item.code}/{item.friendly_name}"

    price_cash = item.price_with_discount or item.price
    price_card = item.price

    specs: dict[str, str] = {}
    if category == "cpu":
        specs = spec_extractor.extract_cpu(item.title, None)
    elif category == "gpu":
        specs = spec_extractor.extract_gpu(item.title, None)
    elif category == "motherboard":
        specs = spec_extractor.extract_motherboard(item.title)
    elif category == "memory":
        specs = spec_extractor.extract_memory(item.title)
    elif category == "psu":
        specs = spec_extractor.extract_psu(item.title)

    return RawListing(
        store_slug="kabum",
        store_name=STORE_NAME,
        product_url=url,
        price_cash=price_cash,
        price_card=price_card,
        installments=None,
        installment_text=item.max_installment,
        in_stock=item.available,
        thumbnail=item.thumbnail,
        title=item.title,
        manufacturer=item.manufacturer,
        part_number=part_number.extract(item.title),
        match_key=match_key.build(item.title),
        category_slug=category,
        specs=specs,
        store_sku=str(item.code),
    )


async def fetch_page(client: httpx.AsyncClient, path: str, page: int) -> list[KabumListItem]:
    url = f"https://www.kabum.com.br/{path}?page_number={page}"
    resp = await client.get(url)
    resp.raise_for_status()
    return parse_next_data(resp.text)


async def collect(
    category: str,
    max_pages: int | None = None,
    delay: tuple[float, float] = (1.5, 2.5),
    concurrency: int = 3,
) -> list[RawListing]:
    """Coleta a categoria inteira (ou até max_pages) e normaliza os itens.

    As páginas são buscadas em ondas concorrentes de N páginas (vs. o fetch
    sequencial do C# original) — várias vezes mais rápido, mantendo o
    intervalo entre ondas para não estourar o rate limit.
    """
    path = CATEGORY_PATHS.get(category)
    if path is None:
        raise ValueError(f"Kabum: categoria '{category}' sem rota mapeada.")

    listings: list[RawListing] = []
    page = 1
    async with httpx.AsyncClient(headers=HEADERS, timeout=httpx.Timeout(30.0), follow_redirects=True) as client:
        while max_pages is None or page <= max_pages:
            end = page + max(1, concurrency)
            if max_pages is not None:
                end = min(end, max_pages + 1)
            batches = await asyncio.gather(
                *(fetch_page(client, path, p) for p in range(page, end))
            )
            short = False
            for batch in batches:
                listings.extend(build_listing(i, category) for i in batch)
                if len(batch) < PAGE_SIZE:
                    short = True  # última página — para após esta onda
            if short:
                break
            page = end
            await asyncio.sleep(random.uniform(*delay))
    return listings


def collect_sync(category: str, max_pages: int | None = None) -> list[RawListing]:
    return asyncio.run(collect(category, max_pages=max_pages))

"""Card de loja browser (Pichau/Terabyte) → RawListing — espelho de
CardListingBuilder.cs. Isolado para testes com fixtures de texto real.
"""

from __future__ import annotations

import re

from .models import RawListing
from ..normalize import match_key, part_number, price, spec_extractor

_INSTALLMENT_RE = re.compile(r"(\d{1,3})x\s*de\s*R\$\s*[\d.,]+", re.IGNORECASE)


def build_card_listing(
    href: str,
    card_text: str,
    category_slug: str,
    price_regex: re.Pattern[str],
    price_marker: str,
    extract_store_sku,
    thumbnail: str | None = None,
) -> RawListing | None:
    m = price_regex.search(card_text)
    if not m:
        return None
    parsed = price.parse_price_br(m.group(1))
    if parsed is None:
        return None

    # nome = linha mais longa antes do bloco de preço (badges ficam em linhas curtas)
    head = card_text.split(price_marker, 2)[0]
    name = max((l.strip() for l in head.split("\n") if l.strip()), key=len, default="")

    installment = _INSTALLMENT_RE.search(card_text)

    specs: dict[str, str] = {}
    if category_slug == "cpu":
        specs = spec_extractor.extract_cpu(name, None)
    elif category_slug == "gpu":
        specs = spec_extractor.extract_gpu(name, None)
    elif category_slug == "motherboard":
        specs = spec_extractor.extract_motherboard(name)
    elif category_slug == "memory":
        specs = spec_extractor.extract_memory(name)
    elif category_slug == "psu":
        specs = spec_extractor.extract_psu(name)

    return RawListing(
        store_slug="",  # preenchido pelo collector da loja
        store_name="",
        product_url=href,
        price_cash=parsed,
        price_card=None,
        installments=int(installment.group(1)) if installment else None,
        installment_text=installment.group(0).strip() if installment else None,
        in_stock="esgotado" not in card_text.lower(),
        thumbnail=thumbnail,
        title=name,
        manufacturer=None,
        part_number=part_number.extract(f"{name} {href}"),
        match_key=match_key.build(name),
        category_slug=category_slug,
        specs=specs,
        store_sku=extract_store_sku(href),
    )

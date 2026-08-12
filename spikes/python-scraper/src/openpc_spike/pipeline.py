"""Pipeline item bruto → listing normalizada — espelho de CardListingBuilder.cs.

URL do produto, marca/modelo/part number e specs por categoria (CPU usa o
título, como no C# M2).
"""

from __future__ import annotations

from .models import KabumListItem, RawListing
from .normalize import extract_cpu_specs

STORE_NAME = "KaBuM!"


def build_listing(item: KabumListItem, category: str) -> RawListing:
    url = f"https://www.kabum.com.br/produto/{item.code}/{item.friendly_name}"

    # Preço: priceWithDiscount (pix/boleto) ou price cheio.
    price_cash = item.price_with_discount or item.price
    price_card = item.price

    # Part number Kabum vive no final do título após " - " (ex: "100-100000644BOX").
    part_number = None
    if " - " in item.title:
        tail = item.title.rsplit(" - ", 1)[1].strip()
        if tail and tail.isalnum():
            part_number = tail

    specs = extract_cpu_specs(item.title) if category == "cpu" else {}

    return RawListing(
        store_slug="kabum",
        store_name=STORE_NAME,
        product_url=url,
        price_cash=price_cash,
        price_card=price_card,
        installments=None,  # maxInstallment é texto ("12x") — enrichment futuro
        installment_text=item.max_installment,
        in_stock=item.available,
        thumbnail=item.thumbnail,
        name=item.title,
        brand=item.manufacturer,
        model=None,  # deriva do match key (enrichment)
        part_number=part_number,
        category_slug=category,
        specs=specs,
    )

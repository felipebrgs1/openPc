"""Modelos de dados — espelham os records do scraper C#.

KabumListItem (KabumPageParser.cs) e RawListing (CardListingBuilder.cs /
KabumCollector.cs).
"""

from __future__ import annotations

from dataclasses import asdict, dataclass, field
from typing import Any


@dataclass(slots=True)
class KabumListItem:
    code: int
    title: str
    friendly_name: str
    manufacturer: str | None
    price_with_discount: float | None
    price: float
    max_installment: str | None
    available: bool
    thumbnail: str | None


@dataclass(slots=True)
class RawListing:
    """Listing normalizada, pronta para a ingestão."""

    store_slug: str
    store_name: str
    product_url: str
    price_cash: float | None
    price_card: float | None
    installments: int | None
    installment_text: str | None
    in_stock: bool
    thumbnail: str | None
    title: str
    manufacturer: str | None
    part_number: str | None
    match_key: str | None
    category_slug: str
    specs: dict[str, str] = field(default_factory=dict)
    store_sku: str | None = None  # âncora primária do dedup (loja+SKU)

    def to_dict(self) -> dict[str, Any]:
        return asdict(self)

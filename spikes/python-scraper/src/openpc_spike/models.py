"""Modelos de dados do spike — espelham os records do scraper C#.

Referência: src/OpenPc.Scraper/Collectors/KabumPageParser.cs (KabumListItem)
e src/OpenPc.Scraper/Collectors/CardListingBuilder.cs (RawListing).
"""

from __future__ import annotations

from dataclasses import asdict, dataclass, field
from typing import Any


@dataclass(slots=True)
class KabumListItem:
    """Item bruto da listagem (espelho de KabumListItem do C#)."""

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
    """Listing normalizada (espelho de RawListing do C#)."""

    store_slug: str
    store_name: str
    product_url: str
    price_cash: float | None
    price_card: float | None
    installments: int | None
    installment_text: str | None
    in_stock: bool
    thumbnail: str | None
    name: str
    brand: str | None
    model: str | None
    part_number: str | None
    category_slug: str
    specs: dict[str, str] = field(default_factory=dict)

    def to_dict(self) -> dict[str, Any]:
        return asdict(self)

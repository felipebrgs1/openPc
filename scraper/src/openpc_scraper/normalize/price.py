"""Parse de preço em formato brasileiro — espelho de PriceParser.cs.

"1.599,99" → 1599.99
"""

from __future__ import annotations


def parse_price_br(value: str | None) -> float | None:
    if not value or not value.strip():
        return None
    normalized = value.replace(".", "").replace(",", ".")
    try:
        return float(normalized)
    except ValueError:
        return None

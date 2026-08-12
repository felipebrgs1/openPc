"""Parser do __NEXT_DATA__ da Kabum — espelho de KabumPageParser.cs.

A listagem embute um JSON com os dados do catálogo; o parser extrai e
valida a estrutura, falhando com mensagem clara se a Kabum mudar o HTML
ou começar a bloquear (mesmo comportamento do C#).
"""

from __future__ import annotations

import json
import re

from .models import KabumListItem

_NEXT_DATA_RE = re.compile(
    r'<script id="__NEXT_DATA__" type="application/json">(.*?)</script>',
    re.DOTALL,
)


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

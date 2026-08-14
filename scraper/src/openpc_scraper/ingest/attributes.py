"""Aplicação de specs EAV com precedência por fonte.

Prioridade: reference (0) < title (1) < page (2) < manual (3).
- reference: banco curado por chip (reference_specs.json) — só preenche lacunas;
- title: specs extraídas do título da listagem (padrão do scraper atual);
- page: ficha técnica da página de produto (boost clock da versão de marca...);
- manual: edição humana (futuro) — nada sobrescreve.

Uma spec só é atualizada por uma fonte de prioridade >= a atual.
"""

from __future__ import annotations

import uuid

from sqlalchemy.ext.asyncio import AsyncSession

from ..db.models import Product, ProductAttribute

_PRIORITY = {"reference": 0, "title": 1, "page": 2, "manual": 3}

MAX_VALUE_LEN = 256


def _parse_num(value: str) -> float | None:
    try:
        return float(value.replace(",", "."))
    except ValueError:
        return None


def _parse_bool(value: str) -> bool | None:
    v = value.lower()
    if v == "true":
        return True
    if v == "false":
        return False
    return None


def apply_specs(
    db: AsyncSession,
    product: Product,
    specs: dict[str, str],
    source: str,
    existing: dict[str, ProductAttribute],
) -> int:
    """Aplica specs ao produto respeitando a precedência. Retorna nº de mudanças.

    `existing` é o cache {key: attr} do produto (pré-carregado em lote pelo
    chamador — evita lazy-load de product.attributes em contexto async)."""
    if not specs:
        return 0
    cache = existing
    changed = 0
    priority = _PRIORITY.get(source, 1)
    for key, value in specs.items():
        if not value:
            continue
        value = value[:MAX_VALUE_LEN]
        attr = cache.get(key)
        if attr is None:
            attr = ProductAttribute(
                id=uuid.uuid4(),
                product_id=product.id,
                key=key,
                value_text=value,
                value_num=_parse_num(value),
                value_bool=_parse_bool(value),
                source=source,
            )
            cache[key] = attr
            db.add(attr)
            changed += 1
        elif priority >= _PRIORITY.get(attr.source or "title", 1) and attr.value_text != value:
            attr.value_text = value
            attr.value_num = _parse_num(value)
            attr.value_bool = _parse_bool(value)
            attr.source = source
            changed += 1
    return changed

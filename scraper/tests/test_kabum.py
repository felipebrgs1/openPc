"""Paginação concorrente do collector Kabum (ondas de páginas)."""

from __future__ import annotations

import pytest

from openpc_scraper.collect import kabum
from openpc_scraper.collect.models import KabumListItem


def _item(code: int) -> KabumListItem:
    return KabumListItem(
        code=code,
        title=f"Produto {code}",
        friendly_name=f"produto-{code}",
        manufacturer=None,
        price_with_discount=None,
        price=10.0,
        max_installment=None,
        available=True,
        thumbnail=None,
    )


def _full_page(first_code: int) -> list[KabumListItem]:
    return [_item(first_code + i) for i in range(kabum.PAGE_SIZE)]


async def test_collect_busca_paginas_em_ondas(monkeypatch):
    calls: list[int] = []

    async def fake_fetch_page(client, path, page):
        calls.append(page)
        return _full_page(page * 100)

    monkeypatch.setattr(kabum, "fetch_page", fake_fetch_page)
    items = await kabum.collect("cpu", max_pages=5, delay=(0, 0), concurrency=3)
    assert len(items) == 5 * kabum.PAGE_SIZE
    assert calls == [1, 2, 3, 4, 5]


async def test_collect_para_na_onda_da_ultima_pagina(monkeypatch):
    calls: list[int] = []

    async def fake_fetch_page(client, path, page):
        calls.append(page)
        return _full_page(page * 100) if page <= 3 else _full_page(page * 100)[:10]

    monkeypatch.setattr(kabum, "fetch_page", fake_fetch_page)
    items = await kabum.collect("cpu", delay=(0, 0), concurrency=2)
    # onda [1,2] cheia, onda [3,4] tem página curta → para sem buscar mais
    assert calls == [1, 2, 3, 4]
    assert len(items) == 3 * kabum.PAGE_SIZE + 10


async def test_collect_catalogo_vazio_interrompe_apos_onda(monkeypatch):
    calls: list[int] = []

    async def fake_fetch_page(client, path, page):
        calls.append(page)
        return []

    monkeypatch.setattr(kabum, "fetch_page", fake_fetch_page)
    items = await kabum.collect("cpu", delay=(0, 0), concurrency=3)
    assert items == []
    assert calls == [1, 2, 3]


async def test_collect_categoria_sem_rota_levanta_erro():
    with pytest.raises(ValueError, match="sem rota"):
        await kabum.collect("monitor")


async def test_rota_case_mapeada_para_perifericos_gabinetes():
    assert kabum.CATEGORY_PATHS["case"] == "perifericos/gabinetes"

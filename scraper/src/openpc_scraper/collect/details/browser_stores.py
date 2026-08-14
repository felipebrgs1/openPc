"""Ficha técnica das páginas de produto Pichau/Terabyte (Playwright).

Pichau: tabela `.table-specification` (tr > th "Rótulo:" + td valor), já no
DOM após o load — validado ao vivo. Terabyte: bloco `div.tecnicas` com
`<p><strong>Rótulo:</strong><br>valor</p>` (sub-itens separados por <br>).
"""

from __future__ import annotations

from playwright.async_api import Page

_PICHAU_JS = """
() => Array.from(document.querySelectorAll('.table-specification tr'))
    .map(tr => {
        const th = tr.querySelector('th'); const td = tr.querySelector('td');
        return th && td ? [th.innerText.trim(), td.innerText.trim()] : null;
    })
    .filter(Boolean)
"""

_TERABYTE_JS = """
() => Array.from(document.querySelectorAll('div.tecnicas p, div[itemprop="description"] p'))
    .map(p => {
        const strong = p.querySelector('strong');
        if (!strong) return null;
        const label = strong.innerText.trim();
        const clone = p.cloneNode(true);
        const s = clone.querySelector('strong');
        if (s) s.remove();
        let value = clone.innerText.replace(/^\\s*:?\\s*/, '').trim();
        return [label, value];
    })
    .filter(r => r && r[0] && r[1])
"""


async def extract_pairs(page: Page, store_slug: str) -> list[tuple[str, str]]:
    """Página de produto já carregada → lista (rótulo, valor) da ficha técnica."""
    js = _PICHAU_JS if store_slug == "pichau" else _TERABYTE_JS
    try:
        rows = await page.evaluate(js)
    except Exception:  # noqa: BLE001 — estrutura mudou; retorna vazio
        return []
    return [(str(r[0]), str(r[1])) for r in rows if isinstance(r, list) and len(r) >= 2]

"""Ficha técnica da página de produto da Kabum — via __NEXT_DATA__ (HTTP
puro, sem Playwright). Duas fontes por página:

- `technicalInformation.text` — ficha estruturada do produto oficial
  ("Motor gráfico", "Memória", "Relógio do motor"...).
- `description` — ficha dos produtos marketplace ("• Rótulo: valor" sob
  "ESPECIFICAÇÕES TÉCNICAS").

A ficha oficial vem por último e vence em caso de conflito.
"""

from __future__ import annotations

import html
import json
import re

import httpx

from ...normalize.spec_map import map_specs

_NEXT_DATA_RE = re.compile(
    r'<script id="__NEXT_DATA__" type="application/json">(.*?)</script>',
    re.DOTALL,
)

HEADERS = {
    "User-Agent": (
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 "
        "(KHTML, like Gecko) Chrome/126.0 Safari/537.36"
    ),
    "Accept-Language": "pt-BR,pt;q=0.9",
    "Accept": "text/html,application/xhtml+xml",
}


def parse_spec_pairs(spec_html: str, only_spec_sections: bool = False) -> list[tuple[str, str]]:
    """HTML da ficha técnica → lista (rótulo, valor) crua.

    Linhas "- rótulo: valor" (ficha oficial) ou "• rótulo: valor"
    (marketplace) viram par direto; linhas "- valor" sem rótulo herdam o
    cabeçalho de seção anterior ("Motor gráfico", "Memória"...).

    only_spec_sections: ignora bullets fora de seções de ficha ("DESTAQUE",
    "ITENS INCLUSOS"...) — usado no campo description, que mistura ficha
    técnica com prosa de marketing.
    """
    text = re.sub(r"<br\s*/?>", "\n", spec_html)
    text = re.sub(r"</p>", "\n", text)
    text = re.sub(r"<li[^>]*>", "\n- ", text)
    text = re.sub(r"<[^>]+>", "", text)
    text = html.unescape(text)

    spec_section = not only_spec_sections  # sem gate, toda seção vale
    pairs: list[tuple[str, str]] = []
    section: str | None = None
    for line in text.splitlines():
        line = line.strip()
        if not line or line in ("-", "\u2022", "*"):
            continue
        if line[0] in ("-", "\u2022", "*"):
            item = line[1:].strip(" \t-\u2022*")
            if not item:
                continue
            if spec_section and ":" in item:
                label, _, value = item.partition(":")
                pairs.append((label.strip(), value.strip()))
            elif spec_section and section:
                pairs.append((section, item))
            continue
        # linha sem marcador = cabeçalho de seção ("Motor gráfico", "Memória"...)
        header = line.rstrip(":").strip()
        if header and len(header) <= 48:
            section = header
            if only_spec_sections:
                norm = re.sub(r"[^a-z0-9 ]", "", header.lower())
                spec_section = any(
                    word in norm for word in ("especifica", "caracter", "ficha", "informac", "detalhe")
                )
    return pairs


def _spec_sources(doc: dict) -> list[tuple[str, bool]]:
    """Fontes de ficha na página: (html, gated). description (marketplace)
    vem primeiro e com gate de seção; technicalInformation.text (produto
    oficial) por último e sem gate — vence em caso de conflito."""
    try:
        product = doc["props"]["pageProps"]["product"]
    except (KeyError, TypeError):
        return []
    sources: list[tuple[str, bool]] = []
    description = product.get("description") or ""
    if description:
        sources.append((description, True))
    technical = product.get("technicalInformation") or {}
    if isinstance(technical, dict):
        spec_html = technical.get("text") or ""
        if spec_html:
            sources.append((spec_html, False))
    return sources


def _extract_specs(doc: dict, category: str) -> dict[str, str]:
    specs: dict[str, str] = {}
    for source, gated in _spec_sources(doc):
        for key, value in map_specs(category, parse_spec_pairs(source, only_spec_sections=gated)).items():
            specs[key] = value
    return specs


def extract_specs_from_next_data(html_text: str, category: str) -> dict[str, str]:
    """Página de produto (HTML) → specs canônicas EAV. {} se não houver ficha."""
    m = _NEXT_DATA_RE.search(html_text)
    if not m:
        return {}
    try:
        doc = json.loads(m.group(1))
    except json.JSONDecodeError:
        return {}
    return _extract_specs(doc, category)


async def fetch_product_specs(
    client: httpx.AsyncClient, url: str, category: str
) -> dict[str, str]:
    resp = await client.get(url)
    if resp.status_code != 200:
        raise httpx.HTTPStatusError(
            f"Kabum: HTTP {resp.status_code} na página de produto", request=resp.request, response=resp
        )
    m = _NEXT_DATA_RE.search(resp.text)
    if not m:
        return {}
    try:
        doc = json.loads(m.group(1))
    except json.JSONDecodeError:
        return {}
    return _extract_specs(doc, category)

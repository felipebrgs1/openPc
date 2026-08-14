"""Banco de specs de REFERÊNCIA (dado curado em data/reference_specs.json).

Cada produto (ex: "RTX 5070 ASUS TUF Gaming OC") é uma versão de marca de um
chip (ex: RTX 5070). A referência traz as specs de fábrica do chip; a ficha
técnica da página da loja (fonte `page`) traz os valores da versão de marca
(boost clock maior, dimensões, conectores) e SOBRESCREVE a referência.
Precedência na ingestão: page > title > reference.
"""

from __future__ import annotations

import json
import re
from functools import lru_cache
from importlib import resources

from .text import normalize

# id do chip a partir do título (mesmo estilo do match_key, mas com sufixo).
_NVIDIA_RE = re.compile(r"\b(rtx|gtx)\s*(\d{3,4})\s*(ti|super)?")
_AMD_GPU_RE = re.compile(r"\b(?:radeon\s+)?rx\s*(\d{3,4})\s*(xtx|xt|gre)?")
_ARC_RE = re.compile(r"\barc\s*([ab])(\d{3})")
_AMD_CPU_RE = re.compile(r"\b(?:ryzen|r)\s*([3579])\s*-?\s*(\d{4})(x3d|xt|gt|x|g|f)?")
_INTEL_CPU_RE = re.compile(
    r"\b(?:core\s+)?(ultra\s+)?(?:i)?([3579])\s*-?\s*(\d{3,5})([kf]{1,2})?"
)

_GPU_SUFFIX = {"ti": "ti", "super": "super", "xtx": "xtx", "xt": "xt", "gre": "gre"}


def chip_id(category: str, title: str | None) -> str | None:
    """id canônico do chip no banco de referência (ex: 'rtx5070ti', 'r77800x3d')."""
    if not title or not title.strip():
        return None
    text = normalize(title)

    if category == "gpu":
        m = _NVIDIA_RE.search(text)
        if m:
            base = f"{m.group(1)}{m.group(2)}"
            suffix = _GPU_SUFFIX.get((m.group(3) or "").lower(), "")
            chip = base + suffix
            # RTX 3050 tem duas versões (8GB GA107 / 6GB GA107 reduzido)
            if chip == "rtx3050" and re.search(r"\b6\s*gb\b", text):
                chip = "rtx3050-6gb"
            return chip
        m = _AMD_GPU_RE.search(text)
        if m:
            return f"rx{m.group(1)}{_GPU_SUFFIX.get((m.group(2) or '').lower(), '')}"
        m = _ARC_RE.search(text)
        if m:
            return f"arc{m.group(1)}{m.group(2)}"
        return None

    if category == "cpu":
        m = _AMD_CPU_RE.search(text)
        if m:
            return f"r{m.group(1)}{m.group(2)}{m.group(3) or ''}"
        m = _INTEL_CPU_RE.search(text)
        if m:
            prefix = "u" if m.group(1) else "i"
            return f"{prefix}{m.group(2)}{m.group(3)}{m.group(4) or ''}"
        return None

    return None


@lru_cache(maxsize=1)
def _load() -> dict:
    data = resources.files("openpc_scraper").joinpath("data/reference_specs.json")
    with data.open("r", encoding="utf-8") as fh:
        return json.load(fh)


def lookup(category: str, title: str | None) -> tuple[str, dict[str, str]] | None:
    """(chip_id, specs) do banco de referência para o título, ou None.

    Specs numéricas viram texto com ponto decimal (ex: 6144.0 → "6144") para
    o EAV — o ingest converte de volta para ValueNum quando possível.
    """
    cid = chip_id(category, title)
    if cid is None:
        return None
    entry = _load().get(category, {}).get(cid)
    if entry is None:
        return None

    specs: dict[str, str] = {"reference_model": entry["name"]}
    if entry.get("launch"):
        specs["launch"] = entry["launch"]
    for key, value in entry.get("specs", {}).items():
        if isinstance(value, bool):
            specs[key] = "true" if value else "false"
        elif isinstance(value, (int, float)):
            specs[key] = str(value)
        else:
            specs[key] = str(value)
    return cid, specs

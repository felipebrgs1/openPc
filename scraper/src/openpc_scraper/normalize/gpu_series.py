"""Série/geração da GPU a partir do título — espelho de GpuSeries.cs.

Valores canônicos: rtx20/rtx30/rtx40/rtx50, gtx16, rx5000/rx6000/rx7000/
rx9000, arc. Fora do padrão retorna None.
"""

from __future__ import annotations

import re

_RTX_RE = re.compile(r"\brtx\s*(\d{4})")
_GTX_RE = re.compile(r"\bgtx\s*16\d{2}")
_RX_RE = re.compile(r"\b(?:rx|radeon)\s*(\d{4})")
_ARC_RE = re.compile(r"\barc\s*[ab]\d{3}")


def classify(title: str | None) -> str | None:
    if not title:
        return None
    t = title.lower()

    m = _RTX_RE.search(t)
    if m:
        return {
            "2": "rtx20",
            "3": "rtx30",
            "4": "rtx40",
            "5": "rtx50",
        }.get(m.group(1)[0])  # ex.: "RTX 9070" (typo de loja) → None

    if _GTX_RE.search(t):
        return "gtx16"

    m = _RX_RE.search(t)
    if m:
        return {
            "5": "rx5000",
            "6": "rx6000",
            "7": "rx7000",
            "9": "rx9000",
        }.get(m.group(1)[0])

    if _ARC_RE.search(t):
        return "arc"

    return None

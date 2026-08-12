"""Normalização de texto — espelho de MatchKey.Normalize (C#).

lowercase, sem acento, só letras/dígitos/espaço, espaços colapsados.
"""

from __future__ import annotations

import re
import unicodedata

_WS_RE = re.compile(r"\s+")


def normalize(value: str) -> str:
    decomposed = unicodedata.normalize("NFD", value)
    chars = [
        c.lower()
        for c in decomposed
        if c.isalnum() or c == " "
    ]
    return _WS_RE.sub(" ", "".join(chars)).strip()

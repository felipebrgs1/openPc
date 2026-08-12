"""Part number do fabricante — espelho de PartNumber.cs.

Âncora primária do dedup (nenhuma loja expõe EAN no front).
"""

from __future__ import annotations

import re

# AMD: 100-100000926WOF, 100-100001721WOF
_AMD_RE = re.compile(r"\b\d{3}-\d{9}[A-Z]{2,4}\b", re.IGNORECASE)
# Intel boxed: BX8071512400F, BX80768250K
_INTEL_BOXED_RE = re.compile(r"\bBX\d{6,9}[A-Z0-9]*\b", re.IGNORECASE)


def extract(text: str | None) -> str | None:
    if not text or not text.strip():
        return None
    m = _AMD_RE.search(text)
    if not m:
        m = _INTEL_BOXED_RE.search(text)
    return normalize(m.group(0)) if m else None


def normalize(part_number: str) -> str:
    """Uppercase, sem hífens/espaços — para comparação."""
    return "".join(c for c in part_number if c.isalnum()).upper()

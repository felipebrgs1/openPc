"""Geração da CPU — espelho de CpuGeneration.cs (OpenPc.Domain).

Classifica a geração a partir do modelo (match key normalizado, ex:
"amd 7600x", "intel 12400f", "intel 265f"). Códigos = matriz de chipsets
(compatibility.json): zen1..zen5, alder-lake, raptor-lake,
raptor-lake-refresh, arrow-lake. Retorna None para CPUs fora da matriz
desktop (Athlon, A-series, FX, Intel < 12ª geração, mobile).
"""

from __future__ import annotations

import re

# (?<!\d)(\d{4})(?!\d)
_FOUR_DIGIT_RE = re.compile(r"(?<!\d)(\d{4})(?!\d)")
# \ba\d{1,2}\b  (A-series da AMD: A4/A6/A8/A9/A10/A12)
_AMD_SERIES_RE = re.compile(r"\ba\d{1,2}\b")
# \bfx\b
_FX_RE = re.compile(r"\bfx\b")
# (?<!\d)(1[2-4]\d{3})(?!\d)
_INTEL_FAMILY_RE = re.compile(r"(?<!\d)(1[2-4]\d{3})(?!\d)")
# (?<!\d)(2\d{2})(?!\d)  (Core Ultra 2xx)
_ULTRA_FAMILY_RE = re.compile(r"(?<!\d)(2\d{2})(?!\d)")


def classify(model: str | None) -> str | None:
    if not model:
        return None

    m = model.lower()

    # AMD fora da matriz desktop (Athlon, A-series, FX, Sempron, Phenom,
    # Opteron) — checado ANTES dos branches: o título cru pode ter
    # "Processador " na frente (não começa com "amd") e "dualcore" contém
    # "core" (entraria no branch Intel e "235e" casaria UltraFamily →
    # arrow-lake falso). Mesmo caso do C#: "Amd Ryzen Athlon 3000g".
    if any(k in m for k in ("athlon", "sempron", "phenom", "opteron")):
        return None
    if _AMD_SERIES_RE.search(m) or _FX_RE.search(m):
        return None

    if "ryzen" in m or m.startswith("amd"):
        four = _FOUR_DIGIT_RE.search(m)
        if not four:
            return None
        return {
            "1": "zen1",                 # Ryzen 1000 (Summit Ridge)
            "2": "zen2",                 # 2000 (Zen+)
            "3": "zen2",                 # 3000
            "4": "zen2",                 # 4000G
            "5": "zen3",
            "6": None,                   # Ryzen 6000 é mobile
            "7": "zen4",                 # 7000 (Raphael)
            "8": "zen4",                 # 8000G (Phoenix)
            "9": "zen5",
        }.get(four.group(1)[0])

    if "core" in m or "ultra" in m or m.startswith("intel"):
        five = _INTEL_FAMILY_RE.search(m)
        if five:
            return {
                "12": "alder-lake",
                "13": "raptor-lake",
                "14": "raptor-lake-refresh",
            }.get(five.group(1)[:2])

        # Core Ultra 2xx (desktop, LGA 1851)
        if _ULTRA_FAMILY_RE.search(m):
            return "arrow-lake"

        return None

    return None

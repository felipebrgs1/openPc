"""Match key determinística (marca + modelo) — espelho de MatchKey.cs.

Categorias com padrão conhecido retornam "marca modelo" (ex: "amd 7600x",
"intel 12400f", "nvidia 5070", "amd 7800xt"); as demais retornam None.
"""

from __future__ import annotations

import re

from .text import normalize

# Espelho dos GeneratedRegex do C# (aplicados sobre o texto normalizado).
_CPU_AMD_RE = re.compile(r"\bryzen\s+(?:[3579]\s*)?(\d{3,5}[a-z0-9]*)\b")
_CPU_INTEL_RE = re.compile(r"\b(?:core\s+)?(?:ultra\s+)?(i[3579]|ultra\s*[579])\s*-?\s*(\d{3,5}[a-z0-9]*)\b")
_GPU_NVIDIA_RE = re.compile(r"\b(?:rtx|gtx|titan)\s*(\d{3,4}[a-z0-9]*)\b")
_GPU_AMD_RE = re.compile(r"\b(?:radeon\s+)?rx\s*(\d{3,4}[a-z0-9]*\s*(?:xt|gre)?)\b")


def build(title: str | None) -> str | None:
    if not title or not title.strip():
        return None

    text = normalize(title)

    m = _CPU_AMD_RE.search(text)
    if m:
        return f"amd {m.group(1).replace(' ', '')}"

    m = _CPU_INTEL_RE.search(text)
    if m:
        return f"intel {m.group(2).replace(' ', '')}"

    m = _GPU_NVIDIA_RE.search(text)
    if m:
        return f"nvidia {m.group(1).replace(' ', '')}"

    m = _GPU_AMD_RE.search(text)
    if m:
        return f"amd {m.group(1).replace(' ', '')}"

    return None

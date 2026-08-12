"""Extração de specs estruturadas (EAV) — espelho de SpecExtractor.cs.

Contrato de chaves igual ao das specs (docs/specs.md §3.2). Spec ausente =
chave omitida (a engine trata como "desconhecido").
"""

from __future__ import annotations

import re

_SOCKET_RE = re.compile(r"\b(AM5|AM4|LGA\s?1?[0-9]{3}|sTR5|sTRX50|sTRX40)\b", re.IGNORECASE)
_TDP_RE = re.compile(r"(?:TDP|consumo|pot[eê]ncia)[^0-9]{0,20}(\d{2,4})\s*W", re.IGNORECASE)
_CORES_RE = re.compile(r"(\d{1,2})\s*[Nn][úu]cleos?")
_THREADS_RE = re.compile(r"(\d{1,3})\s*[Tt]hreads?")
_MEMORY_TYPE_RE = re.compile(r"\b(DDR[45])\b", re.IGNORECASE)
_PSU_WATTAGE_RE = re.compile(r"\b(\d{3,4})\s*[Ww](?:atts?)?\b", re.IGNORECASE)
_CHIPSET_RE = re.compile(r"(?<![A-Za-z0-9])(?:AMD|Intel\s+)?([ABXHZ]\d{3})", re.IGNORECASE)
_FORM_FACTOR_RE = re.compile(r"\b(M(?:icro)?-?ATX|Mini-?ITX|E-?ATX|ATX|ITX)\b", re.IGNORECASE)
_GPU_MEMORY_GB_RE = re.compile(r"(\d{2,3})\s*GB", re.IGNORECASE)
_DIMENSIONS_MM_RE = re.compile(r"(\d{3})\s*[xX]\s*\d{2,3}\s*[xX]\s*\d{1,3}\s*mm", re.IGNORECASE)
_POWER_CONNECTORS_RE = re.compile(
    r"(?:conectores|alimenta[çc][ãa]o|power)[^0-9]{0,20}"
    r"(\d+\s*x\s*\d+\s*(?:pinos?|pin)|16\s*pinos?|8\s*pinos?|12V-?2x?6?)",
    re.IGNORECASE,
)

_SEM_VIDEO_RE = re.compile(r"sem v[íi]deo|without graphics|no i?gpu|sem placa", re.IGNORECASE)
_COM_VIDEO_RE = re.compile(
    r"com v[íi]deo|gr[áa]ficos integrados|integrated graphics|radeon graphics|uhd graphics|hd graphics|iris",
    re.IGNORECASE,
)


def _first(text: str, regex: re.Pattern[str]) -> str | None:
    m = regex.search(text)
    return m.group(1).strip() if m else None


def _set(specs: dict[str, str], key: str, value: str | None) -> None:
    if value:
        specs[key] = value.strip()


def normalize_socket(value: str | None) -> str | None:
    """Socket canônico: minúsculas e sem espaço ("LGA 1700" → "lga1700")."""
    if not value or not value.strip():
        return None
    return value.lower().replace(" ", "")


def normalize_form_factor(value: str | None) -> str | None:
    """Contrato da engine (§3.2): atx | matx | itx | eatx."""
    if not value:
        return None
    return {
        "mini-itx": "itx",
        "miniitx": "itx",
        "m-atx": "matx",
        "matx": "matx",
        "micro-atx": "matx",
        "microatx": "matx",
        "e-atx": "eatx",
        "eatx": "eatx",
        "atx": "atx",
        "itx": "itx",
    }.get(value.lower(), value.lower())


def normalize_chipset(value: str | None) -> str | None:
    if not value or not value.strip():
        return None
    s = value.lower().replace(" ", "")
    if s.startswith("amd"):
        s = s[3:]
    elif s.startswith("intel"):
        s = s[5:]
    return s or None


def _detect_igpu(title: str, spec_text: str) -> str | None:
    combined = f"{title} {spec_text}"
    if _SEM_VIDEO_RE.search(combined):
        return "false"
    if _COM_VIDEO_RE.search(combined):
        return "true"
    return None


def extract_cpu(title: str, spec_text: str | None = None) -> dict[str, str]:
    specs: dict[str, str] = {}
    text = spec_text or ""

    _set(specs, "socket", normalize_socket(_first(text, _SOCKET_RE) or _first(title, _SOCKET_RE)))
    _set(specs, "tdp_w", _first(text, _TDP_RE) or _first(title, _TDP_RE))
    _set(specs, "cores", _first(title, _CORES_RE) or _first(text, _CORES_RE))
    _set(specs, "threads", _first(title, _THREADS_RE) or _first(text, _THREADS_RE))
    _set(specs, "memory_type", _first(text, _MEMORY_TYPE_RE) or _first(title, _MEMORY_TYPE_RE))
    _set(specs, "has_igpu", _detect_igpu(title, text))
    return specs


def extract_gpu(title: str, spec_text: str | None = None) -> dict[str, str]:
    specs: dict[str, str] = {}
    text = spec_text or ""

    _set(specs, "memory_gb", _first(title, _GPU_MEMORY_GB_RE) or _first(text, _GPU_MEMORY_GB_RE))
    _set(specs, "tdp_w", _first(text, _TDP_RE) or _first(title, _TDP_RE))
    _set(specs, "length_mm", _first(text, _DIMENSIONS_MM_RE))
    _set(specs, "power_connectors", _first(text, _POWER_CONNECTORS_RE))
    _set(specs, "series", gpu_series.classify(title))  # filtro de catálogo, não engine
    return specs


def extract_memory(title: str) -> dict[str, str]:
    specs: dict[str, str] = {}
    value = _first(title, _MEMORY_TYPE_RE)
    _set(specs, "type", value.lower() if value else None)
    return specs


def extract_psu(title: str) -> dict[str, str]:
    specs: dict[str, str] = {}
    _set(specs, "wattage", _first(title, _PSU_WATTAGE_RE))
    return specs


def extract_motherboard(title: str) -> dict[str, str]:
    specs: dict[str, str] = {}

    _set(specs, "socket", normalize_socket(_first(title, _SOCKET_RE)))
    _set(specs, "chipset", normalize_chipset(_first(title, _CHIPSET_RE)))
    _set(specs, "form_factor", normalize_form_factor(_first(title, _FORM_FACTOR_RE)))
    value = _first(title, _MEMORY_TYPE_RE)
    _set(specs, "memory_type", value.lower() if value else None)
    return specs


# import no fim para evitar ciclo (gpu_series não importa spec_extractor)
from . import gpu_series  # noqa: E402

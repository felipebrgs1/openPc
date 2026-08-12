"""Normalização — espelho de PriceParser.cs e SpecExtractor.cs (CPU).

Valida se as regras de preço BR e de specs por título (socket, TDP, cores,
threads, iGPU) se traduzem bem para Python.
"""

from __future__ import annotations

import re

# ---- Preço BR ("1.599,99" → 1599.99) — espelho de PriceParser.cs ----

def parse_price_br(value: str | None) -> float | None:
    if not value or not value.strip():
        return None
    normalized = value.replace(".", "").replace(",", ".")
    try:
        return float(normalized)
    except ValueError:
        return None


# ---- Specs de CPU por título — espelho de SpecExtractor.ExtractCpu ----

_SOCKET_RE = re.compile(r"\b(AM5|AM4|LGA\s?1?[0-9]{3}|sTR5|sTRX50|sTRX40)\b", re.IGNORECASE)
_TDP_RE = re.compile(r"(?:TDP|consumo|pot[eê]ncia)[^0-9]{0,20}(\d{2,4})\s*W", re.IGNORECASE)
_CORES_RE = re.compile(r"(\d{1,2})\s*[Nn][úu]cleos?")
_THREADS_RE = re.compile(r"(\d{1,3})\s*[Tt]hreads?")

_SEM_VIDEO_RE = re.compile(r"sem v[íi]deo|without graphics|no i?gpu|sem placa", re.IGNORECASE)
_COM_VIDEO_RE = re.compile(
    r"com v[íi]deo|gr[áa]ficos integrados|integrated graphics|radeon graphics|uhd graphics|hd graphics|iris",
    re.IGNORECASE,
)


def normalize_socket(value: str | None) -> str | None:
    """Socket canônico: minúsculas e sem espaço ("LGA 1700" → "lga1700")."""
    if not value:
        return None
    s = re.sub(r"\s+", "", value.lower())
    return s or None


def extract_cpu_specs(title: str) -> dict[str, str]:
    """Extrai socket/tdp/cores/threads/iGPU do título (mesmas regras do C#)."""
    specs: dict[str, str] = {}

    def first(regex: re.Pattern[str]) -> str | None:
        m = regex.search(title)
        return m.group(1).strip() if m else None

    socket = normalize_socket(first(_SOCKET_RE))
    if socket:
        specs["socket"] = socket
    tdp = first(_TDP_RE)
    if tdp:
        specs["tdp_w"] = tdp
    cores = first(_CORES_RE)
    if cores:
        specs["cores"] = cores
    threads = first(_THREADS_RE)
    if threads:
        specs["threads"] = threads

    igpu: bool | None = None
    if _SEM_VIDEO_RE.search(title):
        igpu = False
    elif _COM_VIDEO_RE.search(title):
        igpu = True
    if igpu is not None:
        specs["igpu"] = "true" if igpu else "false"

    return specs

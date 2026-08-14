"""Mapeamento da ficha técnica das lojas → chaves EAV canônicas.

As páginas de produto usam rótulos livres ("Relógio do motor", "Padrão de
ônibus", "Núcleos CUDA"...). Cada categoria tem um dicionário de rótulos
normalizados (sem acento, minúsculas) → (chave canônica, tipo de parsing).
O contrato de chaves é o da engine (docs/specs.md §3.2) + chaves de detalhe.

parse() devolve lista de (key, value) — um rótulo pode gerar mais de uma
spec (ex: "Memória: GDDR6 16 GB" → memory_type + memory_gb; "Dimensões" →
length_mm + width_mm + height_mm; "Relógio do motor" → base/boost/game).
"""

from __future__ import annotations

import re
from dataclasses import dataclass

from .text import normalize
from .spec_extractor import normalize_chipset, normalize_form_factor, normalize_socket

_INT_RE = re.compile(r"\d+(?:[.,]\d+)?")
_MHZ_RE = re.compile(r"(\d+(?:[.,]\d+)?)\s*mhz")
_GHZ_RE = re.compile(r"(\d+(?:[.,]\d+)?)\s*ghz")
_GB_RE = re.compile(r"(\d+(?:[.,]\d+)?)\s*gb", re.IGNORECASE)
_DDR_RE = re.compile(r"\b(g?ddr\d[\w]*)\b", re.IGNORECASE)
_PCIE_RE = re.compile(
    r"pci\s*-?\s*e(?:xpress)?\s*(?:gen\s*)?(\d+(?:\.\d+)?)?(?:\s*x\s*(\d+))?",
    re.IGNORECASE,
)
_PIN_RE = re.compile(r"(\d+)\s*x\s*(\d+)\s*pi?n")

# (chave, tipo). chave None = rótulo ignorado ("Marca", "Modelo"...).
_RULES: dict[str, list[tuple[str, str | None, str]]] = {
    "gpu": [
        ("motor grafico", "gpu_model", "text"),
        ("gpu", None, "gpu_name_or_chip"),
        ("arquitetura da gpu", "gpu_architecture", "text"),
        ("arquitetura", "gpu_architecture", "text"),
        ("padrao de onibus", "interface", "pcie"),
        ("barramento", "interface", "pcie"),
        ("bus", "interface", "pcie"),
        ("interface pci", "interface", "pcie"),
        ("interface", None, "iface_or_outputs"),
        ("directx", "directx", "text"),
        ("opengl", "opengl", "text"),
        ("vulkan", "vulkan", "text"),
        ("relogio do motor", None, "clock_block"),
        ("clock", None, "clock_block"),
        ("relogio base", "base_clock_mhz", "mhz"),
        ("relogio boost", "boost_clock_mhz", "mhz"),
        ("base clock", "base_clock_mhz", "mhz"),
        ("boost clock", "boost_clock_mhz", "mhz"),
        ("frequencia base", "base_clock_mhz", "mhz"),
        ("frequencia de impulso", "boost_clock_mhz", "mhz"),
        ("frequencia de boost", "boost_clock_mhz", "mhz"),
        ("game clock", "game_clock_mhz", "mhz"),
        ("relogio de jogo", "game_clock_mhz", "mhz"),
        ("clock de memoria", "memory_clock_gbps", "gbps"),
        ("relogio de memoria", "memory_clock_gbps", "gbps"),
        ("relogio da memoria", "memory_clock_gbps", "gbps"),
        ("velocidade de memoria", "memory_clock_gbps", "gbps"),
        ("velocidade da memoria", "memory_clock_gbps", "gbps"),
        ("memory clock", "memory_clock_gbps", "gbps"),
        ("memory speed", "memory_clock_gbps", "gbps"),
        ("memoria", None, "gpu_memory"),
        ("tamanho de memoria", None, "gpu_memory"),
        ("capacidade de memoria", "memory_gb", "int"),
        ("tipo de memoria", "memory_type", "mem_type"),
        ("interface de memoria", "memory_bus_bits", "bits"),
        ("interface da memoria", "memory_bus_bits", "bits"),
        ("memory interface", "memory_bus_bits", "bits"),
        ("memory bus", "memory_bus_bits", "bits"),
        ("largura de banda", "bandwidth_gbps", "gbps"),
        ("largura da banda", "bandwidth_gbps", "gbps"),
        ("bandwidth", "bandwidth_gbps", "gbps"),
        ("nucleos cuda", "cuda_cores", "int"),
        ("cuda cores", "cuda_cores", "int"),
        ("processadores de fluxo", "stream_processors", "int"),
        ("stream processors", "stream_processors", "int"),
        ("unidades de computacao", "compute_units", "int"),
        ("compute units", "compute_units", "int"),
        ("nucleos tensor", "tensor_cores", "int"),
        ("tensor cores", "tensor_cores", "int"),
        ("nucleos rt", "rt_cores", "int"),
        ("rt cores", "rt_cores", "int"),
        ("nucleos xe", "xe_cores", "int"),
        ("xe cores", "xe_cores", "int"),
        ("resolucao", "max_resolution", "text"),
        ("resolucao maxima", "max_resolution", "text"),
        ("resolucao maxima digital", "max_resolution", "text"),
        ("resolucao digital maxima", "max_resolution", "text"),
        ("resolution", "max_resolution", "text"),
        ("suporte a apis", None, "apis_block"),
        ("saidas", "video_outputs", "list"),
        ("saidas de video", "video_outputs", "list"),
        ("entradas", "video_outputs", "list"),
        ("conexoes de video", "video_outputs", "list"),
        ("conexao de video", "video_outputs", "list"),
        ("displayport", "video_outputs", "list"),
        ("hdcp", "hdcp", "bool"),
        ("suporte hdcp", "hdcp", "bool"),
        ("multivisualizacao", "multi_monitor", "int"),
        ("multi monitor", "multi_monitor", "int"),
        ("suporte a multiplos monitores", "multi_monitor", "int"),
        ("fonte de alimentacao recomendada", "recommended_psu_w", "int"),
        ("fonte recomendada", "recommended_psu_w", "int"),
        ("recommended psu", "recommended_psu_w", "int"),
        ("conector de energia", "power_connectors", "power"),
        ("conectores de energia", "power_connectors", "power"),
        ("conector de alimentacao", "power_connectors", "power"),
        ("conector", "power_connectors", "power"),
        ("power connector", "power_connectors", "power"),
        ("consumo", "tdp_w", "int"),
        ("consumo de energia", "tdp_w", "int"),
        ("tdp", "tdp_w", "int"),
        ("board power", "tdp_w", "int"),
        ("graphics card power", "tdp_w", "int"),
        ("dimensoes", None, "dims"),
        ("dimensions", None, "dims"),
        ("comprimento", "length_mm", "int"),
        ("largura", "slots", "int"),
        ("espessura", "slots", "int"),
        ("slots", "slots", "int"),
        ("litografia", "process_nm", "int"),
        ("processo de fabricacao", "process_nm", "int"),
        ("transistores", "transistors", "text"),
        ("transistors", "transistors", "text"),
        ("peso", "weight", "text"),
        ("weight", "weight", "text"),
        ("marca", None, "skip"),
        ("modelo", None, "skip"),
        ("garantia", None, "skip"),
        ("warranty", None, "skip"),
        ("conteudo da embalagem", None, "skip"),
        ("caracteristicas", None, "skip"),
        ("especificacoes", None, "skip"),
    ],
    "cpu": [
        ("nucleos", "cores", "int"),
        ("cores", "cores", "int"),
        ("threads", "threads", "int"),
        ("clock base", "base_clock_mhz", "mhz"),
        ("frequencia base", "base_clock_mhz", "mhz"),
        ("base clock", "base_clock_mhz", "mhz"),
        ("clock boost", "boost_clock_mhz", "mhz"),
        ("clock turbo", "boost_clock_mhz", "mhz"),
        ("frequencia turbo", "boost_clock_mhz", "mhz"),
        ("frequencia boost", "boost_clock_mhz", "mhz"),
        ("boost clock", "boost_clock_mhz", "mhz"),
        ("cache l2", "cache_l2_mb", "int"),
        ("cache l3", "cache_l3_mb", "int"),
        ("cache", "cache_l3_mb", "int"),
        ("tdp", "tdp_w", "int"),
        ("consumo", "tdp_w", "int"),
        ("soquete", "socket", "socket"),
        ("socket", "socket", "socket"),
        ("litografia", "process_nm", "int"),
        ("processo de fabricacao", "process_nm", "int"),
        ("memoria suportada", "memory_type", "mem_type"),
        ("tipo de memoria", "memory_type", "mem_type"),
        ("memory type", "memory_type", "mem_type"),
        ("velocidade de memoria", "max_memory_speed", "int"),
        ("max memory speed", "max_memory_speed", "int"),
        ("pci express", "pcie_lanes", "int"),
        ("pcie", "pcie_lanes", "int"),
        ("video integrado", "has_igpu", "bool"),
        ("graficos integrados", "has_igpu", "bool"),
        ("integrated graphics", "has_igpu", "bool"),
        ("cooler incluso", "cooler_included", "bool"),
        ("cooler", "cooler_included", "bool"),
        ("marca", None, "skip"),
        ("modelo", None, "skip"),
        ("garantia", None, "skip"),
        ("caracteristicas", None, "skip"),
        ("especificacoes", None, "skip"),
    ],
    "memory": [
        ("tipo", "type", "mem_type"),
        ("type", "type", "mem_type"),
        ("capacidade", "capacity_gb", "int"),
        ("capacity", "capacity_gb", "int"),
        ("velocidade", "speed_mhz", "int"),
        ("frequencia", "speed_mhz", "int"),
        ("speed", "speed_mhz", "int"),
        ("latencia", "cas_latency", "int"),
        ("cas", "cas_latency", "int"),
        ("tensao", "voltage_v", "num"),
        ("voltagem", "voltage_v", "num"),
        ("voltage", "voltage_v", "num"),
        ("modulos", "modules", "int"),
        ("altura", "height_mm", "int"),
        ("height", "height_mm", "int"),
        ("dissipador", "heatsink", "bool"),
        ("heatsink", "heatsink", "bool"),
        ("rgb", "rgb", "bool"),
        ("marca", None, "skip"),
        ("modelo", None, "skip"),
        ("garantia", None, "skip"),
        ("caracteristicas", None, "skip"),
        ("especificacoes", None, "skip"),
    ],
    "storage": [
        ("capacidade", "capacity_gb", "int"),
        ("capacity", "capacity_gb", "int"),
        ("interface", "interface", "storage_iface"),
        ("formato", "form_factor", "text"),
        ("form factor", "form_factor", "text"),
        ("leitura", "read_mbps", "int"),
        ("read", "read_mbps", "int"),
        ("gravacao", "write_mbps", "int"),
        ("escrita", "write_mbps", "int"),
        ("write", "write_mbps", "int"),
        ("tbw", "tbw", "int"),
        ("dram", "dram_cache", "bool"),
        ("cache", "dram_cache", "bool"),
        ("nand", "nand", "text"),
        ("marca", None, "skip"),
        ("modelo", None, "skip"),
        ("garantia", None, "skip"),
        ("caracteristicas", None, "skip"),
        ("especificacoes", None, "skip"),
    ],
    "psu": [
        ("potencia", "wattage", "int"),
        ("potencia nominal", "wattage", "int"),
        ("wattage", "wattage", "int"),
        ("eficiencia", "efficiency", "text"),
        ("efficiency", "efficiency", "text"),
        ("certificacao", "efficiency", "text"),
        ("80 plus", "efficiency", "text"),
        ("modular", "modular", "bool"),
        ("conectores", "connectors", "list"),
        ("connectors", "connectors", "list"),
        ("ventoinha", "fan_mm", "int"),
        ("fan", "fan_mm", "int"),
        ("dimensoes", None, "dims"),
        ("dimensions", None, "dims"),
        ("marca", None, "skip"),
        ("modelo", None, "skip"),
        ("garantia", None, "skip"),
        ("caracteristicas", None, "skip"),
        ("especificacoes", None, "skip"),
    ],
    "motherboard": [
        ("soquete", "socket", "socket"),
        ("socket", "socket", "socket"),
        ("chipset", "chipset", "chipset"),
        ("formato", "form_factor", "form_factor"),
        ("form factor", "form_factor", "form_factor"),
        ("fator de forma", "form_factor", "form_factor"),
        ("memoria", None, "mobo_memory"),
        ("memory", None, "mobo_memory"),
        ("memoria maxima", "max_memory_gb", "int"),
        ("max memory", "max_memory_gb", "int"),
        ("slots m.2", "m2_slots", "int"),
        ("m.2", "m2_slots", "int"),
        ("sata", "sata_ports", "int"),
        ("pci express x16", "pcie_x16_gen", "int"),
        ("pcie x16", "pcie_x16_gen", "int"),
        ("usb", "usb_ports", "text"),
        ("rede", "network", "text"),
        ("lan", "network", "text"),
        ("network", "network", "text"),
        ("wifi", "wifi", "text"),
        ("wireless", "wifi", "text"),
        ("audio", "audio", "text"),
        ("som", "audio", "text"),
        ("dimensoes", None, "dims"),
        ("dimensions", None, "dims"),
        ("marca", None, "skip"),
        ("modelo", None, "skip"),
        ("garantia", None, "skip"),
        ("caracteristicas", None, "skip"),
        ("especificacoes", None, "skip"),
    ],
    "cooler": [
        ("tipo", "type", "cooler_type"),
        ("type", "type", "cooler_type"),
        ("soquetes suportados", "socket_support", "list"),
        ("soquete suportado", "socket_support", "list"),
        ("socket support", "socket_support", "list"),
        ("compatibilidade", "socket_support", "list"),
        ("altura", "height_mm", "int"),
        ("height", "height_mm", "int"),
        ("radiador", "radiator_mm", "int"),
        ("radiator", "radiator_mm", "int"),
        ("ventoinhas", "fans", "int"),
        ("fans", "fans", "int"),
        ("rpm", "max_rpm", "int"),
        ("tdp", "tdp_rating_w", "int"),
        ("potencia", "tdp_rating_w", "int"),
        ("ruido", "noise_dba", "num"),
        ("noise", "noise_dba", "num"),
        ("rgb", "rgb", "bool"),
        ("marca", None, "skip"),
        ("modelo", None, "skip"),
        ("garantia", None, "skip"),
        ("caracteristicas", None, "skip"),
        ("especificacoes", None, "skip"),
    ],
    "case": [
        ("formatos suportados", "supported_form_factors", "list"),
        ("placas mae suportadas", "supported_form_factors", "list"),
        ("form factor", "supported_form_factors", "list"),
        ("placa de video", "max_gpu_length_mm", "int"),
        ("gpu", "max_gpu_length_mm", "int"),
        ("comprimento maximo gpu", "max_gpu_length_mm", "int"),
        ("altura cooler", "max_cooler_height_mm", "int"),
        ("cooler", "max_cooler_height_mm", "int"),
        ("radiador", "radiator_support_mm", "list"),
        ("water cooler", "radiator_support_mm", "list"),
        ("fonte", "psu_form_factor", "text"),
        ("psu", "psu_form_factor", "text"),
        ("ventoinhas incluidas", "included_fans", "int"),
        ("fans incluidas", "included_fans", "int"),
        ("dimensoes", None, "dims"),
        ("dimensions", None, "dims"),
        ("peso", "weight", "text"),
        ("marca", None, "skip"),
        ("modelo", None, "skip"),
        ("garantia", None, "skip"),
        ("caracteristicas", None, "skip"),
        ("especificacoes", None, "skip"),
    ],
}

# rótulos que, normalizados, colidem com outros mais específicos: a regra
# mais longa vence (ex: "interface de memoria" antes de "interface").
_RULE_LOOKUP: dict[str, dict[str, tuple[str | None, str]]] = {
    cat: {label: (key, kind) for label, key, kind in rules}
    for cat, rules in _RULES.items()
}
_LABELS_BY_CAT: dict[str, list[str]] = {
    cat: sorted((label for label, _, _ in rules), key=len, reverse=True)
    for cat, rules in _RULES.items()
}


def _num(value: str) -> float | None:
    m = _INT_RE.search(value)
    if not m:
        return None
    return float(m.group(0).replace(",", "."))


def _int(value: str) -> str | None:
    n = _num(value)
    if n is None:
        return None
    return str(int(n))


def _parse_power(value: str) -> str | None:
    text = normalize(value)
    if "12v2x6" in text or "12vhpwr" in text:
        return "1x16pin"
    if re.search(r"\b16\s*pin", text):
        return "1x16pin"
    m = _PIN_RE.search(text)
    if m:
        qty = int(m.group(1))
        pins = m.group(2)
        return f"{qty}x{pins}pin" if qty > 1 else f"1x{pins}pin"
    if re.search(r"\b8\s*pin", text):
        return "1x8pin"
    if re.search(r"\b6\s*pin", text):
        return "1x6pin"
    if "sem" in text or "slot" in text or "nenhum" in text:
        return "nenhum (slot)"
    return None


def _parse_pcie(value: str) -> str | None:
    value = value.replace("®", "").replace("™", "")
    m = _PCIE_RE.search(value)
    if not m:
        return None
    gen = m.group(1)
    lanes = m.group(2)
    if gen is None:
        return None
    return f"pcie{gen}x{lanes}" if lanes else f"pcie{gen}"


def _parse_dims(value: str) -> list[tuple[str, str]]:
    nums = [int(float(x.replace(",", "."))) for x in _INT_RE.findall(value)]
    if len(nums) < 3:
        return [("length_mm", str(nums[0]))] if nums else []
    return [
        ("length_mm", str(nums[0])),
        ("width_mm", str(nums[1])),
        ("height_mm", str(nums[2])),
    ]


def _parse_gpu_memory(value: str) -> list[tuple[str, str]]:
    out: list[tuple[str, str]] = []
    m = _DDR_RE.search(value)
    if m:
        out.append(("memory_type", m.group(1).lower()))
    m = _GB_RE.search(value)
    if m:
        out.append(("memory_gb", _int(m.group(0)) or ""))
    return out


def _parse_mobo_memory(value: str) -> list[tuple[str, str]]:
    out: list[tuple[str, str]] = []
    m = _DDR_RE.search(value)
    if m:
        out.append(("memory_type", m.group(1).lower()))
    m = re.search(r"(\d+)\s*x", value.lower())
    if m:
        out.append(("memory_slots", m.group(1)))
    m = re.search(r"(\d+(?:[.,]\d+)?)\s*gb", value.lower())
    if m:
        out.append(("max_memory_gb", _int(m.group(0)) or ""))
    return out


def _parse_clock_block(value: str) -> list[tuple[str, str]]:
    """Bloco de clocks ("Boost Clock: 3290 MHz", "Clock: Extreme
    Performance: 2497 MHz..."). base_clock_mhz pega o menor valor base;
    boost_clock_mhz o maior valor boost-like (turbo/oc/extreme/gaming...)."""
    base_values: list[int] = []
    boost_values: list[int] = []
    game_value: int | None = None
    text = value.lower()
    for line in re.split(r"[\n,;]", text):
        m = re.search(
            r"(base|boost|game|turbo|oc|extreme|performance|silent|gaming|standard|default)"
            r"\s*[^0-9:]{0,10}:\s*(\d+(?:[.,]\d+)?)\s*(mhz|ghz)?",
            line,
        )
        if not m:
            continue
        kind = m.group(1)
        n = float(m.group(2).replace(",", "."))
        if m.group(3) == "ghz":
            n *= 1000
        mhz = int(n)
        if kind == "base" or kind == "standard" or kind == "default":
            base_values.append(mhz)
        elif kind == "game":
            game_value = mhz
        else:
            boost_values.append(mhz)
    out: list[tuple[str, str]] = []
    if base_values:
        out.append(("base_clock_mhz", str(min(base_values))))
    if boost_values:
        out.append(("boost_clock_mhz", str(max(boost_values))))
    if game_value is not None:
        out.append(("game_clock_mhz", str(game_value)))
    return out


def _parse_gpu_name_or_chip(value: str) -> list[tuple[str, str]]:
    """"GPU" é ambíguo: nome comercial ("GeForce RTX 5090") ou codinome do
    chip ("GB206"). Codinome → gpu_chip; nome de marketing → gpu_model."""
    text = value.strip()
    if re.match(r"^(GB|AD|GA|TU|Navi|ACM|BMG|DG)\b", text, re.IGNORECASE):
        return [("gpu_chip", text)]
    if re.search(r"\b(rtx|gtx|radeon|geforce|arc|rx\s*\d)", text, re.IGNORECASE):
        return [("gpu_model", _parse_text(text) or text)]
    return [("gpu_model", _parse_text(text) or text)]


def _parse_iface_or_outputs(value: str) -> list[tuple[str, str]]:
    """"Interface" é ambíguo: bus PCIe (Pichau) ou saídas de vídeo (Kabum)."""
    text = value.lower()
    if "hdmi" in text or "display" in text or "dp" in text or "vga" in text or "dvi" in text:
        v = _parse_list(value)
        return [("video_outputs", v)] if v else []
    v = _parse_pcie(value)
    return [("interface", v)] if v else []


def _parse_list(value: str) -> str | None:
    parts = re.split(r"[\n/|,;]", value)
    parts = [re.sub(r"\s+", " ", p).strip(" -") for p in parts]
    parts = [p for p in parts if p]
    return ", ".join(parts) if parts else None


def _parse_storage_iface(value: str) -> str | None:
    text = value.lower()
    if "nvme" in text:
        return "nvme"
    if "sata" in text:
        return "sata"
    return _parse_text(value)


def _parse_cooler_type(value: str) -> str | None:
    text = value.lower()
    if "aio" in text or "water" in text or "liquido" in text:
        return "aio"
    if "air" in text or "tower" in text:
        return "air"
    return _parse_text(value)


def _parse_mem_type(value: str) -> str | None:
    m = _DDR_RE.search(value)
    return m.group(1).lower() if m else None


def _parse_bool(value: str) -> str | None:
    text = value.lower()
    if any(w in text for w in ("sim", "yes", "true", "com ", "tem ", "suporta")):
        return "true"
    if any(w in text for w in ("nao", "não", "no", "false", "sem ")):
        return "false"
    return None


def _parse_text(value: str) -> str | None:
    text = re.sub(r"\s+", " ", value).strip()
    return text or None


def parse_value(kind: str, raw: str) -> list[str]:
    """Valor bruto → lista de valores normalizados (string). Vazio = sem parse."""
    value = (raw or "").strip()
    if not value:
        return []

    if kind == "int":
        n = _int(value)
        return [] if n is None else [n]
    if kind == "num":
        n = _num(value)
        return [] if n is None else [str(n)]
    if kind == "mhz":
        m = _MHZ_RE.search(value.lower())
        if m:
            return [str(int(float(m.group(1).replace(",", "."))))]
        m = _GHZ_RE.search(value.lower())
        if m:
            return [str(int(float(m.group(1).replace(",", ".")) * 1000))]
        return []
    if kind == "gbps":
        n = _num(value)
        return [] if n is None else [str(n)]
    if kind == "bits":
        n = _int(value)
        return [] if n is None else [n]
    if kind == "pcie":
        v = _parse_pcie(value)
        return [] if v is None else [v]
    if kind == "power":
        v = _parse_power(value)
        return [] if v is None else [v]
    if kind == "bool":
        v = _parse_bool(value)
        return [] if v is None else [v]
    if kind == "list":
        v = _parse_list(value)
        return [] if v is None else [v]
    if kind == "mem_type":
        v = _parse_mem_type(value)
        return [] if v is None else [v]
    if kind == "socket":
        v = normalize_socket(value)
        return [] if v is None else [v]
    if kind == "form_factor":
        v = normalize_form_factor(value)
        return [] if v is None else [v]
    if kind == "chipset":
        v = normalize_chipset(value)
        return [] if v is None else [v]
    if kind == "storage_iface":
        v = _parse_storage_iface(value)
        return [] if v is None else [v]
    if kind == "cooler_type":
        v = _parse_cooler_type(value)
        return [] if v is None else [v]
    if kind == "skip":
        return []
    v = _parse_text(value)
    return [] if v is None else [v]


def _parse_apis_block(value: str) -> list[tuple[str, str]]:
    """"Suporte a APIs: DirectX 12 Ultimate, OpenGL 4.6" → directx + opengl."""
    out: list[tuple[str, str]] = []
    m = re.search(r"directx\s*([\d.]+\s*\w*)", value, re.IGNORECASE)
    if m:
        out.append(("directx", re.sub(r"\s+", " ", m.group(0)).strip()))
    m = re.search(r"opengl\s*([\d.]+)", value, re.IGNORECASE)
    if m:
        out.append(("opengl", re.sub(r"\s+", " ", m.group(0)).strip()))
    m = re.search(r"vulkan\s*([\d.]+)", value, re.IGNORECASE)
    if m:
        out.append(("vulkan", re.sub(r"\s+", " ", m.group(0)).strip()))
    return out


# kinds que geram mais de uma (key, value) — tratados em map_specs
_MULTI_KINDS = {
    "gpu_memory": _parse_gpu_memory,
    "mobo_memory": _parse_mobo_memory,
    "clock_block": _parse_clock_block,
    "dims": _parse_dims,
    "iface_or_outputs": _parse_iface_or_outputs,
    "apis_block": _parse_apis_block,
    "gpu_name_or_chip": _parse_gpu_name_or_chip,
}

# Faixas de sanidade por chave numérica: valores absurdos (prosa de marketing
# casada por acaso) são descartados em vez de poluir a spec.
_SANITY_RANGES: dict[str, tuple[float, float]] = {
    "cuda_cores": (100, 100_000),
    "stream_processors": (100, 100_000),
    "compute_units": (4, 512),
    "tensor_cores": (32, 10_000),
    "rt_cores": (8, 10_000),
    "xe_cores": (4, 512),
    "base_clock_mhz": (500, 6_000),
    "boost_clock_mhz": (800, 5_000),
    "game_clock_mhz": (500, 5_000),
    "memory_gb": (1, 64),
    "memory_bus_bits": (32, 1_024),
    "memory_clock_gbps": (5, 40),
    "bandwidth_gbps": (50, 2_500),
    "tdp_w": (30, 1_000),
    "recommended_psu_w": (200, 1_600),
    "process_nm": (3, 14),
    "length_mm": (50, 500),
    "width_mm": (50, 500),
    "height_mm": (10, 200),
    "cores": (1, 64),
    "threads": (1, 128),
}


def match_rule(category: str, label: str) -> tuple[str | None, str] | None:
    """Regra (key, kind) para o rótulo, ou None se a categoria não o conhece.

    Correspondência exata primeiro; senão substring (a regra mais longa vence,
    ex: "interface de memoria" ganha de "interface")."""
    norm = normalize(label).rstrip(":")
    lookup = _RULE_LOOKUP.get(category)
    if not lookup:
        return None
    if norm in lookup:
        return lookup[norm]
    for candidate in _LABELS_BY_CAT[category]:
        if candidate in norm and candidate != norm:
            # evita falso positivo curto: "memoria" casaria com "relogio de memoria"
            # — regras longas vêm primeiro; exija que o resto do rótulo seja curto
            rest = norm.replace(candidate, "", 1).strip()
            if len(rest) <= 12:
                return lookup[candidate]
    return None


def _sane(key: str, value: str) -> bool:
    """Valor numérico dentro da faixa esperada da chave (sanidade)."""
    rng = _SANITY_RANGES.get(key)
    if rng is None:
        return True
    m = re.fullmatch(r"\d+(?:[.,]\d+)?", value)
    if not m:
        return True
    n = float(m.group(0).replace(",", "."))
    return rng[0] <= n <= rng[1]


def map_specs(category: str, pairs: list[tuple[str, str]]) -> dict[str, str]:
    """(rótulo, valor) → specs canônicas {key: value} (sem duplicatas).

    Rótulos repetidos de mesmo tipo de lista (ex: duas linhas "Interface"
    com saídas de vídeo distintas) são concatenados."""
    specs: dict[str, str] = {}
    for label, raw in pairs:
        rule = match_rule(category, label)
        if rule is None:
            continue
        key, kind = rule
        if kind in _MULTI_KINDS:
            for parsed_key, parsed_value in _MULTI_KINDS[kind](raw):
                if not _sane(parsed_key, parsed_value):
                    continue
                if parsed_key in specs and kind == "iface_or_outputs":
                    specs[parsed_key] = f"{specs[parsed_key]}, {parsed_value[:256]}"
                else:
                    specs[parsed_key] = parsed_value[:256]
            continue
        for parsed in parse_value(kind, raw):
            if key and _sane(key, parsed):
                if kind == "list" and key in specs:
                    specs[key] = f"{specs[key]}, {parsed[:256]}"
                else:
                    specs[key] = parsed[:256]
    return specs

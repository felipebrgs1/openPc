"""Filtro de ruído de catálogo — espelho de CategoryNoiseFilter.cs.

Produtos que caem na rota/categoria da loja (marketplace/cross-listing)
mas não pertencem a ela. Dois mecanismos:
1. Palavras-chave por categoria.
2. Marcadores de OUTRA categoria (cross-listing), com borda de palavra.

Aplicado na ingestão (descarta) e pelo comando `cleanup-noise`.
"""

from __future__ import annotations

import re

from . import cpu_gen
from .text import normalize

_KEYWORD_RULES: dict[str, tuple[str, ...]] = {
    "cpu": (
        "contact frame",
        "adaptador socket",
        "adaptador de socket",
        "suporte para processador",
        "suporte de processador",
        "moldura para processador",
        "protetor de processador",
        "capa de processador",
        "tampa de processador",
        "bracket",
    ),
    "gpu": (
        "suporte para placa de video",
        "suporte de placa de video",
        "suporte placa de video",
        "suport para placa de video",
        "suport placa de video",
        "suporte gpu",
        "suporte para gpu",
        "suport gpu",
        "suporte vertical",
        "gpu support",
        "gpu bracket",
        "bracket",
        "riser",
        "placa de video vertical",
        "adaptador placa de video",
        "adaptador de video",
        "cabo para gpu",
        "para gpu",
        "cabo argb",
        "cabo rgb",
        "soundbar",
        "caixa de som",
        "espelho",
        "captura de video",
        "capturadora",
        "ddr3",
        "ddr4",
        "ddr5",
        "gdd3",
        "gdrr3",
    ),
    "psu": (
        "notebook",
        "carregador",
        "nobreak",
        "bancada",
        "inversor",
        "transformador",
        "bateria",
        "power bank",
        "adaptador de energia",
        "hot swap",
    ),
    "memory": (
        "para notebook",
        "notebook",
        "sodimm",
        "laptop",
        "ddr2",
        "ddr3",
        "sdram",
        "dissipador de calor memoria ram",
        "aspire",
        "aspiron",
        "thinkpad",
        "inspiron",
        "latitude",
        "ideapad",
        "legion",
        "nitro",
        "envy",
        "pavilion",
        "probook",
        "elitebook",
        "zenbook",
        "vivobook",
        "macbook",
        "galaxy book",
        "vaio",
        "predator",
        "helios",
        "yoga",
        "tuf",
        "omen",
        "optiplex",
        "vostro",
        "precision",
        "xps",
        "thinkcentre",
        "thinkserver",
        "thinksystem",
        "thinkagile",
        "ideacentre",
        "workstation",
        "servidor",
        "server",
        "note",
        "smartwatch",
        "water cooler",
        "watercooler",
        "mid tower",
        "midtower",
        "full tower",
        "fulltower",
    ),
    "motherboard": (
        "notebook",
        "laptop",
        "sucata",
    ),
    "storage": (
        "monitor ",
        "pendrive",
        "pen drive",
    ),
    "cooler": (
        "pasta termica",
        "pasta de cobre",
        "pasta de solda",
        "massa",
        "adesivo",
        "graxa",
        "limpeza",
        "cabo ",
        "hub controladora",
    ),
}

# Marcador de categoria X = título que pertence a X. Produto na categoria Y
# com marcador de X (X ≠ Y) é cross-listing (ruído).
_CROSS_MARKERS: tuple[tuple[str, re.Pattern[str]], ...] = [
    ("cpu", re.compile(r"(?<![a-z0-9])(ryzen|athlon|pentium|celeron|threadripper|xeon)(?![a-z0-9])")),
    ("cpu", re.compile(r"(?<![a-z0-9])core [iu]")),
    ("gpu", re.compile(r"(?<![a-z0-9])(geforce|radeon|rtx|gtx|titan|quadro)(?![a-z0-9])")),
    ("gpu", re.compile(r"(?<![a-z0-9])rx(?![a-z0-9])")),
    ("gpu", re.compile(r"^placa de video")),
    ("memory", re.compile(r"^ddr[45]\b")),
    ("memory", re.compile(r"(?<![a-z0-9])memoria(?![a-z0-9])")),
    ("memory", re.compile(r"(?<![a-z0-9])sodimm(?![a-z0-9])")),
    ("memory", re.compile(r"(?<![a-z0-9])para notebook(?![a-z0-9])")),
    ("motherboard", re.compile(r"^placa mae")),
    ("motherboard", re.compile(r"(?<![a-z0-9])chipset(?![a-z0-9])")),
    ("psu", re.compile(r"^fonte")),
    ("psu", re.compile(r"80 plus")),
    ("storage", re.compile(r"(?<![a-z0-9])(ssd|nvme|hdd)(?![a-z0-9])")),
    ("storage", re.compile(r"disco rigido")),
    ("case", re.compile(r"(?<![a-z0-9])gabinete(?![a-z0-9])")),
]

# Regras regex na PRÓPRIA categoria (keywords usam Contains; estes precisam
# de âncora). Texto já normalizado.
_CATEGORY_REGEX_RULES: dict[str, tuple[re.Pattern[str], ...]] = {
    "memory": (
        re.compile(r"\bddr\b"),                       # DDR de 1ª geração
        re.compile(r"\bpc[23]\s*\d{4,}"),             # módulos DDR2/DDR3
        re.compile(r"\bpara (?!desktop|gamers?|pcs?\b|notebook|laptop|computadores?|upgrade|intel|amd)"),
        re.compile(r"(?<![a-z0-9])(?<!non )(?<!sem )(?<!nao )ecc(?![a-z0-9])"),
        re.compile(r"(?<![a-z0-9])(dell|poweredge|proliant|hpe|apple|synology|alienware)(?![a-z0-9])"),
    ),
}

# Whitelist de placas-mãe: só socket/chipset que suporta a matriz de CPUs
# (Intel ≥ 12ª via LGA 1700/1851; AMD AM4/AM5).
_MODERN_MOTHERBOARD_RE = re.compile(
    r"\b(1700|1851|am4|am5|strx50|str5|h610|b660|b760|z690|z790|h770|w680|b860|z890|h810"
    r"|a320|b350|x370|b450|x470|b550|x570|a520|a620|b650|x670|b840|b850|x870)[a-z0-9]*\b"
)

_MIN_PSU_WATTAGE = 500
_PSU_WATTAGE_RE = re.compile(r"\b(\d{3,4})\s*w")
_PSU_MODERN_RE = re.compile(r"80\s*plus|(?<![a-z0-9])(gold|platinum|titanium)(?![a-z0-9])")

_STORAGE_DRIVE_RE = re.compile(r"(?<![a-z0-9])(ssd|nvme|hdd|hd)(?![a-z0-9])|disco rigido|disco solido|unidade de estado solido")
_STORAGE_ACCESSORY_PREFIX_RE = re.compile(r"^(dissipador|base de|pc gamer)")
_STORAGE_ACCESSORY_RE = re.compile(
    r"\b(adaptador|case|caddy|gaveta|baias?|capas?|caixa extern\w*|cabos?|placa|conversor"
    r"|duplicador|clones?|encaixe|compartimento|cartucho|ventilador|enclosures?|dock\w*)\b"
)

# ==================== Cooler ====================
# O slot do builder é cooler de CPU (torre/AIO); a categoria das lojas
# mistura ventoinhas, microventiladores, controladoras e acessórios.

_COOLER_CPU_MARKER_RE = re.compile(
    r"para processador|para cpu|para computador|air cooler|cpu cooler|water cooler|watercooler"
    r"|(?<![a-z0-9])aio(?![a-z0-9])|torre|tower|tdp|heat ?pipe|radiator|radiad|soquete|socket|lga"
    r"|(?<![0-9])(1700|1851)(?![0-9])|(?<![a-z0-9])(am4|am5)(?![a-z0-9])"
)
_COOLER_FAN_RE = re.compile(r"\bfan\b")
_COOLER_FAN_WORD_RE = re.compile(r"\b(ventoinha\w*|ventilador\w*)\b")
_COOLER_FAN_WORD_EXCLUSION_RE = re.compile(r"air cooler|cpu cooler|torre|tower|heat ?pipe|radiator|radiad")
_COOLER_CONTROLLER_RE = re.compile(r"\b(controlador\w*|hub)\b")
_COOLER_AIO_RE = re.compile(r"water cooler|watercooler|aio")
_COOLER_FAN_SIZE_RE = re.compile(
    r"\bcooler\b.*(?<![0-9])(120\s*mm|12\s*x\s*12|120x120|12\s*cm|80\s*mm|90\s*mm|92\s*mm|140\s*mm)(?![0-9])"
)
_COOLER_FAN_DIMENSIONS_RE = re.compile(r"(?<![0-9])\d{2,3}\s*x\s*\d{2,3}\s*x\s*\d{2,3}\s*mm(?![0-9])")

_COOLER_MACHINE_KEYWORDS = (
    "ideapad", "aspire", "aspiron", "thinkpad", "inspiron", "latitude", "pavilion",
    "probook", "elitebook", "zenbook", "vivobook", "macbook", "vaio", "nitro", "acer",
    "predator", "helios", "omen", "legion", "tuf", "envy", "vostro", "optiplex",
    "thinkcentre", "thinkstation", "ideacentre", "workstation", "servidor", "server",
)

_COOLER_ACCESSORY_KEYWORDS = (
    "microventilador", "micro ventilador", "thermal pad", "almofada termica",
    "thermal grease", "metal liquido", "notebook", "laptop", "parafuso",
    "sincroniza", "splitter",
)

_COOLER_OLD_SOCKET_RE = re.compile(
    r"\b(lga|soquete|socket)\b.*(?<![0-9])(1150|1151|1155|1156|1200|1366|775|115x|20xx)(?![0-9])"
)
_COOLER_MODERN_SOCKET_RE = re.compile(r"(?<![0-9])(1700|1851)(?![0-9])|(?<![a-z0-9])(am4|am5)(?![a-z0-9])")


def is_noise(category_slug: str, title: str | None) -> bool:
    """True = produto não pertence à categoria (descartar/remover)."""
    if not title or not title.strip():
        return False

    text = normalize(title)

    for keyword in _KEYWORD_RULES.get(category_slug, ()):
        if keyword in text:
            return True

    for regex in _CATEGORY_REGEX_RULES.get(category_slug, ()):
        if regex.search(text):
            return True

    # Cooler: o slot do builder é COOLER DE CPU (torre/AIO).
    if category_slug == "cooler":
        if any(kw in text for kw in _COOLER_ACCESSORY_KEYWORDS):
            return True
        if _COOLER_FAN_RE.search(text) and not _COOLER_CPU_MARKER_RE.search(text):
            return True
        if _COOLER_FAN_WORD_RE.search(text) and not _COOLER_FAN_WORD_EXCLUSION_RE.search(text):
            return True
        if _COOLER_CONTROLLER_RE.search(text) and not _COOLER_AIO_RE.search(text):
            return True
        # "Cooler 120mm/80mm/12x12" sem marcador de CPU cooler = ventoinha avulsa.
        if (
            _COOLER_FAN_SIZE_RE.search(text)
            and not _COOLER_CPU_MARKER_RE.search(text)
            and "intel" not in text
            and "amd" not in text
        ):
            return True
        if _COOLER_FAN_DIMENSIONS_RE.search(text) and not _COOLER_CPU_MARKER_RE.search(text):
            return True
        # Cooler de máquina específica (reposição de notebook/OEM); AIOs ficam.
        if any(kw in text for kw in _COOLER_MACHINE_KEYWORDS) and not _COOLER_AIO_RE.search(text):
            return True
        # Socket antigo sem nenhum moderno = não monta CPU do catálogo.
        if _COOLER_OLD_SOCKET_RE.search(text) and not _COOLER_MODERN_SOCKET_RE.search(text):
            return True

    # CPU fora da matriz de compatibilidade (Intel < 12th, Xeon, AMD não-Ryzen,
    # mobile) — classifica o TÍTULO CRU (o normalize juntaria "i5-12400F").
    if category_slug == "cpu" and cpu_gen.classify(title) is None:
        return True

    # Placas-mãe: whitelist de socket/chipset moderno.
    if category_slug == "motherboard" and not _MODERN_MOTHERBOARD_RE.search(text):
        return True

    # Fontes: whitelist de potência — só ≥ 500W serve para montar.
    if category_slug == "psu":
        m = _PSU_WATTAGE_RE.search(text)
        if m:
            return int(m.group(1)) < _MIN_PSU_WATTAGE
        return not _PSU_MODERN_RE.search(text)

    # Armazenamento: só unidades de disco (SSD/HDD).
    if category_slug == "storage":
        if not _STORAGE_DRIVE_RE.search(text):
            return True
        if _STORAGE_ACCESSORY_PREFIX_RE.search(text):
            return True
        if _STORAGE_ACCESSORY_RE.search(text):
            return True

    for marker_category, regex in _CROSS_MARKERS:
        if marker_category == category_slug:
            continue
        if regex.search(text):
            return True

    return False

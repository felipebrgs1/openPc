"""Mapeamento de rótulos das fichas técnicas → chaves canônicas EAV."""

from openpc_scraper.normalize.spec_map import map_specs, match_rule, parse_value


def test_regra_exata_com_acento_e_case() -> None:
    assert match_rule("gpu", "Núcleos CUDA:") == ("cuda_cores", "int")
    assert match_rule("gpu", "Padrão de ônibus") == ("interface", "pcie")


def test_regra_mais_longa_vence() -> None:
    # "interface de memória" ganha de "interface"; "relógio de memória" de "memória"
    assert match_rule("gpu", "Interface de memória") == ("memory_bus_bits", "bits")
    assert match_rule("gpu", "Relógio de memória") == ("memory_clock_gbps", "gbps")
    assert match_rule("gpu", "Tamanho de Memória") == (None, "gpu_memory")


def test_regra_desconhecida_ignorada() -> None:
    assert match_rule("gpu", "Cor da carcaça") is None


def test_parse_int_mhz_ghz() -> None:
    assert parse_value("int", "21760 Unidades") == ["21760"]
    assert parse_value("mhz", "Boost: 3290 MHz") == ["3290"]
    assert parse_value("mhz", "3.8 GHz") == ["3800"]
    assert parse_value("int", "sem valor numérico") == []


def test_parse_pcie_bus() -> None:
    assert parse_value("pcie", "PCI Express 5.0 x16") == ["pcie5.0x16"]
    assert parse_value("pcie", "PCI Express® Gen 5") == ["pcie5"]
    assert parse_value("pcie", "PCIe 4.0 x8") == ["pcie4.0x8"]


def test_parse_power_connectors() -> None:
    assert parse_value("power", "1 x 8 pinos") == ["1x8pin"]
    assert parse_value("power", "2 x 8 pinos") == ["2x8pin"]
    assert parse_value("power", "1 x 16 pinos") == ["1x16pin"]
    assert parse_value("power", "12V-2x6") == ["1x16pin"]


def test_parse_bool() -> None:
    assert parse_value("bool", "Sim") == ["true"]
    assert parse_value("bool", "Não") == ["false"]


def test_gpu_memory_multikey() -> None:
    assert map_specs("gpu", [("Memória", "GDDR6 16 GB")]) == {
        "memory_type": "gddr6",
        "memory_gb": "16",
    }


def test_clock_block() -> None:
    specs = map_specs("gpu", [
        ("Clock", "Extreme Performance: 2497 MHz (MSI Center)\nBoost: 2482 MHz (GAMING & SILENT Mode)"),
    ])
    assert specs["boost_clock_mhz"] == "2497"

    specs = map_specs("gpu", [
        ("Relógio do motor", "Boost Clock: 3290 MHz\nGame Clock: 2700 MHz"),
    ])
    assert specs["boost_clock_mhz"] == "3290"
    assert specs["game_clock_mhz"] == "2700"


def test_dimensoes() -> None:
    specs = map_specs("gpu", [("Dimensões", "359 x 149 x 70 mm")])
    assert specs == {"length_mm": "359", "width_mm": "149", "height_mm": "70"}


def test_interface_ambigua() -> None:
    # "Interface" com saídas de vídeo (Kabum) vs bus PCIe (Pichau)
    specs = map_specs("gpu", [("Interface", "1 x HDMI 2.1b"), ("Interface", "2 x DisplayPort 2.1a")])
    assert "1 x HDMI" in specs["video_outputs"]
    assert "2 x DisplayPort" in specs["video_outputs"]

    specs = map_specs("gpu", [("Interface", "PCI Express Gen 5")])
    assert specs["interface"] == "pcie5"


def test_valor_truncado_em_256() -> None:
    specs = map_specs("gpu", [("Entradas", "A" * 300)])
    assert len(specs["video_outputs"]) == 256


def test_socket_normalizado() -> None:
    specs = map_specs("cpu", [("Soquete", "LGA 1700")])
    assert specs["socket"] == "lga1700"

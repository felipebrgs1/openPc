"""Parser da ficha técnica da página de produto da Kabum (__NEXT_DATA__)."""

from pathlib import Path

from openpc_scraper.collect.details.kabum import (
    extract_specs_from_next_data,
    parse_spec_pairs,
)

FIXTURE = Path(__file__).parent / "fixtures_kabum_product.html"


def test_parse_spec_pairs_secoes_e_itens() -> None:
    pairs = parse_spec_pairs(
        "<p><strong>Características:</strong></p>\n"
        "<p>- Marca: ASRock</p>\n"
        "<p>Especificações:</p>\n"
        "<p>Motor gráfico</p>\n"
        "<p>- AMD Radeon RX 9060 XT</p>\n"
        "<p>Relógio do motor</p>\n"
        "<p>- Boost Clock: 3290 MHz</p>\n"
        "<p>- Game Clock: 2700 MHz</p>"
    )
    assert ("Marca", "ASRock") in pairs
    assert ("Motor gráfico", "AMD Radeon RX 9060 XT") in pairs
    assert ("Boost Clock", "3290 MHz") in pairs
    assert ("Game Clock", "2700 MHz") in pairs


def test_fixture_real_placa_de_video() -> None:
    html = FIXTURE.read_text(encoding="utf-8")
    specs = extract_specs_from_next_data(html, "gpu")
    assert specs["gpu_model"] == "AMD Radeon™ RX 9060 XT"
    assert specs["memory_type"] == "gddr6"
    assert specs["memory_gb"] == "16"
    assert specs["boost_clock_mhz"] == "3290"
    assert specs["game_clock_mhz"] == "2700"
    assert specs["stream_processors"] == "2048"
    assert specs["compute_units"] == "32"
    assert specs["memory_clock_gbps"] == "20.0"
    assert specs["memory_bus_bits"] == "128"
    assert specs["recommended_psu_w"] == "550"
    assert specs["power_connectors"] == "1x8pin"
    assert specs["length_mm"] == "249"
    assert specs["width_mm"] == "132"
    assert specs["height_mm"] == "41"
    assert specs["max_resolution"] == "7680x4320"
    assert specs["directx"] == "12 Ultimate"
    assert specs["opengl"] == "4,6"
    assert specs["hdcp"] == "true"
    assert specs["multi_monitor"] == "3"
    assert "HDMI" in specs["video_outputs"]
    assert "DisplayPort" in specs["video_outputs"]


def test_pagina_sem_ficha() -> None:
    assert extract_specs_from_next_data("<html></html>", "gpu") == {}
    assert extract_specs_from_next_data("", "gpu") == {}

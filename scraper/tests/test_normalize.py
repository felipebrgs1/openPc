"""Testes de normalização — port de NormalizationTests.cs, GpuSeriesTests.cs,
MotherboardSpecTests.cs, PsuSpecTests.cs e ParserTests.cs (C#).
"""

from __future__ import annotations

import json
import re
from pathlib import Path

import pytest

from openpc_scraper.normalize import gpu_series, match_key, part_number, price, spec_extractor
from openpc_scraper.collect.kabum import parse_next_data, KabumParseError

FIXTURE = Path(__file__).parent / "fixtures_kabum.html"


class TestPartNumber:
    @pytest.mark.parametrize(
        "title,expected",
        [
            ("Processador AMD Ryzen 7 5700X ... - 100-100000926WOF", "100100000926WOF"),
            ("Processador AMD Ryzen 5 7600X3D ... 100-100001721WOF", "100100001721WOF"),
            ("Processador Intel Core i5-12400F ... BX8071512400F", "BX8071512400F"),
            ("Processador Intel Core Ultra 5 250K ... bx80768250k", "BX80768250K"),
        ],
    )
    def test_extract_acha_part_number_amd_e_intel(self, title: str, expected: str) -> None:
        assert part_number.extract(title) == expected

    @pytest.mark.parametrize(
        "title",
        ["Processador AMD Ryzen 5 7600, 6-Core, AM5", "Cooler Master Hyper 212", ""],
    )
    def test_extract_retorna_none_sem_padrao(self, title: str) -> None:
        assert part_number.extract(title) is None

    def test_normalize_remove_hifens_e_espacos(self) -> None:
        assert part_number.normalize("bx80715-12400f") == "BX8071512400F"


class TestMatchKey:
    @pytest.mark.parametrize(
        "title,expected",
        [
            ("Processador AMD Ryzen 7 5700X, 3.4GHz, AM4", "amd 5700x"),
            ("Processador AMD Ryzen 5 7600, 6-Core, 12-Threads, AM5", "amd 7600"),
            ("Processador Intel Core i5-12400F, LGA 1700", "intel 12400f"),
            ("Processador Intel Core Ultra 7 265F, 20-Core", "intel 265f"),
            ("Placa de vídeo RTX 5070 12GB", "nvidia 5070"),
            ("Placa de vídeo Radeon RX 7800 XT 16GB", "amd 7800xt"),
        ],
    )
    def test_build_extrai_marca_e_modelo(self, title: str, expected: str) -> None:
        assert match_key.build(title) == expected

    @pytest.mark.parametrize(
        "title",
        ["Memória Kingston Fury 16GB DDR5", "Xeon E5-2637 V2", "Gabinete Gamer"],
    )
    def test_build_retorna_none_sem_padrao(self, title: str) -> None:
        assert match_key.build(title) is None

    def test_build_distingue_variantes(self) -> None:
        assert match_key.build("Ryzen 5 7600 AM5") != match_key.build("Ryzen 5 7600X AM5")
        assert match_key.build("Ryzen 5 7600X AM5") != match_key.build("Ryzen 5 7600X3D AM5")


class TestPriceParser:
    @pytest.mark.parametrize(
        "value,expected",
        [("1.599,99", 1599.99), ("879,99", 879.99), ("12.345,67", 12345.67)],
    )
    def test_parse_br_converte_formato_brasileiro(self, value: str, expected: float) -> None:
        assert price.parse_price_br(value) == expected

    @pytest.mark.parametrize("value", ["", "abc"])
    def test_parse_br_retorna_none_invalido(self, value: str) -> None:
        assert price.parse_price_br(value) is None


class TestGpuSeries:
    @pytest.mark.parametrize(
        "title,expected",
        [
            ("Placa De Video Gamer Nvidia Gtx 1060 6gb 192bits GDDR5 HDMI", None),
            ("Gamer Ninja NVIDIA GeForce RTX 2060 Ronin, 6GB, GDDR6", "rtx20"),
            ("Gamer Nvidia Geforce RTX 2070 Ronin, 8GB, Gddr6", "rtx20"),
            ("Placa De Video MSI NVIDIA GeForce RTX 3050 OC LP, 6GB, GDDR6", "rtx30"),
            ("SuperFrame NVIDIA GeForce RTX 3060 EPIC, 12GB, GDDR6", "rtx30"),
            ("Palit NVIDIA GeForce RTX 3080 Gamingpro, 10GB GDDR6X", "rtx30"),
            ("Nvidia Geforce Msi RTX5070ti 16gb Gddr7", "rtx50"),
            ("Palit Geforce RTX 5070 White Oc, 12gb, Gddr7", "rtx50"),
            ("Placa De Video Palit Geforce RTX 5080 Gamingpro 16gb", "rtx50"),
            ("Gpu Inno3d Geforce RTX 5090 X3 32gb 512-bit Gddr7", "rtx50"),
            ("Gamer Nvidia Geforce Gtx1660s, Gddr6 6gb", "gtx16"),
            ("PNY NVIDIA GeForce GTX 1650, 4GB GDDR6", "gtx16"),
            ("Asrock Rx 6400 Cli 4g 4gb Gddr6 64bits", "rx6000"),
            ("ASRock AMD Radeon, RX 6600 CLD 8G, 8GB, GDDR6", "rx6000"),
            ("Gpu Powercolor Amd Radeon Rx 6500xt 4gb Gddr6", "rx6000"),
            ("RX 6750 XT Mech 2x 12G V1 Radeon, 12GB, GDDR6", "rx6000"),
            ("Asrock Amd Radeon Rx 7600 Challenger Pro 8GB Oc Gddr6", "rx7000"),
            ("Xfx Speedster Swft 210 Radeon Rx 7700 Xt, 12gb, Gddr6", "rx7000"),
            ("Amd Radeon Rx 7900 Gre 16gb Gddr6 256bits - Xfx", "rx7000"),
            ("AMD Radeon RX 9070 XT, 16GB, GDDR6", "rx9000"),
            ("Intel Arc A770 16GB", "arc"),
            ("Intel Arc B580 12GB", "arc"),
            ("Placa de vídeo RTX 9070 12GB", None),  # typo de loja — fora do padrão
        ],
    )
    def test_classify(self, title: str, expected: str | None) -> None:
        assert gpu_series.classify(title) == expected


class TestSpecExtractor:
    def test_extract_cpu_do_titulo_kabum_real(self) -> None:
        title = ("Processador AMD Ryzen 7 5700X, 3.4GHz (4.6GHz Max Turbo), Cache 36MB, "
                 "8 Núcleos, 16 Threads, AM4, Sem Vídeo Integrado - 100-100000926WOF")
        specs = spec_extractor.extract_cpu(title, None)
        assert specs["socket"] == "am4"
        assert specs["cores"] == "8"
        assert specs["threads"] == "16"
        assert specs["has_igpu"] == "false"

    def test_extract_cpu_com_igpu_true(self) -> None:
        title = "Processador AMD Ryzen 5 8600G, 6 Núcleos, 12 Threads, AM5, Com Vídeo Integrado Radeon"
        specs = spec_extractor.extract_cpu(title, None)
        assert specs["has_igpu"] == "true"

    def test_extract_cpu_ficha_tecnica_com_tdp_e_ddr(self) -> None:
        spec_text = "- TDP: 65W\n- Memória suportada: DDR5\n- Arquitetura: Zen 4"
        specs = spec_extractor.extract_cpu("Processador AMD Ryzen 5 7600, 6 Núcleos, AM5", spec_text)
        assert specs["tdp_w"] == "65"
        assert specs["memory_type"] == "DDR5"

    def test_extract_gpu_do_titulo(self) -> None:
        title = "Placa de vídeo RTX 5070 12GB GDDR7"
        specs = spec_extractor.extract_gpu(title, None)
        assert specs["memory_gb"] == "12"

    def test_extract_motherboard(self) -> None:
        title = "Placa-Mãe Asus TUF Gaming B650M-Plus Wifi, DDR5, Socket AM5, M-ATX"
        specs = spec_extractor.extract_motherboard(title)
        assert specs["socket"] == "am5"
        assert specs["chipset"] == "b650"
        assert specs["form_factor"] == "matx"
        assert specs["memory_type"] == "ddr5"

    def test_extract_psu(self) -> None:
        specs = spec_extractor.extract_psu("Fonte Corsair CX650, 650W, 80 Plus Bronze")
        assert specs["wattage"] == "650"

    def test_extract_memory(self) -> None:
        specs = spec_extractor.extract_memory("Memória Kingston Fury 16GB DDR5")
        assert specs["type"] == "ddr5"


class TestKabumParser:
    def test_parse_listings_page_extrai_produtos_reais_da_fixture(self) -> None:
        items = parse_next_data(FIXTURE.read_text(encoding="utf-8"))
        assert len(items) > 0
        first = items[0]
        assert first.code > 0
        assert first.title
        assert first.price > 0

    def test_sem_next_data_lanca(self) -> None:
        with pytest.raises(KabumParseError):
            parse_next_data("<html><body>bloqueado</body></html>")


class TestCardListingBuilder:
    """Port do teste Pichau_CardReal_ExtraiNomePrecoPartNumber (C#)."""

    def test_pichau_card_real_extrai_nome_preco_part_number(self) -> None:
        from openpc_scraper.collect.card import build_card_listing

        card = (
            "20% | OFF | 60 | UNID | Frete Grátis: Sul e Sudeste | "
            "Processador AMD Ryzen 5 7600X3D, 6-Core, 12-Threads, 4.1GHz (4.7GHz Turbo), "
            "Cache 102MB, AM5, 100-100001721WOF | de R$ 2.352,93 por | R$ 1.599,99 | À vista | "
            "15% de desconto no PIX | R$ 1.882,34 | Em até 12x de R$ 156,86 | Sem juros no cartão"
        )
        href = (
            "https://www.pichau.com.br/processador-amd-ryzen-5-7600x3d-6-core-12-threads-"
            "4-1ghz-4-7ghz-turbo-cache-102mb-am5-100-100001721wof"
        )
        price_re = re.compile(r"por\s*\|?\s*R\$\s*([\d.]+,[\d]{2})", re.IGNORECASE)

        listing = build_card_listing(
            href,
            card,
            "cpu",
            price_re,
            "de r$",
            lambda h: h.rstrip("/").split("/")[-1],
            "https://img.pichau.com.br/processador/7600x3d.jpg",
        )
        assert listing is not None
        assert listing.price_cash == 1599.99
        assert listing.installments == 12
        assert listing.in_stock is True
        assert listing.part_number == "100100001721WOF"
        assert listing.match_key == "amd 7600x3d"
        assert listing.specs["socket"] == "am5"
        assert "7600X3D" in listing.title
        assert listing.thumbnail == "https://img.pichau.com.br/processador/7600x3d.jpg"

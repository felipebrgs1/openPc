"""Testes do spike — normalização e parser (base sólida antes de decidir).

Rodar: uv run pytest
"""

from __future__ import annotations

import json

import pytest

from openpc_spike.normalize import extract_cpu_specs, parse_price_br
from openpc_spike.parse import KabumParseError, parse_next_data


class TestPriceBr:
    def test_preco_padrao(self) -> None:
        assert parse_price_br("1.599,99") == 1599.99

    def test_preco_inteiro(self) -> None:
        assert parse_price_br("899") == 899.0

    def test_vazio_e_none(self) -> None:
        assert parse_price_br("") is None
        assert parse_price_br("   ") is None
        assert parse_price_br(None) is None

    def test_entrada_suja(self) -> None:
        assert parse_price_br("R$ 12,50") is None  # C# também falha (não normaliza R$)


class TestCpuSpecs:
    def test_socket_am5_normalizado(self) -> None:
        specs = extract_cpu_specs("Processador AMD Ryzen 5 7600, 6 Núcleos, Socket AM5")
        assert specs["socket"] == "am5"
        assert specs["cores"] == "6"

    def test_cores_em_ingles_nao_casa(self) -> None:
        # Mesmo comportamento do C#: o regex só aceita "núcleos" em português.
        specs = extract_cpu_specs("Processador AMD Ryzen 5 7600, 6-Core, Socket AM5")
        assert "cores" not in specs

    def test_lga_1700(self) -> None:
        specs = extract_cpu_specs("Intel Core i5-12400F, LGA 1700")
        assert specs["socket"] == "lga1700"

    def test_tdp_e_threads(self) -> None:
        specs = extract_cpu_specs("AMD Ryzen 7 7800X3D, TDP 120W, 8 núcleos, 16 threads")
        assert specs["tdp_w"] == "120"
        assert specs["cores"] == "8"
        assert specs["threads"] == "16"

    def test_igpu_detectada(self) -> None:
        assert extract_cpu_specs("Ryzen 5 8600G com Vídeo Integrado Radeon")["igpu"] == "true"
        assert extract_cpu_specs("Ryzen 5 7500F Sem Vídeo")["igpu"] == "false"

    def test_sem_specs(self) -> None:
        assert extract_cpu_specs("Cooler Master Hyper 212") == {}


class TestNextData:
    def test_parse_listagem(self) -> None:
        data = json.dumps(
            {
                "catalogServer": {
                    "data": [
                        {
                            "code": 123,
                            "name": "Ryzen 5 7600",
                            "friendlyName": "ryzen-5-7600",
                            "manufacturer": {"name": "AMD"},
                            "priceWithDiscount": 1199.9,
                            "price": 1299.0,
                            "maxInstallment": "12x",
                            "available": True,
                            "thumbnail": "https://img/ryzen.jpg",
                        }
                    ]
                }
            }
        )
        payload = json.dumps({"props": {"pageProps": {"data": data}}})
        html = f'<script id="__NEXT_DATA__" type="application/json">{payload}</script>'
        items = parse_next_data(html)
        assert len(items) == 1
        item = items[0]
        assert item.code == 123
        assert item.title == "Ryzen 5 7600"
        assert item.manufacturer == "AMD"
        assert item.price_with_discount == 1199.9
        assert item.price == 1299.0
        assert item.available is True

    def test_sem_next_data_falha(self) -> None:
        with pytest.raises(KabumParseError, match="__NEXT_DATA__ ausente"):
            parse_next_data("<html><body>bloqueado</body></html>")

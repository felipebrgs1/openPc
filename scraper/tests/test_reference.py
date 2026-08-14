"""Banco de specs de referência — extração do id do chip e lookup."""

from openpc_scraper.normalize.reference import chip_id, lookup


def test_chip_id_gpu_nvidia_com_sufixo() -> None:
    assert chip_id("gpu", "Placa de Vídeo RTX 5070 Ti 16GB") == "rtx5070ti"
    assert chip_id("gpu", "Placa de Vídeo Gigabyte GeForce RTX 5070 Gaming OC, 12GB, GDDR7") == "rtx5070"
    assert chip_id("gpu", "Placa de Vídeo RTX 4060 TI 8GB GDDR6") == "rtx4060ti"
    assert chip_id("gpu", "Placa de Vídeo RTX 4080 Super 16GB") == "rtx4080super"
    assert chip_id("gpu", "Placa de Vídeo GTX 1660 Super 6GB") == "gtx1660super"
    assert chip_id("gpu", "Placa de Vídeo RTX 3050 6GB GDDR6") == "rtx3050-6gb"
    assert chip_id("gpu", "Placa de Vídeo RTX 3050 8GB GDDR6") == "rtx3050"


def test_chip_id_gpu_amd() -> None:
    assert chip_id("gpu", "Placa de Vídeo ASRock Radeon RX 9070 XT Challenger, 16GB") == "rx9070xt"
    assert chip_id("gpu", "Placa de Vídeo XFX Swift RX 7800 XT 16GB") == "rx7800xt"
    assert chip_id("gpu", "Placa de Vídeo RX 7900 GRE 16GB") == "rx7900gre"
    assert chip_id("gpu", "Placa de Vídeo RX 7600 8GB") == "rx7600"


def test_chip_id_gpu_arc() -> None:
    assert chip_id("gpu", "Placa de Vídeo Intel Arc B580 12GB") == "arcb580"
    assert chip_id("gpu", "Placa de Vídeo Intel Arc A750 8GB") == "arca750"


def test_chip_id_cpu() -> None:
    assert chip_id("cpu", "Processador AMD Ryzen 7 7800X3D, 8 Núcleos, AM5") == "r77800x3d"
    assert chip_id("cpu", "Processador AMD Ryzen 5 5600GT, 6 Núcleos, AM4") == "r55600gt"
    assert chip_id("cpu", "Processador Intel Core i5-12400F, 6 Núcleos, LGA 1700") == "i512400f"
    assert chip_id("cpu", "Processador Intel Core Ultra 5 245K, LGA 1851") == "u5245k"
    assert chip_id("cpu", "Processador AMD Ryzen 9 9950X, 16 Núcleos, AM5") == "r99950x"


def test_chip_id_desconhecido() -> None:
    assert chip_id("gpu", "Placa de Vídeo Genérica XYZ") is None
    assert chip_id("cpu", "Processador Genérico ABC") is None
    assert chip_id("gpu", None) is None


def test_lookup_referencia_rtx5070() -> None:
    result = lookup("gpu", "Placa de Vídeo Gigabyte RTX 5070 Gaming OC 12GB")
    assert result is not None
    cid, specs = result
    assert cid == "rtx5070"
    assert specs["cuda_cores"] == "6144"
    assert specs["boost_clock_mhz"] == "2512"
    assert specs["memory_type"] == "gddr7"
    assert specs["memory_bus_bits"] == "192"
    assert specs["bandwidth_gbps"] == "672"
    assert specs["tdp_w"] == "250"
    assert specs["reference_model"] == "GeForce RTX 5070"
    assert specs["launch"] == "mar/2025"


def test_lookup_referencia_cpu_am5() -> None:
    result = lookup("cpu", "Processador AMD Ryzen 7 7800X3D 8 Núcleos AM5")
    assert result is not None
    cid, specs = result
    assert cid == "r77800x3d"
    assert specs["cores"] == "8"
    assert specs["cache_l3_mb"] == "96"
    assert specs["socket"] == "am5"
    assert specs["tdp_w"] == "120"


def test_lookup_sem_referencia() -> None:
    assert lookup("gpu", "Placa de Vídeo Desconhecida Modelo X") is None
    assert lookup("cpu", "Processador Desconhecido Modelo Y") is None

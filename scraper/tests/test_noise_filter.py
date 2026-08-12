"""Testes do filtro de ruído — port automático de CategoryNoiseFilterTests.cs (paridade total)."""

import pytest

from openpc_scraper.normalize.noise_filter import is_noise

@pytest.mark.parametrize("category,title", [
    ("cpu", "Adaptador Socket Para Processador Amd Am5, Contact Frame, Preto, Coolmoon"),
    ("cpu", "Contact Frame PCyes Abf17 - Moldura Para Processador Intel LGA 1700"),
    ("cpu", "Suporte para Processador AMD AM5 Bracket"),
])
def test_IsNoise_CpuComAdaptadorOuMoldura_True(category, title):
    assert is_noise(category, title) is True

@pytest.mark.parametrize("category,title", [
    ("gpu", "Suporte de Placa de V\u00eddeo Rise Mode, ARGB, 500mm"),
    ("gpu", "Suporte Para Placa De V\u00eddeo Gpu Aste Preto (ajust\u00e1vel 22mm X 190mm)"),
    ("gpu", "Adaptador Placa De V\u00eddeo (GPU) 16 Pinos, Curvo 90 Graus PCI-E 5.0 12VHPWR"),
    ("gpu", "Suporte Placa De V\u00eddeo Vertical 50-80mm Preto Coolmoon"),
])
def test_IsNoise_GpuComSuporteOuAdaptador_True(category, title):
    assert is_noise(category, title) is True

@pytest.mark.parametrize("category,title", [
    ("gpu", "Placa de V\u00eddeo Gigabyte GeForce GT 710, 2GB, DDR3"),
    ("gpu", "PCwinmax NVIDIA GeForce G210, 1GB, DDR3"),
    ("gpu", "Placa De V\u00eddeo ASUS GeForce GT 1030, 2GB, DDR4"),
    ("gpu", "Placa De V\u00eddeo Palit GeForce GT 730, 2GB, 64-Bit, DDR4"),
    ("gpu", "Placa De V\u00eddeo AMD Radeon R7 240, 2GB, DDR3"),
    ("gpu", "Gpu Nvidia Geforce Gt 610 Gdd3 2gb 64bit Single Fan - Low Profile"),
    ("gpu", "Nvidia Geforce 2gb Gt 705, Gdrr3, 64 Bit, Gt705/2g"),
    ("gpu", "Placa De Video Rx 590 8GB Ddr5 256bits PCwinmax"),
    ("gpu", "Placa De V\u00eddeo Gigabyte GeForce GT 1030, 2GB, GDDR5"),
    ("gpu", "Gpu Nvidia Geforce Gt730 2gb Ddr5 128 Bit Projeto Edge"),
])
def test_IsNoise_GpuComVramAntiga_True(category, title):
    assert is_noise(category, title) is True

@pytest.mark.parametrize("category,title", [
    ("psu", "Fonte Para Notebook Dell 65W, 19V, Carregador"),
    ("psu", "Carregador de Notebook 90W Universal"),
    ("psu", "AMD Ryzen 5 5500 3.6GHz, 6-Cores \u2014 cross-listing"),
    ("psu", "ASRock AMD Radeon RX 9070 Challenger, 16GB \u2014 cross-listing"),
    ("psu", "DDR4 Adata, 8GB, 3200MHz \u2014 cross-listing"),
])
def test_IsNoise_PsuComFonteDeNotebookOuCrossListing_True(category, title):
    assert is_noise(category, title) is True

@pytest.mark.parametrize("category,title", [
    ("psu", "Fonte 12 V 3a"),
    ("psu", "Fonte Para Tv Box"),
    ("psu", "Fonte Universal De 3v A 12v"),
    ("psu", "Fonte De Alimenta\u00e7\u00e3o Vxpro Vx230se"),
    ("psu", "Fonte De Alimenta\u00e7\u00e3o Fortrek Pws-2003, ATX, 200W, 20+4P, Sem Cabo, 115/230V - Fk420p"),
    ("psu", "Fonte C3tech Ps-200v4 200w Box S/cabo"),
    ("psu", "Fonte K-Mex ATX, 200W Real - PX300DNG"),
    ("psu", "Fonte Brazilpc ATX, 230W Real, Bpc-230v1.2, O&m C/cabo"),
    ("psu", "Fonte Knup ATX, 350W, Para PC - KP-526"),
    ("psu", "Fonte VKOEM ATX, 200W"),
    ("psu", "Fonte Evus ATX, 200W, 12V, Sem Cabo, 24P, 2SATA, Com Caixa - PS-200"),
    ("psu", "Fonte Corsair VS450, 450W, 80 Plus White"),
    ("psu", "Fonte Cisco Nexus 7000 6000w Ac N7k-ac-6.0kw   Hot Swap"),
])
def test_IsNoise_PsuAbaixoDe500wOuSemPotencia_True(category, title):
    assert is_noise(category, title) is True

@pytest.mark.parametrize("category,title", [
    ("psu", "Fonte Corsair CX650, 650W, 80 Plus Bronze"),
    ("psu", "Fonte Corsair RM850x, 850W, 80 Plus Gold, Modular, 115-230V"),
    ("psu", "Fonte EVGA 600 BR, 600W, 80 Plus Bronze"),
    ("psu", "Fonte Mancer Thunder 500W, 80 Plus Bronze"),
    ("psu", "Fonte Corsair CX650, 80 Plus Bronze, Semi-Modular"),
    ("psu", "Fonte Gigabyte P750GM 750W 80 Plus Titanium"),
    ("psu", "Fonte Cooler Master Elite Gold 1000 Fm A/wo Cord - Mpw-a001-afag-bwo"),
    ("psu", "Fonte Cooler Master Elite Gold 1200 Fm A/wo Cord - Mpw-c001-afag-bwo"),
    ("psu", "Fonte Gamer Atx 850wk-mex Ez8898d"),
])
def test_IsNoise_PsuDe500wOuMais_False(category, title):
    assert is_noise(category, title) is False

@pytest.mark.parametrize("category,title", [
    ("storage", "Cabo Sata 6 Gb/s Sata 3 - Uma Ponta 90 Graus - SSD Hd"),
    ("storage", "Adaptador F3 Su-s01 Baia De 2.5 Polegadas 7mm E 9mm Para 3.5 Polegadas Para SSD E HD De Notebook"),
    ("storage", "Kingston Adaptador de SSD 2,5, para Baia 3,5 - SNA-BR2/35"),
    ("storage", "Adaptador Caddy Case SSD Hd 12.7mm Notebook Dell Hp Samsung"),
    ("storage", "Adaptador Msata SSD Para Sata Hd SSD 2,5"),
    ("storage", "Estojo Prote\u00e7\u00e3o Capa Para Case Hd SSD Neoprene Antichoque F3"),
    ("storage", "Case Hd Externo Tipo C Exbom Cghd-40 Transparente USB 3.1 Case Gaveta HD/SSd 2.5 Ultra R\u00e1pido Slim"),
    ("storage", "Case Gaveta Para SSD M.2 Nvme E Sata Usb 3.0 Usb C Type-c"),
    ("storage", "Gaveta Mtek En-25k809ca Sata 2.5\" Para SSD Hdd Usb-c 3.1 Preto Alta Velocidade Port\u00e1til Externa"),
    ("storage", "Dissipador SSD M.2 2280 Nvme/ngff 3mm Black + Thermal Pad"),
    ("storage", "Dissipador Ctech Para SSD M2 NVME NGFF, 2280mm, 4mm, Alum\u00ednio, Thermalpad, PC, Notebook e PS5, Preto"),
    ("storage", "Dissipador De Calor Coolmoon para SSD M2, Nvme, LED, ARGB, 5v, 3 Pinos, Alum\u00ednio"),
    ("storage", "Base De Fixa\u00e7\u00e3o Para SSD M.2 Nvme 2230 Com Dissipador Met\u00e1lico Cobreado \u2013 Bracket De Fixa\u00e7\u00e3o Notebook/mini PC"),
    ("storage", "Caixa Externa Usb 3.1 Tipo C SSD Liga De Alum\u00ednio M.2 Disco De Estado S\u00f3lido Nvme/ngff"),
    ("storage", "Dock Station Para SSD M.2 Nvme - Usb-c - Backup, Clone E C\u00f3pia - Cs-doc-NVME/ngff"),
    ("storage", "Dock Station Usb-c Para M.2 Nvme Cs-doc-tyc-NVME - 1177"),
    ("storage", "Sata Docking Station, C\u00f3pia Offline Hot Swap Double Slot 2x 18tb Hard Drive USB 3.0 Led RGB"),
    ("storage", "Duplicador Clone De 4 Baias Ss Nvme M.2 Usb-c 4.0 40gbp"),
    ("storage", "Esta\u00e7\u00e3o Encaixe Wavlink Dual Slot P/disco R\u00edgido Preto"),
    ("storage", "Compartimento Case SSD Hdd Usb 3.0 Disco R\u00edgido Kapbom"),
    ("storage", "Hagibis SSD 2230 M2 Nvme SSD Com Ventilador Usb 32 Verde"),
    ("storage", "Placa PCi-e Nvme Ad135 Knup"),
    ("storage", "Placa Pci-e Para SSD M.2 Com Nvme - Pci-e X4 - Pm2-pcie"),
    ("storage", "Placa Sata Para Ssd M.2 - Adaptador Sata 7+15 Pinos - Pm2-sata"),
    ("storage", "placa adaptadora nvme m.2 pci-express ssd fenvi"),
    ("storage", "Transforme Seu M.2 Nvme Em Case Externo Port\u00e1til Lexar Usb-c"),
    ("storage", "PC GAMER CAPTAIN Intel i5 14400F / Intel Arc B580 / 16GB DDR4 (8GBx2) / SSD NVME 512GB"),
    ("storage", "Cartucho 992x Pagewide Ciano M0j91al 193ml"),
    ("storage", "Headset Gamer Corsair HS35 V2, 3.5mm, Drivers de 50mm, Carbono, CA-9011377-NA"),
    ("storage", "Water Cooler Asus TUF Gaming LC III, 360mm, ARGB, Intel-AMD, Preto, 90RC0191-B0UAY0"),
    ("storage", "PCi Riser Card 2 X PCi Flexivel 5cm R4r5r6r7"),
    ("storage", "Monitor Gamer Duex, 27 Pol, Full HD, IPS, 240Hz"),
])
def test_IsNoise_StorageSemUnidadeOuAcessorio_True(category, title):
    assert is_noise(category, title) is True

@pytest.mark.parametrize("category,title", [
    ("storage", "SSD Kingston NV3, 1 TB, M.2 2280, PCIe 4.0 x4, NVMe, Leitura: 6000 MB/s, Grava\u00e7\u00e3o: 4000 MB/s, Azul - SNV3S/1000G"),
    ("storage", "HD SSD M.2 Kingston Fury Renegade, 2TB, Pci-e 4.0x4, Dissipador - Sfyrdk/2000g 5815"),
    ("storage", "SSD Kingston Fury Renegade, 1TB, M.2 2280, PCIe 4.0 x4, NVMe, Leitura: 7300 MB/s, Grava\u00e7\u00e3o: 6000MB/s, com Dissipador, Compat\u00edvel com PS5 - SFYRSK/1000G"),
    ("storage", "SSD Netac Nv3000 250gb M.2 Nvme Pcie Gen3x4, Leitura 3100mb/s, Com Dissipador"),
    ("storage", "SSD Nvme Movespeed 4TB M.2 PCie 4.0 7450mb/s Dissipador Ps5 PC"),
    ("storage", "SSD M.2 Corsair MP600 Pro LPX 1TB, PCIe 4.0, NVMe, Dissipador de Calor para PS5"),
    ("storage", "Hd Externo Seagate 10tb Usb 3.0 Expansion Desktop Backup 3.5 - Preto"),
    ("storage", "Hd 8tb Sata 3 512mb 7200rpm 3,5 S300 Pro Surveillance Md10ada800v Toshiba"),
    ("storage", "Disco Solido Ediloca EN680E M2 256GB NVMe PCIe Gen 3x4"),
    ("storage", "Disco S\u00f3lido Externo Samsung Portable SSD T7 Mu-pc1t0 1TB Azul"),
    ("storage", "Nvme 1TB Corsair Mp700 Elite, M.2 2280, PCie Gen 5x4, Grav 10000mb/s, Leit 8400mb/s - Cssd-f1000gbmp700ehs"),
    ("storage", "M.2 Kingston 4TB Kc3000 Pcie 4.0 Nvme Skc3000d/4096g"),
    ("storage", "Unidade De Estado S\u00f3lido Interna PCie 3.0 M.2 De 512 Gb PCle 3.0 X 4 SSD Nvme M.2 2280 SSD Interno Kootion"),
    ("storage", "SSD Externo Samsung T9 4TB, Usb 3.2 (usb-c), Leitura 2000 Mb/s, Escrita 1950 Mb/s - Preto Mu-pg4t0b/am"),
    ("storage", "SSD. Externo 1TB Usb Tipo C 3.2 800mb/s Leit Preto E Laranja Sdssde30-1t00-g26 Sandisk"),
    ("storage", "SSD Samsung 870 EVO, 2TB, SATA 2.5', Leitura 560MB/s - MZ-77E2T0B/AM."),
    ("storage", "Upgrade SSD 256gb Nvme PCie 3.0 Gen3x4 Ediloca Novo"),
    ("storage", "Ssd Para Servidor Kingston 2.5\" Dc500r, 480GB, Leituras 555MB/s, Grava\u00e7\u00e3o 520MB/s, Sata III 6GB/s"),
    ("storage", "HD SSD 1.92 Tb Sata Para Dell R240 R340 R440 R540 R640 R740"),
    ("storage", "Drive SSD Sata3 2.5 Crucial, 240GB - CT240BX500SSD1"),
])
def test_IsNoise_StorageUnidadesLegitimas_False(category, title):
    assert is_noise(category, title) is False

@pytest.mark.parametrize("category,title", [
    ("cooler", "Pasta T\u00e9rmica de Silicone Implastec Pote 500g"),
])
def test_IsNoise_CoolerComPastaTermica_True(category, title):
    assert is_noise(category, title) is True

@pytest.mark.parametrize("category,title", [
    ("cooler", "Cooler FAN Rise Mode Galaxy G1, S-LED, Azul - RM-FN-01-BB"),
    ("cooler", "Fan 120mm Kalkan Lumen RGB Preto Klk00018"),
    ("cooler", "Ventoinha Rise Mode Wind W1 120mm LED Branco Preto - RM-WN-01-BW"),
    ("cooler", "Kit com 3 Ventoinhas Rise Mode X Led Rainbow, 120mm, Branco - RM-XLD-02-RBW"),
    ("cooler", "Ventoinha, Cooler Fan, Para Processador Intel, AMD, LED, RGB, Silencioso 120mm"),
    ("cooler", "Cooler 120mm Gaming Master - Com LED RGB Rainbow"),
    ("cooler", "Cooler 12x12 Brazilpc BPC, Dl1252 Red Led Duplo - Vermelho"),
    ("cooler", "Cooler Liketec Lighter, RGB, 120x120x25mm, Preto"),
    ("cooler", "Cooler Refrigerador 80x80x25mm Foxconn Pva080g12q"),
    ("cooler", "Cooler 60x60x38mm Dc12v 1.20a Avc Dbtc0638b2u P008 4 Fios"),
    ("cooler", "Microventilador Cooler Ventoinha 40x40x10 12 Volts"),
    ("cooler", "Micro ventilador Loud, 60x60x20mm, 12v - RDD6020S12M"),
    ("cooler", "Cabo de Sincroniza\u00e7\u00e3o PWM 4 Pinos, Para Controlador de Cooler Fan 2 Pinos"),
    ("cooler", "Controladora ARGB K-mex 6 Pinos Para Cooler Para LED Com Controle Remoto"),
    ("cooler", "Hub Multi Fans, Para 5 Cooler, 3 E 4 Pinos, Controle Pwm"),
    ("cooler", "Kit Alloyseed, Modelo Longo 32mm, Parafuso Water Cooler Push-and-pull, Preto"),
    ("cooler", "Thermal Pad Tishric 0.5mm Cpu Gpu Alta Efici\u00eancia 100mm Tsr139"),
    ("cooler", "Cooler Notebook Lenovo Ideapad S145 S145-15iwl"),
    ("cooler", "Cooler Cpu Dissipador Ventilador Para Acer 4830, 4830tg"),
    ("cooler", "Cooler Para Processador Low Profile Cooler Master H115 LGA 115x LGA 1200"),
    ("cooler", "Cooler Para Processador Knup, Intel, LGA 1156 / 1155 / 1150 / 1151, RPM 2200"),
    ("cooler", "Cooler Para Cpu Universal Com 21 Leds Azul Dx-2021 socket 1366/ 1150/ 1156/ 775 - AMD: FM2+/ FM2/ FM1/ AM3+/ AM3/ AM2+/"),
])
def test_IsNoise_CoolerComVentoinhaOuAcessorio_True(category, title):
    assert is_noise(category, title) is True

@pytest.mark.parametrize("category,title", [
    ("cooler", "Air Cooler TRYX Turris T620, Dual Tower, Display LCD, 6 Heat Pipes, 2 Ventoinhas 120mm, AMD e Intel"),
    ("cooler", "Air Cooler Gamerstorm X Redragon Ak400 Rd Preto 4 Heatpipes Fan 120 Mm"),
    ("cooler", "Cooler Thermaltake Ux500 ARGB White 1 Fan Intel/amd Universal Socket Compatibility"),
    ("cooler", "Cooler Jonsbo para CPU Radiador Ventilador, RGB AMD & Intel - Cr-1400"),
    ("cooler", "Water Cooler Acer 240mm ARGB Ac240yn Branco"),
    ("cooler", "Water Cooler ASUS TUF Gaming LC III, 360mm, ARGB, Intel-AMD, Preto"),
    ("cooler", "Water Cooler G-vr360pro 360mm RGB E Controle Pwm Tdp 300w"),
    ("cooler", "Air Cooler Gamer Rise Mode X2 120mm Preto/Azul - RM-ACX-02-BB"),
    ("cooler", "Cooler Para Processador Rise Mode X4, RGB, 90mm, Intel, RM-ACX-04-RGB"),
    ("cooler", "Cooler Processador Ac04 120mm Amdintel Ate LGA 1700multicolor"),
    ("cooler", "Cooler Para Processador Get, Socket Intel LGA 1150/1151/1155/1156/1700, Conector 4 Pinos, 2400 Rpm"),
])
def test_IsNoise_CoolerDeCpuLegitimo_False(category, title):
    assert is_noise(category, title) is False

@pytest.mark.parametrize("category,title", [
    ("gpu", "Suporte para GPU Rise Mode, ARGB, 500mm, Aura V2"),
    ("gpu", "Suporte Vertical Gpu Placa De Video Bracket PCi-e 4.0 16x"),
])
def test_IsNoise_GpuComSuporteParaGpuOuVertical_True(category, title):
    assert is_noise(category, title) is True

@pytest.mark.parametrize("category,title", [
    ("memory", "Para Notebook DDR5 Kingston Fury Impact, 16GB (2x8GB), 4800MHz, CL38"),
    ("memory", "SODIMM DDR4 8GB 3200MHz para Notebook"),
    ("memory", "DDR4 16GB 3200MHz Laptop, Kingston"),
    ("motherboard", "Placa M\u00e3e Notebook Acer Aspire One 722 P1ve6 La-7071p"),
    ("motherboard", "Placa M\u00e3e Dell La-g712p Intel Pentium Gold 5405u Sucata"),
    ("storage", "Pendrive Kingston 64GB USB 3.0"),
    ("storage", "Pen Drive Sandisk Cruzer 32GB"),
    ("cpu", "Processador Intel Core i5-4590, 3.3GHz, LGA 1150"),
    ("cpu", "Intel Xeon E5-2637 V2"),
    ("cpu", "Processador Intel Core 2 Duo E8400"),
    ("cpu", "AMD Athlon 3000g, 3.5GHz, Cache 5MB, AM4, 2 N\u00facleos, 4 Threads"),
    ("cpu", "AMD A10-5800K, 3.8GHz, Quad Core, FM2, OEM"),
    ("cpu", "AMD A10 9700 Bristol Ridge, 3.5GHz, AM4, DDR4 - AD9700AGABBOX"),
    ("cpu", "Amd Ryzen Athlon 3000g Socket Am4 / 3.5ghz / 5mb - Oem"),
    ("cpu", "AMD FX-8350, 4.0GHz, 8-Core, AM3+"),
])
def test_IsNoise_NotebookPendriveOuCpuAntiga_True(category, title):
    assert is_noise(category, title) is True

@pytest.mark.parametrize("category,title", [
    ("motherboard", "Placa M\u00e3e Bluecase Bmbh61-g22hg-itx Oem LGA  1155"),
    ("motherboard", "Placa-M\u00e3e Brazil PC, Intel H61 - BPC-H61C-V2.3"),
    ("motherboard", "Placa M\u00e3e Fm2/fm2 S\u00e9ries Cp Clipset Amda68 A88 16gb Usb 3.0"),
    ("motherboard", "Placa M\u00e3e Dell Latitude E4300 La-4151p Core\u21222 Duo Sp9400"),
    ("motherboard", "Placa L\u00f3gica Nova Tablet Positivo Mini Smb-d7803 - 11097328"),
    ("motherboard", "Placa Principal Positivo Smile Light 563 St-0ad-2621-tew-11"),
    ("motherboard", "Placa-M\u00e3e Biostar H410MH 6.0, Intel 1200, mATX, DDR4"),
    ("motherboard", "Placa M\u00e3e LGA 1151 Get H310 Ddr4 M.2, Suporte Para Processadores Intel De 8\u00aa E 9\u00aa Gera\u00e7\u00e3o"),
    ("motherboard", "Placa M\u00e3e 1150 Biostar H81mhv3 Ddr3 D-sub HDMI"),
])
def test_IsNoise_MotherboardAntigaOuDeMaquinaEspecifica_True(category, title):
    assert is_noise(category, title) is True

@pytest.mark.parametrize("category,title", [
    ("gpu", "Suport Para Placa De V\u00eddeo Gpu Gatinhos Fofo (ajust\u00e1vel 55mm)"),
    ("gpu", "Caixa De Som Soundbar Psb P2 Tv Gamer Subwoofer"),
    ("gpu", "Cabo Argn Para Gpu 24 Pinos RGB | Compat\u00edvel Aura Sync"),
    ("gpu", "Espelho Low Profile Perfil Baixo Para Nvidia Quadro P400 E T400"),
    ("gpu", "Placa De Captura De V\u00eddeo Hagibis 100w Pd Usb 30 Cinza"),
    ("storage", "Monitor Gamer Duex, 27 Pol, Full HD, IPS, 240Hz"),
    ("storage", "Para Notebook DDR5 Kingston Fury Impact, 16GB (2x8GB), 4800MHz"),
    ("gpu", "para Notebook DDR4 Corsair Vengeance, 8GB, 3200MHz, CMSX8GX4M1A3200C22"),
    ("cooler", "Cabo Adaptador Pwm, Para Cooler Fan 3 e 4 Pinos"),
    ("cooler", "Hub Controladora ARGB RGB Gamer 6 Pinos Para Coolers"),
    ("cooler", "Massa Polar Snowdog 20g + Adesivo Massa T\u00e9rmica"),
])
def test_IsNoise_OutrosRuidosDeCategoria_True(category, title):
    assert is_noise(category, title) is True

@pytest.mark.parametrize("category,title", [
    ("cooler", "Pasta De Cobre Implastec Igc 220 - 50g"),
    ("cooler", "Pasta De Solda Soldatec Implastec 50g"),
])
def test_IsNoise_CoolerComPastaDeCobreOuSolda_True(category, title):
    assert is_noise(category, title) is True

@pytest.mark.parametrize("category,title", [
    ("memory", "Memoria Hynix 512mb Ddr2 667mh PC2-5300s Hymp564s64cp6-y5 Ab"),
    ("memory", "Dale7, 2GB, 800Mhz, DDR2, Desktop, 1.8v"),
    ("memory", "Memoria 4gb Ddr3l 1600 Cl 11 1.35v Desktop Udimm Sgm4gl1600cl11d Sgmax"),
    ("memory", "Para Servidor Samsung 2gb Ddr Sdram M312l5628bto-cbo"),
    ("memory", "Memoria Ram Ddr2 2gb 800mhz PC2-6400 1.8v Dimm Desktop PC"),
    ("memory", "Dissipador De Calor Mem\u00f3ria RAM ARGB 5V 3 Pinos Control\u00e1vel Cor: Branco"),
    ("memory", "Memoria 8GB PC3- 12800e 1600mhz Para Dell Poweredge T110 Ii."),
    ("memory", "Memory One, 1GB, 400MHz, DDR - PC3200"),
])
def test_IsNoise_MemoriaComDdr2Ddr3OuSdram_True(category, title):
    assert is_noise(category, title) is True

@pytest.mark.parametrize("category,title", [
    ("memory", "Mem 16gb Acer Aspire 5 A515-54g A515-55 A515-55g A514-51"),
    ("memory", "Mem 16gb Ddr4 Para Dell Inspiron 15 3000 3511 3525 5501 5502"),
    ("memory", "Mem 16gb Ddr4 Para Dell Latitude 13 (5300) 14 (3410) (5400)"),
    ("memory", "Mem 8GB Ddr4 Para Dell Optiplex 3046 3050 5050 7040 Tower"),
    ("memory", "Mem 32gb Ddr4 Para Dell Vostro 3710 3910 Xps 8940 Desktop"),
    ("memory", "Mem 8GB Ddr4 Para Lenovo Ideapad 330s 320s 520s 720s"),
    ("memory", "Mem 8GB Ddr4 Para Lenovo Thinkcentre M820z M920 M920q M920x"),
    ("memory", "Memoria 16gb Ddr4 Acer Predator Helios 300 H315 H317"),
    ("memory", "Memoria 8GB Ddr4 Para Asus Mobile Asuspro Vivobook Xserie"),
    ("memory", "Ram 16gb Ecc Registrada Para Servidores Dell, Hpe E Lenovo"),
    ("memory", "Ram Para Lenovo Thinksystem 16gb Ddr4 2666mhz Ecc"),
    ("memory", "Ram 16gb Dell Poweredge R440 R540 R640 R740 R840 R940"),
    ("memory", "Ram 16gb Ddr4 Ecc Hp Proliant Dl360 E Dl380 Gen9"),
    ("memory", "Ram 8GB Ddr4 3200aa-r 25600 Registrada Hpe Cloudline"),
    ("memory", "Ram 8GB Para Mac Pro 2013"),
    ("memory", "Mem 16gb Ddr4 Para Dell G15 5510 G15 5511 G15 5515 G15 5520"),
    ("memory", "Memoria Macrovip Note 8GB Ddr5 4800mhz Mv48s40/8"),
    ("memory", "Para PC Samsung Server Ecc 16gb 2666mhz Udimm Ddr4"),
])
def test_IsNoise_MemoriaParaMaquinaEspecifica_True(category, title):
    assert is_noise(category, title) is True

@pytest.mark.parametrize("category,title", [
    ("memory", "Smartwatch Haylou Solar Lite 2 Hcr01, Tela AmoLED 1,43, Chamadas, 1 Atm, Assistente De Voz - Preto"),
    ("memory", "Water Cooler Corsair Nautilus 360 RS, 360mm, Preto, CW-9060089-WW"),
    ("memory", "Gamer Mancer Mugen, Full-Tower, Lateral de Vidro, Preto, MCR-MGN-BK"),
    ("memory", "Gamer NZXT H7 Flow, RGB, Mid-Tower, Lateral de Vidro, Com 3 Fans, Branco"),
])
def test_IsNoise_MemoriaComCrossListingDeGadgets_True(category, title):
    assert is_noise(category, title) is True

@pytest.mark.parametrize("category,title", [
    ("gpu", "Placa M\u00e3e ASRock B850 Challenger WiFi White, Chipset B850, AMD AM5, ATX, DDR5"),
    ("gpu", "DDR4 Kingston Fury Beast, RGB, 8GB, 3200MHz"),
    ("memory", "Placa de Video INNO3D GeForce RTX 5060, 8GB, GDDR7"),
    ("memory", "Placa De Video Arktek Radeon Rx550 4gb GDDR5"),
    ("memory", "Placa De Video Rx 590 8GB Ddr5 256bits PCwinmax"),
    ("memory", "AMD Ryzen 7 5700, 3.7GHz, 8-Cores 16-Threads, AM4"),
    ("memory", "Fonte Pichau Cluster, 750W, PCIe 5.0, Full Modular"),
    ("memory", "Placa Mae Asus TUF Gaming B650M-E WIFI, DDR5, Socket AMD AM5"),
])
def test_IsNoise_CrossListingDeOutraCategoria_True(category, title):
    assert is_noise(category, title) is True

@pytest.mark.parametrize("category,title", [
    ("gpu", "Quadro Pny Nvidia RTX A400 4gb Gddr6"),
    ("gpu", "Palit Geforce RTX 5070 White Oc, 12gb, Gddr7, 192-bit, PCie 5.0"),
    ("gpu", "Galax NVIDIA GeForce GTX 1630, 4GB DDR6, 64 Bits"),
    ("psu", "Fonte Gigabyte P750GM 750W 80 Plus Titanium"),
    ("cpu", "Processador AMD Ryzen 5 7600, 6-Core, Socket AM5"),
    ("motherboard", "Placa-M\u00e3e Asus TUF Gaming B650M-Plus Wifi, DDR5, Socket AM5, M-ATX"),
    ("motherboard", "Placa M\u00e3e ASRock B850 LiveMixer WiFi, Chipset B850, AMD AM5, ATX, DDR5"),
    ("motherboard", "Placa-M\u00e3e MSI PRO B840M-B, AMD B840, DDR5, Preto - PRO B840M-B"),
    ("motherboard", "Placa M\u00e3e Gigabyte H610M S2H V2, Chipset H610, Intel LGA 1700, mATX, DDR5"),
    ("motherboard", "Placa-M\u00e3e MSI PRO H810M-B WIFI6E, Intel, LGA 1851, Micro-ATX, DDR5"),
    ("motherboard", "Placa Mae Bpc-a520m.2-tg Am4 (2xDDR4/1xhdmi/1xvga/m.2/2xusb3.0/rede Giga) Oem"),
    ("memory", "Mem\u00f3ria Kingston Fury 16GB DDR5"),
    ("memory", "DDR4 Corsair Vengeance RGB Pro, 16GB (2x8GB) 3200MHz"),
    ("memory", "Memoria De Desktop Sk Hynix 4gb 1rx8 Ddr4 PC4 2133 Mhz 1.2v Oem Hma451u6"),
    ("memory", "Memoria Ddr4 8GB PC3200 Upgamer Up3200"),
    ("memory", "Memoria 32gb Ddr4 Brazilpc 3200mhz Bpc3200d4cl22/32g O&m"),
    ("memory", "Keepdata Kd24n17/16g, 16GB, 2400MHz, DDR4 - Pc2400, 1 2v"),
    ("memory", "Kingston DIMM, 16GB, 2400MHz, DDR4, CL17, Non-ECC, Para Desktop - KVR24N17D8/16"),
    ("memory", "8GB Ddr4 3200mhz, Kingston Fury Beast Para Desktop/gamers, Kf432c16bb/8wp"),
    ("memory", "RAM Kingston Fury Beast, 32GB, 5600MHz, DDR5, CL40, para Intel XMP, Preto"),
    ("memory", "RAM Corsair Vengeance para AMD, 64GB, 5200MHz, DDR5, CL40, Preto"),
    ("cooler", "Water Cooler DeepCool LS720 360mm ARGB"),
    ("cooler", "Cooler Master Hyper 212, Air Cooler"),
])
def test_IsNoise_ProdutosLegitimos_False(category, title):
    assert is_noise(category, title) is False

@pytest.mark.parametrize("category,title", [
    ("cpu", "AMD Ryzen 5 Pro 5650G, 16MB, 6 Cores, Vega 7, AM4"),
    ("cpu", "Processador AMD Ryzen 5 7600, 6-Core, Socket AM5"),
    ("cpu", "Processador Intel Core i5-12400F, LGA 1700"),
    ("cpu", "Intel Core Ultra 7 265F, 20-Core"),
    ("cpu", "Amd Ryzen 5 5600xt Am4 4.7 Ghz 6 Cores"),
])
def test_IsNoise_CpusModernas_False(category, title):
    assert is_noise(category, title) is False

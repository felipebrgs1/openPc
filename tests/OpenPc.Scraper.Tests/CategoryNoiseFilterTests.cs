using OpenPc.Scraper.Normalization;

namespace OpenPc.Scraper.Tests;

public class CategoryNoiseFilterTests
{
    [Theory]
    [InlineData("cpu", "Adaptador Socket Para Processador Amd Am5, Contact Frame, Preto, Coolmoon")]
    [InlineData("cpu", "Contact Frame PCyes Abf17 - Moldura Para Processador Intel LGA 1700")]
    [InlineData("cpu", "Suporte para Processador AMD AM5 Bracket")]
    public void IsNoise_CpuComAdaptadorOuMoldura_True(string category, string title)
    {
        Assert.True(CategoryNoiseFilter.IsNoise(category, title));
    }

    [Theory]
    [InlineData("gpu", "Suporte de Placa de Vídeo Rise Mode, ARGB, 500mm")]
    [InlineData("gpu", "Suporte Para Placa De Vídeo Gpu Aste Preto (ajustável 22mm X 190mm)")]
    [InlineData("gpu", "Adaptador Placa De Vídeo (GPU) 16 Pinos, Curvo 90 Graus PCI-E 5.0 12VHPWR")]
    [InlineData("gpu", "Suporte Placa De Vídeo Vertical 50-80mm Preto Coolmoon")]
    public void IsNoise_GpuComSuporteOuAdaptador_True(string category, string title)
    {
        Assert.True(CategoryNoiseFilter.IsNoise(category, title));
    }

    [Theory]
    [InlineData("gpu", "Placa de Vídeo Gigabyte GeForce GT 710, 2GB, DDR3")]
    [InlineData("gpu", "PCwinmax NVIDIA GeForce G210, 1GB, DDR3")]
    [InlineData("gpu", "Placa De Vídeo ASUS GeForce GT 1030, 2GB, DDR4")]
    [InlineData("gpu", "Placa De Vídeo Palit GeForce GT 730, 2GB, 64-Bit, DDR4")]
    [InlineData("gpu", "Placa De Vídeo AMD Radeon R7 240, 2GB, DDR3")]
    [InlineData("gpu", "Gpu Nvidia Geforce Gt 610 Gdd3 2gb 64bit Single Fan - Low Profile")] // typo de GDDR3
    [InlineData("gpu", "Nvidia Geforce 2gb Gt 705, Gdrr3, 64 Bit, Gt705/2g")]              // typo de GDDR3
    [InlineData("gpu", "Placa De Video Rx 590 8GB Ddr5 256bits PCwinmax")]      // "Ddr5" = grafia de GDDR5
    [InlineData("gpu", "Placa De Vídeo Gigabyte GeForce GT 1030, 2GB, GDDR5")]
    [InlineData("gpu", "Gpu Nvidia Geforce Gt730 2gb Ddr5 128 Bit Projeto Edge")]
    public void IsNoise_GpuComVramAntiga_True(string category, string title)
    {
        Assert.True(CategoryNoiseFilter.IsNoise(category, title));
    }

    [Theory]
    [InlineData("psu", "Fonte Para Notebook Dell 65W, 19V, Carregador")]
    [InlineData("psu", "Carregador de Notebook 90W Universal")]
    [InlineData("psu", "AMD Ryzen 5 5500 3.6GHz, 6-Cores — cross-listing")]
    [InlineData("psu", "ASRock AMD Radeon RX 9070 Challenger, 16GB — cross-listing")]
    [InlineData("psu", "DDR4 Adata, 8GB, 3200MHz — cross-listing")]
    public void IsNoise_PsuComFonteDeNotebookOuCrossListing_True(string category, string title)
    {
        Assert.True(CategoryNoiseFilter.IsNoise(category, title));
    }

    [Theory]
    [InlineData("cooler", "Pasta Térmica de Silicone Implastec Pote 500g")]
    public void IsNoise_CoolerComPastaTermica_True(string category, string title)
    {
        Assert.True(CategoryNoiseFilter.IsNoise(category, title));
    }

    [Theory]
    [InlineData("gpu", "Suporte para GPU Rise Mode, ARGB, 500mm, Aura V2")]
    [InlineData("gpu", "Suporte Vertical Gpu Placa De Video Bracket PCi-e 4.0 16x")]
    public void IsNoise_GpuComSuporteParaGpuOuVertical_True(string category, string title)
    {
        Assert.True(CategoryNoiseFilter.IsNoise(category, title));
    }

    [Theory]
    [InlineData("memory", "Para Notebook DDR5 Kingston Fury Impact, 16GB (2x8GB), 4800MHz, CL38")]
    [InlineData("memory", "SODIMM DDR4 8GB 3200MHz para Notebook")]
    [InlineData("memory", "DDR4 16GB 3200MHz Laptop, Kingston")]
    [InlineData("motherboard", "Placa Mãe Notebook Acer Aspire One 722 P1ve6 La-7071p")]
    [InlineData("motherboard", "Placa Mãe Dell La-g712p Intel Pentium Gold 5405u Sucata")]
    [InlineData("storage", "Pendrive Kingston 64GB USB 3.0")]
    [InlineData("storage", "Pen Drive Sandisk Cruzer 32GB")]
    [InlineData("cpu", "Processador Intel Core i5-4590, 3.3GHz, LGA 1150")] // 4ª geração Haswell
    [InlineData("cpu", "Intel Xeon E5-2637 V2")]                          // fora da matriz
    [InlineData("cpu", "Processador Intel Core 2 Duo E8400")]
    [InlineData("cpu", "AMD Athlon 3000g, 3.5GHz, Cache 5MB, AM4, 2 Núcleos, 4 Threads")]
    [InlineData("cpu", "AMD A10-5800K, 3.8GHz, Quad Core, FM2, OEM")]
    [InlineData("cpu", "AMD A10 9700 Bristol Ridge, 3.5GHz, AM4, DDR4 - AD9700AGABBOX")]
    [InlineData("cpu", "Amd Ryzen Athlon 3000g Socket Am4 / 3.5ghz / 5mb - Oem")] // título com ruído
    [InlineData("cpu", "AMD FX-8350, 4.0GHz, 8-Core, AM3+")]
    public void IsNoise_NotebookPendriveOuCpuAntiga_True(string category, string title)
    {
        Assert.True(CategoryNoiseFilter.IsNoise(category, title));
    }

    [Theory]
    [InlineData("motherboard", "Placa Mãe Bluecase Bmbh61-g22hg-itx Oem LGA  1155")] // H61, 2ª/3ª geração
    [InlineData("motherboard", "Placa-Mãe Brazil PC, Intel H61 - BPC-H61C-V2.3")]
    [InlineData("motherboard", "Placa Mãe Fm2/fm2 Séries Cp Clipset Amda68 A88 16gb Usb 3.0")] // FM2+
    [InlineData("motherboard", "Placa Mãe Dell Latitude E4300 La-4151p Core™2 Duo Sp9400")]    // notebook
    [InlineData("motherboard", "Placa Lógica Nova Tablet Positivo Mini Smb-d7803 - 11097328")] // tablet
    [InlineData("motherboard", "Placa Principal Positivo Smile Light 563 St-0ad-2621-tew-11")] // máquina específica
    [InlineData("motherboard", "Placa-Mãe Biostar H410MH 6.0, Intel 1200, mATX, DDR4")]  // 10ª gen (LGA 1200)
    [InlineData("motherboard", "Placa Mãe LGA 1151 Get H310 Ddr4 M.2, Suporte Para Processadores Intel De 8ª E 9ª Geração")]
    [InlineData("motherboard", "Placa Mãe 1150 Biostar H81mhv3 Ddr3 D-sub HDMI")]
    public void IsNoise_MotherboardAntigaOuDeMaquinaEspecifica_True(string category, string title)
    {
        Assert.True(CategoryNoiseFilter.IsNoise(category, title));
    }

    [Theory]
    [InlineData("gpu", "Suport Para Placa De Vídeo Gpu Gatinhos Fofo (ajustável 55mm)")]
    [InlineData("gpu", "Caixa De Som Soundbar Psb P2 Tv Gamer Subwoofer")]
    [InlineData("gpu", "Cabo Argn Para Gpu 24 Pinos RGB | Compatível Aura Sync")]
    [InlineData("gpu", "Espelho Low Profile Perfil Baixo Para Nvidia Quadro P400 E T400")]
    [InlineData("gpu", "Placa De Captura De Vídeo Hagibis 100w Pd Usb 30 Cinza")]
    [InlineData("storage", "Monitor Gamer Duex, 27 Pol, Full HD, IPS, 240Hz")]
    [InlineData("storage", "Para Notebook DDR5 Kingston Fury Impact, 16GB (2x8GB), 4800MHz")]
    [InlineData("gpu", "para Notebook DDR4 Corsair Vengeance, 8GB, 3200MHz, CMSX8GX4M1A3200C22")]
    [InlineData("cooler", "Cabo Adaptador Pwm, Para Cooler Fan 3 e 4 Pinos")]
    [InlineData("cooler", "Hub Controladora ARGB RGB Gamer 6 Pinos Para Coolers")]
    [InlineData("cooler", "Massa Polar Snowdog 20g + Adesivo Massa Térmica")]
    public void IsNoise_OutrosRuidosDeCategoria_True(string category, string title)
    {
        Assert.True(CategoryNoiseFilter.IsNoise(category, title));
    }

    [Theory]
    [InlineData("cooler", "Pasta De Cobre Implastec Igc 220 - 50g")]
    [InlineData("cooler", "Pasta De Solda Soldatec Implastec 50g")]
    public void IsNoise_CoolerComPastaDeCobreOuSolda_True(string category, string title)
    {
        Assert.True(CategoryNoiseFilter.IsNoise(category, title));
    }

    [Theory]
    [InlineData("memory", "Memoria Hynix 512mb Ddr2 667mh PC2-5300s Hymp564s64cp6-y5 Ab")]
    [InlineData("memory", "Dale7, 2GB, 800Mhz, DDR2, Desktop, 1.8v")]
    [InlineData("memory", "Memoria 4gb Ddr3l 1600 Cl 11 1.35v Desktop Udimm Sgm4gl1600cl11d Sgmax")]
    [InlineData("memory", "Para Servidor Samsung 2gb Ddr Sdram M312l5628bto-cbo")]
    [InlineData("memory", "Memoria Ram Ddr2 2gb 800mhz PC2-6400 1.8v Dimm Desktop PC")]
    [InlineData("memory", "Dissipador De Calor Memória RAM ARGB 5V 3 Pinos Controlável Cor: Branco")]
    [InlineData("memory", "Memoria 8GB PC3- 12800e 1600mhz Para Dell Poweredge T110 Ii.")] // PC3-12800 = DDR3
    [InlineData("memory", "Memory One, 1GB, 400MHz, DDR - PC3200")]                       // DDR de 1ª geração
    public void IsNoise_MemoriaComDdr2Ddr3OuSdram_True(string category, string title)
    {
        Assert.True(CategoryNoiseFilter.IsNoise(category, title));
    }

    [Theory]
    [InlineData("memory", "Mem 16gb Acer Aspire 5 A515-54g A515-55 A515-55g A514-51")]
    [InlineData("memory", "Mem 16gb Ddr4 Para Dell Inspiron 15 3000 3511 3525 5501 5502")]
    [InlineData("memory", "Mem 16gb Ddr4 Para Dell Latitude 13 (5300) 14 (3410) (5400)")]
    [InlineData("memory", "Mem 8GB Ddr4 Para Dell Optiplex 3046 3050 5050 7040 Tower")]
    [InlineData("memory", "Mem 32gb Ddr4 Para Dell Vostro 3710 3910 Xps 8940 Desktop")]
    [InlineData("memory", "Mem 8GB Ddr4 Para Lenovo Ideapad 330s 320s 520s 720s")]
    [InlineData("memory", "Mem 8GB Ddr4 Para Lenovo Thinkcentre M820z M920 M920q M920x")]
    [InlineData("memory", "Memoria 16gb Ddr4 Acer Predator Helios 300 H315 H317")]
    [InlineData("memory", "Memoria 8GB Ddr4 Para Asus Mobile Asuspro Vivobook Xserie")]
    [InlineData("memory", "Ram 16gb Ecc Registrada Para Servidores Dell, Hpe E Lenovo")]
    [InlineData("memory", "Ram Para Lenovo Thinksystem 16gb Ddr4 2666mhz Ecc")]
    [InlineData("memory", "Ram 16gb Dell Poweredge R440 R540 R640 R740 R840 R940")]        // sem "para"
    [InlineData("memory", "Ram 16gb Ddr4 Ecc Hp Proliant Dl360 E Dl380 Gen9")]
    [InlineData("memory", "Ram 8GB Ddr4 3200aa-r 25600 Registrada Hpe Cloudline")]
    [InlineData("memory", "Ram 8GB Para Mac Pro 2013")]
    [InlineData("memory", "Mem 16gb Ddr4 Para Dell G15 5510 G15 5511 G15 5515 G15 5520")]  // gaming laptop
    [InlineData("memory", "Memoria Macrovip Note 8GB Ddr5 4800mhz Mv48s40/8")]             // linha SODIMM
    [InlineData("memory", "Para PC Samsung Server Ecc 16gb 2666mhz Udimm Ddr4")]
    public void IsNoise_MemoriaParaMaquinaEspecifica_True(string category, string title)
    {
        Assert.True(CategoryNoiseFilter.IsNoise(category, title));
    }

    [Theory]
    [InlineData("memory", "Smartwatch Haylou Solar Lite 2 Hcr01, Tela AmoLED 1,43, Chamadas, 1 Atm, Assistente De Voz - Preto")]
    [InlineData("memory", "Water Cooler Corsair Nautilus 360 RS, 360mm, Preto, CW-9060089-WW")]
    [InlineData("memory", "Gamer Mancer Mugen, Full-Tower, Lateral de Vidro, Preto, MCR-MGN-BK")]
    [InlineData("memory", "Gamer NZXT H7 Flow, RGB, Mid-Tower, Lateral de Vidro, Com 3 Fans, Branco")]
    public void IsNoise_MemoriaComCrossListingDeGadgets_True(string category, string title)
    {
        Assert.True(CategoryNoiseFilter.IsNoise(category, title));
    }

    [Theory]
    [InlineData("gpu", "Placa Mãe ASRock B850 Challenger WiFi White, Chipset B850, AMD AM5, ATX, DDR5")]
    [InlineData("gpu", "DDR4 Kingston Fury Beast, RGB, 8GB, 3200MHz")]
    [InlineData("memory", "Placa de Video INNO3D GeForce RTX 5060, 8GB, GDDR7")]
    [InlineData("memory", "Placa De Video Arktek Radeon Rx550 4gb GDDR5")]
    [InlineData("memory", "Placa De Video Rx 590 8GB Ddr5 256bits PCwinmax")]
    [InlineData("memory", "AMD Ryzen 7 5700, 3.7GHz, 8-Cores 16-Threads, AM4")]
    [InlineData("memory", "Fonte Pichau Cluster, 750W, PCIe 5.0, Full Modular")]
    [InlineData("memory", "Placa Mae Asus TUF Gaming B650M-E WIFI, DDR5, Socket AMD AM5")]
    public void IsNoise_CrossListingDeOutraCategoria_True(string category, string title)
    {
        Assert.True(CategoryNoiseFilter.IsNoise(category, title));
    }

    [Theory]
    [InlineData("gpu", "Quadro Pny Nvidia RTX A400 4gb Gddr6")]
    [InlineData("gpu", "Palit Geforce RTX 5070 White Oc, 12gb, Gddr7, 192-bit, PCie 5.0")]
    [InlineData("gpu", "Galax NVIDIA GeForce GTX 1630, 4GB DDR6, 64 Bits")]   // "DDR6" = GDDR6
    [InlineData("psu", "Fonte Gigabyte P750GM 750W 80 Plus Titanium")]          // "titanium" ≠ titan
    [InlineData("cpu", "Processador AMD Ryzen 5 7600, 6-Core, Socket AM5")]    // "socket am5" é da CPU
    [InlineData("motherboard", "Placa-Mãe Asus TUF Gaming B650M-Plus Wifi, DDR5, Socket AM5, M-ATX")]
    [InlineData("motherboard", "Placa Mãe ASRock B850 LiveMixer WiFi, Chipset B850, AMD AM5, ATX, DDR5")]
    [InlineData("motherboard", "Placa-Mãe MSI PRO B840M-B, AMD B840, DDR5, Preto - PRO B840M-B")]
    [InlineData("motherboard", "Placa Mãe Gigabyte H610M S2H V2, Chipset H610, Intel LGA 1700, mATX, DDR5")]
    [InlineData("motherboard", "Placa-Mãe MSI PRO H810M-B WIFI6E, Intel, LGA 1851, Micro-ATX, DDR5")]
    [InlineData("motherboard", "Placa Mae Bpc-a520m.2-tg Am4 (2xDDR4/1xhdmi/1xvga/m.2/2xusb3.0/rede Giga) Oem")]
    [InlineData("memory", "Memória Kingston Fury 16GB DDR5")]
    [InlineData("memory", "DDR4 Corsair Vengeance RGB Pro, 16GB (2x8GB) 3200MHz")]
    [InlineData("memory", "Memoria De Desktop Sk Hynix 4gb 1rx8 Ddr4 PC4 2133 Mhz 1.2v Oem Hma451u6")] // fabricante de RAM, não máquina
    [InlineData("memory", "Memoria Ddr4 8GB PC3200 Upgamer Up3200")]                    // PC3200 = velocidade DDR4
    [InlineData("memory", "Memoria 32gb Ddr4 Brazilpc 3200mhz Bpc3200d4cl22/32g O&m")]  // part number Bpc3200d4
    [InlineData("memory", "Keepdata Kd24n17/16g, 16GB, 2400MHz, DDR4 - Pc2400, 1 2v")]  // PC2400 = velocidade DDR4
    [InlineData("memory", "Kingston DIMM, 16GB, 2400MHz, DDR4, CL17, Non-ECC, Para Desktop - KVR24N17D8/16")]
    [InlineData("memory", "8GB Ddr4 3200mhz, Kingston Fury Beast Para Desktop/gamers, Kf432c16bb/8wp")] // "/" junta: desktopgamers
    [InlineData("memory", "RAM Kingston Fury Beast, 32GB, 5600MHz, DDR5, CL40, para Intel XMP, Preto")]
    [InlineData("memory", "RAM Corsair Vengeance para AMD, 64GB, 5200MHz, DDR5, CL40, Preto")]
    [InlineData("cooler", "Water Cooler DeepCool LS720 360mm ARGB")]
    [InlineData("cooler", "Cooler Master Hyper 212, Air Cooler")]               // marca "cooler master"
    public void IsNoise_ProdutosLegitimos_False(string category, string title)
    {
        Assert.False(CategoryNoiseFilter.IsNoise(category, title));
    }

    [Theory]
    [InlineData("cpu", "AMD Ryzen 5 Pro 5650G, 16MB, 6 Cores, Vega 7, AM4")] // "Pro" quebra match key, mas é AM4 moderna
    [InlineData("cpu", "Processador AMD Ryzen 5 7600, 6-Core, Socket AM5")]
    [InlineData("cpu", "Processador Intel Core i5-12400F, LGA 1700")]
    [InlineData("cpu", "Intel Core Ultra 7 265F, 20-Core")]
    [InlineData("cpu", "Amd Ryzen 5 5600xt Am4 4.7 Ghz 6 Cores")]
    public void IsNoise_CpusModernas_False(string category, string title)
    {
        Assert.False(CategoryNoiseFilter.IsNoise(category, title));
    }

    [Fact]
    public void IsNoise_TituloVazio_False()
    {
        Assert.False(CategoryNoiseFilter.IsNoise("cpu", null));
        Assert.False(CategoryNoiseFilter.IsNoise("cpu", ""));
    }
}

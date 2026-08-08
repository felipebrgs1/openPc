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
    public void IsNoise_NotebookPendriveOuCpuAntiga_True(string category, string title)
    {
        Assert.True(CategoryNoiseFilter.IsNoise(category, title));
    }

    [Theory]
    [InlineData("gpu", "Suport Para Placa De Vídeo Gpu Gatinhos Fofo (ajustável 55mm)")]
    [InlineData("gpu", "Caixa De Som Soundbar Psb P2 Tv Gamer Subwoofer")]
    [InlineData("gpu", "Cabo Argn Para Gpu 24 Pinos RGB | Compatível Aura Sync")]
    [InlineData("gpu", "Espelho Low Profile Perfil Baixo Para Nvidia Quadro P400 E T400")]
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
    [InlineData("gpu", "Placa De Video Rx 590 8GB Ddr5 256bits PCwinmax")]      // GPU legítima (gddr/ddr no nome)
    [InlineData("gpu", "PCwinmax NVIDIA GeForce G210, 1GB, DDR3")]
    [InlineData("gpu", "Quadro Pny Nvidia RTX A400 4gb Gddr6")]
    [InlineData("psu", "Fonte Gigabyte P750GM 750W 80 Plus Titanium")]          // "titanium" ≠ titan
    [InlineData("cpu", "Processador AMD Ryzen 5 7600, 6-Core, Socket AM5")]    // "socket am5" é da CPU
    [InlineData("motherboard", "Placa-Mãe Asus TUF Gaming B650M-Plus Wifi, DDR5, Socket AM5, M-ATX")]
    [InlineData("memory", "Memória Kingston Fury 16GB DDR5")]
    [InlineData("memory", "DDR4 Corsair Vengeance RGB Pro, 16GB (2x8GB) 3200MHz")]
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

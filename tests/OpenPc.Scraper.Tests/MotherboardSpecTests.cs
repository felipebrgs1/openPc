using OpenPc.Scraper.Normalization;

namespace OpenPc.Scraper.Tests;

/// <summary>
/// Extração de specs de placa-mãe a partir de títulos reais das 3 lojas v1
/// (socket, chipset, form factor e DDR alimentam a engine M3).
/// </summary>
public class MotherboardSpecTests
{
    [Theory]
    [InlineData("Placa Mae Asus TUF Gaming B650M-Plus Wifi, DDR5, Socket AM5, M-ATX, Chipset B650, Wi-Fi",
        "am5", "b650", "matx", "ddr5")]
    [InlineData("Placa-Mãe Gigabyte B550M Aorus Elite, DDR4, Socket AM4, M-ATX, Chipset AMD B550",
        "am4", "b550", "matx", "ddr4")]
    [InlineData("Placa Mãe MSI PRO Z790-P WIFI, DDR5, Socket LGA1700, ATX",
        "lga1700", "z790", "atx", "ddr5")]
    [InlineData("Placa-Mãe ASRock B650I Lightning WiFi, DDR5, Socket AM5, Mini-ITX",
        "am5", "b650", "itx", "ddr5")]
    [InlineData("Placa Mãe Gigabyte X870E Aorus Elite WIFI7, DDR5, Socket AM5, E-ATX, Chipset AMD X870E",
        "am5", "x870", "eatx", "ddr5")]
    public void ExtractMotherboard_TituloReal(string title, string socket, string chipset, string formFactor, string memoryType)
    {
        var specs = SpecExtractor.ExtractMotherboard(title);

        Assert.Equal(socket, specs["socket"]);
        Assert.Equal(chipset, specs["chipset"]);
        Assert.Equal(formFactor, specs["form_factor"]);
        Assert.Equal(memoryType, specs["memory_type"]);
    }

    [Fact]
    public void ExtractMotherboard_TituloMinimo_SoExtraiOQueExiste()
    {
        var specs = SpecExtractor.ExtractMotherboard("Placa-Mãe para servidor sem informações");

        Assert.Empty(specs);
    }

    [Fact]
    public void ExtractMotherboard_IgnoraChipsetForaDoPadrao()
    {
        // "AB350" (Gigabyte) não casa com o padrão [ABXHZ] + 3 dígitos em sequência limpa
        var specs = SpecExtractor.ExtractMotherboard("Placa Gigabyte AB350M Gaming 3, DDR4, Socket AM4");

        Assert.Equal("am4", specs["socket"]);
        Assert.False(specs.ContainsKey("chipset"));
    }

    [Fact]
    public void Extract_SocketNormalizadoSemEspaco()
    {
        // "LGA 1700" (com espaço) e "LGA1700" precisam gerar o mesmo valor —
        // a engine compara o valor bruto.
        var comEspaco = SpecExtractor.ExtractMotherboard(
            "Placa-Mãe Asrock B760m-h2/m.2, Intel LGA 1700, Matx, DDR5, Chipset B760");
        var semEspaco = SpecExtractor.ExtractMotherboard(
            "Placa Mãe MSI PRO Z790-P WIFI, DDR5, Socket LGA1700, ATX");

        Assert.Equal("lga1700", comEspaco["socket"]);
        Assert.Equal(semEspaco["socket"], comEspaco["socket"]);
    }

    [Fact]
    public void ExtractCpu_SocketNormalizadoSemEspaco()
    {
        var specs = SpecExtractor.ExtractCpu(
            "Processador Intel Core i5-12400F, 6-Core, 12-Threads, 4.4GHz, LGA 1700", null);

        Assert.Equal("lga1700", specs["socket"]);
    }
}

using OpenPc.Scraper.Normalization;

namespace OpenPc.Scraper.Tests;

public class PartNumberTests
{
    [Theory]
    [InlineData("Processador AMD Ryzen 7 5700X ... - 100-100000926WOF", "100100000926WOF")]
    [InlineData("Processador AMD Ryzen 5 7600X3D ... 100-100001721WOF", "100100001721WOF")]
    [InlineData("Processador Intel Core i5-12400F ... BX8071512400F", "BX8071512400F")]
    [InlineData("Processador Intel Core Ultra 5 250K ... bx80768250k", "BX80768250K")] // lowercase → normalize
    public void Extract_AchaPartNumberAmdEIntel(string title, string expected)
    {
        var part = PartNumber.Extract(title);
        Assert.Equal(expected, part);
    }

    [Theory]
    [InlineData("Processador AMD Ryzen 5 7600, 6-Core, AM5")]
    [InlineData("Cooler Master Hyper 212")]
    [InlineData("")]
    public void Extract_RetornaNullSemPadrao(string title)
    {
        Assert.Null(PartNumber.Extract(title));
    }

    [Fact]
    public void Normalize_RemoveHifensEEspacos()
    {
        Assert.Equal("BX8071512400F", PartNumber.Normalize("bx80715-12400f"));
    }
}

public class MatchKeyTests
{
    [Theory]
    [InlineData("Processador AMD Ryzen 7 5700X, 3.4GHz, AM4", "amd 5700x")]
    [InlineData("Processador AMD Ryzen 5 7600, 6-Core, 12-Threads, AM5", "amd 7600")]
    [InlineData("Processador Intel Core i5-12400F, LGA 1700", "intel 12400f")]
    [InlineData("Processador Intel Core Ultra 7 265F, 20-Core", "intel 265f")]
    [InlineData("Placa de vídeo RTX 5070 12GB", "nvidia 5070")]
    [InlineData("Placa de vídeo Radeon RX 7800 XT 16GB", "amd 7800xt")]
    public void Build_ExtraiMarcaEModelo(string title, string expected)
    {
        Assert.Equal(expected, MatchKey.Build(title));
    }

    [Theory]
    [InlineData("Memória Kingston Fury 16GB DDR5")]
    [InlineData("Xeon E5-2637 V2")]
    [InlineData("Gabinete Gamer")]
    public void Build_RetornaNullSemPadrao(string title)
    {
        Assert.Null(MatchKey.Build(title));
    }

    [Fact]
    public void Build_DistingueVariantes()
    {
        Assert.NotEqual(MatchKey.Build("Ryzen 5 7600 AM5"), MatchKey.Build("Ryzen 5 7600X AM5"));
        Assert.NotEqual(MatchKey.Build("Ryzen 5 7600X AM5"), MatchKey.Build("Ryzen 5 7600X3D AM5"));
    }
}

public class SpecExtractorTests
{
    [Fact]
    public void ExtractCpu_DoTituloKabumReal()
    {
        var title = "Processador AMD Ryzen 7 5700X, 3.4GHz (4.6GHz Max Turbo), Cache 36MB, " +
                    "8 Núcleos, 16 Threads, AM4, Sem Vídeo Integrado - 100-100000926WOF";
        var specs = SpecExtractor.ExtractCpu(title, null);

        Assert.Equal("am4", specs["socket"]);
        Assert.Equal("8", specs["cores"]);
        Assert.Equal("16", specs["threads"]);
        Assert.Equal("false", specs["has_igpu"]);
    }

    [Fact]
    public void ExtractCpu_ComIgpuTrue()
    {
        var title = "Processador AMD Ryzen 5 8600G, 6 Núcleos, 12 Threads, AM5, Com Vídeo Integrado Radeon";
        var specs = SpecExtractor.ExtractCpu(title, null);
        Assert.Equal("true", specs["has_igpu"]);
    }

    [Fact]
    public void ExtractCpu_FichaTecnicaComTdpEDdr()
    {
        var specText = "- TDP: 65W\n- Memória suportada: DDR5\n- Arquitetura: Zen 4";
        var specs = SpecExtractor.ExtractCpu("Processador AMD Ryzen 5 7600, 6 Núcleos, AM5", specText);
        Assert.Equal("65", specs["tdp_w"]);
        Assert.Equal("DDR5", specs["memory_type"]);
    }

    [Fact]
    public void ExtractGpu_DoTitulo()
    {
        var title = "Placa de vídeo RTX 5070 12GB GDDR7";
        var specs = SpecExtractor.ExtractGpu(title, null);
        Assert.Equal("12", specs["memory_gb"]);
    }
}

public class PriceParserTests
{
    [Theory]
    [InlineData("1.599,99", 1599.99)]
    [InlineData("879,99", 879.99)]
    [InlineData("12.345,67", 12345.67)]
    public void ParseBr_ConverteFormatoBrasileiro(string value, decimal expected)
    {
        Assert.Equal(expected, PriceParser.ParseBr(value));
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    public void ParseBr_RetornaNullInvalido(string value)
    {
        Assert.Null(PriceParser.ParseBr(value));
    }
}

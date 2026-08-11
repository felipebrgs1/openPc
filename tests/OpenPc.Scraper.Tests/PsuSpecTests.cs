using OpenPc.Scraper.Normalization;

namespace OpenPc.Scraper.Tests;

/// <summary>
/// Extração de specs de fonte a partir de títulos reais das 3 lojas v1
/// (`wattage` alimenta a regra PSU_WATTAGE_LOW da engine M3).
/// </summary>
public class PsuSpecTests
{
    [Theory]
    [InlineData("Fonte De Alimentacao 500w P/computador Ref: Af-500a", "500")]
    [InlineData("Fonte Corsair CX650 650W 80 Plus Bronze, PFC Ativo", "650")]
    [InlineData("Fonte Gamer 850W 80 Plus Gold Modular, ATX, Preto", "850")]
    [InlineData("Fonte ATX 550 Watts, 120mm Fan, 80 Plus White", "550")]
    [InlineData("Fonte Corsair RM750e, 750W, 80 Plus Gold", "750")]
    public void ExtractPsu_TituloReal(string title, string wattage)
    {
        var specs = SpecExtractor.ExtractPsu(title);

        Assert.Equal(wattage, specs["wattage"]);
    }

    [Fact]
    public void ExtractPsu_SemPotencia_NaoExtrai()
    {
        var specs = SpecExtractor.ExtractPsu("Fonte genérica para computador");

        Assert.Empty(specs);
    }

    [Fact]
    public void ExtractPsu_NaoConfundeComOutrosNumeros()
    {
        // "120mm" não tem W; primeiro número com W ganha
        var specs = SpecExtractor.ExtractPsu("Fonte 120mm Fan, 650W, ATX");

        Assert.Equal("650", specs["wattage"]);
    }
}

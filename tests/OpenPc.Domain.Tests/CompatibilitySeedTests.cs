using OpenPc.Infrastructure.Compatibility;

namespace OpenPc.Domain.Tests;

public class CompatibilitySeedTests
{
    [Fact]
    public void Loader_CarregaSeedRealDoRepositorio()
    {
        var seed = CompatibilitySeedLoader.Load();

        // Matriz esperada (docs/specs.md §4.4): AM4, AM5, LGA1700, LGA1851
        Assert.Contains(seed.Chipsets, c => c.Name == "b650" && c.Socket == "am5");
        Assert.Contains(seed.Chipsets, c => c.Name == "b550" && c.Socket == "am4");
        Assert.Contains(seed.Chipsets, c => c.Name == "z790" && c.Socket == "lga1700");
        Assert.Contains(seed.Chipsets, c => c.Name == "z890" && c.Socket == "lga1851");
    }

    [Fact]
    public void Loader_Ryzen9000EmB650ExigeBios()
    {
        var seed = CompatibilitySeedLoader.Load();

        var b650 = seed.Find("b650");
        var zen5 = b650!.FindGeneration("zen5");

        Assert.NotNull(zen5);
        Assert.False(string.IsNullOrWhiteSpace(zen5!.RequiredBios)); // AGESA 1.2.0.2

        Assert.NotNull(b650.FindGeneration("zen4"));
        Assert.Null(b650.FindGeneration("zen4")!.RequiredBios);
    }

    [Fact]
    public void Find_NormalizaChipset()
    {
        var seed = TestBuilds.Am5Am4Seed();

        Assert.NotNull(seed.Find("B650"));
        Assert.NotNull(seed.Find("AMD B550"));
        Assert.NotNull(seed.Find("b650m"));   // variante M-ATX
        Assert.Null(seed.Find("h510"));       // fora da matriz
        Assert.Null(seed.Find(""));

        // sufixo E (variante premium): x870e → x870 no seed real
        var real = CompatibilitySeedLoader.Load();
        Assert.NotNull(real.Find("x670e"));
    }

    [Fact]
    public void Find_SufixoMDeChipsetForaDaMatriz_NaoEncontra()
    {
        var seed = TestBuilds.Am5Am4Seed();

        // "a620m" não existe no seed sintético — mas o fallback de sufixo exige
        // que o restante seja conhecido; aqui testamos o caminho contrário.
        Assert.Null(seed.Find("zzz9m"));
    }
}

using OpenPc.Domain.Compatibility;

namespace OpenPc.Domain.Tests;

public class CpuGenerationTests
{
    [Theory]
    [InlineData("amd 1200", "zen1")]
    [InlineData("amd 1600", "zen1")]
    [InlineData("amd 2700x", "zen2")]     // Zen+
    [InlineData("amd 3600", "zen2")]
    [InlineData("amd 4600g", "zen2")]     // Renoir
    [InlineData("amd 5700x", "zen3")]
    [InlineData("amd 5600gt", "zen3")]
    [InlineData("amd 5800x3d", "zen3")]
    [InlineData("amd 7600x", "zen4")]
    [InlineData("amd 7800x3d", "zen4")]
    [InlineData("amd 8600g", "zen4")]     // Phoenix APU
    [InlineData("amd 9600x", "zen5")]
    [InlineData("amd 9800x3d", "zen5")]
    public void Classify_Amd(string model, string expected)
    {
        Assert.Equal(expected, CpuGeneration.Classify(model));
    }

    [Theory]
    [InlineData("intel 12400f", "alder-lake")]
    [InlineData("intel 12900k", "alder-lake")]
    [InlineData("intel 13600k", "raptor-lake")]
    [InlineData("intel 14700k", "raptor-lake-refresh")]
    [InlineData("intel 265f", "arrow-lake")]      // Core Ultra 7 265F
    [InlineData("Core Ultra 9 285K", "arrow-lake")]
    [InlineData("Core i5-12400F", "alder-lake")]
    public void Classify_Intel(string model, string expected)
    {
        Assert.Equal(expected, CpuGeneration.Classify(model));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("nvidia 5070")]     // GPU
    [InlineData("intel 9400f")]     // LGA1151 — fora da matriz
    [InlineData("amd 6200")]        // Ryzen mobile
    [InlineData("intel 155h")]      // Core Ultra mobile (Meteor Lake)
    [InlineData("amd athlon 3000g")]            // tem 4 dígitos, mas não é Ryzen
    [InlineData("AMD A10-5800K, 3.8GHz, Quad Core, FM2, OEM")] // "5800" daria zen3
    [InlineData("AMD A10 9700 Bristol Ridge, 3.5GHz, AM4, DDR4")] // "9700" daria zen5
    [InlineData("amd fx-8350")]                 // "8350" daria zen4
    [InlineData("AMD A8-7600, 3.1GHz, FM2+")]
    [InlineData("amd sempron 145")]
    [InlineData("amd phenom ii x4 965")]
    [InlineData("Amd Ryzen Athlon 3000g Socket Am4 / 3.5ghz / 5mb - Oem")] // título com ruído
    public void Classify_ForaDaMatrizRetornaNull(string? model)
    {
        Assert.Null(CpuGeneration.Classify(model));
    }
}

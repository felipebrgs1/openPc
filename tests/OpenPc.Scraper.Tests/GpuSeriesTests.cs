using OpenPc.Scraper.Normalization;

namespace OpenPc.Scraper.Tests;

public class GpuSeriesTests
{
    [Theory]
    [InlineData("Placa De Video Gamer Nvidia Gtx 1060 6gb 192bits GDDR5 HDMI", null)] // GDDR5 — nem chegaria, mas não classifica
    [InlineData("Gamer Ninja NVIDIA GeForce RTX 2060 Ronin, 6GB, GDDR6", "rtx20")]
    [InlineData("Gamer Nvidia Geforce RTX 2070 Ronin, 8GB, Gddr6", "rtx20")]
    [InlineData("Placa De Video MSI NVIDIA GeForce RTX 3050 OC LP, 6GB, GDDR6", "rtx30")]
    [InlineData("SuperFrame NVIDIA GeForce RTX 3060 EPIC, 12GB, GDDR6", "rtx30")]
    [InlineData("Palit NVIDIA GeForce RTX 3080 Gamingpro, 10GB GDDR6X", "rtx30")]
    [InlineData("Nvidia Geforce Msi RTX5070ti 16gb Gddr7", "rtx50")]
    [InlineData("Palit Geforce RTX 5070 White Oc, 12gb, Gddr7", "rtx50")]
    [InlineData("Placa De Video Palit Geforce RTX 5080 Gamingpro 16gb", "rtx50")]
    [InlineData("Gpu Inno3d Geforce RTX 5090 X3 32gb 512-bit Gddr7", "rtx50")]
    [InlineData("Gamer Nvidia Geforce Gtx1660s, Gddr6 6gb", "gtx16")]
    [InlineData("PNY NVIDIA GeForce GTX 1650, 4GB GDDR6", "gtx16")]
    [InlineData("Asrock Rx 6400 Cli 4g 4gb Gddr6 64bits", "rx6000")]
    [InlineData("ASRock AMD Radeon, RX 6600 CLD 8G, 8GB, GDDR6", "rx6000")]
    [InlineData("Gpu Powercolor Amd Radeon Rx 6500xt 4gb Gddr6", "rx6000")]
    [InlineData("RX 6750 XT Mech 2x 12G V1 Radeon, 12GB, GDDR6", "rx6000")]
    [InlineData("Asrock Amd Radeon Rx 7600 Challenger Pro 8GB Oc Gddr6", "rx7000")]
    [InlineData("Xfx Speedster Swft 210 Radeon Rx 7700 Xt, 12gb, Gddr6", "rx7000")]
    [InlineData("Amd Radeon Rx 7900 Gre 16gb Gddr6 256bits - Xfx", "rx7000")]
    [InlineData("Placa De Video Amd Radeon 9070 Xt Xfx Quicksilver Gaming 16gb", "rx9000")]
    [InlineData("XFX Swift AMD Radeon RX 9060 XT OC Triple Fan, 16GB, GDDR6", "rx9000")]
    [InlineData("Rx 5600 Xt 6gb Gddr6 | HDMI Dvi | PCi Express Gamer", "rx5000")]
    [InlineData("Asrock Arc A750 Challenger D Oc, 8GB, Gddr6", "arc")]
    [InlineData("ASRock Intel Arc B580 Challenger 12G OC, 12GB, GDDR6", "arc")]
    [InlineData("Quadro Pny Nvidia RTX A400 4gb Gddr6 64 Bits", null)] // pro — fora do padrão
    [InlineData("PNY NVIDIA GeForce Quadro T400, 2GB GDDR6", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void Classify(string? title, string? expected)
    {
        Assert.Equal(expected, GpuSeries.Classify(title));
    }
}

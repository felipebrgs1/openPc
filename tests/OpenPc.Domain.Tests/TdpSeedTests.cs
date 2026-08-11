using OpenPc.Domain.Compatibility;
using OpenPc.Infrastructure.Compatibility;

namespace OpenPc.Domain.Tests;

public class TdpSeedTests
{
    [Fact]
    public void Loader_CarregaSeedRealDoRepositorio()
    {
        var seed = TdpSeedLoader.Load();

        Assert.NotEmpty(seed.Entries);
        Assert.Contains(seed.Entries, e => e.Category == PartCategory.Cpu && e.Target == TdpTarget.Model);
        Assert.Contains(seed.Entries, e => e.Category == PartCategory.Gpu && e.Target == TdpTarget.Name);
    }

    [Fact]
    public void Find_Ryzen75700EPorModelo()
    {
        var seed = TdpSeedLoader.Load();

        Assert.Equal(65m, seed.Find(PartCategory.Cpu, "amd 5700", "AMD Ryzen 7 5700, 3.7GHz (4.6GHz Turbo), 8-Cores 16-Threads, AM4"));
    }

    [Fact]
    public void Find_Rx9060Xt16gbPeloNome()
    {
        var seed = TdpSeedLoader.Load();

        // nome da loja real do usuário — sem acentos, sem pontuação
        var watts = seed.Find(
            PartCategory.Gpu,
            "amd 9060xt",
            "Asus AMD Radeon RX 9060 XT TUF Gaming OC, 16GB, GDDR6, FSR, Ray Tracing, 90YV0LF0-M0NA00");

        Assert.Equal(250m, watts);
    }

    [Fact]
    public void Find_VarianteAntesDaBase()
    {
        var seed = TdpSeedLoader.Load();

        Assert.Equal(220m, seed.Find(PartCategory.Gpu, "nvidia 4070", "Gigabyte GeForce RTX 4070 SUPER AERO OC 12G"));
        Assert.Equal(200m, seed.Find(PartCategory.Gpu, "nvidia 4070", "MSI GeForce RTX 4070 VENTUS 2X 12G"));
        Assert.Equal(285m, seed.Find(PartCategory.Gpu, "nvidia 4070", "ASUS TUF Gaming GeForce RTX 4070 TI SUPER OC 16GB"));
    }

    [Fact]
    public void Find_ModeloDesconhecido_RetornaNull()
    {
        var seed = TdpSeedLoader.Load();

        Assert.Null(seed.Find(PartCategory.Cpu, "amd 9999", "AMD Modelo Futuro 9999"));
        Assert.Null(seed.Find(PartCategory.Gpu, null, "Placa de vídeo genérica sem modelo"));
    }
}

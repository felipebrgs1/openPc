using OpenPc.Domain.Compatibility;
using OpenPc.Infrastructure.Compatibility;

namespace OpenPc.Domain.Tests;

public class WattageEstimatorTests
{
    [Fact]
    public void Estimate_BuildVazio_SoOverhead()
    {
        var estimate = WattageEstimator.Estimate(TestBuilds.Build());

        Assert.Equal(100m, estimate.BaseW);
        Assert.Equal(140m, estimate.RecommendedW);
    }

    [Fact]
    public void Estimate_Ryzen7600xMaisRtx4070()
    {
        // 7600X (105 W TDP) + RTX 4070 (200 W): base = 405 W, recomendado = 567 W
        // — alinhado com calculadoras de referência (Outervision ~550-600 W) dentro de ±10%.
        var build = TestBuilds.Build(
            TestBuilds.Cpu(("tdp_w", 105)),
            TestBuilds.Gpu(("tdp_w", 200)));

        var estimate = WattageEstimator.Estimate(build);

        Assert.Equal(405m, estimate.BaseW);
        Assert.Equal(567m, estimate.RecommendedW);
    }

    [Fact]
    public void Estimate_SemGpu_BaseSomaSoCpuEOverhead()
    {
        var build = TestBuilds.Build(TestBuilds.Cpu(("tdp_w", 65)));

        var estimate = WattageEstimator.Estimate(build);

        Assert.Equal(165m, estimate.BaseW);
    }

    [Fact]
    public void Estimate_SemTdpScrapado_UsaSeedCuradoPorModelo()
    {
        // Ryzen 7 5700 (65 W) + RX 9060 XT (250 W): título da loja não cita
        // TDP e a ficha técnica não é coletada — o seed cobre a lacuna.
        var build = TestBuilds.Build(
            TestBuilds.Cpu("amd 5700"),
            TestBuilds.Gpu("amd 9060xt", "Asus AMD Radeon RX 9060 XT TUF Gaming OC, 16GB, GDDR6, FSR, Ray Tracing"));

        var estimate = WattageEstimator.Estimate(build, TdpSeedLoader.Load());

        Assert.Equal(415m, estimate.BaseW);
        Assert.Equal(581m, estimate.RecommendedW);
        Assert.True(estimate.Known);
    }

    [Fact]
    public void Estimate_TdpScrapado_PreponderaSobreSeed()
    {
        var build = TestBuilds.Build(TestBuilds.Cpu("amd 5700", ("tdp_w", 105)));

        var estimate = WattageEstimator.Estimate(build, TdpSeedLoader.Load());

        Assert.Equal(205m, estimate.BaseW); // 105 do scraping, não 65 do seed
        Assert.True(estimate.Known);
    }

    [Fact]
    public void Estimate_NomeComVariante_DistingueSuperTi()
    {
        // MatchKey colapsa "rtx 4070 super/ti" em "nvidia 4070" — o nome
        // completo precisa prevalecer para o valor certo.
        var build = TestBuilds.Build(
            TestBuilds.Gpu("nvidia 4070", "Gigabyte GeForce RTX 4070 SUPER AERO OC 12G"));

        var estimate = WattageEstimator.Estimate(build, TdpSeedLoader.Load());

        Assert.Equal(320m, estimate.BaseW); // 220 (super) + 100 overhead
    }

    [Fact]
    public void Estimate_ModeloDesconhecido_MarcaComoDesconhecido()
    {
        var build = TestBuilds.Build(TestBuilds.Cpu("amd 9999", "AMD Modelo Futuro 9999"));

        var estimate = WattageEstimator.Estimate(build, TdpSeedLoader.Load());

        Assert.False(estimate.Known);
    }

    [Fact]
    public void Estimate_BuildVazio_EhConhecido()
    {
        // Sem CPU/GPU o baseline de overhead (100 W) é o próprio estimado.
        var estimate = WattageEstimator.Estimate(TestBuilds.Build());

        Assert.True(estimate.Known);
        Assert.Equal(100m, estimate.BaseW);
    }
}

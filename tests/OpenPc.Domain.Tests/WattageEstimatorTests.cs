using OpenPc.Domain.Compatibility;

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
}

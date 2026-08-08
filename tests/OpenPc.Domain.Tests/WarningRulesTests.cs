using OpenPc.Domain.Compatibility;
using OpenPc.Domain.Compatibility.Rules;

namespace OpenPc.Domain.Tests;

/// <summary>Regras Warning prioritárias (§4.3) — positivo, negativo e borda.</summary>
public class WarningRulesTests
{
    // ---------- PSU_WATTAGE_LOW ----------

    [Fact]
    public void PsuWattageLow_Fonte450wParaCpu105Gpu200_Warning()
    {
        // base = 105 + 200 + 100 = 405; recomendado = 567 → 450 < 567
        var build = TestBuilds.Build(
            TestBuilds.Cpu(("tdp_w", 105)),
            TestBuilds.Gpu(("tdp_w", 200)),
            TestBuilds.Psu(("wattage", 450)));

        var result = new PsuWattageLowRule().Evaluate(build);

        Assert.NotNull(result);
        Assert.Equal("PSU_WATTAGE_LOW", result!.Code);
        Assert.Equal(Severity.Warning, result.Severity);
    }

    [Fact]
    public void PsuWattageLow_Fonte600w_SemWarning()
    {
        var build = TestBuilds.Build(
            TestBuilds.Cpu(("tdp_w", 105)),
            TestBuilds.Gpu(("tdp_w", 200)),
            TestBuilds.Psu(("wattage", 600)));

        Assert.Null(new PsuWattageLowRule().Evaluate(build));
    }

    [Fact]
    public void PsuWattageLow_RecomendacaoDoFabricanteDaGpuPrepondera()
    {
        // gpu.recommended_psu_w (750) > estimativa (567) → 650 ainda é pouco
        var build = TestBuilds.Build(
            TestBuilds.Cpu(("tdp_w", 105)),
            TestBuilds.Gpu(("tdp_w", 200), ("recommended_psu_w", 750)),
            TestBuilds.Psu(("wattage", 650)));

        Assert.NotNull(new PsuWattageLowRule().Evaluate(build));
    }

    [Fact]
    public void PsuWattageLow_SemCpuEGpu_NaoAvalia()
    {
        var build = TestBuilds.Build(TestBuilds.Psu(("wattage", 300)));

        Assert.Null(new PsuWattageLowRule().Evaluate(build));
    }

    // ---------- NO_GPU_NO_IGPU ----------

    [Fact]
    public void NoGpuNoIgpu_CpuSemVideoIntegrado_Warning()
    {
        var build = TestBuilds.Build(TestBuilds.Cpu(("has_igpu", false)));

        var result = new NoGpuNoIgpuRule().Evaluate(build);

        Assert.NotNull(result);
        Assert.Equal("NO_GPU_NO_IGPU", result!.Code);
    }

    [Fact]
    public void NoGpuNoIgpu_CpuComVideoIntegrado_SemWarning()
    {
        var build = TestBuilds.Build(TestBuilds.Cpu(("has_igpu", true)));

        Assert.Null(new NoGpuNoIgpuRule().Evaluate(build));
    }

    [Fact]
    public void NoGpuNoIgpu_ComGpu_SemWarning()
    {
        var build = TestBuilds.Build(
            TestBuilds.Cpu(("has_igpu", false)),
            TestBuilds.Gpu(("tdp_w", 200)));

        Assert.Null(new NoGpuNoIgpuRule().Evaluate(build));
    }

    [Fact]
    public void NoGpuNoIgpu_VideoIntegradoDesconhecido_NaoAvisa()
    {
        var build = TestBuilds.Build(TestBuilds.Cpu());

        Assert.Null(new NoGpuNoIgpuRule().Evaluate(build));
    }

    // ---------- BIOS_UPDATE_NEEDED ----------

    [Fact]
    public void BiosUpdateNeeded_Ryzen9000EmB650_Warning()
    {
        // b650: zen5 exige AGESA 1.2.0.2
        var build = TestBuilds.Build(
            TestBuilds.Cpu("amd 9600x", ("socket", "am5")),
            TestBuilds.Mobo(("socket", "am5"), ("chipset", "b650")));

        var result = new BiosUpdateNeededRule(TestBuilds.Am5Am4Seed()).Evaluate(build);

        Assert.NotNull(result);
        Assert.Equal("BIOS_UPDATE_NEEDED", result!.Code);
        Assert.Equal(Severity.Warning, result.Severity);
    }

    [Fact]
    public void BiosUpdateNeeded_Ryzen7000EmB650_SemWarning()
    {
        var build = TestBuilds.Build(
            TestBuilds.Cpu("amd 7600", ("socket", "am5")),
            TestBuilds.Mobo(("socket", "am5"), ("chipset", "b650")));

        Assert.Null(new BiosUpdateNeededRule(TestBuilds.Am5Am4Seed()).Evaluate(build));
    }

    [Fact]
    public void BiosUpdateNeeded_GeracaoNaoSuportada_NaoAvisa()
    {
        // zen5 fora da matriz do b550 → erro CPU_CHIPSET_UNSUPPORTED, não warning
        var build = TestBuilds.Build(
            TestBuilds.Cpu("amd 9600x", ("socket", "am4")),
            TestBuilds.Mobo(("socket", "am4"), ("chipset", "b550")));

        Assert.Null(new BiosUpdateNeededRule(TestBuilds.Am5Am4Seed()).Evaluate(build));
    }

    // ---------- RAM_SPEED_CAPPED ----------

    [Fact]
    public void RamSpeedCapped_6000MhzCpuSuporta5200_Warning()
    {
        var build = TestBuilds.Build(
            TestBuilds.Memory(("speed_mhz", 6000)),
            TestBuilds.Cpu(("max_memory_speed", 5200)));

        var result = new RamSpeedCappedRule().Evaluate(build);

        Assert.NotNull(result);
        Assert.Equal("RAM_SPEED_CAPPED", result!.Code);
        Assert.Equal(Severity.Warning, result.Severity);
    }

    [Fact]
    public void RamSpeedCapped_VelocidadeIgualAoLimite_SemWarning()
    {
        var build = TestBuilds.Build(
            TestBuilds.Memory(("speed_mhz", 5200)),
            TestBuilds.Cpu(("max_memory_speed", 5200)));

        Assert.Null(new RamSpeedCappedRule().Evaluate(build));
    }

    [Fact]
    public void RamSpeedCapped_AbaixoDoLimite_SemWarning()
    {
        var build = TestBuilds.Build(
            TestBuilds.Memory(("speed_mhz", 4800)),
            TestBuilds.Cpu(("max_memory_speed", 5200)));

        Assert.Null(new RamSpeedCappedRule().Evaluate(build));
    }
}

using OpenPc.Domain.Compatibility;

namespace OpenPc.Domain.Tests;

/// <summary>Execução integrada do executor — cenários de aceite do M3 (docs/roadmap.md).</summary>
public class CompatibilityEngineTests
{
    [Fact]
    public void BuildVazio_NenhumProblema()
    {
        var evaluation = TestBuilds.Engine().Evaluate(TestBuilds.Build());

        Assert.Empty(evaluation.Issues);
        Assert.False(evaluation.HasErrors);
    }

    /// <summary>Critério de aceite: Ryzen AM5 + placa AM4 → CPU_SOCKET_MISMATCH.</summary>
    [Fact]
    public void BuildRyzenAm5EmPlacaAm4_ErroCpuSocketMismatch()
    {
        var build = TestBuilds.Build(
            TestBuilds.Cpu("amd 7600", ("socket", "am5"), ("tdp_w", 65), ("has_igpu", true)),
            TestBuilds.Mobo(("socket", "am4"), ("chipset", "b550"), ("memory_type", "ddr4"), ("memory_slots", 4), ("max_memory_gb", 128), ("m2_slots", 2)));

        var evaluation = TestBuilds.Engine().Evaluate(build);

        Assert.True(evaluation.HasErrors);
        var error = Assert.Single(evaluation.Errors);
        Assert.Equal("CPU_SOCKET_MISMATCH", error.Code);
    }

    [Fact]
    public void BuildAm5Compativel_SemErros()
    {
        var build = TestBuilds.Build(
            TestBuilds.Cpu("amd 7600", ("socket", "am5"), ("tdp_w", 65), ("has_igpu", true), ("max_memory_speed", 5200)),
            TestBuilds.Mobo(("socket", "am5"), ("chipset", "b650"), ("memory_type", "ddr5"), ("memory_slots", 4), ("max_memory_gb", 128), ("m2_slots", 2), ("form_factor", "atx")),
            TestBuilds.Memory(("type", "ddr5"), ("modules", 2), ("capacity_gb", 32), ("speed_mhz", 5200)),
            TestBuilds.Gpu(("tdp_w", 200), ("length_mm", 300), ("power_connectors", "2x8 pinos")),
            TestBuilds.Psu(("wattage", 650), ("connectors", "2x8 pinos")),
            TestBuilds.Chassis(("supported_form_factors", new[] { "atx", "matx" }), ("max_gpu_length_mm", 350), ("max_cooler_height_mm", 170)),
            TestBuilds.Cooler(("type", "air"), ("height_mm", 150), ("socket_support", new[] { "am5" })),
            TestBuilds.Storage(("interface", "nvme")));

        var evaluation = TestBuilds.Engine().Evaluate(build);

        Assert.False(evaluation.HasErrors, string.Join("; ", evaluation.Errors.Select(e => e.Code)));
    }

    [Fact]
    public void With_TrocaDePecaHipotetica()
    {
        var build = TestBuilds.Build(
            TestBuilds.Cpu("amd 7600", ("socket", "am5")),
            TestBuilds.Mobo(("socket", "am4"), ("chipset", "b550")));

        var engine = TestBuilds.Engine();
        Assert.True(engine.Evaluate(build).HasErrors);

        var placaAm5 = TestBuilds.Mobo(("socket", "am5"), ("chipset", "b650"));
        Assert.False(engine.Evaluate(build.With(placaAm5)).HasErrors);
    }

    [Fact]
    public void SeveridadeSeparada_ErrorsEWarningsNaoSeMisturam()
    {
        // sem GPU + CPU sem vídeo (warning) + socket errado (error)
        var build = TestBuilds.Build(
            TestBuilds.Cpu("amd 7600", ("socket", "am5"), ("has_igpu", false)),
            TestBuilds.Mobo(("socket", "am4")));

        var evaluation = TestBuilds.Engine().Evaluate(build);

        Assert.Contains(evaluation.Errors, e => e.Code == "CPU_SOCKET_MISMATCH");
        Assert.Contains(evaluation.Warnings, e => e.Code == "NO_GPU_NO_IGPU");
    }

    [Fact]
    public void SpecDesconhecida_NuncaGeraErroFalso()
    {
        // placa sem atributos (ex: antes do scrape de specs) — engine silenciosa
        var build = TestBuilds.Build(
            TestBuilds.Cpu("amd 7600", ("socket", "am5")),
            TestBuilds.Mobo());

        var evaluation = TestBuilds.Engine().Evaluate(build);

        Assert.False(evaluation.HasErrors);
    }
}

namespace OpenPc.Domain.Compatibility.Rules;

/// <summary>Erro §4.2: gpu.length_mm &gt; case.max_gpu_length_mm (igual cabe).</summary>
public sealed class GpuCaseLengthRule : ICompatibilityRule
{
    public string Code => "GPU_CASE_LENGTH";

    public CompatibilityResult? Evaluate(BuildSnapshot build)
    {
        var gpu = build.Part(PartCategory.Gpu);
        var chassis = build.Part(PartCategory.Case);
        if (gpu is null || chassis is null)
            return null;

        var length = gpu.Num("length_mm");
        var max = chassis.Num("max_gpu_length_mm");
        if (length is null || max is null)
            return null;

        if (length <= max)
            return null;

        return new CompatibilityResult(Severity.Error, Code,
            $"Placa de vídeo com {length:0} mm não cabe no gabinete (máx {max:0} mm).",
            [gpu.ProductId, chassis.ProductId]);
    }
}

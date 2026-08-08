namespace OpenPc.Domain.Compatibility.Rules;

/// <summary>Warning §4.3: build sem GPU e cpu.has_igpu = false → sem saída de vídeo.</summary>
public sealed class NoGpuNoIgpuRule : ICompatibilityRule
{
    public string Code => "NO_GPU_NO_IGPU";

    public CompatibilityResult? Evaluate(BuildSnapshot build)
    {
        if (build.Part(PartCategory.Gpu) is not null)
            return null;

        var cpu = build.Part(PartCategory.Cpu);
        if (cpu is null)
            return null;

        // desconhecido ≠ sem vídeo: só avisa quando confirmado false.
        if (cpu.Bool("has_igpu") != false)
            return null;

        return new CompatibilityResult(Severity.Warning, Code,
            "Build sem placa de vídeo e processador sem vídeo integrado — o PC não terá saída de vídeo.",
            [cpu.ProductId]);
    }
}

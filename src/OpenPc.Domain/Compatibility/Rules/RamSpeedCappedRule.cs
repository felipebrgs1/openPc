namespace OpenPc.Domain.Compatibility.Rules;

/// <summary>Warning §4.3: memory.speed_mhz &gt; cpu.max_memory_speed → funciona, mas capado.</summary>
public sealed class RamSpeedCappedRule : ICompatibilityRule
{
    public string Code => "RAM_SPEED_CAPPED";

    public CompatibilityResult? Evaluate(BuildSnapshot build)
    {
        var memories = build.PartsOf(PartCategory.Memory);
        var cpu = build.Part(PartCategory.Cpu);
        if (memories.Count == 0 || cpu is null)
            return null;

        var max = cpu.Num("max_memory_speed");
        if (max is null)
            return null;

        var capped = memories
            .Where(m => m.Num("speed_mhz") is decimal s && s > max)
            .ToArray();
        if (capped.Length == 0)
            return null;

        var ids = capped.Select(m => m.ProductId).Append(cpu.ProductId).ToArray();
        return new CompatibilityResult(Severity.Warning, Code,
            $"Memória opera abaixo da velocidade nominal — CPU suporta até {max:0} MHz.",
            ids);
    }
}

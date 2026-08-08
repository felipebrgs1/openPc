namespace OpenPc.Domain.Compatibility.Rules;

/// <summary>Erro §4.2: memory.capacity_gb > motherboard.max_memory_gb.</summary>
public sealed class RamCapacityExceededRule : ICompatibilityRule
{
    public string Code => "RAM_CAPACITY_EXCEEDED";

    public CompatibilityResult? Evaluate(BuildSnapshot build)
    {
        var memory = build.Part(PartCategory.Memory);
        var mobo = build.Part(PartCategory.Motherboard);
        if (memory is null || mobo is null)
            return null;

        var capacity = memory.Num("capacity_gb");
        var max = mobo.Num("max_memory_gb");
        if (capacity is null || max is null)
            return null;

        if (capacity <= max)
            return null;

        return new CompatibilityResult(Severity.Error, Code,
            $"Capacidade de memória ({capacity:0} GB) excede o máximo da placa-mãe ({max:0} GB).",
            [memory.ProductId, mobo.ProductId]);
    }
}

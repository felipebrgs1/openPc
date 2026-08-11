namespace OpenPc.Domain.Compatibility.Rules;

/// <summary>Erro §4.2: soma de memory.capacity_gb (todos os módulos/kits) &gt; motherboard.max_memory_gb.</summary>
public sealed class RamCapacityExceededRule : ICompatibilityRule
{
    public string Code => "RAM_CAPACITY_EXCEEDED";

    public CompatibilityResult? Evaluate(BuildSnapshot build)
    {
        var memories = build.PartsOf(PartCategory.Memory);
        var mobo = build.Part(PartCategory.Motherboard);
        if (memories.Count == 0 || mobo is null)
            return null;

        var max = mobo.Num("max_memory_gb");
        if (max is null)
            return null;

        var total = 0m;
        var withCapacity = new List<PartSpec>();
        foreach (var memory in memories)
        {
            var capacity = memory.Num("capacity_gb");
            if (capacity is null)
                continue;
            total += capacity.Value;
            withCapacity.Add(memory);
        }
        if (withCapacity.Count == 0 || total <= max)
            return null;

        var ids = withCapacity.Select(m => m.ProductId).Append(mobo.ProductId).ToArray();
        return new CompatibilityResult(Severity.Error, Code,
            $"Capacidade total de memória ({total:0} GB) excede o máximo da placa-mãe ({max:0} GB).",
            ids);
    }
}

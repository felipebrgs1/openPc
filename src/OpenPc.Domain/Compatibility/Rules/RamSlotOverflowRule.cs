namespace OpenPc.Domain.Compatibility.Rules;

/// <summary>Erro §4.2: módulos de memória (1+ kits) &gt; motherboard.memory_slots.</summary>
public sealed class RamSlotOverflowRule : ICompatibilityRule
{
    public string Code => "RAM_SLOT_OVERFLOW";

    public CompatibilityResult? Evaluate(BuildSnapshot build)
    {
        var mobo = build.Part(PartCategory.Motherboard);
        var slots = mobo?.Num("memory_slots");
        if (slots is null)
            return null;

        var memoryParts = build.PartsOf(PartCategory.Memory);
        var modules = memoryParts
            .Select(m => m.Num("modules"))
            .Where(m => m is not null)
            .Sum(m => m!.Value);
        if (modules == 0)
            return null;

        if (modules <= slots)
            return null;

        var ids = memoryParts.Select(m => m.ProductId).Append(mobo!.ProductId).ToArray();
        return new CompatibilityResult(Severity.Error, Code,
            $"{modules:0} módulos de memória excedem os {slots:0} slots da placa-mãe.",
            ids);
    }
}

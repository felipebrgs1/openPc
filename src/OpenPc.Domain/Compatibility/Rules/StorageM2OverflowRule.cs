namespace OpenPc.Domain.Compatibility.Rules;

/// <summary>Erro §4.2: nº de SSDs NVMe &gt; motherboard.m2_slots.</summary>
public sealed class StorageM2OverflowRule : ICompatibilityRule
{
    public string Code => "STORAGE_M2_OVERFLOW";

    public CompatibilityResult? Evaluate(BuildSnapshot build)
    {
        var mobo = build.Part(PartCategory.Motherboard);
        var slots = mobo?.Num("m2_slots");
        if (slots is null)
            return null;

        var nvme = build.PartsOf(PartCategory.Storage)
            .Where(s => string.Equals(s.Str("interface"), "nvme", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (nvme.Length == 0)
            return null;

        if (nvme.Length <= slots)
            return null;

        var ids = nvme.Select(s => s.ProductId).Append(mobo!.ProductId).ToArray();
        return new CompatibilityResult(Severity.Error, Code,
            $"{nvme.Length} SSDs NVMe excedem os {slots:0} slots M.2 da placa-mãe.",
            ids);
    }
}

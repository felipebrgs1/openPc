namespace OpenPc.Domain.Compatibility.Rules;

/// <summary>Erro §4.2: qualquer memory.type != motherboard.memory_type (ddr4|ddr5|ambos).</summary>
public sealed class RamTypeMismatchRule : ICompatibilityRule
{
    public string Code => "RAM_TYPE_MISMATCH";

    public CompatibilityResult? Evaluate(BuildSnapshot build)
    {
        var memories = build.PartsOf(PartCategory.Memory);
        var mobo = build.Part(PartCategory.Motherboard);
        if (memories.Count == 0 || mobo is null)
            return null;

        var moboType = mobo.Str("memory_type");
        if (moboType is null)
            return null;

        if (moboType == "ambos")
            return null;

        var mismatched = memories
            .Where(m => m.Str("type") is string t
                && !string.Equals(t, moboType, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (mismatched.Length == 0)
            return null;

        var types = string.Join("/", mismatched
            .Select(m => m.Str("type"))
            .Distinct(StringComparer.OrdinalIgnoreCase));
        var ids = mismatched.Select(m => m.ProductId).Append(mobo.ProductId).ToArray();
        return new CompatibilityResult(Severity.Error, Code,
            $"Memória {types} incompatível com a placa-mãe ({moboType}).",
            ids);
    }
}

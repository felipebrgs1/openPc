namespace OpenPc.Domain.Compatibility.Rules;

/// <summary>Erro §4.2: memory.type != motherboard.memory_type (ddr4|ddr5|ambos).</summary>
public sealed class RamTypeMismatchRule : ICompatibilityRule
{
    public string Code => "RAM_TYPE_MISMATCH";

    public CompatibilityResult? Evaluate(BuildSnapshot build)
    {
        var memory = build.Part(PartCategory.Memory);
        var mobo = build.Part(PartCategory.Motherboard);
        if (memory is null || mobo is null)
            return null;

        var type = memory.Str("type");
        var moboType = mobo.Str("memory_type");
        if (type is null || moboType is null)
            return null;

        if (moboType == "ambos" || string.Equals(type, moboType, StringComparison.OrdinalIgnoreCase))
            return null;

        return new CompatibilityResult(Severity.Error, Code,
            $"Memória {type} incompatível com a placa-mãe ({moboType}).",
            [memory.ProductId, mobo.ProductId]);
    }
}

namespace OpenPc.Domain.Compatibility;

/// <summary>Slots do build (docs/specs.md §7.2 — 8 slots, CPU → cooler).</summary>
public enum PartCategory
{
    Cpu,
    Motherboard,
    Gpu,
    Memory,
    Storage,
    Psu,
    Case,
    Cooler,
}

public static class PartCategorySlugs
{
    public static string ToSlug(this PartCategory category) => category switch
    {
        PartCategory.Cpu => "cpu",
        PartCategory.Motherboard => "motherboard",
        PartCategory.Gpu => "gpu",
        PartCategory.Memory => "memory",
        PartCategory.Storage => "storage",
        PartCategory.Psu => "psu",
        PartCategory.Case => "case",
        PartCategory.Cooler => "cooler",
        _ => throw new ArgumentOutOfRangeException(nameof(category)),
    };

    public static PartCategory? FromSlug(string? slug) => slug?.ToLowerInvariant() switch
    {
        "cpu" => PartCategory.Cpu,
        "motherboard" => PartCategory.Motherboard,
        "gpu" => PartCategory.Gpu,
        "memory" => PartCategory.Memory,
        "storage" => PartCategory.Storage,
        "psu" => PartCategory.Psu,
        "case" => PartCategory.Case,
        "cooler" => PartCategory.Cooler,
        _ => null,
    };
}

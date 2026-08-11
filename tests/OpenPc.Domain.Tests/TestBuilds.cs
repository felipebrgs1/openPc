using System.Text.Json;
using OpenPc.Domain.Compatibility;
using Rules = OpenPc.Domain.Compatibility.Rules;

namespace OpenPc.Domain.Tests;

/// <summary>Fábricas de teste para PartSpec/BuildSnapshot/CompatibilitySeed.</summary>
internal static class TestBuilds
{
    public static PartSpec Part(PartCategory category, params (string Key, object? Value)[] attrs) =>
        Part(category, Guid.NewGuid(), "Modelo", attrs);

    public static PartSpec Part(PartCategory category, Guid id, params (string Key, object? Value)[] attrs) =>
        Part(category, id, "Modelo", attrs);

    public static PartSpec Part(PartCategory category, Guid id, string model, params (string Key, object? Value)[] attrs) =>
        Part(category, id, model, "Peça de teste", attrs);

    public static PartSpec Part(PartCategory category, Guid id, string model, string name, params (string Key, object? Value)[] attrs)
    {
        var dict = new Dictionary<string, AttrValue>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in attrs)
        {
            dict[key] = value switch
            {
                string s => new AttrValue(s, null, null),
                int i => new AttrValue(null, i, null),
                decimal d => new AttrValue(null, d, null),
                bool b => new AttrValue(null, null, b),
                string[] arr => new AttrValue(JsonSerializer.Serialize(arr), null, null),
                int[] arr => new AttrValue(JsonSerializer.Serialize(arr), null, null),
                null => default,
                _ => throw new ArgumentException($"Valor de atributo não suportado: {value?.GetType()}"),
            };
        }

        return new PartSpec
        {
            ProductId = id,
            Category = category,
            Brand = "Marca",
            Model = model,
            Name = name,
            Attributes = dict,
        };
    }

    /// <summary>CPU com modelo real (match key) — necessário para as regras de geração (BIOS/chipset).</summary>
    public static PartSpec Cpu(string model, params (string Key, object? Value)[] attrs) =>
        Part(PartCategory.Cpu, Guid.NewGuid(), model, attrs);

    public static PartSpec Cpu(string model, string name, params (string Key, object? Value)[] attrs) =>
        Part(PartCategory.Cpu, Guid.NewGuid(), model, name, attrs);

    public static PartSpec Cpu(params (string Key, object? Value)[] attrs) =>
        Part(PartCategory.Cpu, attrs);

    public static PartSpec Mobo(params (string Key, object? Value)[] attrs) =>
        Part(PartCategory.Motherboard, attrs);

    public static PartSpec Gpu(params (string Key, object? Value)[] attrs) =>
        Part(PartCategory.Gpu, attrs);

    public static PartSpec Gpu(string model, string name, params (string Key, object? Value)[] attrs) =>
        Part(PartCategory.Gpu, Guid.NewGuid(), model, name, attrs);

    public static PartSpec Memory(params (string Key, object? Value)[] attrs) =>
        Part(PartCategory.Memory, attrs);

    public static PartSpec Storage(params (string Key, object? Value)[] attrs) =>
        Part(PartCategory.Storage, attrs);

    public static PartSpec Psu(params (string Key, object? Value)[] attrs) =>
        Part(PartCategory.Psu, attrs);

    public static PartSpec Chassis(params (string Key, object? Value)[] attrs) =>
        Part(PartCategory.Case, attrs);

    public static PartSpec Cooler(params (string Key, object? Value)[] attrs) =>
        Part(PartCategory.Cooler, attrs);

    public static BuildSnapshot Build(params PartSpec[] parts) =>
        new() { BuildId = Guid.NewGuid(), Slug = "teste", Parts = parts };

    public static CompatibilitySeed Seed(params (string Name, string Socket, (string Id, string? Bios)[] Gens)[] chipsets) =>
        new()
        {
            Chipsets = chipsets
                .Select(c => new ChipsetSupport
                {
                    Name = c.Name,
                    Socket = c.Socket,
                    Generations = c.Gens
                        .Select(g => new GenerationSupport { Id = g.Id, RequiredBios = g.Bios })
                        .ToArray(),
                })
                .ToArray(),
        };

    /// <summary>Seed sintético com os 4 soquetes da matriz (AM4/AM5/LGA1700/LGA1851).</summary>
    public static CompatibilitySeed Am5Am4Seed() => Seed(
        ("b650", "am5", [("zen4", null), ("zen5", "AGESA 1.2.0.2")]),
        ("b550", "am4", [("zen2", null), ("zen3", null)]),
        ("z790", "lga1700", [("alder-lake", null), ("raptor-lake", null), ("raptor-lake-refresh", null)]),
        ("z890", "lga1851", [("arrow-lake", null)]));

    /// <summary>Engine com todas as 16 regras (mesma ordem/registro da DI de produção).</summary>
    public static CompatibilityEngine Engine(CompatibilitySeed? seed = null)
    {
        seed ??= Am5Am4Seed();
        return new CompatibilityEngine(
        [
            new Rules.CpuSocketMismatchRule(),
            new Rules.CpuChipsetUnsupportedRule(seed),
            new Rules.RamTypeMismatchRule(),
            new Rules.RamCapacityExceededRule(),
            new Rules.RamSlotOverflowRule(),
            new Rules.MoboCaseFormFactorRule(),
            new Rules.GpuCaseLengthRule(),
            new Rules.CoolerSocketMismatchRule(),
            new Rules.CoolerCaseHeightRule(),
            new Rules.AioRadiatorFitRule(),
            new Rules.StorageM2OverflowRule(),
            new Rules.PsuConnectorMissingRule(),
            new Rules.PsuWattageLowRule(),
            new Rules.NoGpuNoIgpuRule(),
            new Rules.BiosUpdateNeededRule(seed),
            new Rules.RamSpeedCappedRule(),
        ]);
    }
}

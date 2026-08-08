namespace OpenPc.Domain.Compatibility.Rules;

/// <summary>Erro §4.2: cooler.height_mm &gt; case.max_cooler_height_mm (só air cooler).</summary>
public sealed class CoolerCaseHeightRule : ICompatibilityRule
{
    public string Code => "COOLER_CASE_HEIGHT";

    public CompatibilityResult? Evaluate(BuildSnapshot build)
    {
        var cooler = build.Part(PartCategory.Cooler);
        var chassis = build.Part(PartCategory.Case);
        if (cooler is null || chassis is null)
            return null;

        // AIO não usa altura de air cooler (o radiador é coberto por AIO_RADIATOR_FIT).
        if (string.Equals(cooler.Str("type"), "aio", StringComparison.OrdinalIgnoreCase))
            return null;

        var height = cooler.Num("height_mm");
        var max = chassis.Num("max_cooler_height_mm");
        if (height is null || max is null)
            return null;

        if (height <= max)
            return null;

        return new CompatibilityResult(Severity.Error, Code,
            $"Cooler com {height:0} mm de altura não cabe no gabinete (máx {max:0} mm).",
            [cooler.ProductId, chassis.ProductId]);
    }
}

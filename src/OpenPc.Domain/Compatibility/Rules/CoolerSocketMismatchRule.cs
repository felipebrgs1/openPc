namespace OpenPc.Domain.Compatibility.Rules;

/// <summary>Erro §4.2: cpu.socket ∉ cooler.socket_support.</summary>
public sealed class CoolerSocketMismatchRule : ICompatibilityRule
{
    public string Code => "COOLER_SOCKET_MISMATCH";

    public CompatibilityResult? Evaluate(BuildSnapshot build)
    {
        var cpu = build.Part(PartCategory.Cpu);
        var cooler = build.Part(PartCategory.Cooler);
        if (cpu is null || cooler is null)
            return null;

        var socket = cpu.Str("socket");
        var supported = cooler.StrList("socket_support");
        if (socket is null || supported.Count == 0)
            return null;

        if (supported.Any(s => string.Equals(s, socket, StringComparison.OrdinalIgnoreCase)))
            return null;

        return new CompatibilityResult(Severity.Error, Code,
            $"Cooler não suporta o socket {socket} do processador.",
            [cpu.ProductId, cooler.ProductId]);
    }
}

namespace OpenPc.Domain.Compatibility.Rules;

/// <summary>Erro §4.2: cpu.socket != motherboard.socket.</summary>
public sealed class CpuSocketMismatchRule : ICompatibilityRule
{
    public string Code => "CPU_SOCKET_MISMATCH";

    public CompatibilityResult? Evaluate(BuildSnapshot build)
    {
        var cpu = build.Part(PartCategory.Cpu);
        var mobo = build.Part(PartCategory.Motherboard);
        if (cpu is null || mobo is null)
            return null;

        var cpuSocket = cpu.Str("socket");
        var moboSocket = mobo.Str("socket");
        if (cpuSocket is null || moboSocket is null)
            return null; // spec desconhecida nunca vira erro falso

        if (string.Equals(cpuSocket, moboSocket, StringComparison.OrdinalIgnoreCase))
            return null;

        return new CompatibilityResult(Severity.Error, Code,
            $"Socket do processador ({cpuSocket}) incompatível com a placa-mãe ({moboSocket}).",
            [cpu.ProductId, mobo.ProductId]);
    }
}

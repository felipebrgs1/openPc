using System.Text.Json;
using OpenPc.Domain.Compatibility;

namespace OpenPc.Infrastructure.Compatibility;

/// <summary>
/// Carrega o seed curado de consumo (docs/specs.md §4.4) a partir de
/// Infrastructure/Seeds/tdp.json, copiado para a saída pelos csproj que
/// consomem a engine (API, testes).
/// </summary>
public static class TdpSeedLoader
{
    public static TdpSeed Load()
    {
        var baseDir = AppContext.BaseDirectory;
        var path = new[] { "Seeds/tdp.json", "tdp.json" }
            .Select(p => Path.Combine(baseDir, p))
            .FirstOrDefault(File.Exists)
            ?? throw new FileNotFoundException(
                "Seed de consumo não encontrado. Esperado em Infrastructure/Seeds/tdp.json (copiado para a saída do build).");

        var dto = JsonSerializer.Deserialize<SeedFileDto>(File.ReadAllText(path),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException("tdp.json vazio ou inválido.");

        return new TdpSeed
        {
            Entries = dto.Entries
                .Select(e => new TdpEntry(
                    ParseCategory(e.Category),
                    ParseTarget(e.Target),
                    e.Pattern,
                    e.Watts))
                .ToArray(),
        };
    }

    private static PartCategory ParseCategory(string value) => value switch
    {
        "cpu" => PartCategory.Cpu,
        "gpu" => PartCategory.Gpu,
        _ => throw new InvalidDataException($"tdp.json: categoria desconhecida '{value}'."),
    };

    private static TdpTarget ParseTarget(string value) => value switch
    {
        "name" => TdpTarget.Name,
        "model" => TdpTarget.Model,
        _ => throw new InvalidDataException($"tdp.json: target desconhecido '{value}'."),
    };

    private sealed class SeedFileDto
    {
        public TdpEntryDto[] Entries { get; init; } = [];
    }

    private sealed class TdpEntryDto
    {
        public string Category { get; init; } = "";
        public string Target { get; init; } = "";
        public string Pattern { get; init; } = "";
        public decimal Watts { get; init; }
    }
}

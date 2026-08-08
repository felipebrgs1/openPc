using System.Text.Json;
using OpenPc.Domain.Compatibility;

namespace OpenPc.Infrastructure.Compatibility;

/// <summary>
/// Carrega o seed curado de compatibilidade (docs/specs.md §4.4) a partir de
/// Infrastructure/Seeds/compatibility.json, copiado para a saída pelos csproj
/// que consomem a engine (API, testes).
/// </summary>
public static class CompatibilitySeedLoader
{
    public static CompatibilitySeed Load()
    {
        var baseDir = AppContext.BaseDirectory;
        var path = new[] { "Seeds/compatibility.json", "compatibility.json" }
            .Select(p => Path.Combine(baseDir, p))
            .FirstOrDefault(File.Exists)
            ?? throw new FileNotFoundException(
                "Seed de compatibilidade não encontrado. Esperado em Infrastructure/Seeds/compatibility.json (copiado para a saída do build).");

        var dto = JsonSerializer.Deserialize<SeedFileDto>(File.ReadAllText(path),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException("compatibility.json vazio ou inválido.");

        return new CompatibilitySeed
        {
            Chipsets = dto.Chipsets
                .Select(c => new ChipsetSupport
                {
                    Name = c.Name,
                    Socket = c.Socket,
                    Generations = c.Generations
                        .Select(g => new GenerationSupport { Id = g.Id, RequiredBios = g.Bios })
                        .ToArray(),
                })
                .ToArray(),
        };
    }

    private sealed class SeedFileDto
    {
        public ChipsetDto[] Chipsets { get; init; } = [];
    }

    private sealed class ChipsetDto
    {
        public string Name { get; init; } = "";
        public string Socket { get; init; } = "";
        public GenerationDto[] Generations { get; init; } = [];
    }

    private sealed class GenerationDto
    {
        public string Id { get; init; } = "";
        public string? Bios { get; init; }
    }
}

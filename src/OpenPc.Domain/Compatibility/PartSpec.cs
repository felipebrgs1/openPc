using System.Globalization;
using System.Text.Json;

namespace OpenPc.Domain.Compatibility;

/// <summary>Valor de um atributo EAV (docs/specs.md §3.2) — no máximo um dos campos preenchido.</summary>
public readonly record struct AttrValue(string? Text, decimal? Num, bool? Bool);

/// <summary>
/// Uma peça do build com suas specs, como vista pela engine. Construída a
/// partir de Product + ProductAttributes pela camada de aplicação — a engine
/// em si é pura (sem I/O).
/// </summary>
public sealed class PartSpec
{
    public required Guid ProductId { get; init; }
    public required PartCategory Category { get; init; }
    public required string Brand { get; init; }
    public required string Model { get; init; }
    public required string Name { get; init; }
    public required IReadOnlyDictionary<string, AttrValue> Attributes { get; init; }

    public string? Str(string key) =>
        Attributes.TryGetValue(key, out var v) ? v.Text : null;

    public decimal? Num(string key) =>
        Attributes.TryGetValue(key, out var v) ? v.Num : null;

    public bool? Bool(string key) =>
        Attributes.TryGetValue(key, out var v) ? v.Bool : null;

    /// <summary>Lista a partir de ValueText: array JSON ou lista separada por vírgula.</summary>
    public IReadOnlyList<string> StrList(string key)
    {
        var text = Str(key);
        if (string.IsNullOrWhiteSpace(text))
            return [];

        text = text.Trim();
        if (text.StartsWith('['))
        {
            try
            {
                return JsonSerializer.Deserialize<string[]>(text) ?? [];
            }
            catch (JsonException)
            {
                return [];
            }
        }

        return text.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();
    }

    /// <summary>Lista numérica a partir de ValueText: array JSON ou lista separada por vírgula.</summary>
    public IReadOnlyList<decimal> NumList(string key)
    {
        var text = Str(key);
        if (string.IsNullOrWhiteSpace(text))
            return [];

        text = text.Trim();
        if (text.StartsWith('['))
        {
            try
            {
                return JsonSerializer.Deserialize<decimal[]>(text) ?? [];
            }
            catch (JsonException)
            {
                return [];
            }
        }

        return text.Split(',')
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .Select(s => decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var n) ? n : (decimal?)null)
            .Where(n => n is not null)
            .Select(n => n!.Value)
            .ToArray();
    }
}

namespace OpenPc.Scraper.Collectors;

/// <summary>
/// Oferta bruta extraída de uma loja, já com specs normalizadas.
/// Um listing SEM `PartNumber` e SEM `MatchKey` ainda é persistido — o
/// dedup cria produto próprio e a fila de revisão sinaliza para correção.
/// </summary>
public sealed record RawListing(
    string StoreSku,
    string Title,
    string Url,
    decimal? PriceCash,
    decimal? PriceCard,
    int? Installments,
    string? InstallmentText,
    bool InStock,
    string? Thumbnail,
    string? Manufacturer,
    string? PartNumber,
    string? MatchKey,
    IReadOnlyDictionary<string, string> Specs);

/// <summary>Coletor de catálogo de uma loja.</summary>
public interface IStoreCollector
{
    string StoreSlug { get; }

    /// <summary>
    /// Coleta todos os produtos de uma categoria (pagina internamente).
    /// Deve respeitar o rate limit da loja. Lança em falha catastrófica;
    /// itens individuais inválidos são pulados com log.
    /// </summary>
    Task<IReadOnlyList<RawListing>> CollectAsync(string categorySlug, CancellationToken ct);
}

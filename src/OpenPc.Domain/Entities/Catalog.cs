namespace OpenPc.Domain.Entities;

/// <summary>Produto canônico (normalizado, independente de loja).</summary>
public sealed class Product
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public required string Brand { get; set; }
    public required string Model { get; set; }
    public required string Name { get; set; }
    public string? PartNumber { get; set; }
    public string? Ean { get; set; }
    public string? ImageUrl { get; set; }
    public required string SpecSource { get; set; } // scraper | manual | seed
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Category Category { get; set; } = null!;
    public List<ProductAttribute> Attributes { get; set; } = [];
    public List<Listing> Listings { get; set; } = [];
}

/// <summary>
/// Spec estruturada (EAV) que alimenta a engine de compatibilidade (M3).
/// Source define a precedência do valor: manual &gt; page &gt; title &gt; reference
/// (specs da página de produto da loja sobrescrevem o título; dados curados de
/// referência só preenchem lacunas).
/// </summary>
public sealed class ProductAttribute
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public required string Key { get; set; }
    public string? ValueText { get; set; }
    public decimal? ValueNum { get; set; }
    public bool? ValueBool { get; set; }
    public string Source { get; set; } = "title"; // reference | title | page | manual

    public Product Product { get; set; } = null!;
}

/// <summary>Oferta de um produto em uma loja.</summary>
public sealed class Listing
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid StoreId { get; set; }
    public required string StoreSku { get; set; }
    public required string Url { get; set; }
    public required string Title { get; set; }
    public decimal? PriceCash { get; set; }      // menor preço (PIX à vista)
    public decimal? PriceCard { get; set; }      // preço no cartão
    public int? Installments { get; set; }
    public string? InstallmentText { get; set; }
    public bool InStock { get; set; }
    public string? Thumbnail { get; set; }
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;

    /// <summary>Quando a ficha técnica da página do produto foi coletada (collect-details).</summary>
    public DateTime? SpecsCollectedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Product Product { get; set; } = null!;
    public Store Store { get; set; } = null!;
    public List<PriceHistory> PriceHistory { get; set; } = [];
}

/// <summary>Série temporal de preços (append-only).</summary>
public sealed class PriceHistory
{
    public Guid Id { get; set; }
    public Guid ListingId { get; set; }
    public decimal PriceCash { get; set; }
    public decimal? PriceCard { get; set; }
    public bool InStock { get; set; }
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;

    public Listing Listing { get; set; } = null!;
}

/// <summary>Job de coleta agendado por loja.</summary>
public sealed class ScrapeJob
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public Guid CategoryId { get; set; }
    public required string ScheduleCron { get; set; }
    public bool Enabled { get; set; } = true;

    public Store Store { get; set; } = null!;
    public Category Category { get; set; } = null!;
}

/// <summary>Execução de um job (observabilidade do scraper).</summary>
public sealed class ScrapeRun
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public required string Status { get; set; } // ok | partial | failed
    public int ItemsFound { get; set; }
    public int ItemsNew { get; set; }
    public string? Error { get; set; }
    public long DurationMs { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedAt { get; set; }

    public ScrapeJob Job { get; set; } = null!;
}

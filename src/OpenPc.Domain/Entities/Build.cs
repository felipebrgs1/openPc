namespace OpenPc.Domain.Entities;

/// <summary>Montagem do usuário (docs/specs.md §3.2). Anônimo por slug; owner na fase de auth (M7).</summary>
public sealed class Build
{
    public Guid Id { get; set; }
    public required string Slug { get; set; }
    public Guid? OwnerId { get; set; }
    public required string Name { get; set; }
    public bool IsPublic { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<BuildItem> Items { get; set; } = [];
}

/// <summary>Peça de um build: um item por categoria (slot). ProductId nulo = slot vazio.</summary>
public sealed class BuildItem
{
    public Guid Id { get; set; }
    public Guid BuildId { get; set; }
    public Guid CategoryId { get; set; }
    public Guid? ProductId { get; set; }
    public Guid? ListingId { get; set; } // loja escolhida; nulo = menor preço atual

    public Build Build { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public Product? Product { get; set; }
    public Listing? Listing { get; set; }
}

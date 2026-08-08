namespace OpenPc.Domain.Entities;

/// <summary>Loja rastreada pelo scraper.</summary>
public sealed class Store
{
    public Guid Id { get; set; }
    public required string Slug { get; set; }
    public required string Name { get; set; }
    public required string BaseUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

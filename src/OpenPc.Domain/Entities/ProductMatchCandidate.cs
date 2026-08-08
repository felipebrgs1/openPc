namespace OpenPc.Domain.Entities;

/// <summary>
/// Fila de revisão do dedup: listings sem match automático confiável.
/// Nível 3 do dedup (docs/specs.md §3.3) — aprovação manual via admin (futuro).
/// </summary>
public sealed class ProductMatchCandidate
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid StoreId { get; set; }
    public required string StoreSku { get; set; }
    public required string Title { get; set; }
    public required string Reason { get; set; } // new_product | ambiguous_match
    public double? Similarity { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Product Product { get; set; } = null!;
    public Store Store { get; set; } = null!;
}

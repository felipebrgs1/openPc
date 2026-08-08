namespace OpenPc.Domain.Entities;

/// <summary>Categoria de peça (cpu, gpu, memory...).</summary>
public sealed class Category
{
    public Guid Id { get; set; }
    public required string Slug { get; set; }
    public required string Name { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

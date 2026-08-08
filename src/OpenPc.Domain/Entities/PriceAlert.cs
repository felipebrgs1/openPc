namespace OpenPc.Domain.Entities;

/// <summary>
/// Preço mínimo diário agregado por produto (docs/specs.md §3.2, M6).
/// Fonte da série histórica de longo prazo: o raw (price_history) é
/// retido 90 dias; a partir daí o gráfico usa esta tabela (retenção 24 meses).
/// </summary>
public sealed class PriceDaily
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public DateTime Date { get; set; }          // UTC, normalizado para dia
    public decimal MinPrice { get; set; }        // menor preço em estoque do dia
    public Guid? ListingId { get; set; }         // listing que ofereceu o menor preço
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Product Product { get; set; } = null!;
}

/// <summary>Alerta de preço criado pelo usuário (M6). Anônimo por magic link.</summary>
public sealed class PriceAlert
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public required string Email { get; set; }
    public decimal TargetPrice { get; set; }     // dispara quando preço <= TargetPrice
    public required string Token { get; set; }   // magic link para cancelar/verificar
    public bool Confirmed { get; set; }          // confirmação por e-mail (M6)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? LastTriggeredAt { get; set; }
    public int TriggerCount { get; set; }

    public Product Product { get; set; } = null!;
}

/// <summary>Disparo de um alerta (append-only, audit/M6).</summary>
public sealed class PriceAlertEvent
{
    public Guid Id { get; set; }
    public Guid AlertId { get; set; }
    public Guid ListingId { get; set; }
    public decimal PriceAtTrigger { get; set; }
    public bool EmailSent { get; set; }
    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;

    public PriceAlert Alert { get; set; } = null!;
}

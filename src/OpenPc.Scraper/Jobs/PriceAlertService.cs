using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenPc.Domain.Entities;
using OpenPc.Infrastructure.Persistence;
using OpenPc.Scraper.Email;

namespace OpenPc.Scraper.Jobs;

/// <summary>
/// Dispara alertas de preço (M6) quando o re-scrape encontra um preço em
/// estoque &lt;= o alvo do usuário. Um alerta só dispara de novo depois de
/// 24 h (evita spam em coletas frequentes de CPU/GPU).
/// </summary>
public sealed class PriceAlertService(
    AppDbContext db,
    IEmailSender email,
    ILogger<PriceAlertService> logger)
{
    public static readonly TimeSpan Cooldown = TimeSpan.FromHours(24);

    /// <summary>
    /// Verifica os alertas confirmados do produto com o preço mais barato em
    /// estoque. Chamado após a ingestão de um run (re-scrape).
    /// </summary>
    public async Task<int> CheckProductAsync(Guid productId, CancellationToken ct)
    {
        var current = await db.Listings.AsNoTracking()
            .Where(l => l.ProductId == productId && l.InStock && l.PriceCash != null)
            .OrderBy(l => l.PriceCash)
            .Select(l => new { l.Id, l.PriceCash })
            .FirstOrDefaultAsync(ct);
        if (current?.PriceCash is not { } price)
            return 0; // sem preço em estoque — nada a fazer

        var product = await db.Products.AsNoTracking()
            .Where(p => p.Id == productId)
            .Select(p => new { p.Id, p.Name, p.Brand, p.Model, p.PartNumber })
            .FirstOrDefaultAsync(ct);
        if (product is null)
            return 0;

        var cutoff = DateTime.UtcNow - Cooldown;
        var alerts = await db.PriceAlerts
            .Where(a => a.ProductId == productId
                        && a.Confirmed
                        && a.TargetPrice >= price
                        && (a.LastTriggeredAt == null || a.LastTriggeredAt < cutoff))
            .ToListAsync(ct);
        if (alerts.Count == 0)
            return 0;

        var productUrl = $"https://openpc.example/pecas/{product.Id}";
        var sent = 0;
        foreach (var alert in alerts)
        {
            var cancelUrl = $"https://openpc.example/api/v1/alerts/{alert.Id}/cancel?token={alert.Token}";
            var body = $"""
                <p>O preço de <b>{product.Brand} {product.Model}</b> chegou ao seu alvo!</p>
                <p><b>Preço atual: R$ {price:F2}</b> (seu alvo: R$ {alert.TargetPrice:F2})</p>
                <p><a href="{productUrl}">Ver produto</a></p>
                <p style="color:#888;font-size:12px">
                  <a href="{cancelUrl}">Cancelar este alerta</a>
                </p>
                """;

            try
            {
                await email.SendAsync(alert.Email, $"OpenPC: {product.Name} atingiu seu preço alvo", body, ct);
                alert.LastTriggeredAt = DateTime.UtcNow;
                alert.TriggerCount++;
                db.PriceAlertEvents.Add(new PriceAlertEvent
                {
                    Id = Guid.NewGuid(),
                    AlertId = alert.Id,
                    ListingId = current.Id,
                    PriceAtTrigger = price,
                    EmailSent = true,
                });
                sent++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Falha ao enviar alerta de preço para {Email} ({Product})",
                    alert.Email, product.Name);
            }
        }

        await db.SaveChangesAsync(ct);
        if (sent > 0)
            logger.LogInformation("Alertas de preço disparados: {Sent} para {Product}", sent, product.Name);
        return sent;
    }
}

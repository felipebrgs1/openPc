using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using OpenPc.Domain.Entities;
using OpenPc.Infrastructure.Persistence;

namespace OpenPc.Api.Endpoints;

/// <summary>
/// Alertas de preço (M6): criação anônima com e-mail + alvo, confirmação por
/// magic link (token no e-mail) e cancelamento. O disparo acontece no scraper
/// (PriceAlertService) durante o re-scrape — este módulo só persiste o alerta.
/// </summary>
public static class AlertEndpoints
{
    public static void MapAlertEndpoints(this WebApplication app)
    {
        var alerts = app.MapGroup("/api/v1/alerts");
        alerts.MapPost("", CreateAlertAsync);
        alerts.MapGet("/confirm", ConfirmAlertAsync);
        alerts.MapGet("/cancel", CancelAlertAsync); // magic link GET (e-mail) + DELETE
        alerts.MapDelete("/{id:guid}", CancelAlertAsync);
    }

    private static async Task<IResult> CreateAlertAsync(
        AppDbContext db,
        [Microsoft.AspNetCore.Mvc.FromBody] CreateAlertRequest? request,
        CancellationToken ct)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Email) || request.TargetPrice <= 0)
            return Results.BadRequest("E-mail e preço alvo são obrigatórios.");

        var email = request.Email.Trim().ToLowerInvariant();
        if (!email.Contains('@') || email.Length > 320)
            return Results.BadRequest("E-mail inválido.");

        var product = await db.Products.AsNoTracking()
            .AnyAsync(p => p.Id == request.ProductId, ct);
        if (!product)
            return Results.NotFound("Produto não encontrado.");

        var alert = new PriceAlert
        {
            Id = Guid.NewGuid(),
            ProductId = request.ProductId,
            Email = email,
            TargetPrice = request.TargetPrice,
            Token = RandomNumberGenerator.GetHexString(32),
            Confirmed = false,
        };
        db.PriceAlerts.Add(alert);
        await db.SaveChangesAsync(ct);

        // (envio do e-mail de confirmação fica para o agendador/deploy — o
        // link de confirmação é GET /api/v1/alerts/confirm?token=...)
        return Results.Created($"/api/v1/alerts/{alert.Id}", new
        {
            alert.Id,
            alert.ProductId,
            alert.Email,
            alert.TargetPrice,
            alert.Confirmed,
            ConfirmUrl = $"/api/v1/alerts/confirm?token={alert.Token}",
        });
    }

    private static async Task<IResult> ConfirmAlertAsync(
        AppDbContext db,
        [Microsoft.AspNetCore.Mvc.FromQuery] string token,
        CancellationToken ct)
    {
        var alert = await db.PriceAlerts
            .FirstOrDefaultAsync(a => a.Token == token, ct);
        if (alert is null)
            return Results.NotFound();

        if (!alert.Confirmed)
        {
            alert.Confirmed = true;
            alert.ConfirmedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        return Results.Ok(new { alert.Id, alert.ProductId, alert.Email, alert.TargetPrice, alert.Confirmed });
    }

    private static async Task<IResult> CancelAlertAsync(
        Guid id, AppDbContext db,
        [Microsoft.AspNetCore.Mvc.FromQuery] string? token,
        CancellationToken ct)
    {
        var alert = await db.PriceAlerts.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (alert is null)
            return Results.NotFound();

        // magic link carrega o token; DELETE sem token exige confirmação de
        // posse (token no corpo/query) — por simplicidade o token é obrigatório.
        if (token is null || !string.Equals(token, alert.Token, StringComparison.Ordinal))
            return Results.Unauthorized();

        db.PriceAlerts.Remove(alert);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    internal sealed record CreateAlertRequest(Guid ProductId, string? Email, decimal TargetPrice);
}

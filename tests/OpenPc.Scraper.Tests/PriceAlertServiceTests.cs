using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OpenPc.Domain.Entities;
using OpenPc.Infrastructure.Persistence;
using OpenPc.Scraper.Email;
using OpenPc.Scraper.Jobs;

namespace OpenPc.Scraper.Tests;

public class PriceAlertServiceTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static Product Product(string name = "Ryzen 5 7600") => new()
    {
        Id = Guid.NewGuid(),
        CategoryId = Guid.NewGuid(),
        Brand = "AMD",
        Model = "7600",
        Name = name,
        SpecSource = "scraper",
    };

    private static Listing Listing(Product p, decimal? price, bool inStock = true) => new()
    {
        Id = Guid.NewGuid(),
        ProductId = p.Id,
        StoreId = Guid.NewGuid(),
        StoreSku = Guid.NewGuid().ToString("N"),
        Url = "https://store.example/x",
        Title = p.Name,
        PriceCash = price,
        InStock = inStock,
    };

    private static PriceAlert Alert(Product p, string email, decimal target, bool confirmed = true, DateTime? lastTriggered = null) => new()
    {
        Id = Guid.NewGuid(),
        ProductId = p.Id,
        Email = email,
        TargetPrice = target,
        Token = Guid.NewGuid().ToString("N"),
        Confirmed = confirmed,
        LastTriggeredAt = lastTriggered,
    };

    [Fact]
    public async Task PrecoAbaixoDoAlvo_DisparaEmailEEventos()
    {
        await using var db = CreateDb();
        var product = Product();
        var listing = Listing(product, 899.99m);
        var alert = Alert(product, "user@example.com", 950m);
        db.AddRange(product, listing, alert);
        await db.SaveChangesAsync();

        var sent = new List<(string To, string Subject)>();
        var service = new PriceAlertService(db, new FakeEmailSender(sent), NullLogger<PriceAlertService>.Instance);

        var count = await service.CheckProductAsync(product.Id, CancellationToken.None);

        Assert.Equal(1, count);
        var saved = await db.PriceAlerts.SingleAsync();
        Assert.Equal(1, saved.TriggerCount);
        Assert.NotNull(saved.LastTriggeredAt);
        Assert.Single(await db.PriceAlertEvents.ToListAsync());
        Assert.Single(sent);
        Assert.Equal("user@example.com", sent[0].To);
        Assert.Contains("Ryzen 5 7600", sent[0].Subject);
    }

    [Fact]
    public async Task PrecoAcimaDoAlvo_NaoDispara()
    {
        await using var db = CreateDb();
        var product = Product();
        var listing = Listing(product, 1200m);
        var alert = Alert(product, "user@example.com", 950m);
        db.AddRange(product, listing, alert);
        await db.SaveChangesAsync();

        var sent = new List<(string To, string Subject)>();
        var service = new PriceAlertService(db, new FakeEmailSender(sent), NullLogger<PriceAlertService>.Instance);

        var count = await service.CheckProductAsync(product.Id, CancellationToken.None);

        Assert.Equal(0, count);
        Assert.Empty(sent);
        Assert.Empty(await db.PriceAlertEvents.ToListAsync());
    }

    [Fact]
    public async Task AlertaNaoConfirmado_NaoDispara()
    {
        await using var db = CreateDb();
        var product = Product();
        var listing = Listing(product, 899.99m);
        var alert = Alert(product, "user@example.com", 950m, confirmed: false);
        db.AddRange(product, listing, alert);
        await db.SaveChangesAsync();

        var sent = new List<(string To, string Subject)>();
        var service = new PriceAlertService(db, new FakeEmailSender(sent), NullLogger<PriceAlertService>.Instance);

        var count = await service.CheckProductAsync(product.Id, CancellationToken.None);

        Assert.Equal(0, count);
        Assert.Empty(sent);
    }

    [Fact]
    public async Task Cooldown_ImpedeReDisparoEmMenosDe24h()
    {
        await using var db = CreateDb();
        var product = Product();
        var listing = Listing(product, 899.99m);
        var alert = Alert(product, "user@example.com", 950m,
            lastTriggered: DateTime.UtcNow.AddHours(-2));
        db.AddRange(product, listing, alert);
        await db.SaveChangesAsync();

        var sent = new List<(string To, string Subject)>();
        var service = new PriceAlertService(db, new FakeEmailSender(sent), NullLogger<PriceAlertService>.Instance);

        var count = await service.CheckProductAsync(product.Id, CancellationToken.None);

        Assert.Equal(0, count); // dentro do cooldown de 24h
        Assert.Empty(sent);
    }

    [Fact]
    public async Task CooldownExpirado_DisparaNovamente()
    {
        await using var db = CreateDb();
        var product = Product();
        var listing = Listing(product, 899.99m);
        var alert = Alert(product, "user@example.com", 950m,
            lastTriggered: DateTime.UtcNow.AddHours(-25));
        alert.TriggerCount = 1;
        db.AddRange(product, listing, alert);
        await db.SaveChangesAsync();

        var sent = new List<(string To, string Subject)>();
        var service = new PriceAlertService(db, new FakeEmailSender(sent), NullLogger<PriceAlertService>.Instance);

        var count = await service.CheckProductAsync(product.Id, CancellationToken.None);

        Assert.Equal(1, count);
        Assert.Equal(2, (await db.PriceAlerts.SingleAsync()).TriggerCount);
        Assert.Single(sent); // um disparo = um e-mail
    }

    [Fact]
    public async Task SemPrecoEmEstoque_NaoDispara()
    {
        await using var db = CreateDb();
        var product = Product();
        var listing = Listing(product, null, inStock: false);
        var alert = Alert(product, "user@example.com", 950m);
        db.AddRange(product, listing, alert);
        await db.SaveChangesAsync();

        var sent = new List<(string To, string Subject)>();
        var service = new PriceAlertService(db, new FakeEmailSender(sent), NullLogger<PriceAlertService>.Instance);

        var count = await service.CheckProductAsync(product.Id, CancellationToken.None);

        Assert.Equal(0, count);
        Assert.Empty(sent);
    }

    private sealed class FakeEmailSender(List<(string To, string Subject)> sent) : IEmailSender
    {
        public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct)
        {
            sent.Add((to, subject));
            return Task.CompletedTask;
        }
    }
}

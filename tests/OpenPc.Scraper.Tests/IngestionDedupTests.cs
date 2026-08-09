using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OpenPc.Domain.Entities;
using OpenPc.Infrastructure.Persistence;
using OpenPc.Scraper.Collectors;
using OpenPc.Scraper.Ingest;

namespace OpenPc.Scraper.Tests;

/// <summary>
/// Dedup de ingestão: o listing (loja+StoreSku) é a identidade estável do
/// produto entre scrapes. Categorias sem part number/match key estáveis
/// (motherboard, memory, case, psu de loja browser) não têm outra âncora —
/// sem isso, cada re-scrape cria produto novo e orfana o antigo (bug real
/// de produção: re-coleta de case/motherboard/memory duplicou o catálogo).
/// </summary>
public class IngestionDedupTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static RawListing Listing(string sku, string title, string? thumbnail = null) => new(
        StoreSku: sku,
        Title: title,
        Url: $"https://store.example/{sku}",
        PriceCash: 199.90m,
        PriceCard: null,
        Installments: null,
        InstallmentText: null,
        InStock: true,
        Thumbnail: thumbnail,
        Manufacturer: null,
        PartNumber: null,
        MatchKey: null,
        Specs: new Dictionary<string, string>());

    private static async Task<Category> SeedCategoryAsync(AppDbContext db, string slug)
    {
        var category = new Category { Id = Guid.NewGuid(), Slug = slug, Name = slug };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return category;
    }

    private static async Task<Store> SeedStoreAsync(AppDbContext db)
    {
        var store = new Store { Id = Guid.NewGuid(), Slug = "terabyte", Name = "Terabyte", BaseUrl = "https://www.terabyteshop.com.br" };
        db.Stores.Add(store);
        await db.SaveChangesAsync();
        return store;
    }

    [Fact]
    public async Task ReIngest_MesmoStoreSku_ReusaProdutoSemDuplicar()
    {
        var db = CreateDb();
        var category = await SeedCategoryAsync(db, "motherboard");
        var store = await SeedStoreAsync(db);
        var product = new Product
        {
            Id = Guid.NewGuid(),
            CategoryId = category.Id,
            Brand = "asrock",
            // sem part number e sem match key — como em motherboard/memory/case
            Model = Guid.NewGuid().ToString("N")[..12],
            Name = "Placa-Mãe ASRock B650M",
            ImageUrl = null,
            SpecSource = "scraper",
        };
        db.Products.Add(product);
        db.Listings.Add(new Listing
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            StoreId = store.Id,
            StoreSku = "placa-mae-asrock-b650m",
            Url = "https://store.example/placa-mae-asrock-b650m",
            Title = "Placa-Mãe ASRock B650M",
            PriceCash = 199.90m,
            InStock = true,
        });
        await db.SaveChangesAsync();

        var service = new IngestionService(db, NullLogger<IngestionService>.Instance);

        // re-coleta: mesmo SKU, agora com thumbnail (a foto é o objetivo da coleta)
        var result = await service.IngestAsync(store, category.Slug, [Listing("placa-mae-asrock-b650m", "Placa-Mãe ASRock B650M", "https://img/placa.jpg")], CancellationToken.None);

        Assert.Equal(0, result.NewProducts);
        Assert.Equal(0, result.NewListings);
        Assert.Equal(1, await db.Products.CountAsync());
        Assert.Equal(1, await db.Listings.CountAsync());

        var reloaded = await db.Products.SingleAsync();
        Assert.Equal(product.Id, reloaded.Id);
        Assert.Equal("https://img/placa.jpg", reloaded.ImageUrl);
    }

    [Fact]
    public async Task PrimeiraColeta_CriaProdutoEListing()
    {
        var db = CreateDb();
        var category = await SeedCategoryAsync(db, "case");
        var store = await SeedStoreAsync(db);

        var service = new IngestionService(db, NullLogger<IngestionService>.Instance);
        var result = await service.IngestAsync(store, category.Slug, [Listing("gabinete-abc", "Gabinete Gamer ABC")], CancellationToken.None);

        Assert.Equal(1, result.NewProducts);
        Assert.Equal(1, result.NewListings);
        Assert.Equal(1, await db.Products.CountAsync());
        Assert.Equal(1, await db.Listings.CountAsync());
    }
}

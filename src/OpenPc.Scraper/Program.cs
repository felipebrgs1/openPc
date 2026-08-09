using Microsoft.EntityFrameworkCore;
using Minio;
using OpenPc.Domain.Entities;
using OpenPc.Infrastructure.Persistence;
using OpenPc.Infrastructure.Persistence.Seed;
using OpenPc.Infrastructure.Prices;
using OpenPc.Scraper.Collectors;
using OpenPc.Scraper.Email;
using OpenPc.Scraper.Ingest;
using OpenPc.Scraper.Jobs;
using OpenPc.Scraper.Normalization;
using Quartz;
using Serilog;
using Serilog.Formatting.Json;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.Services.AddSerilog((_, cfg) =>
    {
        cfg.ReadFrom.Configuration(builder.Configuration);
        // Formato do stdout: texto em dev, JSON em produção (env Logging__Format=json).
        if (string.Equals(builder.Configuration["Logging:Format"], "json", StringComparison.OrdinalIgnoreCase))
            cfg.WriteTo.Console(new JsonFormatter());
        else
            cfg.WriteTo.Console();
    });

    var conn = builder.Configuration.GetConnectionString("Default")
        ?? throw new InvalidOperationException("Connection string 'Default' não configurada.");

    builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(conn));
    builder.Services.AddScoped<PriceAggregationService>();
    builder.Services.AddHttpClient<KabumCollector>(c => c.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0 Safari/537.36"));
    builder.Services.AddHttpClient<ScrapeAlertService>(c => c.Timeout = TimeSpan.FromSeconds(5));

    // Download das fotos dos CDNs (sync-images): UA de browser + timeout.
    builder.Services.AddHttpClient("images", c =>
    {
        c.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0 Safari/537.36");
        c.Timeout = TimeSpan.FromSeconds(30);
    });

    // MinIO (bucket de fotos). Sem Minio:Endpoint o client fica null e o
    // ImageSyncService vira no-op — o scraper funciona sem imagens próprias.
    builder.Services.AddSingleton<IMinioClient>(_ =>
    {
        var endpoint = builder.Configuration["Minio:Endpoint"];
        if (string.IsNullOrWhiteSpace(endpoint))
            return null!;
        var useSsl = bool.TryParse(builder.Configuration["Minio:UseSsl"], out var ssl) && ssl;
        return new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(
                builder.Configuration["Minio:AccessKey"] ?? "",
                builder.Configuration["Minio:SecretKey"] ?? "")
            .WithSSL(useSsl)
            .Build();
    });
    builder.Services.AddSingleton<ImageSyncService>();

    builder.Services.AddSingleton<BrowserPool>();
    builder.Services.AddSingleton<IngestionService>();
    builder.Services.AddSingleton<CollectionService>();
    builder.Services.AddSingleton<IStoreCollector, KabumCollector>();
    builder.Services.AddSingleton<IStoreCollector, PichauCollector>();
    builder.Services.AddSingleton<IStoreCollector, TerabyteCollector>();
    builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
    builder.Services.AddSingleton<PriceAlertService>();
    builder.Services.AddQuartz(q => { });
    builder.Services.AddHostedService<ScrapeScheduler>();

    var host = builder.Build();

    // seed (jobs de scraping). Migrações NÃO rodam aqui — a API aplica com
    // advisory lock no startup; em produção o compose garante a ordem
    // (scraper depende de api healthy).
    using (var scope = host.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");
        await DbSeeder.SeedAsync(db, logger);
    }

    if (args.Length > 0 && args[0] == "run-once")
    {
        var collection = host.Services.GetRequiredService<CollectionService>();
        var store = args.Length > 1 ? args[1] : null;
        var category = args.Length > 2 ? args[2] : null;
        await collection.RunAllEnabledAsync(store, category, CancellationToken.None);
        Log.CloseAndFlush();
        return;
    }

    if (args.Length > 0 && args[0] == "alerts-check")
    {
        // Verifica/dispara alertas de preço de um produto manualmente (não
        // coleta nada). Uso: alerts-check <productId> — útil para validar o
        // disparo com preço simulado em staging (M6).
        if (args.Length < 2 || !Guid.TryParse(args[1], out var alertProductId))
        {
            Log.Error("alerts-check exige um productId (GUID)");
            Log.CloseAndFlush();
            return;
        }
        await using var scope = host.Services.CreateAsyncScope();
        var alerts = scope.ServiceProvider.GetRequiredService<PriceAlertService>();
        var sent = await alerts.CheckProductAsync(alertProductId, CancellationToken.None);
        Log.Information("alerts-check {ProductId}: {Sent} alertas disparados", alertProductId, sent);
        Log.CloseAndFlush();
        return;
    }

    if (args.Length > 0 && args[0] == "aggregate-prices")
    {
        // Roda a agregação price_daily + retenção manualmente (não coleta
        // nada — só consolida o histórico existente). Uso: aggregate-prices [dias]
        var days = args.Length > 1 && int.TryParse(args[1], out var d) ? d : 30;
        await using var scope = host.Services.CreateAsyncScope();
        var aggregation = scope.ServiceProvider.GetRequiredService<PriceAggregationService>();
        await aggregation.RunAsync(days: days, ct: CancellationToken.None);
        Log.CloseAndFlush();
        return;
    }

    if (args.Length > 0 && args[0] == "cleanup-noise")
    {
        // Remove do banco produtos que não pertencem à categoria (mesmo filtro
        // da ingestão) — para limpar o que entrou antes do filtro. Não coleta nada.
        // `--dry-run` apenas conta e amostra, sem deletar.
        var category = args.Length > 1 && !args[1].StartsWith("--") ? args[1] : null;
        var dryRun = args.Contains("--dry-run");
        await using var scope = host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("CleanupNoise");

        var products = await db.Products
            .Include(p => p.Category)
            .ToListAsync(CancellationToken.None);

        var toDelete = products
            .Where(p => (category is null || p.Category.Slug == category)
                        && CategoryNoiseFilter.IsNoise(p.Category.Slug, p.Name))
            .ToList();

        if (dryRun)
        {
            foreach (var g in toDelete.GroupBy(p => p.Category.Slug).OrderBy(g => g.Key))
                logger.LogInformation("dry-run {Category}: {Count} produtos (ex: {Sample})",
                    g.Key, g.Count(), g.First().Name);
            Log.CloseAndFlush();
            return;
        }

        db.Products.RemoveRange(toDelete);
        await db.SaveChangesAsync(CancellationToken.None);

        logger.LogInformation("cleanup-noise: {Count} produtos removidos ({Category})",
            toDelete.Count, category ?? "todas as categorias");
        Log.CloseAndFlush();
        return;
    }

    if (args.Length > 0 && args[0] == "sync-images")
    {
        // Baixa as fotos dos CDNs das lojas e sobe para o bucket MinIO,
        // reescrevendo ImageUrl para /images/{key} (mesmo domínio, via Caddy).
        // Idempotente: chave = hash da URL; re-rodar só completa falhas.
        // Uso: sync-images
        await using var scope = host.Services.CreateAsyncScope();
        var sync = scope.ServiceProvider.GetRequiredService<ImageSyncService>();
        var synced = await sync.SyncAsync(CancellationToken.None);
        Log.Information("sync-images: {Count} fotos sincronizadas", synced);
        Log.CloseAndFlush();
        return;
    }

    if (args.Length > 0 && args[0] == "backfill-attributes")
    {
        // Calcula atributos de catálogo (série de GPU, tipo de memória) para
        // produtos ingeridos antes dos extractors existirem — mesmo código da
        // ingestão (SpecExtractor/GpuSeries). Não coleta nada. Upsert idempotente:
        // só adiciona quando a chave ainda não existe no produto.
        await using var scope = host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("BackfillAttributes");

        var products = await db.Products
            .Include(p => p.Category)
            .Include(p => p.Attributes)
            .Where(p => p.Category.Slug == "gpu" || p.Category.Slug == "memory")
            .ToListAsync(CancellationToken.None);

        var added = 0;
        var byValue = new SortedDictionary<string, int>();
        foreach (var product in products)
        {
            var (key, value) = product.Category.Slug switch
            {
                "gpu" => ("series", GpuSeries.Classify(product.Name)),
                "memory" => ("type",
                    SpecExtractor.ExtractMemory(product.Name).TryGetValue("type", out var t) ? t : null),
                _ => ((string?)null, (string?)null),
            };
            if (key is null || value is null || product.Attributes.Any(a => a.Key == key))
                continue;

            db.ProductAttributes.Add(new ProductAttribute
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Key = key,
                ValueText = value,
            });
            byValue[$"{key}={value}"] = byValue.GetValueOrDefault($"{key}={value}") + 1;
            added++;
        }

        await db.SaveChangesAsync(CancellationToken.None);

        foreach (var (k, v) in byValue)
            logger.LogInformation("backfill-attributes: {Key}: {Count} produtos", k, v);
        logger.LogInformation("backfill-attributes: {Count} atributos adicionados", added);
        Log.CloseAndFlush();
        return;
    }

    await host.RunAsync();
}
catch (Exception ex) when (ex is not OperationCanceledException)
{
    Log.Fatal(ex, "Falha fatal no scraper");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

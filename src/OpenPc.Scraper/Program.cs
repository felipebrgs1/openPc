using Microsoft.EntityFrameworkCore;
using OpenPc.Infrastructure.Persistence;
using OpenPc.Infrastructure.Persistence.Seed;
using OpenPc.Scraper.Collectors;
using OpenPc.Scraper.Ingest;
using OpenPc.Scraper.Jobs;
using Quartz;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.Services.AddSerilog((ctx, cfg) => cfg.WriteTo.Console());

    var conn = builder.Configuration.GetConnectionString("Default")
        ?? throw new InvalidOperationException("Connection string 'Default' não configurada.");

    builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(conn));
    builder.Services.AddHttpClient<KabumCollector>(c => c.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0 Safari/537.36"));

    builder.Services.AddSingleton<BrowserPool>();
    builder.Services.AddSingleton<IngestionService>();
    builder.Services.AddSingleton<CollectionService>();
    builder.Services.AddSingleton<IStoreCollector, KabumCollector>();
    builder.Services.AddSingleton<IStoreCollector, PichauCollector>();
    builder.Services.AddSingleton<IStoreCollector, TerabyteCollector>();
    builder.Services.AddQuartz(q => { });
    builder.Services.AddHostedService<ScrapeScheduler>();

    var host = builder.Build();

    // aplica migrações + seed (jobs de scraping)
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

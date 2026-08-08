using Microsoft.EntityFrameworkCore;
using OpenPc.Api.Endpoints;
using OpenPc.Infrastructure;
using OpenPc.Infrastructure.Persistence;
using OpenPc.Infrastructure.Persistence.Seed;
using Serilog;
using Serilog.Formatting.Json;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, cfg) =>
    {
        cfg.ReadFrom.Configuration(ctx.Configuration);
        // Formato do stdout: texto em dev, JSON em produção (env Logging__Format=json).
        if (string.Equals(ctx.Configuration["Logging:Format"], "json", StringComparison.OrdinalIgnoreCase))
            cfg.WriteTo.Console(new JsonFormatter());
        else
            cfg.WriteTo.Console();
    });

    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddStackExchangeRedisCache(o =>
        o.Configuration = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379");

    // CORS restrito: origens via config (env Cors__AllowedOrigins, vírgula-separada).
    // Em produção o front é servido pelo Caddy no mesmo domínio (same-origin),
    // então CORS não é necessário — a lista fica vazia por padrão em prod.
    var corsOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? "")
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if (corsOrigins.Length > 0)
    {
        builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
            .WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()));
    }
    builder.Services.AddOpenApi();

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    if (corsOrigins.Length > 0)
        app.UseCors();
    app.MapOpenApi();
    app.MapCatalogEndpoints();
    app.MapBuildEndpoints();
    app.MapOffersEndpoints();
    app.MapAlertEndpoints();

    app.MapGet("/api/v1/health", async (AppDbContext db) =>
    {
        var dbOk = await db.Database.CanConnectAsync();
        return dbOk
            ? Results.Ok(new HealthResponse("healthy", "ok"))
            : Results.Json(new HealthResponse("degraded", "error"), statusCode: 503);
    });

    app.MapGet("/api/v1/categories", async (AppDbContext db) =>
        await db.Categories
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new CategoryDto(c.Id, c.Slug, c.Name, c.DisplayOrder))
            .ToListAsync());

    app.MapGet("/api/v1/stores", async (AppDbContext db) =>
        await db.Stores
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new StoreDto(s.Id, s.Slug, s.Name, s.BaseUrl))
            .ToListAsync());

    await using (var scope = app.Services.CreateAsyncScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("DbSeeder");
        // Migrações com lock (advisory lock do Postgres) antes do seed.
        await DatabaseMigrator.MigrateWithLockAsync(db);
        await DbSeeder.SeedAsync(db, logger);
    }

    app.Run();
}
catch (Exception ex) when (ex is not OperationCanceledException)
{
    Log.Fatal(ex, "Falha fatal na inicialização do host");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

internal sealed record HealthResponse(string Status, string Database);
internal sealed record CategoryDto(Guid Id, string Slug, string Name, int DisplayOrder);
internal sealed record StoreDto(Guid Id, string Slug, string Name, string BaseUrl);

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenPc.Domain.Compatibility;
using OpenPc.Infrastructure.Compatibility;
using OpenPc.Infrastructure.Persistence;
using OpenPc.Infrastructure.Prices;

namespace OpenPc.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Connection string 'Default' não configurada.");

        services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connectionString));

        // Agregação diária de preços + retenção (M6).
        services.AddScoped<PriceAggregationService>();

        // Engine de compatibilidade (M3): seed curado + regras + executor.
        services.AddSingleton(CompatibilitySeedLoader.Load());
        services.AddSingleton(TdpSeedLoader.Load()); // fallback de consumo (tdp.json)
        services.AddSingleton<CompatibilityEngine>();
        foreach (var type in typeof(ICompatibilityRule).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                        && typeof(ICompatibilityRule).IsAssignableFrom(t)))
        {
            services.AddSingleton(typeof(ICompatibilityRule), type);
        }

        return services;
    }
}

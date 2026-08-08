using OpenPc.Domain.Entities;

namespace OpenPc.Infrastructure.Persistence.Seed;

public static class SeedData
{
    public const string CronCatalogDaily = "0 30 4 * * ?";      // 04:30 diário
    public const string CronHotPrices = "0 0 */6 * * ?";        // a cada 6h

    public static readonly Category[] Categories =
    [
        new() { Slug = "cpu", Name = "Processadores", DisplayOrder = 1 },
        new() { Slug = "motherboard", Name = "Placas-mãe", DisplayOrder = 2 },
        new() { Slug = "gpu", Name = "Placas de vídeo", DisplayOrder = 3 },
        new() { Slug = "memory", Name = "Memórias RAM", DisplayOrder = 4 },
        new() { Slug = "storage", Name = "Armazenamento", DisplayOrder = 5 },
        new() { Slug = "psu", Name = "Fontes", DisplayOrder = 6 },
        new() { Slug = "case", Name = "Gabinetes", DisplayOrder = 7 },
        new() { Slug = "cooler", Name = "Coolers e watercoolers", DisplayOrder = 8 },
    ];

    public static readonly Store[] Stores =
    [
        new() { Slug = "kabum", Name = "KaBuM!", BaseUrl = "https://www.kabum.com.br" },
        new() { Slug = "terabyte", Name = "Terabyte Shop", BaseUrl = "https://www.terabyteshop.com.br" },
        new() { Slug = "pichau", Name = "Pichau", BaseUrl = "https://www.pichau.com.br" },
    ];

    /// <summary>Categorias de preço volátil: coleta 4×/dia; o resto, diário.</summary>
    public static readonly string[] HotCategories = ["cpu", "gpu"];

    public static ScrapeJob[] BuildJobs(Guid[] categoryIds, Guid[] storeIds) =>
        categoryIds
            .SelectMany(catId => storeIds.Select(storeId => new ScrapeJob
            {
                StoreId = storeId,
                CategoryId = catId,
                ScheduleCron = CronCatalogDaily,
                Enabled = true,
            }))
            .ToArray();
}

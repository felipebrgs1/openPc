using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace OpenPc.Infrastructure.Persistence.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, ILogger logger, CancellationToken ct = default)
    {
        // Migrações NÃO rodam aqui: são aplicadas no startup da API com
        // advisory lock (DatabaseMigrator) — nunca via scraper (specs §8.2).
        if (!await db.Categories.AnyAsync(ct))
        {
            db.Categories.AddRange(SeedData.Categories);
            logger.LogInformation("Seed: {Count} categorias inseridas", SeedData.Categories.Length);
        }

        if (!await db.Stores.AnyAsync(ct))
        {
            db.Stores.AddRange(SeedData.Stores);
            logger.LogInformation("Seed: {Count} lojas inseridas", SeedData.Stores.Length);
        }

        // Persiste categorias/lojas ANTES de buildar os jobs: BuildJobs consulta
        // os ids no banco, e sem SaveChanges eles ainda não existem (bug latente:
        // a API logava 0 jobs e o scraper os criava depois por acidente de ordem).
        if (db.ChangeTracker.HasChanges())
            await db.SaveChangesAsync(ct);

        if (!await db.ScrapeJobs.AnyAsync(ct))
        {
            var categoryIds = db.Categories.Select(c => c.Id).ToArray();
            var storeIds = db.Stores.Select(s => s.Id).ToArray();
            var jobs = SeedData.BuildJobs(categoryIds, storeIds);

            // Categorias quentes (CPU/GPU) sobrescrevem a cron para 4×/dia.
            var hotIds = db.Categories
                .Where(c => SeedData.HotCategories.Contains(c.Slug))
                .Select(c => c.Id)
                .ToArray();
            foreach (var job in jobs.Where(j => hotIds.Contains(j.CategoryId)))
                job.ScheduleCron = SeedData.CronHotPrices;

            db.ScrapeJobs.AddRange(jobs);
            logger.LogInformation("Seed: {Count} jobs de scraping inseridos", jobs.Length);
        }

        await db.SaveChangesAsync(ct);
    }
}

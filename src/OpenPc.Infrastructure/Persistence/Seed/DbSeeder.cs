using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace OpenPc.Infrastructure.Persistence.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, ILogger logger, CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);

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

        await db.SaveChangesAsync(ct);
    }
}

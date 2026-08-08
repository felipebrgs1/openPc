using Microsoft.EntityFrameworkCore;

namespace OpenPc.Infrastructure.Persistence;

/// <summary>
/// Aplica migrações EF Core com exclusão mútua via advisory lock do Postgres.
/// Vários processos (api, instâncias futuras, deploy manual) podem tentar
/// migrar ao mesmo tempo no startup; só um passa, os demais esperam o lock
/// liberar — e o <see cref="DatabaseFacade.MigrateAsync"/> é idempotente.
/// </summary>
public static class DatabaseMigrator
{
    // Chave fixa e arbitrária do lock global de migração (0x4F50454E5F50434D = "OPEN_PCM").
    private const long MigrationLockKey = 0x4F50454E_5F50434D;

    public static async Task MigrateWithLockAsync(AppDbContext db, CancellationToken ct = default)
    {
        var conn = db.Database.GetDbConnection();
        var wasClosed = conn.State != System.Data.ConnectionState.Open;
        if (wasClosed)
            await conn.OpenAsync(ct);

        try
        {
            await using (var lockCmd = conn.CreateCommand())
            {
                lockCmd.CommandText = "SELECT pg_advisory_lock(@key)";
                var key = lockCmd.CreateParameter();
                key.ParameterName = "key";
                key.Value = MigrationLockKey;
                lockCmd.Parameters.Add(key);
                await lockCmd.ExecuteNonQueryAsync(ct);
            }

            try
            {
                // A conexão do contexto já está aberta: a migração roda nela
                // mesma, sob o lock da sessão.
                await db.Database.MigrateAsync(ct);
            }
            finally
            {
                await using var unlockCmd = conn.CreateCommand();
                unlockCmd.CommandText = "SELECT pg_advisory_unlock(@key)";
                var key = unlockCmd.CreateParameter();
                key.ParameterName = "key";
                key.Value = MigrationLockKey;
                unlockCmd.Parameters.Add(key);
                await unlockCmd.ExecuteNonQueryAsync(ct);
            }
        }
        finally
        {
            if (wasClosed)
                await conn.CloseAsync();
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OpenPc.Infrastructure.Persistence;

/// <summary>
/// Factory de design-time para o `dotnet ef`. Usada somente para gerar
/// migrações sem depender do host da API.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=openpc;Username=openpc;Password=openpc_dev")
            .Options;
        return new AppDbContext(options);
    }
}

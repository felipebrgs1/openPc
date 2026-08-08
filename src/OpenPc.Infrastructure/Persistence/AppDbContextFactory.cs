using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OpenPc.Infrastructure.Persistence;

/// <summary>
/// Factory design-time para as ferramentas do EF Core (`dotnet ef migrations
/// add/update`): permite gerar/aplicar migrações sem o startup project da API
/// (que não referencia o pacote Design). A connection string vem do ambiente
/// (ConnectionStrings__Default) com fallback para o padrão de dev local.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=localhost;Port=5432;Database=openpc;Username=openpc;Password=openpc_dev";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(conn)
            .Options;
        return new AppDbContext(options);
    }
}

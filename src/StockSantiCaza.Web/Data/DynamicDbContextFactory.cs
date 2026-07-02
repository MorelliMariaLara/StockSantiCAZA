using EntityFrameworkCore.UseRowNumberForPaging;
using Microsoft.EntityFrameworkCore;
using StockSantiCaza.Web.Configuration;

namespace StockSantiCaza.Web.Data;

public sealed class DynamicDbContextFactory : IDbContextFactory<ApplicationDbContext>
{
    private readonly FerozoConnectionStringProvider connectionProvider;
    private readonly IHostEnvironment environment;

    public DynamicDbContextFactory(
        FerozoConnectionStringProvider connectionProvider,
        IHostEnvironment environment)
    {
        this.connectionProvider = connectionProvider;
        this.environment = environment;
    }

    public ApplicationDbContext CreateDbContext()
    {
        var cadena = connectionProvider.ObtenerCadena();
        return CrearContexto(cadena);
    }

    public async Task<ApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        var cadena = environment.IsDevelopment()
            ? connectionProvider.ObtenerCadena()
            : await connectionProvider.ObtenerCadenaAsync(cancellationToken);
        return CrearContexto(cadena);
    }

    private static ApplicationDbContext CrearContexto(string connectionString)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString, sql =>
            {
                sql.UseRowNumberForPaging();
                sql.CommandTimeout(30);
            })
            .Options;
        return new ApplicationDbContext(options);
    }
}

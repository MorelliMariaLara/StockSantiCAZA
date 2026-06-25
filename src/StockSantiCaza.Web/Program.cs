using Microsoft.EntityFrameworkCore;
using StockSantiCaza.Web.Components;
using StockSantiCaza.Web.Data;
using StockSantiCaza.Web.Services.Facturacion;
using StockSantiCaza.Web.Services.Reportes;
using StockSantiCaza.Web.Services.Ventas;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

var connectionString = DatabaseConnection.Resolve(builder.Configuration);

builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<IVentasService, VentasService>();
builder.Services.AddScoped<IReportesService, ReportesService>();
builder.Services.AddScoped<IFacturacionElectronicaService, FacturacionElectronicaSimuladaService>();

var app = builder.Build();

app.Logger.LogInformation(
    "Conectando a SQL Server: {DatabaseTarget}",
    DatabaseConnection.Describe(connectionString));

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
    await using var db = await dbFactory.CreateDbContextAsync();
    await DbInitializer.InitializeAsync(db, app.Logger);
    app.Logger.LogInformation("Base de datos inicializada correctamente.");
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

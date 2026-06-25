using Microsoft.EntityFrameworkCore;
using StockSantiCaza.Web.Components;
using StockSantiCaza.Web.Data;
using StockSantiCaza.Web.Services.Facturacion;
using StockSantiCaza.Web.Services.Reportes;
using StockSantiCaza.Web.Services.Ventas;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not configured.");

builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<IVentasService, VentasService>();
builder.Services.AddScoped<IReportesService, ReportesService>();
builder.Services.AddScoped<IFacturacionElectronicaService, FacturacionElectronicaSimuladaService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
    await using var db = await dbFactory.CreateDbContextAsync();
    await DbInitializer.InitializeAsync(db);
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

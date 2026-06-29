using EntityFrameworkCore.UseRowNumberForPaging;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StockSantiCaza.Web.Data;
using StockSantiCaza.Web.Models;
using StockSantiCaza.Web.Services.Auth;
using StockSantiCaza.Web.Services.Reportes;
using StockSantiCaza.Web.Services.Stock;
using StockSantiCaza.Web.Services.Usuarios;
using StockSantiCaza.Web.Services.Ventas;
using StockSantiCaza.Web.Services.Ui;

var builder = WebApplication.CreateBuilder(args);

// Opcional: appsettings.Local.json (gitignored) pisa Development/Production en tu máquina.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not configured.");

var sqlServer = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries)
    .Select(part => part.Trim())
    .FirstOrDefault(part => part.StartsWith("Server=", StringComparison.OrdinalIgnoreCase))
    ?? "Server=?";

Console.WriteLine($"[StockSantiCAZA] Entorno: {builder.Environment.EnvironmentName}");
Console.WriteLine($"[StockSantiCAZA] {sqlServer}");

builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(
        connectionString,
        providerOptions =>
        {
            providerOptions.UseRowNumberForPaging();
            providerOptions.EnableRetryOnFailure();
            providerOptions.CommandTimeout(60);
        }));

builder.Services.AddSingleton<PasswordHasher<Usuario>>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUsuariosService, UsuariosService>();
builder.Services.AddScoped<IVentasService, VentasService>();
builder.Services.AddScoped<IReportesService, ReportesService>();
builder.Services.AddScoped<IStockImportService, StockImportService>();
builder.Services.AddScoped<ConfirmDialogService>();
builder.Services.AddScoped<AlertDialogService>();

var app = builder.Build();

await DbInitializer.InitializeAsync(app.Services);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();

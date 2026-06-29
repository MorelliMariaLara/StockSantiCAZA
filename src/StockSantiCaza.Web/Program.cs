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

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not configured.");

builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(
        connectionString,
        sqlOptions =>
        {
            // SQL Server 2016 (Ferozo): timeout y reintentos; Encrypt=False va en la cadena de conexión.
            sqlOptions.EnableRetryOnFailure();
            sqlOptions.CommandTimeout(60);
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

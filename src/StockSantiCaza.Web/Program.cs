using EntityFrameworkCore.UseRowNumberForPaging;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StockSantiCaza.Web.Data;
using StockSantiCaza.Web.Helpers;
using StockSantiCaza.Web.Models;
using StockSantiCaza.Web.Services.Auth;
using StockSantiCaza.Web.Services.Reportes;
using StockSantiCaza.Web.Services.Stock;
using StockSantiCaza.Web.Services.Usuarios;
using StockSantiCaza.Web.Services.Ventas;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.Cookie.Name = "StockSanti.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromHours(8);
});

builder.Services.AddHttpContextAccessor();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.WriteLine("[StockSantiCAZA] ADVERTENCIA: falta ConnectionStrings:DefaultConnection. Agregue appsettings.Production.json en el servidor.");
    connectionString = "Server=127.0.0.1;Database=__sin_configurar__;User Id=__;Password=__;TrustServerCertificate=True;Encrypt=False;Connect Timeout=5";
}

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

var app = builder.Build();

// La base de datos ya debe existir en DonWeb (sin migración automática al iniciar).

app.Lifetime.ApplicationStarted.Register(() =>
{
    app.Logger.LogInformation(
        "[StockSantiCAZA] Aplicación iniciada. Entorno={Environment}. Diagnóstico: GET /api/health y GET /api/health/db",
        app.Environment.EnvironmentName);
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            var ex = context.Features.Get<IExceptionHandlerFeature>()?.Error;
            var mensaje = ex is null ? "Error interno del servidor." : ExceptionHelper.ObtenerMensaje(ex);

            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsJsonAsync(new { error = mensaje });
                return;
            }

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "text/html; charset=utf-8";
            var mensajeHtml = System.Net.WebUtility.HtmlEncode(mensaje);
            await context.Response.WriteAsync(
                "<!DOCTYPE html><html lang=\"es\"><head><meta charset=\"utf-8\"><title>Error</title></head>" +
                "<body style=\"font-family:sans-serif;padding:2rem\">" +
                "<h1>Error en el servidor</h1><p>" + mensajeHtml + "</p>" +
                "<p><a href=\"/login\">Volver al login</a> · <a href=\"/api/health\">Diagnóstico API</a></p>" +
                "</body></html>");
        });
    });
}

app.UseForwardedHeaders();

// En Ferozo/IIS el HTTPS ya lo maneja el hosting; forzar redirección suele causar error 500.
var disableHttpsRedirect = app.Configuration.GetValue("Hosting:DisableHttpsRedirection", true);
if (!disableHttpsRedirect && !app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
else if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.MapControllers();

var htmlRoutes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    ["/"] = "login.html",
    ["/login"] = "login.html",
    ["/inicio"] = "index.html",
    ["/clientes"] = "clientes.html",
    ["/stock"] = "stock.html",
    ["/ventas"] = "ventas.html",
    ["/ventas/nueva"] = "ventas/nueva.html",
    ["/proveedores"] = "proveedores.html",
    ["/reportes"] = "reportes.html",
    ["/usuarios"] = "usuarios.html",
    ["/error"] = "error.html"
};

foreach (var (route, file) in htmlRoutes)
{
    app.MapGet(route, async context =>
    {
        var filePath = Path.Combine(app.Environment.WebRootPath, file);
        if (!File.Exists(filePath))
        {
            context.Response.StatusCode = 404;
            return;
        }

        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.SendFileAsync(filePath);
    });
}

// Cualquier otra ruta (excepto /api y archivos estáticos) muestra el login.
app.MapFallback(async context =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.StatusCode = 404;
        return;
    }

    var loginPath = Path.Combine(app.Environment.WebRootPath, "login.html");
    if (!File.Exists(loginPath))
    {
        context.Response.StatusCode = 404;
        return;
    }

    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.SendFileAsync(loginPath);
});

app.Run();
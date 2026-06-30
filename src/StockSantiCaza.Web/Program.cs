using EntityFrameworkCore.UseRowNumberForPaging;
using Microsoft.AspNetCore.Diagnostics;
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

builder.Services.AddSingleton<DatabaseInitializationState>();
builder.Services.AddHostedService<DatabaseInitializationHostedService>();

var app = builder.Build();

app.Use(async (context, next) =>
{
    if (!context.Request.Path.StartsWithSegments("/api") ||
        context.Request.Path.StartsWithSegments("/api/health"))
    {
        await next();
        return;
    }

    var initState = context.RequestServices.GetRequiredService<DatabaseInitializationState>();
    if (initState.IsReady)
    {
        await next();
        return;
    }

    if (initState.Status == DatabaseInitStatus.Failed)
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsJsonAsync(new
        {
            error = "No se pudo inicializar la base de datos. Revise la cadena de conexión en appsettings.Production.json.",
            detail = initState.ErrorMessage
        });
        return;
    }

    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
    context.Response.ContentType = "application/json; charset=utf-8";
    await context.Response.WriteAsJsonAsync(new
    {
        error = "La base de datos se está inicializando. Espere unos segundos e intente de nuevo."
    });
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

            context.Response.Redirect("/error");
        });
    });
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.MapControllers();

var htmlRoutes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    ["/"] = "index.html",
    ["/login"] = "login.html",
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

app.Run();

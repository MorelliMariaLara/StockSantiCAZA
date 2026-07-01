using EntityFrameworkCore.UseRowNumberForPaging;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StockSantiCaza.Web.Configuration;
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

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddResponseCompression(options => options.EnableForHttps = true);
}

var keysPath = Path.Combine(builder.Environment.ContentRootPath, "keys");
Directory.CreateDirectory(keysPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
    .SetApplicationName("StockSantiCaza.Web");

builder.Services.AddHttpContextAccessor();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var connectionString = ConnectionStringResolver.Resolve(builder.Configuration);

var sqlServer = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries)
    .Select(part => part.Trim())
    .FirstOrDefault(part => part.StartsWith("Server=", StringComparison.OrdinalIgnoreCase))
    ?? "Server=?";

Console.WriteLine($"[StockSantiCAZA] Entorno: {builder.Environment.EnvironmentName}");
Console.WriteLine($"[StockSantiCAZA] SQL: {sqlServer}");

if (!builder.Environment.IsDevelopment()
    && (sqlServer.Contains("LARA-NB", StringComparison.OrdinalIgnoreCase)
        || sqlServer.Contains("localhost", StringComparison.OrdinalIgnoreCase)
        || sqlServer.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase)))
{
    Console.WriteLine("[StockSantiCAZA] ADVERTENCIA: en producción está usando un servidor SQL local.");
    Console.WriteLine("[StockSantiCAZA] Suba appsettings.Production.json con Server=sql2016 a public_html.");
}

builder.Services.AddPooledDbContextFactory<ApplicationDbContext>(
    options => options.UseSqlServer(
        connectionString,
        providerOptions =>
        {
            providerOptions.UseRowNumberForPaging();
            providerOptions.CommandTimeout(30);
            if (builder.Environment.IsDevelopment())
            {
                providerOptions.EnableRetryOnFailure();
            }
        }),
    poolSize: builder.Environment.IsDevelopment() ? 32 : 8);

builder.Services.AddSingleton<PasswordHasher<Usuario>>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUsuariosService, UsuariosService>();
builder.Services.AddScoped<IVentasService, VentasService>();
builder.Services.AddScoped<IReportesService, ReportesService>();
builder.Services.AddScoped<IStockImportService, StockImportService>();

var app = builder.Build();

if (app.Environment.IsDevelopment()
    && !builder.Configuration.GetValue<bool>("Database:SkipInitialization"))
{
    try
    {
        await DbInitializer.InitializeAsync(
            app.Services,
            builder.Configuration,
            app.Logger);
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "[StockSantiCAZA] DbInitializer omitido (revise la conexión local).");
    }
}

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

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseResponseCompression();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
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

var publicHtmlRoutes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "/login", "/error" };

static string HomeRuta(UsuarioSesion usuario) =>
    usuario.EsAdministrador ? "/" : "/ventas/nueva";

static ModuloSistema? ModuloPorRuta(string route) => route switch
{
    "/" => ModuloSistema.Dashboard,
    "/clientes" => ModuloSistema.Clientes,
    "/stock" => ModuloSistema.Stock,
    "/ventas" or "/ventas/nueva" => ModuloSistema.Ventas,
    "/proveedores" => ModuloSistema.Proveedores,
    "/reportes" => ModuloSistema.Reportes,
    "/usuarios" => ModuloSistema.Usuarios,
    _ => null
};

foreach (var (route, file) in htmlRoutes)
{
    app.MapGet(route, async context =>
    {
        if (!publicHtmlRoutes.Contains(route))
        {
            var authService = context.RequestServices.GetRequiredService<IAuthService>();
            var usuario = authService.UsuarioActual;
            if (usuario is null)
            {
                context.Response.Redirect("/login");
                return;
            }

            var modulo = ModuloPorRuta(route);
            if (modulo is not null && !usuario.PuedeAcceder(modulo.Value))
            {
                context.Response.Redirect(HomeRuta(usuario));
                return;
            }
        }

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

# Publica StockSantiCAZA listo para subir a Ferozo por FileZilla.
# Uso: clic derecho -> "Ejecutar con PowerShell" o desde la consola:
#   .\scripts\publicar-donweb.ps1

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "src\StockSantiCaza.Web\StockSantiCaza.Web.csproj"
$publishDir = Join-Path $env:USERPROFILE "Desktop\Publish"
$productionSettings = Join-Path $repoRoot "src\StockSantiCaza.Web\appsettings.Production.json"
$productionExample = Join-Path $repoRoot "src\StockSantiCaza.Web\appsettings.Production.example.json"

Write-Host "=== StockSantiCAZA - Publicar para DonWeb ===" -ForegroundColor Cyan

if (-not (Test-Path $productionSettings)) {
    Write-Host "No existe appsettings.Production.json" -ForegroundColor Yellow
    if (Test-Path $productionExample) {
        Copy-Item $productionExample $productionSettings
        Write-Host "Se copio el ejemplo. EDITA la contraseña SQL en:" -ForegroundColor Yellow
        Write-Host "  $productionSettings" -ForegroundColor White
        Read-Host "Presiona Enter cuando hayas guardado la contraseña"
    } else {
        throw "Falta appsettings.Production.example.json"
    }
}

if (Test-Path $publishDir) {
    Write-Host "Limpiando carpeta publish anterior..." -ForegroundColor Gray
    Remove-Item $publishDir -Recurse -Force
}

Write-Host "Compilando y publicando en Release..." -ForegroundColor Green
dotnet publish $project -c Release -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish fallo" }

Copy-Item $productionSettings (Join-Path $publishDir "appsettings.Production.json") -Force

$logsDir = Join-Path $publishDir "logs"
if (-not (Test-Path $logsDir)) {
    New-Item -ItemType Directory -Path $logsDir | Out-Null
}

Write-Host ""
Write-Host "LISTO. Carpeta para FileZilla:" -ForegroundColor Green
Write-Host "  $publishDir" -ForegroundColor White
Write-Host ""
Write-Host "Subi TODO el contenido a public_html en Ferozo (no solo wwwroot)." -ForegroundColor Cyan
Write-Host "Luego proba: https://TU-DOMINIO/api/health" -ForegroundColor Cyan

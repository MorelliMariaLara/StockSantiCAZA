# Publicar StockSantiCAZA para Ferozo / DonWeb (carpeta local + FileZilla)
# Uso: .\scripts\publicar-donweb.ps1

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "src\StockSantiCaza.Web\StockSantiCaza.Web.csproj"
$publishDir = Join-Path $repoRoot "publish"
$productionSettings = Join-Path $repoRoot "src\StockSantiCaza.Web\appsettings.Production.json"

Write-Host "=== StockSantiCAZA - Publicar ===" -ForegroundColor Cyan

if (-not (Test-Path $productionSettings)) {
    throw "Falta appsettings.Production.json en src\StockSantiCaza.Web\"
}

if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}

Write-Host "Publicando Release..." -ForegroundColor Green
dotnet publish $project -c Release -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish fallo" }

Copy-Item $productionSettings (Join-Path $publishDir "appsettings.Production.json") -Force
New-Item -ItemType Directory -Path (Join-Path $publishDir "logs") -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $publishDir "keys") -Force | Out-Null

$dll = Join-Path $publishDir "StockSantiCaza.Web.dll"
$webConfig = Join-Path $publishDir "web.config"
if (-not (Test-Path $dll)) { throw "Falta StockSantiCaza.Web.dll" }
if (-not (Test-Path $webConfig)) { throw "Falta web.config" }

Write-Host ""
Write-Host "LISTO: $publishDir" -ForegroundColor Green
Write-Host ""
Write-Host "=== Subir con FileZilla ===" -ForegroundColor Cyan
Write-Host "Servidor:  w400048.ferozo.com"
Write-Host "Usuario:   w400048@w400048.ferozo.com"
Write-Host "Ruta:      stock.santicazaarmeria.com.ar/public_html"
Write-Host "IMPORTANTE: subi appsettings.Production.json junto al .dll (misma carpeta en public_html)." -ForegroundColor Yellow
Write-Host "Subi TODO el contenido de publish\ (no la carpeta publish en si)."
Write-Host ""
Write-Host "Probar: https://stock.santicazaarmeria.com.ar/api/health" -ForegroundColor Cyan

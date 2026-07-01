# Publicar StockSantiCAZA para Ferozo / DonWeb
# Uso (PowerShell en Windows): .\scripts\publicar-donweb.ps1

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "src\StockSantiCaza.Web\StockSantiCaza.Web.csproj"
$publishDir = Join-Path $repoRoot "publish"
$productionSettings = Join-Path $repoRoot "src\StockSantiCaza.Web\appsettings.Production.json"
$productionExample = Join-Path $repoRoot "src\StockSantiCaza.Web\appsettings.Production.example.json"

Write-Host "=== StockSantiCAZA - Publicar ===" -ForegroundColor Cyan

if (-not (Test-Path $productionSettings)) {
    if (Test-Path $productionExample) {
        Copy-Item $productionExample $productionSettings
        Write-Host "Se creo appsettings.Production.json desde el ejemplo." -ForegroundColor Yellow
        Write-Host "EDITA la contraseña SQL en: $productionSettings" -ForegroundColor Yellow
        Read-Host "Presiona Enter cuando hayas guardado la contraseña"
    } else {
        throw "Falta appsettings.Production.example.json"
    }
}

if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}

Write-Host "Publicando Release (framework-dependent)..." -ForegroundColor Green
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
Write-Host "Subi TODO el contenido a public_html en Ferozo." -ForegroundColor Cyan
Write-Host "Proba: https://TU-DOMINIO/api/health" -ForegroundColor Cyan

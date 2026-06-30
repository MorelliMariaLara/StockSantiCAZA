# Publish 32-bit para pools x86 en Ferozo (si win-x64 da error 500.31).
# Uso: .\scripts\publicar-donweb-x86.ps1

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "src\StockSantiCaza.Web\StockSantiCaza.Web.csproj"
$publishDir = Join-Path $env:USERPROFILE "Desktop\Publish"
$productionSettings = Join-Path $repoRoot "src\StockSantiCaza.Web\appsettings.Production.json"

Write-Host "=== Publish autocontenido win-x86 (32 bits) ===" -ForegroundColor Cyan

if (-not (Test-Path $productionSettings)) {
    throw "Crea appsettings.Production.json antes de publicar."
}

if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}

dotnet publish $project -c Release -r win-x86 --self-contained true -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish fallo" }

Copy-Item $productionSettings (Join-Path $publishDir "appsettings.Production.json") -Force
New-Item -ItemType Directory -Path (Join-Path $publishDir "logs") -Force | Out-Null

Write-Host "Listo: $publishDir" -ForegroundColor Green
Write-Host "Subi todo a public_html y proba /api/health" -ForegroundColor Cyan

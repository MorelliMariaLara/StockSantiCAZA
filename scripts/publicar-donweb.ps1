# Publica StockSantiCAZA para Ferozo (32 bits — habitual en hosting compartido).
# Uso: .\scripts\publicar-donweb.ps1

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "src\StockSantiCaza.Web\StockSantiCaza.Web.csproj"
$publishDir = Join-Path $env:USERPROFILE "Desktop\Publish"
$productionSettings = Join-Path $repoRoot "src\StockSantiCaza.Web\appsettings.Production.json"
$productionExample = Join-Path $repoRoot "src\StockSantiCaza.Web\appsettings.Production.example.json"

Write-Host "=== StockSantiCAZA - Publicar para DonWeb (win-x86) ===" -ForegroundColor Cyan

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

Write-Host "Compilando publish autocontenido 32 bits (win-x86)..." -ForegroundColor Green
dotnet publish $project -c Release -r win-x86 --self-contained true -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish fallo" }

Copy-Item $productionSettings (Join-Path $publishDir "appsettings.Production.json") -Force

$logsDir = Join-Path $publishDir "logs"
New-Item -ItemType Directory -Path $logsDir -Force | Out-Null

$exe = Join-Path $publishDir "StockSantiCaza.Web.exe"
$webConfig = Join-Path $publishDir "web.config"
$dllCount = (Get-ChildItem $publishDir -Filter "*.dll" -File).Count
$fileCount = (Get-ChildItem $publishDir -Recurse -File).Count

if (-not (Test-Path $exe)) { throw "Falta StockSantiCaza.Web.exe en el publish" }
if (-not (Test-Path $webConfig)) { throw "Falta web.config en el publish" }
if ($dllCount -lt 30) {
    Write-Host "ADVERTENCIA: solo $dllCount DLLs. ¿Subiste toda la carpeta Publish?" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "LISTO ($fileCount archivos, $dllCount DLLs)" -ForegroundColor Green
Write-Host "  $publishDir" -ForegroundColor White
Write-Host ""
Write-Host "Subi TODO el contenido a public_html (no solo wwwroot ni solo el .exe)." -ForegroundColor Cyan
Write-Host "Crea o verifica carpeta logs\ en el servidor." -ForegroundColor Cyan
Write-Host "Proba: https://TU-DOMINIO/api/health" -ForegroundColor Cyan
Write-Host ""
Write-Host "Si da error 502.5, probá 64 bits: .\scripts\publicar-donweb-x64.ps1" -ForegroundColor Yellow

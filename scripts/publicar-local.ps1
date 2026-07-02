# Publica StockSantiCAZA en C:\StockSantiCaza para uso como app local de escritorio.
# Uso: .\scripts\publicar-local.ps1

$ErrorActionPreference = "Stop"
$destino = "C:\StockSantiCaza"
$proyecto = Join-Path $PSScriptRoot "..\src\StockSantiCaza.Web\StockSantiCaza.Web.csproj"

Write-Host "Publicando en $destino ..." -ForegroundColor Cyan
dotnet publish $proyecto -c Release -o $destino

$batOrigen = Join-Path $PSScriptRoot "iniciar-local.bat"
$batDestino = Join-Path $destino "Abrir StockSantiCAZA.bat"
Copy-Item -Path $batOrigen -Destination $batDestino -Force

Write-Host ""
Write-Host "Listo." -ForegroundColor Green
Write-Host "  Carpeta:  $destino"
Write-Host "  Lanzador: $batDestino"
Write-Host ""
Write-Host "Doble clic en 'Abrir StockSantiCAZA.bat' o creá un acceso directo en el Escritorio / Inicio de Windows."

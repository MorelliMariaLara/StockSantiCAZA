#!/usr/bin/env bash
# Publicar StockSantiCAZA para Ferozo / DonWeb (carpeta local + FileZilla)
# Uso: ./scripts/publicar-donweb.sh

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PROJECT="$REPO_ROOT/src/StockSantiCaza.Web/StockSantiCaza.Web.csproj"
PUBLISH_DIR="$REPO_ROOT/publish"
PRODUCTION_SETTINGS="$REPO_ROOT/src/StockSantiCaza.Web/appsettings.Production.json"

echo "=== StockSantiCAZA - Publicar ==="

if [[ ! -f "$PRODUCTION_SETTINGS" ]]; then
  echo "Falta appsettings.Production.json en src/StockSantiCaza.Web/" >&2
  exit 1
fi

rm -rf "$PUBLISH_DIR"

echo "Publicando Release..."
dotnet publish "$PROJECT" -c Release -o "$PUBLISH_DIR"

cp "$PRODUCTION_SETTINGS" "$PUBLISH_DIR/appsettings.Production.json"
mkdir -p "$PUBLISH_DIR/logs" "$PUBLISH_DIR/keys"

test -f "$PUBLISH_DIR/StockSantiCaza.Web.dll"
test -f "$PUBLISH_DIR/web.config"

echo ""
echo "LISTO: $PUBLISH_DIR"
echo ""
echo "=== Subir con FileZilla ==="
echo "Servidor:  w400048.ferozo.com"
echo "Usuario:   w400048@w400048.ferozo.com"
echo "Ruta:      stock.santicazaarmeria.com.ar/public_html"
echo "Subi TODO el contenido de publish/ (no la carpeta publish en si)."
echo ""
echo "Probar: https://stock.santicazaarmeria.com.ar/api/health"

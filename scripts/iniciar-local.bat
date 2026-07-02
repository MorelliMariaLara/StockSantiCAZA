@echo off
setlocal EnableExtensions

REM ============================================================
REM  StockSantiCAZA — lanzador local (tipo aplicación de escritorio)
REM  Copiá este archivo junto a la carpeta publicada o editá APP_DIR.
REM ============================================================

REM Carpeta donde publicaste la app (dotnet publish -o C:\StockSantiCaza)
set "APP_DIR=C:\StockSantiCaza"

REM Puerto HTTP local (mismo que launchSettings.json)
set "APP_PORT=53096"
set "APP_URL=http://127.0.0.1:%APP_PORT%"
set "LOGIN_URL=%APP_URL%/login"

REM Si este .bat está dentro de scripts\ del repo, descomentá la línea de abajo:
REM set "APP_DIR=%~dp0..\publish"

set "APP_EXE=%APP_DIR%\StockSantiCaza.Web.exe"

if not exist "%APP_EXE%" (
    echo.
    echo [ERROR] No se encuentra:
    echo   %APP_EXE%
    echo.
    echo Publicá primero con:
    echo   dotnet publish src\StockSantiCaza.Web -c Release -o C:\StockSantiCaza
    echo.
    echo O cambiá APP_DIR al inicio de este archivo.
    echo.
    pause
    exit /b 1
)

cd /d "%APP_DIR%"

REM ¿Ya está corriendo?
powershell -NoProfile -Command ^
  "try { $r = Invoke-WebRequest -Uri '%APP_URL%/api/health' -UseBasicParsing -TimeoutSec 2; exit 0 } catch { exit 1 }" >nul 2>&1
if %errorlevel%==0 goto abrir

echo Iniciando StockSantiCAZA en %APP_URL% ...
set "ASPNETCORE_ENVIRONMENT=Development"
start "StockSantiCAZA" /min "" "%APP_EXE%" --urls "%APP_URL%"

REM Esperar a que el servidor responda (hasta ~30 s)
set /a INTENTOS=0
:esperar
set /a INTENTOS+=1
if %INTENTOS% GTR 15 (
    echo [ERROR] El servidor no respondió a tiempo. Revisá SQL Server local.
    pause
    exit /b 1
)
timeout /t 2 /nobreak >nul
powershell -NoProfile -Command ^
  "try { $r = Invoke-WebRequest -Uri '%APP_URL%/api/health' -UseBasicParsing -TimeoutSec 2; exit 0 } catch { exit 1 }" >nul 2>&1
if not %errorlevel%==0 goto esperar

:abrir
echo Abriendo StockSantiCAZA...

REM Edge (viene con Windows 10/11)
if exist "%ProgramFiles(x86)%\Microsoft\Edge\Application\msedge.exe" (
    start "" "%ProgramFiles(x86)%\Microsoft\Edge\Application\msedge.exe" --app="%LOGIN_URL%"
    exit /b 0
)
if exist "%ProgramFiles%\Microsoft\Edge\Application\msedge.exe" (
    start "" "%ProgramFiles%\Microsoft\Edge\Application\msedge.exe" --app="%LOGIN_URL%"
    exit /b 0
)

REM Chrome
if exist "%ProgramFiles%\Google\Chrome\Application\chrome.exe" (
    start "" "%ProgramFiles%\Google\Chrome\Application\chrome.exe" --app="%LOGIN_URL%"
    exit /b 0
)
if exist "%LocalAppData%\Google\Chrome\Application\chrome.exe" (
    start "" "%LocalAppData%\Google\Chrome\Application\chrome.exe" --app="%LOGIN_URL%"
    exit /b 0
)

REM Sin navegador compatible: abre el predeterminado
start "" "%LOGIN_URL%"
exit /b 0

# StockSantiCAZA

ASP.NET Core 6 web app for an armory's stock/sales management. Single web process
(`src/StockSantiCaza.Web`) serving an HTML+JS frontend (`wwwroot/`) plus a REST API
(`/api/*`) with cookie sessions, backed by **SQL Server** via EF Core. See `README.md`
for routes and module overview.

## Cursor Cloud specific instructions

### Services
There is one app service plus a database:
- **Web app** — `dotnet run --project src/StockSantiCaza.Web` (ASP.NET Core / Kestrel).
  Standard commands: `dotnet restore`, `dotnet build src/StockSantiCaza.Web`,
  `dotnet run --project src/StockSantiCaza.Web`. There are no automated test projects.
- **SQL Server** — required. The app fails fast at startup if `DefaultConnection` is not
  reachable. It runs as a Docker container named `mssql` (image
  `mcr.microsoft.com/mssql/server:2022-latest`).

### Database connection (non-obvious)
- `appsettings.json` / `appsettings.Development.json` point at a Windows SQL Express host
  that does NOT exist here. The working local connection string lives in
  `src/StockSantiCaza.Web/appsettings.Local.json` (gitignored, loaded last so it wins).
  It targets `Server=127.0.0.1,1433;User Id=sa;Password=StockSanti123!`.
- The connection can also be overridden with the `ConnectionStrings__DefaultConnection`
  env var.
- Schema is **not** created at startup in production (DonWeb). Use SQL scripts in
  `scripts/sql/` for the remote database. For local dev, ensure SQL Server is running and
  the schema exists (or run the app once with `DbInitializer` enabled locally if needed).
- Default admin (if seeded manually): **`admin` / `Admin123!`** or see
  `scripts/sql/003-limpiar-bd-y-admin-santi.sql`.

### Starting services on a fresh VM (Docker is not auto-started here)
The update script only refreshes NuGet packages. Before running the app you must bring up
Docker and SQL Server (the container persists in the snapshot but is not auto-started):
1. Start the daemon if not running: `sudo dockerd` (run it in a background/tmux session).
2. Start SQL Server: `sudo docker start mssql` (or recreate it if missing:
   `sudo docker run -d --name mssql -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD=StockSanti123! -e MSSQL_PID=Developer -p 1433:1433 --restart unless-stopped mcr.microsoft.com/mssql/server:2022-latest`).
   Wait ~20s for "SQL Server is now ready for client connections".
3. Run the app: `ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/StockSantiCaza.Web`.
   It listens on `https://localhost:53095` and `http://localhost:53096` (HTTP redirects to
   HTTPS via `UseHttpsRedirection`, so use the HTTPS URL for curl with `-k`).

### Browser testing gotcha
The pages render fine, but in the cloud VM's virtual display Chrome can leave the page
blank due to a GPU compositing/paint glitch (the DOM is present; the canvas just doesn't
paint). Launch Chrome with `--disable-gpu --disable-gpu-compositing` and, if a page still
shows blank, force one repaint (toggle fullscreen F11 then Escape, or open/close DevTools).
This is an environment rendering quirk, not an app bug.

### Known pre-existing app bug (not environment-related)
`GET /api/reportes/dashboard` returns HTTP 500 because a nullable `DateOnly?` query
parameter (`fecha`) cannot be model-bound without a value, so the dashboard indicators show
an "Error 500" toast. The rest of the app (login, clients, stock, ventas, etc.) works.

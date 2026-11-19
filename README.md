# VELORA

Arabic-first online store for mirrors and handmade home decor.

## Run locally

```powershell
npm install
dotnet run --project backend/Velora.Api
```

In a second terminal:

```powershell
npm run dev
```

The storefront runs at the local Vite URL. The store dashboard is available at `/dashboard` or `/portal.html`. Vite proxies `/api` requests to the local ASP.NET Core service on port `5080`.

## Storefront pages

- `/` — brand home page
- `/shop` — searchable, filterable product collection
- `/mirror/:id` — product details, options, and shopping bag
- `/custom` — custom mirror request builder
- `/contact` — customer contact form

The ASP.NET Core 8 API stores products, customer requests, and store settings in SQL Server. Copy `backend/Velora.Api/appsettings.example.json` to `backend/Velora.Api/appsettings.json` for a local Windows-authenticated SQL Server Express setup, then adjust the connection string for your instance. For SQL authentication, set `ConnectionStrings__Velora` in your local environment rather than committing credentials. Start SQL Server, run `backend/Velora.Api/Data/sqlserver-setup.sql` in SQL Server Management Studio, then run the API. It creates the application tables on startup.

To override the connection string without editing files, set `ConnectionStrings__Velora` before starting the API. Example for SQL Server Express: `Server=localhost\\SQLEXPRESS;Database=Velora;Trusted_Connection=True;TrustServerCertificate=True`. When the SQL Server tables are empty, the API migrates products and requests from the previous `App_Data/velora.db` SQLite file; otherwise it imports the legacy JSON files or the seed catalog. Uploaded product photos are resized in the browser and stored with the product record. If the API is offline, the dashboard and storefront fall back to browser storage. Requests continue to open a prefilled WhatsApp conversation.

Dashboard access uses the configured `VeloraAdmin:Email` and `VeloraAdmin:Password` server settings. Set these as environment variables before starting the API; do not commit them.

API endpoints include `GET /api/products`, `GET/PUT /api/admin/products`, `POST /api/requests`, and `GET/PATCH /api/admin/requests`.

## Build

```powershell
npm run build
npm run preview
```

## Free hosting

See [the MonsterASP.NET deployment guide](docs/free-hosting.md) for the current GitHub Actions deployment setup and the free-plan limits.

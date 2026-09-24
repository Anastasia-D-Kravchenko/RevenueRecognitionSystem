# Revenue Recognition System

REST API for corporation ABC: customers, software, contracts, payments, subscriptions, discounts and revenue calculation.

## Structure

```
RevenueRecognitionSystem.Api/
├── Program.cs
├── appsettings.json
├── Data/
│   ├── AppDbContext.cs
│   └── DbInitializer.cs
├── Entities/
├── Configurations/
├── DTOs/
├── Exceptions/
├── Middleware/
├── Services/
├── Controllers/
└── OpenApi/
    └── BearerSecuritySchemeTransformer.cs
```

## Run

Start SQL Server in Docker:

```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong!Passw0rd" \
  -p 1433:1433 --name sqlserver -d mcr.microsoft.com/mssql/server:2022-latest
```
The connection string in `appsettings.json` uses this local placeholder password; change both together, and use an environment variable (`ConnectionStrings__DefaultConnection`) or user-secrets for anything beyond a local demo.

```bash
cd RevenueRecognitionSystem.Api
dotnet ef database update
dotnet run
```

Run the tests with `dotnet test` (xUnit).

Swagger UI: http://localhost:5080/swagger

## Authorize in Swagger

1. `POST /api/auth/login` with `{ "login": "admin", "password": "Admin123!" }`.
2. Copy the `accessToken` value.
3. Click the **Authorize** button at the top of Swagger and paste the token.

Seeded employees (created on first run if the table is empty):

| Login | Password   | Role  |
|-------|------------|-------|
| admin | Admin123!  | Admin |
| user  | User123!   | User  |

## License

MIT, see [LICENSE](LICENSE).

# Property App Scaffold

## Prerequisites
- .NET 7 SDK
- Node.js (optional, for future tooling)
- AWS CLI configured with IAM credentials

## Setup
1. `git clone <repo>` and `cd Property App`
2. Copy `env.template` to `.env` at the project root and fill in AWS RDS/S3/SNS/SQS details (SQL Server on RDS). For RDS you can set `DB_CONNECTION_STRING` or the discrete vars `DB_HOST/DB_NAME/DB_USER/DB_PASSWORD`.
3. Restore dependencies: `dotnet restore src/PropertyApp.sln`
4. Run the site: `dotnet watch --project src/PropertyWeb/PropertyWeb.csproj`

## Structure
- `src/PropertyApp.sln` – solution file
- `src/PropertyWeb` – ASP.NET Core MVC app (shared layout + Razor views)
- `src/PropertyWeb/Data` – EF Core context (`Application_context`)
- `src/PropertyWeb/Models` – Domain entities (users, properties, tickets, payments)
- `wwwroot` – static assets (CSS, JS, images)
- `ToDo.md` – high-level project checklist
- `database/schema.sql` – SQL Server bootstrap script for AWS RDS

## Next Steps
- Implement DbContext/models against the SQL Server RDS (or swap provider if you decide to use MySQL/PostgreSQL).
- Build role-specific layouts in `Views/Shared` and componentized sections under `wwwroot`.
- Integrate AWS SDK + storage once the infrastructure is provisioned.

## Database & Migrations
- Update `.env` with `DB_*` values once the SQL Server RDS endpoint is ready.
- Optional: execute `database/schema.sql` to bootstrap tables quickly (followed by `database/seed.sql` for demo data). Scripts target SQL Server syntax.
- EF Core migrations:
  - `dotnet tool install --global dotnet-ef` (if not installed)
  - `dotnet ef migrations add InitialCreate --project src/PropertyWeb/PropertyWeb.csproj`
  - `dotnet ef database update --project src/PropertyWeb/PropertyWeb.csproj`

dotnet watch --project src/PropertyWeb/PropertyWeb.csproj -- run localhost


 cd "G:\My Drive\Degree Year 3\Sem2\Cloud\Property App"
 $env:ASPNETCORE_URLS="http://localhost:5182"
 dotnet run --project src/PropertyWeb/PropertyWeb.csproj
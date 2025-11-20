using System.IO;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PropertyWeb.Data;

/// <summary>
/// Provides design-time creation of <see cref="Application_context"/> for EF Core tooling.
/// Ensures the same SQL Server connection string defined via DB_* environment variables is used when running migrations.
/// </summary>
public class ApplicationContextFactory : IDesignTimeDbContextFactory<Application_context>
{
    public Application_context CreateDbContext(string[] args)
    {
        LoadEnvFile();

        var connectionString = BuildConnectionStringFromEnvironment();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Database connection settings are missing. Ensure DB_* variables are configured (e.g. via .env).");
        }

        var optionsBuilder = new DbContextOptionsBuilder<Application_context>();
        optionsBuilder.UseSqlServer(connectionString);

        return new Application_context(optionsBuilder.Options);
    }

    private static void LoadEnvFile()
    {
        try
        {
            var basePath = Directory.GetCurrentDirectory();
            var candidates = new[]
            {
                Path.Combine(basePath, ".env"),
                Path.Combine(basePath, "..", ".env"),
                Path.Combine(basePath, "..", "..", ".env")
            };

            foreach (var path in candidates)
            {
                if (File.Exists(path))
                {
                    Env.Load(path);
                    break;
                }
            }
        }
        catch
        {
            // Ignore loading failures – we'll fall back to existing environment variables.
        }
    }

    private static string BuildConnectionStringFromEnvironment()
    {
        var host = Environment.GetEnvironmentVariable("DB_HOST");
        var port = Environment.GetEnvironmentVariable("DB_PORT") ?? "1433";
        var database = Environment.GetEnvironmentVariable("DB_NAME");
        var user = Environment.GetEnvironmentVariable("DB_USER");
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD");
        var encrypt = Environment.GetEnvironmentVariable("DB_SSL_MODE") ?? "Require";
        var encryptFlag = encrypt.Equals("disable", StringComparison.OrdinalIgnoreCase) ? "False" : "True";

        if (string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(database) ||
            string.IsNullOrWhiteSpace(user) ||
            string.IsNullOrWhiteSpace(password))
        {
            return string.Empty;
        }

        return $"Server={host},{port};Database={database};User Id={user};Password={password};Encrypt={encryptFlag};TrustServerCertificate=False;";
    }
}


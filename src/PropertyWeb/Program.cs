using DotNetEnv;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PropertyWeb.Data;
using PropertyWeb.Models;
using PropertyWeb.Controllers;
using PropertyWeb.Services;
using Amazon.S3;
using Amazon;
using Amazon.Runtime;

var builder = WebApplication.CreateBuilder(args);

Load_env_file(builder.Environment.ContentRootPath);
Configure_database(builder);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.Configure<TicketImageApiOptions>(builder.Configuration.GetSection("TicketImageApi"));

// Configure AWS S3 client
// Try to get bucket name from configuration first, then fall back to environment variable
var ticketImageOptions = new TicketImageApiOptions();
builder.Configuration.GetSection("TicketImageApi").Bind(ticketImageOptions);
var bucketName = ticketImageOptions.BucketName ?? Environment.GetEnvironmentVariable("AWS_S3_BUCKET");
var awsRegion = ticketImageOptions.Region ?? Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1";

if (!string.IsNullOrWhiteSpace(bucketName))
{
    var region = RegionEndpoint.GetBySystemName(awsRegion);
    
    // AWS SDK will automatically try to get credentials from:
    // 1. Environment variables (AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY, AWS_SESSION_TOKEN)
    // 2. AWS credentials file (~/.aws/credentials)
    // 3. EC2 instance role (if running on EC2)
    // If credentials are not found, it will throw an exception when used
    builder.Services.AddSingleton<IAmazonS3>(sp => new AmazonS3Client(region));
}

// Add HttpClient for API Gateway fallback (optional)
builder.Services.AddHttpClient<ITicketImageService, TicketImageService>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Home/Privacy";
        // For local development over HTTP, relax cookie settings
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.None;
    });

var app = builder.Build();

// Seed a default admin user if none exists
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<Application_context>();

    // Optional: ensure database is created/migrated in development
    db.Database.EnsureCreated();

    if (!db.User_set.Any(u => u.Role == "admin"))
    {
        var admin = new User_account
        {
            Id = Guid.NewGuid(),
            User_name = "System Admin",
            Email = "admin@propertyapp.local",
            Password_hash = AccountController.HashFor_admin("Admin123!"),
            Role = "admin",
            Created_at = DateTime.UtcNow
        };

        db.User_set.Add(admin);
        db.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static void Load_env_file(string content_root)
{
    var env_candidates = new[]
    {
        Path.Combine(content_root, ".env"),
        Path.Combine(content_root, "..", ".env")
    };

    foreach (var env_path in env_candidates)
    {
        if (File.Exists(env_path))
        {
            // Load .env file - DotNetEnv automatically sets environment variables
            Env.Load(env_path);
            break;
        }
    }
}

static void Configure_database(WebApplicationBuilder builder)
{
    // Prefer environment-driven configuration so we can switch RDS instances per account.
    var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        // Build from discrete env vars if full connection string not supplied
        var host = Environment.GetEnvironmentVariable("DB_HOST");
        var port = Environment.GetEnvironmentVariable("DB_PORT") ?? "1433";
        var database = Environment.GetEnvironmentVariable("DB_NAME");
        var user = Environment.GetEnvironmentVariable("DB_USER");
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD");
        var encrypt = Environment.GetEnvironmentVariable("DB_ENCRYPT") ?? "False";
        var trustCert = Environment.GetEnvironmentVariable("DB_TRUST_SERVER_CERTIFICATE") ?? "True";

        if (string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(database) ||
            string.IsNullOrWhiteSpace(user) ||
            string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Database configuration missing. Set DB_CONNECTION_STRING or DB_HOST/DB_NAME/DB_USER/DB_PASSWORD.");
        }

        connectionString =
            $"Server={host},{port};" +
            $"Database={database};" +
            $"User Id={user};" +
            $"Password={password};" +
            $"Encrypt={encrypt};" +
            $"TrustServerCertificate={trustCert};";
    }

    builder.Services.AddDbContext<Application_context>(options =>
        options.UseSqlServer(connectionString));
}

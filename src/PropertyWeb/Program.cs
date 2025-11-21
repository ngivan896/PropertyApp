using DotNetEnv;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PropertyWeb.Data;
using PropertyWeb.Models;
using PropertyWeb.Controllers;

var builder = WebApplication.CreateBuilder(args);

Load_env_file(builder.Environment.ContentRootPath);
Configure_database(builder);

// Add services to the container.
builder.Services.AddControllersWithViews();

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
            Env.Load(env_path);
            break;
        }
    }
}

static void Configure_database(WebApplicationBuilder builder)
{
    var connection_string = Build_connection_string();

    if (string.IsNullOrWhiteSpace(connection_string))
    {
        builder.Services.AddDbContext<Application_context>(options =>
            options.UseInMemoryDatabase("property_app_dev"));
        Console.WriteLine("Warning: DB_* environment variables missing. Using in-memory database.");
        return;
    }

    builder.Services.AddDbContext<Application_context>(options =>
        options.UseSqlServer(connection_string));
}

static string Build_connection_string()
{
    var host = Environment.GetEnvironmentVariable("DB_HOST");
    var port = Environment.GetEnvironmentVariable("DB_PORT") ?? "1433";
    var database = Environment.GetEnvironmentVariable("DB_NAME");
    var user = Environment.GetEnvironmentVariable("DB_USER");
    var password = Environment.GetEnvironmentVariable("DB_PASSWORD");

    if (string.IsNullOrWhiteSpace(host) ||
        string.IsNullOrWhiteSpace(database) ||
        string.IsNullOrWhiteSpace(user) ||
        string.IsNullOrWhiteSpace(password))
    {
        return string.Empty;
    }

    // Development connection string for RDS SQL Server – disable encryption and trust certificate
    return $"Server={host},{port};Database={database};User Id={user};Password={password};Encrypt=False;TrustServerCertificate=True;";
}

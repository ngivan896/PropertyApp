using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using PropertyWeb.Data;

var builder = WebApplication.CreateBuilder(args);

Load_env_file(builder.Environment.ContentRootPath);
Configure_database(builder);

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

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
    var port = Environment.GetEnvironmentVariable("DB_PORT") ?? "3306";
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

    var encrypt_pref = Environment.GetEnvironmentVariable("DB_SSL_MODE") ?? "Require";
    var encrypt_flag = encrypt_pref.Equals("disable", StringComparison.OrdinalIgnoreCase) ? "False" : "True";

    return $"Server={host},{port};Database={database};User Id={user};Password={password};Encrypt={encrypt_flag};TrustServerCertificate=False;";
}

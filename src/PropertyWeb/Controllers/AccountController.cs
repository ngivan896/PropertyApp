using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyWeb.Data;
using PropertyWeb.Models;

namespace PropertyWeb.Controllers;

public class AccountController : Controller
{
    private readonly Application_context _db;

    public AccountController(Application_context db)
    {
        _db = db;
    }

    // Public self-registration is currently disabled. Accounts are provisioned by admins.

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        User_account? user = null;

        try
        {
            // Try to authenticate from database
            var passwordHash = Hash_password(model.Password);
            user = await _db.User_set.FirstOrDefaultAsync(x => x.Email == model.Email && x.Password_hash == passwordHash);
        }
        catch
        {
            // Database connection failed - use development mode test accounts
            if (HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
            {
                user = GetDevelopmentTestUser(model.Email, model.Password);
            }
        }

        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        await SignInAsync(user);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        // Simple role-based landing: admins go to Admin, others to Dashboard
        if (string.Equals(user.Role, "admin", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction("Index", "Admin");
        }

        return RedirectToAction("Index", "Dashboard");
    }

    private User_account? GetDevelopmentTestUser(string email, string password)
    {
        // Development mode test accounts (only works when database is unavailable)
        // These are hardcoded for frontend development only
        var testUsers = new Dictionary<string, (string password, string role, string name)>
        {
            { "admin@test.com", ("Admin123!", "admin", "Test Admin") },
            { "owner@test.com", ("Owner123!", "owner", "Test Owner") },
            { "worker@test.com", ("Worker123!", "worker", "Test Worker") }
        };

        if (testUsers.TryGetValue(email.ToLowerInvariant(), out var userInfo))
        {
            if (userInfo.password == password)
            {
                return new User_account
                {
                    Id = Guid.NewGuid(),
                    Email = email,
                    User_name = userInfo.name,
                    Role = userInfo.role,
                    Password_hash = Hash_password(password),
                    Created_at = DateTime.UtcNow
                };
            }
        }

        return null;
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    // External login (Google/Apple) is disabled in this version.

    private async Task SignInAsync(User_account user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.User_name),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    }

    internal static string HashFor_admin(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }

    private static string Hash_password(string password) => HashFor_admin(password);
}



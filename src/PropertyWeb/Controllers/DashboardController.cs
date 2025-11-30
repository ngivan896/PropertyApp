using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PropertyWeb.Controllers;

[Authorize]
public class DashboardController : Controller
{
    public IActionResult Index()
    {
        // Redirect based on role to their respective dashboards
        if (User.IsInRole("admin"))
        {
            return RedirectToAction("Index", "Admin");
        }
        else if (User.IsInRole("worker"))
        {
            return RedirectToAction("Index", "Worker");
        }
        else if (User.IsInRole("owner"))
        {
            return RedirectToAction("Index", "Owner");
        }
        
        return View();
    }
}



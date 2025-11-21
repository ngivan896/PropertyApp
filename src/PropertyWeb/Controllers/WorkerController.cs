using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyWeb.Data;
using PropertyWeb.Models;

namespace PropertyWeb.Controllers;

[Authorize(Roles = "worker")]
public class WorkerController : Controller
{
    private readonly Application_context _db;

    public WorkerController(Application_context db)
    {
        _db = db;
    }

    // GET: /Worker/MyTickets
    public async Task<IActionResult> MyTickets()
    {
        var workerId = GetCurrentUserId();
        if (workerId == null) return Unauthorized();

        var tickets = await _db.Repair_ticket_set
            .Include(t => t.Property)
            .Include(t => t.Owner)
            .Where(t => t.Assigned_user_id == workerId)
            .OrderByDescending(t => t.Created_at)
            .ToListAsync();

        return View(tickets);
    }

    // GET: /Worker/EditTicket/{id}
    [HttpGet]
    public async Task<IActionResult> EditTicket(Guid id)
    {
        var workerId = GetCurrentUserId();
        if (workerId == null) return Unauthorized();

        var ticket = await _db.Repair_ticket_set
            .Include(t => t.Property)
            .Include(t => t.Owner)
            .FirstOrDefaultAsync(t => t.Id == id && t.Assigned_user_id == workerId);

        if (ticket == null)
        {
            return NotFound();
        }

        var vm = new WorkerEditTicketViewModel
        {
            Id = ticket.Id,
            Title = ticket.Title,
            Description = ticket.Description,
            Status = ticket.Status,
            OwnerName = ticket.Owner?.User_name,
            PropertyAddress = ticket.Property?.Address_line
        };

        return View(vm);
    }

    // POST: /Worker/EditTicket/{id}
    [HttpPost]
    public async Task<IActionResult> EditTicket(WorkerEditTicketViewModel model)
    {
        var workerId = GetCurrentUserId();
        if (workerId == null) return Unauthorized();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var ticket = await _db.Repair_ticket_set
            .FirstOrDefaultAsync(t => t.Id == model.Id && t.Assigned_user_id == workerId);
        if (ticket == null)
        {
            return NotFound();
        }

        ticket.Status = model.Status;
        ticket.Updated_at = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(MyTickets));
    }

    private Guid? GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(idClaim, out var id) ? id : null;
    }
}



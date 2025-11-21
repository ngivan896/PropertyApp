using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyWeb.Data;
using PropertyWeb.Models;

namespace PropertyWeb.Controllers;

[Authorize(Roles = "owner")]
public class OwnerController : Controller
{
    private readonly Application_context _db;

    public OwnerController(Application_context db)
    {
        _db = db;
    }

    // GET: /Owner/Properties
    public async Task<IActionResult> Properties()
    {
        var ownerId = GetCurrentUserId();
        if (ownerId == null) return Unauthorized();

        var properties = await _db.Property_set
            .Where(p => p.Owner_id == ownerId)
            .OrderByDescending(p => p.Created_at)
            .ToListAsync();

        return View(properties);
    }

    // GET: /Owner/Profile
    public async Task<IActionResult> Profile()
    {
        var ownerId = GetCurrentUserId();
        if (ownerId == null) return Unauthorized();

        var user = await _db.User_set.FirstOrDefaultAsync(u => u.Id == ownerId.Value);
        if (user == null) return NotFound();

        var properties = await _db.Property_set
            .Where(p => p.Owner_id == ownerId)
            .OrderByDescending(p => p.Created_at)
            .ToListAsync();

        var ticketCount = await _db.Repair_ticket_set.CountAsync(t => t.Owner_id == ownerId);

        var vm = new OwnerProfileViewModel
        {
            User = user,
            Properties = properties,
            TicketCount = ticketCount
        };

        return View(vm);
    }

    // GET: /Owner/MyTickets
    public async Task<IActionResult> MyTickets()
    {
        var ownerId = GetCurrentUserId();
        if (ownerId == null) return Unauthorized();

        var tickets = await _db.Repair_ticket_set
            .Include(t => t.Property)
            .Where(t => t.Owner_id == ownerId)
            .OrderByDescending(t => t.Created_at)
            .ToListAsync();

        return View(tickets);
    }

    // GET: /Owner/NewTicket
    [HttpGet]
    public async Task<IActionResult> NewTicket()
    {
        var ownerId = GetCurrentUserId();
        if (ownerId == null) return Unauthorized();

        var properties = await _db.Property_set
            .Where(p => p.Owner_id == ownerId)
            .OrderBy(p => p.Address_line)
            .ToListAsync();

        var vm = new OwnerCreateTicketViewModel
        {
            PropertyOptions = properties
        };

        return View(vm);
    }

    // POST: /Owner/NewTicket
    [HttpPost]
    public async Task<IActionResult> NewTicket(OwnerCreateTicketViewModel model)
    {
        var ownerId = GetCurrentUserId();
        if (ownerId == null) return Unauthorized();

        if (!ModelState.IsValid)
        {
            model.PropertyOptions = await _db.Property_set
                .Where(p => p.Owner_id == ownerId)
                .OrderBy(p => p.Address_line)
                .ToListAsync();
            return View(model);
        }

        var ticket = new Repair_ticket
        {
            Id = Guid.NewGuid(),
            Title = model.Title,
            Description = model.Description,
            Status = Ticket_status.Pending,
            Owner_id = ownerId.Value,
            Property_id = model.PropertyId,
            Created_at = DateTime.UtcNow
        };

        _db.Repair_ticket_set.Add(ticket);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(MyTickets));
    }

    private Guid? GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(idClaim, out var id) ? id : null;
    }
}



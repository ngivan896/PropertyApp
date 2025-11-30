using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyWeb.Data;
using PropertyWeb.Models;
using PropertyWeb.Services;

namespace PropertyWeb.Controllers;

[Authorize(Roles = "worker")]
public class WorkerController : Controller
{
    private readonly Application_context _db;
    private readonly ITicketImageService _ticketImageService;

    public WorkerController(Application_context db, ITicketImageService ticketImageService)
    {
        _db = db;
        _ticketImageService = ticketImageService;
    }

    // GET: /Worker/Index (Dashboard)
    public async Task<IActionResult> Index()
    {
        var workerId = GetCurrentUserId();
        if (workerId == null) return Unauthorized();

        var tickets = await _db.Repair_ticket_set
            .Include(t => t.Property)
            .Include(t => t.Owner)
            .Where(t => t.Assigned_user_id == workerId)
            .ToListAsync();

        var totalTickets = tickets.Count;
        var pendingTickets = tickets.Count(t => t.Status == Ticket_status.Pending || t.Status == Ticket_status.Assigned);
        var inProgressTickets = tickets.Count(t => t.Status == Ticket_status.In_progress);
        var completedTickets = tickets.Count(t => t.Status == Ticket_status.Completed);

        ViewBag.TotalTickets = totalTickets;
        ViewBag.PendingTickets = pendingTickets;
        ViewBag.InProgressTickets = inProgressTickets;
        ViewBag.CompletedTickets = completedTickets;
        ViewBag.RecentTickets = tickets.OrderByDescending(t => t.Created_at).Take(5).ToList();
        ViewBag.CompletionRate = totalTickets > 0 ? (int)Math.Round((double)completedTickets / totalTickets * 100) : 0;

        return View();
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

    // GET: /Worker/ViewTicket/{id}
    [HttpGet]
    public async Task<IActionResult> ViewTicket(Guid id)
    {
        var workerId = GetCurrentUserId();
        if (workerId == null) return Unauthorized();

        var ticket = await _db.Repair_ticket_set
            .Include(t => t.Owner)
            .Include(t => t.Assigned_user)
            .Include(t => t.Property)
            .FirstOrDefaultAsync(t => t.Id == id && t.Assigned_user_id == workerId);

        if (ticket == null)
        {
            return NotFound();
        }

        // Load messages separately to avoid relationship issues
        var messages = await _db.Ticket_message_set
            .Include(m => m.User)
            .Where(m => m.Ticket_id == ticket.Id)
            .OrderBy(m => m.Created_at)
            .ToListAsync();
        
        ticket.Messages = messages;

        // Generate presigned URL for image viewing if image exists
        if (!string.IsNullOrWhiteSpace(ticket.Image_url))
        {
            ticket.Image_url = _ticketImageService.GetPresignedViewUrl(ticket.Image_url);
        }

        var vm = new TicketDetailViewModel
        {
            Ticket = ticket,
            Messages = messages,
            CanSendMessage = true
        };

        return View(vm);
    }

    // POST: /Worker/SendMessage
    [HttpPost]
    public async Task<IActionResult> SendMessage(Guid ticketId, string messageText)
    {
        var workerId = GetCurrentUserId();
        if (workerId == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(messageText))
        {
            TempData["MessageError"] = "Message cannot be empty.";
            return RedirectToAction(nameof(ViewTicket), new { id = ticketId });
        }

        var ticket = await _db.Repair_ticket_set.FirstOrDefaultAsync(t => t.Id == ticketId && t.Assigned_user_id == workerId);
        if (ticket == null)
        {
            return NotFound();
        }

        var message = new Ticket_message
        {
            Id = Guid.NewGuid(),
            Ticket_id = ticketId,
            User_id = workerId.Value,
            Message_text = messageText.Trim(),
            Created_at = DateTime.UtcNow
        };

        _db.Ticket_message_set.Add(message);
        await _db.SaveChangesAsync();

        TempData["MessageSent"] = "Message sent successfully.";
        return RedirectToAction(nameof(ViewTicket), new { id = ticketId });
    }

    private Guid? GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(idClaim, out var id) ? id : null;
    }
}



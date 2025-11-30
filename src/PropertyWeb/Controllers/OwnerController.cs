using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PropertyWeb.Data;
using PropertyWeb.Models;
using PropertyWeb.Services;

namespace PropertyWeb.Controllers;

[Authorize(Roles = "owner")]
public class OwnerController : Controller
{
    private readonly Application_context _db;
    private readonly ITicketImageService _ticketImageService;

    public OwnerController(Application_context db, ITicketImageService ticketImageService)
    {
        _db = db;
        _ticketImageService = ticketImageService;
    }

    // GET: /Owner/Index (Dashboard)
    public async Task<IActionResult> Index()
    {
        var ownerId = GetCurrentUserId();
        if (ownerId == null) return Unauthorized();

        var tickets = await _db.Repair_ticket_set
            .Include(t => t.Property)
            .Include(t => t.Assigned_user)
            .Where(t => t.Owner_id == ownerId)
            .ToListAsync();

        var properties = await _db.Property_set
            .Where(p => p.Owner_id == ownerId)
            .ToListAsync();

        var totalTickets = tickets.Count;
        var openTickets = tickets.Count(t => t.Status == Ticket_status.Pending || 
                                            t.Status == Ticket_status.Assigned || 
                                            t.Status == Ticket_status.In_progress);
        var completedTickets = tickets.Count(t => t.Status == Ticket_status.Completed);
        var totalProperties = properties.Count;

        ViewBag.TotalTickets = totalTickets;
        ViewBag.OpenTickets = openTickets;
        ViewBag.CompletedTickets = completedTickets;
        ViewBag.TotalProperties = totalProperties;
        ViewBag.RecentTickets = tickets.OrderByDescending(t => t.Created_at).Take(5).ToList();
        ViewBag.CompletionRate = totalTickets > 0 ? (int)Math.Round((double)completedTickets / totalTickets * 100) : 0;

        return View();
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
            PropertyOptions = properties,
            DraftTicketId = Guid.NewGuid()
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
            Created_at = DateTime.UtcNow,
            Image_url = BuildImageUrl(model.UploadedImageBucket, model.UploadedImageKey)
        };

        _db.Repair_ticket_set.Add(ticket);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(MyTickets));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestTicketImageUpload([FromBody] TicketImageUploadRequestDto request)
    {
        var ownerId = GetCurrentUserId();
        if (ownerId == null)
        {
            return Unauthorized();
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var ownsProperty = await _db.Property_set.AnyAsync(p => p.Id == request.PropertyId && p.Owner_id == ownerId);
        if (!ownsProperty)
        {
            return Forbid();
        }

        var uploadRequest = new TicketImageUploadRequest(
            request.TicketId,
            request.PropertyId,
            request.FileName,
            request.ContentType ?? "application/octet-stream",
            ownerId.Value);

        var response = await _ticketImageService.RequestUploadUrlAsync(uploadRequest);
        if (response == null)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = "Unable to request upload URL." });
        }

        return Json(response);
    }

    // GET: /Owner/ViewTicket/{id}
    [HttpGet]
    public async Task<IActionResult> ViewTicket(Guid id)
    {
        var ownerId = GetCurrentUserId();
        if (ownerId == null) return Unauthorized();

        var ticket = await _db.Repair_ticket_set
            .Include(t => t.Owner)
            .Include(t => t.Assigned_user)
            .Include(t => t.Property)
            .FirstOrDefaultAsync(t => t.Id == id && t.Owner_id == ownerId);

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

    // POST: /Owner/SendMessage
    [HttpPost]
    public async Task<IActionResult> SendMessage(Guid ticketId, string messageText)
    {
        var ownerId = GetCurrentUserId();
        if (ownerId == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(messageText))
        {
            TempData["MessageError"] = "Message cannot be empty.";
            return RedirectToAction(nameof(ViewTicket), new { id = ticketId });
        }

        var ticket = await _db.Repair_ticket_set.FirstOrDefaultAsync(t => t.Id == ticketId && t.Owner_id == ownerId);
        if (ticket == null)
        {
            return NotFound();
        }

        var message = new Ticket_message
        {
            Id = Guid.NewGuid(),
            Ticket_id = ticketId,
            User_id = ownerId.Value,
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

    private static string? BuildImageUrl(string? bucket, string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(bucket))
        {
            return key;
        }

        return $"https://{bucket}.s3.amazonaws.com/{key}";
    }
}

public class TicketImageUploadRequestDto
{
    [Required]
    public Guid TicketId { get; set; }

    [Required]
    public Guid PropertyId { get; set; }

    [Required]
    [MaxLength(260)]
    public string FileName { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? ContentType { get; set; }
}



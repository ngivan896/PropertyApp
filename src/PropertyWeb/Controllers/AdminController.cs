using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyWeb.Data;
using PropertyWeb.Models;
using PropertyWeb.Services;

namespace PropertyWeb.Controllers;

[Authorize(Roles = "admin")]
public class AdminController : Controller
{
    private readonly Application_context _db;
    private readonly ITicketImageService _ticketImageService;

    public AdminController(Application_context db, ITicketImageService ticketImageService)
    {
        _db = db;
        _ticketImageService = ticketImageService;
    }

    // GET: /Admin
    public async Task<IActionResult> Index()
    {
        // Dashboard view with statistics
        var users = await _db.User_set.ToListAsync();
        var tickets = await _db.Repair_ticket_set
            .Include(t => t.Owner)
            .Include(t => t.Assigned_user)
            .ToListAsync();
        var properties = await _db.Property_set.ToListAsync();

        var totalUsers = users.Count;
        var totalAdmins = users.Count(u => string.Equals(u.Role, "admin", StringComparison.OrdinalIgnoreCase));
        var totalOwners = users.Count(u => string.Equals(u.Role, "owner", StringComparison.OrdinalIgnoreCase));
        var totalWorkers = users.Count(u => string.Equals(u.Role, "worker", StringComparison.OrdinalIgnoreCase));
        
        var totalTickets = tickets.Count;
        var openTickets = tickets.Count(t => 
            t.Status == Ticket_status.Pending || 
            t.Status == Ticket_status.Assigned || 
            t.Status == Ticket_status.In_progress);
        var completedTickets = tickets.Count(t => t.Status == Ticket_status.Completed);
        var closedTickets = tickets.Count(t => t.Status == Ticket_status.Closed);
        
        var totalProperties = properties.Count;
        
        // Calculate completion rate
        var completionRate = totalTickets > 0 
            ? (int)Math.Round((double)(completedTickets + closedTickets) / totalTickets * 100)
            : 0;

        ViewBag.TotalUsers = totalUsers;
        ViewBag.TotalAdmins = totalAdmins;
        ViewBag.TotalOwners = totalOwners;
        ViewBag.TotalWorkers = totalWorkers;
        ViewBag.TotalTickets = totalTickets;
        ViewBag.OpenTickets = openTickets;
        ViewBag.CompletedTickets = completedTickets;
        ViewBag.ClosedTickets = closedTickets;
        ViewBag.TotalProperties = totalProperties;
        ViewBag.CompletionRate = completionRate;
        ViewBag.RecentTickets = tickets
            .OrderByDescending(t => t.Created_at)
            .Take(5)
            .ToList();
        ViewBag.RecentUsers = users
            .OrderByDescending(u => u.Created_at)
            .Take(5)
            .ToList();

        return View();
    }

    // GET: /Admin/Users
    [HttpGet]
    public async Task<IActionResult> Users()
    {
        var users = await _db.User_set
            .OrderByDescending(u => u.Created_at)
            .ToListAsync();

        return View(users);
    }

    // GET: /Admin/Tickets
    [HttpGet]
    public async Task<IActionResult> Tickets()
    {
        var tickets = await _db.Repair_ticket_set
            .Include(t => t.Owner)
            .Include(t => t.Assigned_user)
            .Include(t => t.Property)
            .OrderByDescending(t => t.Created_at)
            .Take(50)
            .ToListAsync();

        var openStatuses = new[] { Ticket_status.Pending, Ticket_status.Assigned, Ticket_status.In_progress };

        ViewBag.TotalTickets = tickets.Count;
        ViewBag.OpenTickets = tickets.Count(t => openStatuses.Contains(t.Status));
        ViewBag.CompletedTickets = tickets.Count(t => t.Status == Ticket_status.Completed);
        ViewBag.ClosedTickets = tickets.Count(t => t.Status == Ticket_status.Closed);

        return View(tickets);
    }

    // GET: /Admin/Properties
    [HttpGet]
    public async Task<IActionResult> Properties()
    {
        var properties = await _db.Property_set
            .Include(p => p.Owner)
            .OrderByDescending(p => p.Created_at)
            .ToListAsync();

        return View(properties);
    }

    // GET: /Admin/CreateProperty
    [HttpGet]
    public async Task<IActionResult> CreateProperty()
    {
        var owners = await _db.User_set
            .Where(u => u.Role == "owner")
            .OrderBy(u => u.User_name)
            .ToListAsync();

        var vm = new AdminPropertyFormViewModel
        {
            OwnerOptions = owners
        };

        return View(vm);
    }

    // POST: /Admin/CreateProperty
    [HttpPost]
    public async Task<IActionResult> CreateProperty(AdminPropertyFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.OwnerOptions = await _db.User_set
                .Where(u => u.Role == "owner")
                .OrderBy(u => u.User_name)
                .ToListAsync();
            return View(model);
        }

        var property = new Property_record
        {
            Id = Guid.NewGuid(),
            Owner_id = model.OwnerId,
            Address_line = model.Address_line,
            Unit_label = model.Unit_label,
            Property_type = model.Property_type,
            Created_at = DateTime.UtcNow
        };

        _db.Property_set.Add(property);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Properties));
    }

    // GET: /Admin/EditTicket/{id}
    [HttpGet]
    public async Task<IActionResult> EditTicket(Guid id)
    {
        var ticket = await _db.Repair_ticket_set
            .Include(t => t.Owner)
            .Include(t => t.Property)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null)
        {
            return NotFound();
        }

        var workers = await _db.User_set
            .Where(u => u.Role == "worker")
            .OrderBy(u => u.User_name)
            .ToListAsync();

        var vm = new AdminEditTicketViewModel
        {
            Id = ticket.Id,
            Title = ticket.Title,
            Description = ticket.Description,
            Status = ticket.Status,
            Assigned_user_id = ticket.Assigned_user_id,
            OwnerName = ticket.Owner?.User_name,
            PropertyAddress = ticket.Property?.Address_line,
            WorkerOptions = workers
        };

        return View(vm);
    }

    // POST: /Admin/EditTicket/{id}
    [HttpPost]
    public async Task<IActionResult> EditTicket(AdminEditTicketViewModel model)
    {
        if (!ModelState.IsValid)
        {
            // repopulate worker options if validation fails
            model.WorkerOptions = await _db.User_set
                .Where(u => u.Role == "worker")
                .OrderBy(u => u.User_name)
                .ToListAsync();
            return View(model);
        }

        var ticket = await _db.Repair_ticket_set.FirstOrDefaultAsync(t => t.Id == model.Id);
        if (ticket == null)
        {
            return NotFound();
        }

        ticket.Status = model.Status;
        ticket.Assigned_user_id = model.Assigned_user_id;
        ticket.Updated_at = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        TempData["TicketUpdated"] = "Ticket updated successfully.";

        return RedirectToAction(nameof(Tickets));
    }

    // GET: /Admin/CreateUser
    [HttpGet]
    public IActionResult CreateUser()
    {
        return View(new AdminCreateUserViewModel());
    }

    // POST: /Admin/CreateUser
    [HttpPost]
    public async Task<IActionResult> CreateUser(AdminCreateUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var exists = await _db.User_set.AnyAsync(x => x.Email == model.Email);
        if (exists)
        {
            ModelState.AddModelError(nameof(model.Email), "Email already exists.");
            return View(model);
        }

        var user = new User_account
        {
            Id = Guid.NewGuid(),
            User_name = model.UserName,
            Email = model.Email,
            Password_hash = AccountController.HashFor_admin(model.Password),
            Role = model.Role,
            Phone = model.Phone,
            Created_at = DateTime.UtcNow
        };

        _db.User_set.Add(user);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Users));
    }

    // GET: /Admin/ViewTicket/{id}
    [HttpGet]
    public async Task<IActionResult> ViewTicket(Guid id)
    {
        var ticket = await _db.Repair_ticket_set
            .Include(t => t.Owner)
            .Include(t => t.Assigned_user)
            .Include(t => t.Property)
            .FirstOrDefaultAsync(t => t.Id == id);

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

        // Messages are already loaded above

        // Attach messages to ticket for view
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
            CanSendMessage = true // Admin can always send messages
        };

        return View(vm);
    }

    // POST: /Admin/SendMessage
    [HttpPost]
    public async Task<IActionResult> SendMessage(Guid ticketId, string messageText)
    {
        if (string.IsNullOrWhiteSpace(messageText))
        {
            TempData["MessageError"] = "Message cannot be empty.";
            return RedirectToAction(nameof(ViewTicket), new { id = ticketId });
        }

        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized();
        }

        // Verify ticket exists
        var ticket = await _db.Repair_ticket_set.FindAsync(ticketId);
        if (ticket == null)
        {
            return NotFound();
        }

        // Verify user exists
        var user = await _db.User_set.FindAsync(userId);
        if (user == null)
        {
            return Unauthorized();
        }

        var message = new Ticket_message
        {
            Id = Guid.NewGuid(),
            Ticket_id = ticketId,
            User_id = userId,
            Message_text = messageText.Trim(),
            Created_at = DateTime.UtcNow
        };

        _db.Ticket_message_set.Add(message);
        await _db.SaveChangesAsync();

        TempData["MessageSent"] = "Message sent successfully.";
        return RedirectToAction(nameof(ViewTicket), new { id = ticketId });
    }
}



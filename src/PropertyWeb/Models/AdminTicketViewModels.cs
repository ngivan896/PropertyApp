using System.ComponentModel.DataAnnotations;

namespace PropertyWeb.Models;

public class AdminEditTicketViewModel
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public Ticket_status Status { get; set; }

    public Guid? Assigned_user_id { get; set; }

    public string? OwnerName { get; set; }
    public string? PropertyAddress { get; set; }

    public IEnumerable<User_account> WorkerOptions { get; set; } = Enumerable.Empty<User_account>();
}



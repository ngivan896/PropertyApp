using System.ComponentModel.DataAnnotations;

namespace PropertyWeb.Models;

public class WorkerEditTicketViewModel
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public Ticket_status Status { get; set; }

    public string? OwnerName { get; set; }

    public string? PropertyAddress { get; set; }
}



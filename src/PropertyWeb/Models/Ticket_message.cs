namespace PropertyWeb.Models;

public class Ticket_message
{
    public Guid Id { get; set; }

    public Guid Ticket_id { get; set; }

    public Guid User_id { get; set; }

    public string Message_text { get; set; } = string.Empty;

    public DateTime Created_at { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Repair_ticket? Ticket { get; set; }

    public User_account? User { get; set; }
}

















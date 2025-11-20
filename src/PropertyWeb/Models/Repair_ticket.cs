namespace PropertyWeb.Models;

public class Repair_ticket
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Ticket_status Status { get; set; } = Ticket_status.Pending;

    public string? Image_url { get; set; }

    public DateTime Created_at { get; set; } = DateTime.UtcNow;

    public DateTime? Updated_at { get; set; }

    public Guid Owner_id { get; set; }

    public Guid? Assigned_user_id { get; set; }

    public Guid Property_id { get; set; }

    public User_account? Owner { get; set; }

    public User_account? Assigned_user { get; set; }

    public Property_record? Property { get; set; }
}

public enum Ticket_status
{
    Pending = 0,
    Assigned = 1,
    In_progress = 2,
    Completed = 3,
    Closed = 4
}


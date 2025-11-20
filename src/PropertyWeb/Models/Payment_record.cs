namespace PropertyWeb.Models;

public class Payment_record
{
    public Guid Id { get; set; }

    public Guid Property_id { get; set; }

    public Guid Owner_id { get; set; }

    public decimal Amount { get; set; }

    public string? Description { get; set; }

    public DateTime Recorded_at { get; set; } = DateTime.UtcNow;

    public Property_record? Property { get; set; }

    public User_account? Owner { get; set; }
}


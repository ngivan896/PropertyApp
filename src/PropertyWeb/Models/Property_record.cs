namespace PropertyWeb.Models;

public class Property_record
{
    public Guid Id { get; set; }

    public Guid Owner_id { get; set; }

    public string Address_line { get; set; } = string.Empty;

    public string? Unit_label { get; set; }

    public string? Property_type { get; set; }

    public DateTime Created_at { get; set; } = DateTime.UtcNow;

    public User_account? Owner { get; set; }

    public ICollection<Repair_ticket> Ticket_list { get; set; } = new List<Repair_ticket>();

    public ICollection<Payment_record> Payment_list { get; set; } = new List<Payment_record>();
}


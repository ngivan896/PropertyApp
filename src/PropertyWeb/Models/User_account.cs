using System.ComponentModel.DataAnnotations;

namespace PropertyWeb.Models;

public class User_account
{
    public Guid Id { get; set; }

    [MaxLength(80)]
    public string User_name { get; set; } = string.Empty;

    [MaxLength(120)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(255)]
    public string Password_hash { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Role { get; set; } = string.Empty;

    [MaxLength(25)]
    public string? Phone { get; set; }

    public DateTime Created_at { get; set; } = DateTime.UtcNow;

    public ICollection<Property_record> Property_list { get; set; } = new List<Property_record>();

    public ICollection<Repair_ticket> Submitted_ticket_list { get; set; } = new List<Repair_ticket>();

    public ICollection<Repair_ticket> Assigned_ticket_list { get; set; } = new List<Repair_ticket>();
}


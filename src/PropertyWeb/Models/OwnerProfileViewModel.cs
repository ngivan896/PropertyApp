namespace PropertyWeb.Models;

public class OwnerProfileViewModel
{
    public User_account User { get; set; } = new User_account();
    public IEnumerable<Property_record> Properties { get; set; } = Enumerable.Empty<Property_record>();
    public int TicketCount { get; set; }
}



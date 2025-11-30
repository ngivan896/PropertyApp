namespace PropertyWeb.Models;

public class TicketDetailViewModel
{
    public Repair_ticket Ticket { get; set; } = null!;
    
    public List<Ticket_message> Messages { get; set; } = new();
    
    public bool CanSendMessage { get; set; }
}











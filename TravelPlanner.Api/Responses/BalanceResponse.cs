namespace TravelPlanner.Api.Responses;

public class BalanceResponse
{
    public int UserId { get; set; }
    public string Name { get; set; } = "";
    public decimal Paid { get; set; }
    public decimal Share { get; set; }
    public decimal Sent { get; set; }
    public decimal Received { get; set; }
    public decimal Balance => Paid - Share + Sent - Received;
}

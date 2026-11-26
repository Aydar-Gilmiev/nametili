namespace TravelPlanner.Blazor.Models;

public class MemberBalance
{
    public int UserId { get; set; }
    public string Name { get; set; } = "";
    public decimal Paid { get; set; }
    public decimal Share { get; set; }
    public decimal Sent { get; set; }
    public decimal Received { get; set; }
    public decimal Balance { get; set; }
}

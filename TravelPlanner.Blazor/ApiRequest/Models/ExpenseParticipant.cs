namespace TravelPlanner.Blazor.Models;

public class ExpenseParticipant
{
    public int UserId { get; set; }
    public string Name { get; set; } = "";
    public bool Selected { get; set; } = true;
    public decimal Amount { get; set; }
}

namespace TravelPlanner.Blazor.Models;

public class ExpenseShare
{
    public int UserId { get; set; }
    public string Name { get; set; } = "";
    public decimal Amount { get; set; }
}

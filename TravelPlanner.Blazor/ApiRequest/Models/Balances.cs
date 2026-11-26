namespace TravelPlanner.Blazor.Models;

public class Balances
{
    public decimal TotalExpenses { get; set; }
    public decimal TotalRepayments { get; set; }
    public List<MemberBalance> Members { get; set; } = new();
    public Settlement Settlement { get; set; } = new();
}

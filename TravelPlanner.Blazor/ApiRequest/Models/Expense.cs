namespace TravelPlanner.Blazor.Models;

public class Expense
{
    public int Id { get; set; }
    public int? ProposalId { get; set; }
    public string Title { get; set; } = "";
    public int PayerUserId { get; set; }
    public string PayerName { get; set; } = "";
    public decimal Amount { get; set; }
    public DateOnly OccurredOn { get; set; }
    public List<ExpenseShare> Shares { get; set; } = new();
}

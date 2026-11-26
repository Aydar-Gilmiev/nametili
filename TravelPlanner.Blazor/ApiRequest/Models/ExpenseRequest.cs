namespace TravelPlanner.Blazor.Models;

public class ExpenseRequest
{
    public string Title { get; set; } = "";
    public int? ProposalId { get; set; }
    public int PayerUserId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly OccurredOn { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string SplitMode { get; set; } = "Equal";
    public List<int> ParticipantUserIds { get; set; } = new();
    public List<ExpenseShare> Shares { get; set; } = new();
}

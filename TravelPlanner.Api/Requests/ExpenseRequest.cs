namespace TravelPlanner.Api.Requests;

public class ExpenseRequest
{
    public string Title { get; set; } = "";
    public int? ProposalId { get; set; }
    public int PayerUserId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly OccurredOn { get; set; }
    public string SplitMode { get; set; } = "Equal";
    public List<int> ParticipantUserIds { get; set; } = [];
    public List<ExpenseShareRequest> Shares { get; set; } = [];
}

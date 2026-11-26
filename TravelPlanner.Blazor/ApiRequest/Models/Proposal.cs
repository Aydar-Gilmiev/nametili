namespace TravelPlanner.Blazor.Models;

public class Proposal
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal PlannedCost { get; set; }
    public string Status { get; set; } = "";
    public bool VotingClosed { get; set; }
    public int Score { get; set; }
    public int VoteCount { get; set; }
    public int MyVote { get; set; }
}

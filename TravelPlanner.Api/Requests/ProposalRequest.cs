namespace TravelPlanner.Api.Requests;

public class ProposalRequest
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal PlannedCost { get; set; }
}

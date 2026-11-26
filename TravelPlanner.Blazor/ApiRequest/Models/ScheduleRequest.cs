namespace TravelPlanner.Blazor.Models;

public class ScheduleRequest
{
    public int? ProposalId { get; set; }
    public string Title { get; set; } = "";
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
}

namespace TravelPlanner.Api.Requests;

public class ScheduleItemRequest
{
    public string Title { get; set; } = "";
    public int? ProposalId { get; set; }
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
}

namespace TravelPlanner.Blazor.Models;

public class ScheduleItem
{
    public int Id { get; set; }
    public int? ProposalId { get; set; }
    public string Title { get; set; } = "";
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
}

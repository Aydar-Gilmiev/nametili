namespace TravelPlanner.Blazor.Models;

public class TripRequest
{
    public string Name { get; set; } = "";
    public string Destination { get; set; } = "";
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
}

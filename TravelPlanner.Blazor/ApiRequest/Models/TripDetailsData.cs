namespace TravelPlanner.Blazor.Models;

public class TripDetailsData
{
    public Trip Trip { get; set; } = new();
    public List<TripMember> Members { get; set; } = new();
}

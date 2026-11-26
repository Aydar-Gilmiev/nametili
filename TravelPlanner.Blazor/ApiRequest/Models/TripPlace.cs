namespace TravelPlanner.Blazor.Models;

public class TripPlace
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = "";
    public string Note { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

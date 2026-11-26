namespace TravelPlanner.Blazor.Models;

public class TripPhoto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = "";
    public string Caption { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

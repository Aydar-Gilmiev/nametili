namespace TravelPlanner.Blazor.Models;

public class ApiStatus
{
    public bool status { get; set; }
    public string message { get; set; } = "";
    public int StatusCode { get; set; }
}

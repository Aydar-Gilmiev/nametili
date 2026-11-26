namespace TravelPlanner.Blazor.Models;

public class AuthData
{
    public string Token { get; set; } = "";
    public User User { get; set; } = new();
}

namespace TravelPlanner.Api.Requests;

public class InvitationRequest
{
    public string Email { get; set; } = "";
    public string Role { get; set; } = "Participant";
}

namespace TravelPlanner.Api.Responses;

public class TransferResponse
{
    public int FromUserId { get; set; }
    public string FromName { get; set; } = "";
    public int ToUserId { get; set; }
    public string ToName { get; set; } = "";
    public decimal Amount { get; set; }
}

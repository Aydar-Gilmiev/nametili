namespace TravelPlanner.Api.Requests;

public class RepaymentRequest
{
    public int FromUserId { get; set; }
    public int ToUserId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly PaidOn { get; set; }
}

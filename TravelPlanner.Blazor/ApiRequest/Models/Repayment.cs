namespace TravelPlanner.Blazor.Models;

public class Repayment
{
    public int Id { get; set; }
    public int FromUserId { get; set; }
    public string FromName { get; set; } = "";
    public int ToUserId { get; set; }
    public string ToName { get; set; } = "";
    public decimal Amount { get; set; }
    public DateOnly PaidOn { get; set; } = DateOnly.FromDateTime(DateTime.Today);
}

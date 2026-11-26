namespace TravelPlanner.Blazor.Models;

public class Settlement
{
    public bool IsOptimal { get; set; }
    public List<Repayment> Transfers { get; set; } = new();
}

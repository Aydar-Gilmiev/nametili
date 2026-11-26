namespace TravelPlanner.Api.Responses;

public class SettlementResponse
{
    public bool IsOptimal { get; set; }
    public List<TransferResponse> Transfers { get; set; } = new();
}

using Microsoft.AspNetCore.Mvc;

namespace TravelPlanner.Api.Interfaces;

public interface IMessagesService
{
    Task<IActionResult> Get(int tripId, int? beforeId);
}

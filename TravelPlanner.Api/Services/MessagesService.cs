using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelPlanner.Api.DatabaseContext;
using TravelPlanner.Api.Interfaces;
using TravelPlanner.Api.Responses;

namespace TravelPlanner.Api.Services;

public class MessagesService : IMessagesService
{
    private readonly ContextDb _db;
    public MessagesService(ContextDb db) { _db = db; }

    public async Task<IActionResult> Get(int tripId, int? beforeId)
    {
        if (beforeId <= 0) return new BadRequestObjectResult("Неверный номер сообщения.");
        var messages = await _db.ChatMessages.Where(message => message.Trip_Id_Fk == tripId
                && (!beforeId.HasValue || message.Chat_Message_Id < beforeId.Value))
            .OrderByDescending(message => message.Chat_Message_Id).Take(50)
            .Select(message => new ChatMessageResponse { Id = message.Chat_Message_Id, UserId = message.User_Id_Fk,
                Name = message.User.User_Name, Text = message.Text, CreatedAt = message.Created_At }).ToListAsync();
        messages.Reverse();
        return new OkObjectResult(messages);
    }
}

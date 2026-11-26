using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TravelPlanner.Api.DatabaseContext;
using TravelPlanner.Api.Models.Entities;
using TravelPlanner.Api.Responses;
using TravelPlanner.Api.Services;

namespace TravelPlanner.Api.Hubs;

[Authorize]
public class TripChatHub : Hub
{
    private readonly ContextDb _db;
    private readonly TripAccess _access;

    public TripChatHub(ContextDb db, TripAccess access)
    {
        _db = db;
        _access = access;
    }

    public override async Task OnConnectedAsync()
    {
        var trip = Context.GetHttpContext()?.Request.Query["tripId"].ToString();
        if (!int.TryParse(trip, out var tripId) || !await _access.IsMember(tripId, _access.UserId(Context.User!)))
        {
            Context.Abort();
            throw new HubException("Нет доступа к чату поездки.");
        }
        Context.Items["TripId"] = tripId;
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(tripId, _access.UserId(Context.User!)));
        await base.OnConnectedAsync();
    }

    public async Task<ChatMessageResponse> SendMessage(string text)
    {
        if (!Context.Items.TryGetValue("TripId", out var value) || value is not int tripId)
            throw new HubException("Сначала подключитесь к поездке.");
        var userId = _access.UserId(Context.User!);
        if (!await _access.IsMember(tripId, userId))
            throw new HubException("Нет доступа к чату поездки.");
        text = (text ?? "").Trim();
        if (text.Length is < 1 or > 2000) throw new HubException("Сообщение должно содержать от 1 до 2000 символов.");
        var name = await _db.Users.Where(user => user.User_Id == userId).Select(user => user.User_Name).SingleAsync();
        var message = new ChatMessage { Trip_Id_Fk = tripId, User_Id_Fk = userId, Text = text, Created_At = DateTime.UtcNow };
        _db.ChatMessages.Add(message);
        await _db.SaveChangesAsync();
        var response = new ChatMessageResponse { Id = message.Chat_Message_Id, UserId = userId, Name = name, Text = text, CreatedAt = message.Created_At };
        var members = await _db.TripMembers.Where(member => member.Trip_Id_Fk == tripId)
            .Select(member => member.User_Id_Fk).ToListAsync();
        // Исключённый участник не получает новые сообщения даже со старым соединением.
        await Clients.Groups(members.Select(id => GroupName(tripId, id)).ToList()).SendAsync("MessageReceived", response);
        return response;
    }

    private static string GroupName(int tripId, int userId) => $"trip:{tripId}:user:{userId}";
}

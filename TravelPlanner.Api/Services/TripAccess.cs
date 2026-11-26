using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TravelPlanner.Api.DatabaseContext;

namespace TravelPlanner.Api.Services;

public class TripAccess
{
    private readonly ContextDb _db;

    public TripAccess(ContextDb db)
    {
        _db = db;
    }

    public int UserId(ClaimsPrincipal user) => int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    public Task<bool> IsMember(int tripId, int userId) =>
        _db.TripMembers.AnyAsync(x => x.Trip_Id_Fk == tripId && x.User_Id_Fk == userId);

    public Task<bool> IsOrganizer(int tripId, int userId) =>
        _db.TripMembers.AnyAsync(x => x.Trip_Id_Fk == tripId && x.User_Id_Fk == userId && x.Role == "Organizer");
}

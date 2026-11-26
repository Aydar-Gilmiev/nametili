using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelPlanner.Api.DatabaseContext;
using TravelPlanner.Api.Interfaces;
using TravelPlanner.Api.Models.Entities;

namespace TravelPlanner.Api.Services;

public class InvitationsService : IInvitationsService
{
    private readonly ContextDb _db;
    private readonly TripAccess _access;

    public InvitationsService(ContextDb db, TripAccess access)
    {
        _db = db;
        _access = access;
    }

    public async Task<IActionResult> GetMineAsync(string email)
    {
        var invitations = await _db.Invitations
            .Where(invitation => invitation.Email == email && invitation.Status == "Pending")
            .Select(invitation => new
            {
                Id = invitation.Invitation_Id,
                invitation.Role,
                Name = invitation.Trip.Trip_Name,
                invitation.Trip.Destination
            })
            .ToListAsync();

        return new OkObjectResult(invitations);
    }

    public async Task<IActionResult> AcceptAsync(int invitationId, string email, int userId)
    {
        var invitation = await _db.Invitations.SingleOrDefaultAsync(x =>
            x.Invitation_Id == invitationId && x.Email == email && x.Status == "Pending");
        if (invitation == null) return new NotFoundResult();

        if (!await _access.IsMember(invitation.Trip_Id_Fk, userId))
        {
            _db.TripMembers.Add(new TripMember
            {
                Trip_Id_Fk = invitation.Trip_Id_Fk,
                User_Id_Fk = userId,
                Role = invitation.Role
            });
        }

        invitation.Status = "Accepted";
        await _db.SaveChangesAsync();
        return new OkResult();
    }
}

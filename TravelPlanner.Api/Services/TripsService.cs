using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TravelPlanner.Api.DatabaseContext;
using TravelPlanner.Api.Interfaces;
using TravelPlanner.Api.Models.Entities;
using TravelPlanner.Api.Requests;
using TravelPlanner.Api.UniversalMethod;

namespace TravelPlanner.Api.Services;

public class TripsService : ITripsService
{
    private readonly ContextDb _db;
    private readonly TripAccess _access;

    public TripsService(ContextDb db, TripAccess access)
    {
        _db = db;
        _access = access;
    }

    public async Task<IActionResult> GetMineAsync(int userId)
    {
        var trips = await _db.TripMembers
            .Where(member => member.User_Id_Fk == userId)
            .OrderBy(member => member.Trip.Start_Date)
            .Select(member => new
            {
                Id = member.Trip.Trip_Id,
                Name = member.Trip.Trip_Name,
                member.Trip.Destination,
                StartDate = member.Trip.Start_Date,
                EndDate = member.Trip.End_Date,
                member.Role
            })
            .ToListAsync();

        return new OkObjectResult(trips);
    }

    public async Task<IActionResult> CreateAsync(TripRequest request, int userId)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 200
            || (request.Destination?.Trim().Length ?? 0) > 300
            || request.StartDate == default || request.EndDate == default || request.EndDate < request.StartDate)
            return new BadRequestObjectResult("Проверьте название и даты поездки.");

        var trip = new Trip
        {
            Trip_Name = request.Name.Trim(),
            Destination = (request.Destination ?? "").Trim(),
            Start_Date = request.StartDate,
            End_Date = request.EndDate,
            Owner_User_Id_Fk = userId
        };

        _db.Trips.Add(trip);
        _db.TripMembers.Add(new TripMember { Trip = trip, User_Id_Fk = userId, Role = "Organizer" });
        await _db.SaveChangesAsync();

        return new OkObjectResult(new
        {
            Id = trip.Trip_Id,
            Name = trip.Trip_Name,
            trip.Destination,
            StartDate = trip.Start_Date,
            EndDate = trip.End_Date,
            OwnerUserId = trip.Owner_User_Id_Fk
        });
    }

    public async Task<IActionResult> GetAsync(int tripId, int userId)
    {
        if (!await _access.IsMember(tripId, userId)) return new StatusCodeResult(403);

        var trip = await _db.Trips.FindAsync(tripId);
        if (trip == null) return new NotFoundResult();

        var members = await _db.TripMembers
            .Where(member => member.Trip_Id_Fk == tripId)
            .Select(member => new
            {
                Id = member.User.User_Id,
                Name = member.User.User_Name,
                member.User.Email,
                member.Role
            })
            .ToListAsync();

        return new OkObjectResult(new
        {
            trip = new
            {
                Id = trip.Trip_Id,
                Name = trip.Trip_Name,
                trip.Destination,
                StartDate = trip.Start_Date,
                EndDate = trip.End_Date,
                OwnerUserId = trip.Owner_User_Id_Fk
            },
            members
        });
    }

    public async Task<IActionResult> UpdateAsync(int tripId, TripRequest request, int userId)
    {
        if (!await _access.IsOrganizer(tripId, userId)) return new StatusCodeResult(403);
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 200
            || (request.Destination?.Trim().Length ?? 0) > 300
            || request.StartDate == default || request.EndDate == default || request.EndDate < request.StartDate)
            return new BadRequestObjectResult("Проверьте название, направление и даты поездки.");

        try
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var trip = await _db.Trips.FindAsync(tripId);
            if (trip == null) return new NotFoundResult();

            var schedule = await _db.ScheduleItems.Where(item => item.Trip_Id_Fk == tripId)
                .Select(item => new { item.Start_At, item.End_At }).ToListAsync();
            if (schedule.Any(item => DateOnly.FromDateTime(item.Start_At) < request.StartDate
                || DateOnly.FromDateTime(item.End_At.AddTicks(-1)) > request.EndDate))
                return new ConflictObjectResult("Новым датам поездки не соответствуют события расписания. Сначала измените расписание.");

            trip.Trip_Name = request.Name.Trim();
            trip.Destination = (request.Destination ?? "").Trim();
            trip.Start_Date = request.StartDate;
            trip.End_Date = request.EndDate;
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return new OkObjectResult(new
            {
                Id = trip.Trip_Id,
                Name = trip.Trip_Name,
                trip.Destination,
                StartDate = trip.Start_Date,
                EndDate = trip.End_Date,
                OwnerUserId = trip.Owner_User_Id_Fk
            });
        }
        catch (Exception exception) when (exception.GetBaseException() is PostgresException { SqlState: "40001" or "40P01" })
        {
            return new ConflictObjectResult("Поездка изменилась одновременно. Обновите данные и повторите действие.");
        }
    }

    public async Task<IActionResult> DeleteAsync(int tripId, int userId)
    {
        if (!await _access.IsOrganizer(tripId, userId)) return new StatusCodeResult(403);
        var trip = await _db.Trips.FindAsync(tripId);
        if (trip == null) return new NotFoundResult();

        _db.Trips.Remove(trip);
        await _db.SaveChangesAsync();
        return new OkResult();
    }

    public async Task<IActionResult> InviteAsync(int tripId, InvitationRequest request, int userId)
    {
        if (!await _access.IsOrganizer(tripId, userId)) return new StatusCodeResult(403);

        var email = (request.Email ?? "").Trim().ToLowerInvariant();
        if (!EmailHelper.IsValid(email) || request.Role is not ("Organizer" or "Participant"))
            return new BadRequestObjectResult("Проверьте почту и роль.");

        if (await _db.Invitations.AnyAsync(x => x.Trip_Id_Fk == tripId && x.Email == email))
            return new ConflictObjectResult("Приглашение для этой почты уже существует.");

        _db.Invitations.Add(new Invitation
        {
            Trip_Id_Fk = tripId,
            Email = email,
            Role = request.Role,
            Status = "Pending",
            Created_At = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return new OkResult();
    }

    public async Task<IActionResult> ChangeRoleAsync(int tripId, int memberUserId, string role, int userId)
    {
        if (role is not ("Organizer" or "Participant"))
            return new BadRequestObjectResult("Неизвестная роль.");

        try
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            if (!await _access.IsOrganizer(tripId, userId)) return new StatusCodeResult(403);

            var member = await _db.TripMembers.SingleOrDefaultAsync(x => x.Trip_Id_Fk == tripId && x.User_Id_Fk == memberUserId);
            if (member == null) return new NotFoundResult();

            if (member.Role == "Organizer" && role == "Participant" &&
                await _db.TripMembers.CountAsync(x => x.Trip_Id_Fk == tripId && x.Role == "Organizer") <= 1)
                return new BadRequestObjectResult("В поездке должен остаться хотя бы один организатор.");

            member.Role = role;
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return new OkResult();
        }
        catch (Exception exception) when (exception.GetBaseException() is PostgresException { SqlState: "40001" or "40P01" })
        {
            return new ConflictObjectResult("Роли изменились одновременно. Обновите данные и повторите действие.");
        }
    }
}

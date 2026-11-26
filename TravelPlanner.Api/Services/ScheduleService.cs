using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TravelPlanner.Api.DatabaseContext;
using TravelPlanner.Api.Interfaces;
using TravelPlanner.Api.Models.Entities;
using TravelPlanner.Api.Requests;

namespace TravelPlanner.Api.Services;

public class ScheduleService : IScheduleService
{
    private readonly ContextDb _contextDb;
    private readonly TripAccess _access;

    public ScheduleService(ContextDb contextDb, TripAccess access)
    {
        _contextDb = contextDb;
        _access = access;
    }

    public async Task<IActionResult> GetAsync(int tripId, DateOnly? date, int userId)
    {
        if (!await _access.IsMember(tripId, userId)) return new StatusCodeResult(403);

        var query = _contextDb.ScheduleItems.Where(item => item.Trip_Id_Fk == tripId);
        if (date.HasValue)
        {
            var start = date.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var end = date.Value == DateOnly.MaxValue ? DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc) : start.AddDays(1);
            query = query.Where(item => item.Start_At < end && item.End_At > start);
        }

        var items = await query.OrderBy(item => item.Start_At)
            .Select(item => new
            {
                Id = item.ScheduleItem_Id,
                ProposalId = item.Proposal_Id_Fk,
                item.Title,
                StartAt = item.Start_At,
                EndAt = item.End_At
            }).ToListAsync();

        return new OkObjectResult(items);
    }

    public Task<IActionResult> CreateAsync(int tripId, ScheduleItemRequest request, int userId)
    {
        return SaveAsync(tripId, null, request, userId);
    }

    public Task<IActionResult> UpdateAsync(int tripId, int itemId, ScheduleItemRequest request, int userId)
    {
        return SaveAsync(tripId, itemId, request, userId);
    }

    private async Task<IActionResult> SaveAsync(int tripId, int? itemId, ScheduleItemRequest request, int userId)
    {
        if (!await _access.IsOrganizer(tripId, userId)) return new StatusCodeResult(403);
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length > 200
            || request.StartAt == default || request.EndAt == default || request.EndAt <= request.StartAt)
            return new BadRequestObjectResult("Укажите название до 200 символов и корректный интервал времени.");

        var startAt = request.StartAt.UtcDateTime;
        var endAt = request.EndAt.UtcDateTime;

        try
        {
            await using var transaction = await _contextDb.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var trip = await _contextDb.Trips.FindAsync(tripId);
            if (trip == null) return new NotFoundResult();

            if (DateOnly.FromDateTime(startAt) < trip.Start_Date
                || DateOnly.FromDateTime(endAt.AddTicks(-1)) > trip.End_Date)
                return new BadRequestObjectResult("Событие должно находиться в пределах дат поездки по UTC.");

            var item = itemId.HasValue
                ? await _contextDb.ScheduleItems.SingleOrDefaultAsync(value =>
                    value.ScheduleItem_Id == itemId.Value && value.Trip_Id_Fk == tripId)
                : new ScheduleItem { Trip_Id_Fk = tripId };

            if (item == null) return new NotFoundResult();

            if (request.ProposalId.HasValue && !await _contextDb.Proposals.AnyAsync(proposal =>
                proposal.Proposal_Id == request.ProposalId.Value && proposal.Trip_Id_Fk == tripId
                && proposal.Status == "Approved"))
                return new BadRequestObjectResult("Выберите утверждённое предложение из этой поездки.");

            if (await _contextDb.ScheduleItems.AnyAsync(value => value.Trip_Id_Fk == tripId
                && (!itemId.HasValue || value.ScheduleItem_Id != itemId.Value)
                && value.Start_At < endAt && value.End_At > startAt))
                return new ConflictObjectResult("Это время уже занято другим событием.");

            item.Title = request.Title.Trim();
            item.Proposal_Id_Fk = request.ProposalId;
            item.Start_At = startAt;
            item.End_At = endAt;

            if (!itemId.HasValue) _contextDb.ScheduleItems.Add(item);
            await _contextDb.SaveChangesAsync();
            await transaction.CommitAsync();

            return new OkObjectResult(new
            {
                Id = item.ScheduleItem_Id,
                ProposalId = item.Proposal_Id_Fk,
                item.Title,
                StartAt = item.Start_At,
                EndAt = item.End_At
            });
        }
        catch (Exception exception) when (exception.GetBaseException() is PostgresException { SqlState: "40001" or "40P01" })
        {
            return new ConflictObjectResult("Расписание изменилось одновременно. Обновите данные и повторите действие.");
        }
    }

    public async Task<IActionResult> DeleteAsync(int tripId, int itemId, int userId)
    {
        if (!await _access.IsOrganizer(tripId, userId)) return new StatusCodeResult(403);
        var item = await _contextDb.ScheduleItems.SingleOrDefaultAsync(value =>
            value.ScheduleItem_Id == itemId && value.Trip_Id_Fk == tripId);
        if (item == null) return new NotFoundResult();

        _contextDb.ScheduleItems.Remove(item);
        await _contextDb.SaveChangesAsync();
        return new OkResult();
    }
}

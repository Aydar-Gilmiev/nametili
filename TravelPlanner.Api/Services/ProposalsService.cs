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

public class ProposalsService : IProposalsService
{
    private readonly ContextDb _db;
    private readonly TripAccess _access;

    public ProposalsService(ContextDb db, TripAccess access)
    {
        _db = db;
        _access = access;
    }

    public async Task<IActionResult> GetAsync(int tripId, int userId)
    {
        if (!await _access.IsMember(tripId, userId)) return new StatusCodeResult(403);

        var items = await _db.Proposals.Where(x => x.Trip_Id_Fk == tripId)
            .Select(x => new
            {
                Id = x.Proposal_Id,
                UserId = x.User_Id_Fk,
                x.Title,
                x.Description,
                PlannedCost = x.Planned_Cost,
                x.Status,
                VotingClosed = x.Voting_Closed,
                VoteCount = _db.Votes.Count(v => v.Proposal_Id_Fk == x.Proposal_Id),
                Score = _db.Votes.Where(v => v.Proposal_Id_Fk == x.Proposal_Id).Sum(v => (int?)v.Value) ?? 0,
                MyVote = _db.Votes.Where(v => v.Proposal_Id_Fk == x.Proposal_Id && v.User_Id_Fk == userId)
                    .Select(v => (int?)v.Value).FirstOrDefault() ?? 0
            }).ToListAsync();

        return new OkObjectResult(items);
    }

    public async Task<IActionResult> CreateAsync(int tripId, ProposalRequest request, int userId)
    {
        if (!await _access.IsMember(tripId, userId)) return new StatusCodeResult(403);
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length > 200
            || (request.Description?.Length ?? 0) > 4000
            || !MoneyHelper.IsValid(request.PlannedCost, true))
            return new BadRequestObjectResult("Проверьте название и стоимость.");

        var proposal = new Proposal
        {
            Trip_Id_Fk = tripId,
            User_Id_Fk = userId,
            Title = request.Title.Trim(),
            Description = (request.Description ?? "").Trim(),
            Planned_Cost = request.PlannedCost,
            Status = "New",
            Voting_Closed = false
        };

        _db.Proposals.Add(proposal);
        await _db.SaveChangesAsync();
        return new OkObjectResult(new
        {
            Id = proposal.Proposal_Id,
            TripId = proposal.Trip_Id_Fk,
            UserId = proposal.User_Id_Fk,
            proposal.Title,
            proposal.Description,
            PlannedCost = proposal.Planned_Cost,
            proposal.Status,
            VotingClosed = proposal.Voting_Closed
        });
    }

    public async Task<IActionResult> UpdateAsync(int tripId, int proposalId, ProposalRequest request, int userId)
    {
        if (!await _access.IsMember(tripId, userId)) return new StatusCodeResult(403);
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length > 200
            || (request.Description?.Length ?? 0) > 4000
            || !MoneyHelper.IsValid(request.PlannedCost, true))
            return new BadRequestObjectResult("Проверьте название, описание и стоимость.");

        try
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var proposal = await _db.Proposals.SingleOrDefaultAsync(value =>
                value.Proposal_Id == proposalId && value.Trip_Id_Fk == tripId);
            if (proposal == null) return new NotFoundResult();
            if (proposal.User_Id_Fk != userId && !await _access.IsOrganizer(tripId, userId))
                return new StatusCodeResult(403);
            if (proposal.Voting_Closed || proposal.Status != "New"
                || await _db.Votes.AnyAsync(vote => vote.Proposal_Id_Fk == proposalId))
                return new ConflictObjectResult("Изменять можно только новое предложение, за которое ещё не голосовали.");

            proposal.Title = request.Title.Trim();
            proposal.Description = (request.Description ?? "").Trim();
            proposal.Planned_Cost = request.PlannedCost;
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return new OkObjectResult(new
            {
                Id = proposal.Proposal_Id,
                TripId = proposal.Trip_Id_Fk,
                UserId = proposal.User_Id_Fk,
                proposal.Title,
                proposal.Description,
                PlannedCost = proposal.Planned_Cost,
                proposal.Status,
                VotingClosed = proposal.Voting_Closed
            });
        }
        catch (Exception exception) when (exception.GetBaseException() is PostgresException { SqlState: "40001" or "40P01" })
        {
            return new ConflictObjectResult("Предложение изменилось одновременно. Обновите данные и повторите действие.");
        }
    }

    public async Task<IActionResult> DeleteAsync(int tripId, int proposalId, int userId)
    {
        if (!await _access.IsMember(tripId, userId)) return new StatusCodeResult(403);
        var proposal = await _db.Proposals.SingleOrDefaultAsync(value =>
            value.Proposal_Id == proposalId && value.Trip_Id_Fk == tripId);
        if (proposal == null) return new NotFoundResult();
        if (proposal.User_Id_Fk != userId && !await _access.IsOrganizer(tripId, userId))
            return new StatusCodeResult(403);

        _db.Proposals.Remove(proposal);
        await _db.SaveChangesAsync();
        return new OkResult();
    }

    public async Task<IActionResult> VoteAsync(int tripId, int proposalId, VoteRequest request, int userId)
    {
        try
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            if (!await _access.IsMember(tripId, userId)) return new StatusCodeResult(403);

            var proposal = await _db.Proposals.SingleOrDefaultAsync(x => x.Proposal_Id == proposalId && x.Trip_Id_Fk == tripId);
            if (proposal == null) return new NotFoundResult();
            if (proposal.Voting_Closed) return new BadRequestObjectResult("Голосование закрыто.");
            if (request.Value is not (-1 or 1)) return new BadRequestObjectResult("Голос должен быть 1 или -1.");

            // Запись блокирует предложение до сохранения голоса. Закрытие голосования меняет ту же строку.
            var openProposals = await _db.Proposals
                .Where(x => x.Proposal_Id == proposalId && x.Trip_Id_Fk == tripId && !x.Voting_Closed)
                .ExecuteUpdateAsync(fields => fields.SetProperty(x => x.Voting_Closed, false));
            if (openProposals == 0) return new BadRequestObjectResult("Голосование закрыто.");

            var vote = await _db.Votes.SingleOrDefaultAsync(x => x.Proposal_Id_Fk == proposalId && x.User_Id_Fk == userId);
            if (vote == null)
                _db.Votes.Add(new Vote { Proposal_Id_Fk = proposalId, User_Id_Fk = userId, Value = request.Value });
            else
                vote.Value = request.Value;

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return new OkResult();
        }
        catch (Exception exception) when (exception.GetBaseException() is PostgresException { SqlState: "40001" or "40P01" })
        {
            return new ConflictObjectResult("Голосование изменилось одновременно. Обновите данные и повторите действие.");
        }
    }

    public async Task<IActionResult> SetStatusAsync(int tripId, int proposalId, ProposalStatusRequest request, int userId)
    {
        if (!await _access.IsOrganizer(tripId, userId)) return new StatusCodeResult(403);
        if (request.Status is not ("Approved" or "Rejected"))
            return new BadRequestObjectResult("Неизвестный статус.");

        try
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var proposal = await _db.Proposals.SingleOrDefaultAsync(x => x.Proposal_Id == proposalId && x.Trip_Id_Fk == tripId);
            if (proposal == null) return new NotFoundResult();

            if (request.Status == "Rejected" && await _db.ScheduleItems.AnyAsync(item => item.Proposal_Id_Fk == proposalId))
                return new ConflictObjectResult("Сначала удалите связанное событие из расписания или уберите его связь с предложением.");

            proposal.Status = request.Status;
            proposal.Voting_Closed = true;
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return new OkResult();
        }
        catch (Exception exception) when (exception.GetBaseException() is PostgresException { SqlState: "40001" or "40P01" })
        {
            return new ConflictObjectResult("Предложение изменилось одновременно. Обновите данные и повторите действие.");
        }
    }

    public async Task<IActionResult> BudgetAsync(int tripId, int userId)
    {
        if (!await _access.IsMember(tripId, userId)) return new StatusCodeResult(403);

        var approved = await _db.Proposals.Where(x => x.Trip_Id_Fk == tripId && x.Status == "Approved")
            .Select(x => x.Planned_Cost).ToListAsync();

        return new OkObjectResult(new { Total = approved.Sum() });
    }
}

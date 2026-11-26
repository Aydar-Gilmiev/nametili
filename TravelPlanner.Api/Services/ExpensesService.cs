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

public class ExpensesService : IExpensesService
{
    private readonly ContextDb _contextDb;
    private readonly TripAccess _access;

    public ExpensesService(ContextDb contextDb, TripAccess access)
    {
        _contextDb = contextDb;
        _access = access;
    }

    public async Task<IActionResult> GetAsync(int tripId, int userId)
    {
        if (!await _access.IsMember(tripId, userId)) return new StatusCodeResult(403);

        var expenses = await _contextDb.Expenses.Where(expense => expense.Trip_Id_Fk == tripId)
            .OrderByDescending(expense => expense.Occurred_On)
            .ThenByDescending(expense => expense.Expense_Id)
            .Select(expense => new
            {
                Id = expense.Expense_Id,
                ProposalId = expense.Proposal_Id_Fk,
                expense.Title,
                expense.Amount,
                PayerUserId = expense.Payer_User_Id_Fk,
                PayerName = expense.User.User_Name,
                OccurredOn = expense.Occurred_On,
                Shares = _contextDb.ExpenseShares.Where(share => share.Expense_Id_Fk == expense.Expense_Id)
                    .OrderBy(share => share.User_Id_Fk)
                    .Select(share => new { UserId = share.User_Id_Fk, Name = share.User.User_Name, share.Amount })
                    .ToList()
            }).ToListAsync();

        return new OkObjectResult(expenses);
    }

    public Task<IActionResult> CreateAsync(int tripId, ExpenseRequest request, int userId)
    {
        return SaveAsync(tripId, null, request, userId);
    }

    public Task<IActionResult> UpdateAsync(int tripId, int expenseId, ExpenseRequest request, int userId)
    {
        return SaveAsync(tripId, expenseId, request, userId);
    }

    private async Task<IActionResult> SaveAsync(int tripId, int? expenseId, ExpenseRequest request, int userId)
    {
        if (!await _access.IsMember(tripId, userId)) return new StatusCodeResult(403);
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length > 200
            || !MoneyHelper.IsValid(request.Amount) || request.OccurredOn == default)
            return new BadRequestObjectResult("Укажите название до 200 символов, дату и положительную сумму с точностью до копейки.");

        try
        {
            await using var transaction = await _contextDb.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var organizer = await _access.IsOrganizer(tripId, userId);
            var expense = expenseId.HasValue
                ? await _contextDb.Expenses.SingleOrDefaultAsync(value =>
                    value.Expense_Id == expenseId.Value && value.Trip_Id_Fk == tripId)
                : new Expense { Trip_Id_Fk = tripId };

            if (expense == null) return new NotFoundResult();
            if (!organizer && (request.PayerUserId != userId
                || (expenseId.HasValue && expense.Payer_User_Id_Fk != userId)))
                return new StatusCodeResult(403);

            if (expenseId.HasValue)
            {
                if (request.ProposalId.HasValue && request.ProposalId != expense.Proposal_Id_Fk)
                    return new BadRequestObjectResult("Нельзя изменить связанное с расходом предложение.");
            }
            else if (request.ProposalId.HasValue)
            {
                if (!await _contextDb.Proposals.AnyAsync(proposal => proposal.Proposal_Id == request.ProposalId.Value
                    && proposal.Trip_Id_Fk == tripId && proposal.Status == "Approved"))
                    return new BadRequestObjectResult("Выберите утверждённое предложение из этой поездки.");

                if (await _contextDb.Expenses.AnyAsync(value => value.Proposal_Id_Fk == request.ProposalId.Value))
                    return new ConflictObjectResult("Расход по этому предложению уже сохранён. Обновите список расходов.");

                expense.Proposal_Id_Fk = request.ProposalId;
            }

            var memberIds = await _contextDb.TripMembers.Where(member => member.Trip_Id_Fk == tripId)
                .Select(member => member.User_Id_Fk).ToListAsync();
            if (!memberIds.Contains(request.PayerUserId))
                return new BadRequestObjectResult("Плательщик должен быть участником поездки.");

            Dictionary<int, decimal> amounts;
            if (request.SplitMode == "Equal")
            {
                var participantIds = request.ParticipantUserIds;
                if (participantIds == null || participantIds.Count == 0
                    || participantIds.Distinct().Count() != participantIds.Count
                    || participantIds.Any(id => !memberIds.Contains(id))
                    || request.Shares?.Count > 0)
                    return new BadRequestObjectResult("Выберите участников без повторений; при равном делении ручные доли не нужны.");

                amounts = MoneyHelper.SplitEqually(request.Amount, participantIds);
            }
            else if (request.SplitMode == "Custom")
            {
                var shares = request.Shares;
                if (shares == null || shares.Count == 0 || shares.Any(share => share == null)
                    || shares.Select(share => share.UserId).Distinct().Count() != shares.Count
                    || shares.Any(share => !memberIds.Contains(share.UserId) || !MoneyHelper.IsValid(share.Amount, true))
                    || shares.Sum(share => share.Amount) != request.Amount
                    || request.ParticipantUserIds?.Count > 0)
                    return new BadRequestObjectResult("Доли должны принадлежать участникам поездки и в сумме точно совпадать с расходом.");

                amounts = shares.ToDictionary(share => share.UserId, share => share.Amount);
            }
            else
            {
                return new BadRequestObjectResult("Способ распределения: Equal или Custom.");
            }

            expense.Title = request.Title.Trim();
            expense.Amount = request.Amount;
            expense.Payer_User_Id_Fk = request.PayerUserId;
            expense.Occurred_On = request.OccurredOn;
            if (!expenseId.HasValue) _contextDb.Expenses.Add(expense);

            var oldShares = expenseId.HasValue
                ? await _contextDb.ExpenseShares.Where(share => share.Expense_Id_Fk == expenseId.Value).ToListAsync()
                : new List<ExpenseShare>();

            foreach (var oldShare in oldShares)
            {
                if (amounts.TryGetValue(oldShare.User_Id_Fk, out var amount))
                    oldShare.Amount = amount;
                else
                    _contextDb.ExpenseShares.Remove(oldShare);
            }

            foreach (var pair in amounts)
            {
                if (oldShares.All(share => share.User_Id_Fk != pair.Key))
                    _contextDb.ExpenseShares.Add(new ExpenseShare
                    {
                        Expense = expense,
                        User_Id_Fk = pair.Key,
                        Amount = pair.Value
                    });
            }

            await _contextDb.SaveChangesAsync();
            await transaction.CommitAsync();
            return new OkObjectResult(new
            {
                Id = expense.Expense_Id,
                ProposalId = expense.Proposal_Id_Fk,
                expense.Title,
                expense.Amount,
                PayerUserId = expense.Payer_User_Id_Fk,
                OccurredOn = expense.Occurred_On,
                Shares = amounts.OrderBy(pair => pair.Key).Select(pair => new { UserId = pair.Key, Amount = pair.Value })
            });
        }
        catch (DbUpdateException exception) when (exception.GetBaseException() is PostgresException
            { SqlState: "23505", ConstraintName: "IX_Expenses_Proposal_Id_Fk" })
        {
            return new ConflictObjectResult("Расход по этому предложению уже сохранён. Обновите список расходов.");
        }
        catch (Exception exception) when (exception.GetBaseException() is PostgresException { SqlState: "40001" or "40P01" })
        {
            return new ConflictObjectResult("Расходы изменились одновременно. Обновите данные и повторите действие.");
        }
    }

    public async Task<IActionResult> DeleteAsync(int tripId, int expenseId, int userId)
    {
        if (!await _access.IsMember(tripId, userId)) return new StatusCodeResult(403);
        var expense = await _contextDb.Expenses.SingleOrDefaultAsync(value =>
            value.Expense_Id == expenseId && value.Trip_Id_Fk == tripId);
        if (expense == null) return new NotFoundResult();
        if (expense.Payer_User_Id_Fk != userId && !await _access.IsOrganizer(tripId, userId))
            return new StatusCodeResult(403);

        _contextDb.Expenses.Remove(expense);
        await _contextDb.SaveChangesAsync();
        return new OkResult();
    }
}

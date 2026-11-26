using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelPlanner.Api.DatabaseContext;
using TravelPlanner.Api.Interfaces;
using TravelPlanner.Api.Responses;
using TravelPlanner.Api.UniversalMethod;

namespace TravelPlanner.Api.Services;

public class BalancesService : IBalancesService
{
    private readonly ContextDb _contextDb;
    private readonly TripAccess _access;

    public BalancesService(ContextDb contextDb, TripAccess access)
    {
        _contextDb = contextDb;
        _access = access;
    }

    public async Task<IActionResult> GetAsync(int tripId, int userId)
    {
        if (!await _access.IsMember(tripId, userId)) return new StatusCodeResult(403);

        await using var transaction = await _contextDb.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var balances = await CalculateAsync(tripId);
        await transaction.CommitAsync();
        return new OkObjectResult(new
        {
            TotalExpenses = balances.Sum(balance => balance.Paid),
            TotalRepayments = balances.Sum(balance => balance.Sent),
            Members = balances,
            Settlement = SettlementHelper.Calculate(balances)
        });
    }

    public async Task<List<BalanceResponse>> CalculateAsync(int tripId, int? excludedRepaymentId = null)
    {
        var members = await _contextDb.TripMembers
            .Where(member => member.Trip_Id_Fk == tripId)
            .OrderBy(member => member.User_Id_Fk)
            .Select(member => new BalanceResponse
            {
                UserId = member.User_Id_Fk,
                Name = member.User.User_Name
            }).ToListAsync();

        var expenses = await _contextDb.Expenses
            .Where(expense => expense.Trip_Id_Fk == tripId)
            .Select(expense => new { expense.Payer_User_Id_Fk, expense.Amount })
            .ToListAsync();

        var shares = await _contextDb.ExpenseShares
            .Where(share => share.Expense.Trip_Id_Fk == tripId)
            .Select(share => new { share.User_Id_Fk, share.Amount })
            .ToListAsync();

        var repayments = await _contextDb.Repayments
            .Where(repayment => repayment.Trip_Id_Fk == tripId
                && (!excludedRepaymentId.HasValue || repayment.Repayment_Id != excludedRepaymentId.Value))
            .Select(repayment => new { repayment.From_User_Id_Fk, repayment.To_User_Id_Fk, repayment.Amount })
            .ToListAsync();

        foreach (var member in members)
        {
            member.Paid = expenses.Where(expense => expense.Payer_User_Id_Fk == member.UserId).Sum(expense => expense.Amount);
            member.Share = shares.Where(share => share.User_Id_Fk == member.UserId).Sum(share => share.Amount);
            member.Sent = repayments.Where(repayment => repayment.From_User_Id_Fk == member.UserId).Sum(repayment => repayment.Amount);
            member.Received = repayments.Where(repayment => repayment.To_User_Id_Fk == member.UserId).Sum(repayment => repayment.Amount);
        }

        return members;
    }
}

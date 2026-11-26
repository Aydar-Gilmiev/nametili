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

public class RepaymentsService : IRepaymentsService
{
    private readonly ContextDb _contextDb;
    private readonly TripAccess _access;
    private readonly IBalancesService _balancesService;

    public RepaymentsService(ContextDb contextDb, TripAccess access, IBalancesService balancesService)
    {
        _contextDb = contextDb;
        _access = access;
        _balancesService = balancesService;
    }

    public async Task<IActionResult> GetAsync(int tripId, int userId)
    {
        if (!await _access.IsMember(tripId, userId)) return new StatusCodeResult(403);

        var repayments = await _contextDb.Repayments.Where(repayment => repayment.Trip_Id_Fk == tripId)
            .OrderByDescending(repayment => repayment.Paid_On)
            .ThenByDescending(repayment => repayment.Repayment_Id)
            .Select(repayment => new
            {
                Id = repayment.Repayment_Id,
                FromUserId = repayment.From_User_Id_Fk,
                FromName = repayment.Sender.User_Name,
                ToUserId = repayment.To_User_Id_Fk,
                ToName = repayment.User.User_Name,
                repayment.Amount,
                PaidOn = repayment.Paid_On
            }).ToListAsync();

        return new OkObjectResult(repayments);
    }

    public Task<IActionResult> CreateAsync(int tripId, RepaymentRequest request, int userId)
    {
        return SaveAsync(tripId, null, request, userId);
    }

    public Task<IActionResult> UpdateAsync(int tripId, int repaymentId, RepaymentRequest request, int userId)
    {
        return SaveAsync(tripId, repaymentId, request, userId);
    }

    private async Task<IActionResult> SaveAsync(int tripId, int? repaymentId, RepaymentRequest request, int userId)
    {
        if (!await _access.IsMember(tripId, userId)) return new StatusCodeResult(403);
        if (request.FromUserId == request.ToUserId || request.PaidOn == default || !MoneyHelper.IsValid(request.Amount))
            return new BadRequestObjectResult("Выберите разных участников, дату и положительную сумму с точностью до копейки.");

        try
        {
            await using var transaction = await _contextDb.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var organizer = await _access.IsOrganizer(tripId, userId);
            var repayment = repaymentId.HasValue
                ? await _contextDb.Repayments.SingleOrDefaultAsync(value =>
                    value.Repayment_Id == repaymentId.Value && value.Trip_Id_Fk == tripId)
                : new Repayment { Trip_Id_Fk = tripId };

            if (repayment == null) return new NotFoundResult();
            if (!organizer && (request.FromUserId != userId
                || (repaymentId.HasValue && repayment.From_User_Id_Fk != userId)))
                return new StatusCodeResult(403);

            var balances = await _balancesService.CalculateAsync(tripId, repaymentId);
            var from = balances.SingleOrDefault(balance => balance.UserId == request.FromUserId);
            var to = balances.SingleOrDefault(balance => balance.UserId == request.ToUserId);
            if (from == null || to == null)
                return new BadRequestObjectResult("Оба пользователя должны быть участниками поездки.");

            if (from.Balance >= 0 || to.Balance <= 0 || request.Amount > -from.Balance || request.Amount > to.Balance)
                return new BadRequestObjectResult("Сумма возврата превышает текущий долг или получатель не является кредитором.");

            repayment.From_User_Id_Fk = request.FromUserId;
            repayment.To_User_Id_Fk = request.ToUserId;
            repayment.Amount = request.Amount;
            repayment.Paid_On = request.PaidOn;
            if (!repaymentId.HasValue) _contextDb.Repayments.Add(repayment);

            await _contextDb.SaveChangesAsync();
            await transaction.CommitAsync();
            return new OkObjectResult(new
            {
                Id = repayment.Repayment_Id,
                FromUserId = repayment.From_User_Id_Fk,
                ToUserId = repayment.To_User_Id_Fk,
                repayment.Amount,
                PaidOn = repayment.Paid_On
            });
        }
        catch (Exception exception) when (exception.GetBaseException() is PostgresException { SqlState: "40001" or "40P01" })
        {
            return new ConflictObjectResult("Баланс изменился одновременно. Обновите данные и повторите действие.");
        }
    }

    public async Task<IActionResult> DeleteAsync(int tripId, int repaymentId, int userId)
    {
        if (!await _access.IsMember(tripId, userId)) return new StatusCodeResult(403);
        var repayment = await _contextDb.Repayments.SingleOrDefaultAsync(value =>
            value.Repayment_Id == repaymentId && value.Trip_Id_Fk == tripId);
        if (repayment == null) return new NotFoundResult();
        if (repayment.From_User_Id_Fk != userId && !await _access.IsOrganizer(tripId, userId))
            return new StatusCodeResult(403);

        _contextDb.Repayments.Remove(repayment);
        await _contextDb.SaveChangesAsync();
        return new OkResult();
    }
}

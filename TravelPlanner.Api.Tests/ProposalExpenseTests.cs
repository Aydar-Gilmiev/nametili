using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TravelPlanner.Api.DatabaseContext;
using TravelPlanner.Api.Models.Entities;
using TravelPlanner.Api.Requests;
using Xunit;
using static TravelPlanner.Api.Tests.ApiScenario;

namespace TravelPlanner.Api.Tests;

public class ProposalExpenseTests
{
    [Fact]
    public async Task ApprovalDoesNotChangeBalancesUntilExpenseIsSaved()
    {
        using var app = new ApiFactory();
        var group = await GroupAsync(app);
        var proposalId = await ProposalAsync(group);
        var path = $"/api/trips/{group.TripId}";
        var proposals = await JsonAsync(await group.Participant.Client.GetAsync(path + "/proposals"));
        Assert.Equal("Approved", proposals[0].GetProperty("status").GetString());
        Assert.Equal("Билеты в музей", proposals[0].GetProperty("title").GetString());
        Assert.Equal(120m, proposals[0].GetProperty("plannedCost").GetDecimal());
        Assert.Empty((await JsonAsync(await group.Participant.Client.GetAsync(path + "/expenses"))).EnumerateArray());
        Assert.All((await BalancesAsync(group)).Values, value => Assert.Equal(0m, value));

        var request = ExpenseBody(group, proposalId);
        var saved = await JsonAsync(await group.Participant.Client.PostAsJsonAsync(path + "/expenses", request));
        Assert.Equal(proposalId, saved.GetProperty("proposalId").GetInt32());
        Assert.Equal(90m, saved.GetProperty("amount").GetDecimal());
        var balances = await BalancesAsync(group);
        Assert.Equal(45m, balances[group.Participant.Id]);
        Assert.Equal(-45m, balances[group.Organizer.Id]);
        Assert.Equal(0m, balances[group.Third.Id]);
        var expense = Assert.Single((await JsonAsync(await group.Participant.Client.GetAsync(path + "/expenses"))).EnumerateArray());
        Assert.Equal(proposalId, expense.GetProperty("proposalId").GetInt32());
    }

    [Theory]
    [InlineData("New")]
    [InlineData("Rejected")]
    [InlineData("OtherTrip")]
    [InlineData("Missing")]
    public async Task OnlyApprovedProposalFromCurrentTripCanBeUsed(string state)
    {
        using var app = new ApiFactory();
        var group = await GroupAsync(app);
        var tripId = group.TripId;
        if (state == "OtherTrip")
            tripId = (await JsonAsync(await group.Organizer.Client.PostAsJsonAsync("/api/trips", TripBody("Другая поездка")))).GetProperty("id").GetInt32();
        var proposalId = state == "Missing" ? int.MaxValue : await ProposalAsync(group, state == "OtherTrip" ? "Approved" : state, tripId);
        var response = await group.Participant.Client.PostAsJsonAsync($"/api/trips/{group.TripId}/expenses", ExpenseBody(group, proposalId));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ContextDb>();
        Assert.Empty(await db.Expenses.ToListAsync());
        Assert.Empty(await db.ExpenseShares.ToListAsync());
    }

    [Fact]
    public async Task SavedProposalCannotBeUsedTwiceAndReturnsAfterExpenseDeletion()
    {
        using var app = new ApiFactory();
        var group = await GroupAsync(app);
        var proposalId = await ProposalAsync(group);
        var path = $"/api/trips/{group.TripId}/expenses";
        var request = ExpenseBody(group, proposalId);
        var expenseId = (await JsonAsync(await group.Participant.Client.PostAsJsonAsync(path, request))).GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.Conflict, (await group.Organizer.Client.PostAsJsonAsync(path, request)).StatusCode);
        Assert.Single((await JsonAsync(await group.Organizer.Client.GetAsync(path))).EnumerateArray());
        await OkAsync(await group.Participant.Client.DeleteAsync(path + $"/{expenseId}"));
        Assert.All((await BalancesAsync(group)).Values, value => Assert.Equal(0m, value));
        await OkAsync(await group.Participant.Client.PostAsJsonAsync(path, request));
        Assert.Single((await JsonAsync(await group.Organizer.Client.GetAsync(path))).EnumerateArray());
    }

    [Fact]
    public async Task ExpenseEditPreservesProposalAndRejectsSwitchingIt()
    {
        using var app = new ApiFactory();
        var group = await GroupAsync(app);
        var proposalId = await ProposalAsync(group);
        var secondId = await ProposalAsync(group);
        var path = $"/api/trips/{group.TripId}/expenses";
        var request = ExpenseBody(group, proposalId);
        var id = (await JsonAsync(await group.Participant.Client.PostAsJsonAsync(path, request))).GetProperty("id").GetInt32();
        request.Amount = 100m;
        request.ProposalId = null;
        var saved = await JsonAsync(await group.Participant.Client.PutAsJsonAsync(path + $"/{id}", request));
        Assert.Equal(proposalId, saved.GetProperty("proposalId").GetInt32());
        Assert.Equal(50m, (await BalancesAsync(group))[group.Participant.Id]);
        request.ProposalId = secondId;
        request.Amount = 200m;
        Assert.Equal(HttpStatusCode.BadRequest, (await group.Participant.Client.PutAsJsonAsync(path + $"/{id}", request)).StatusCode);
        Assert.Equal(50m, (await BalancesAsync(group))[group.Participant.Id]);
        Assert.Equal(HttpStatusCode.Forbidden, (await group.Third.Client.PutAsJsonAsync(path + $"/{id}", ExpenseBody(group, proposalId))).StatusCode);
    }

    [Fact]
    public async Task ProposalDeletionKeepsSavedExpenseAndBalances()
    {
        using var app = new ApiFactory();
        var group = await GroupAsync(app);
        var proposalId = await ProposalAsync(group);
        var path = $"/api/trips/{group.TripId}";
        await OkAsync(await group.Participant.Client.PostAsJsonAsync(path + "/expenses", ExpenseBody(group, proposalId)));
        var before = await BalancesAsync(group);
        await OkAsync(await group.Organizer.Client.DeleteAsync(path + $"/proposals/{proposalId}"));
        var saved = Assert.Single((await JsonAsync(await group.Participant.Client.GetAsync(path + "/expenses"))).EnumerateArray());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, saved.GetProperty("proposalId").ValueKind);
        Assert.Equal(2, saved.GetProperty("shares").GetArrayLength());
        var after = await BalancesAsync(group);
        foreach (var value in before) Assert.Equal(value.Value, after[value.Key]);
    }

    [Fact]
    public async Task DatabasePreventsDuplicateLinksEvenWhenServiceCheckIsBypassed()
    {
        using var app = new ApiFactory();
        var group = await GroupAsync(app);
        var proposalId = await ProposalAsync(group);
        await OkAsync(await group.Participant.Client.PostAsJsonAsync($"/api/trips/{group.TripId}/expenses", ExpenseBody(group, proposalId)));
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ContextDb>();
        db.Expenses.Add(new Expense { Trip_Id_Fk = group.TripId, Proposal_Id_Fk = proposalId,
            Payer_User_Id_Fk = group.Participant.Id, Title = "Повторная запись", Amount = 90m, Occurred_On = new DateOnly(2026, 10, 20) });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    private static async Task<int> ProposalAsync(TravelGroup group, string status = "Approved", int? tripId = null)
    {
        var path = $"/api/trips/{tripId ?? group.TripId}/proposals";
        var id = (await JsonAsync(await group.Organizer.Client.PostAsJsonAsync(path,
            new ProposalRequest { Title = "Билеты в музей", Description = "Входные билеты", PlannedCost = 120m }))).GetProperty("id").GetInt32();
        if (status != "New")
            await OkAsync(await group.Organizer.Client.PatchAsJsonAsync(path + $"/{id}/status", new { Status = status }));
        return id;
    }

    private static ExpenseRequest ExpenseBody(TravelGroup group, int proposalId) => new()
    {
        ProposalId = proposalId, Title = "Билеты в музей", Amount = 90m,
        PayerUserId = group.Participant.Id, OccurredOn = new DateOnly(2026, 10, 20),
        ParticipantUserIds = [group.Organizer.Id, group.Participant.Id]
    };
}

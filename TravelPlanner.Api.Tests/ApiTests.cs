using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TravelPlanner.Api.DatabaseContext;
using TravelPlanner.Api.Models.Entities;
using TravelPlanner.Api.Requests;
using TravelPlanner.Api.UniversalMethod;
using Xunit;
using static TravelPlanner.Api.Tests.ApiScenario;

namespace TravelPlanner.Api.Tests;

public class ApiTests
{
    [Fact]
    public async Task RegistrationStoresHashAndLoginChecksPassword()
    {
        using var app = new ApiFactory();
        var actor = await RegisterAsync(app, "owner");
        var login = await JsonAsync(await actor.Client.PostAsJsonAsync("/api/auth/login",
            new { Email = "OWNER@EXAMPLE.COM", Password = "Test-Password-123" }));
        Assert.Equal(actor.Id, login.GetProperty("user").GetProperty("id").GetInt32());
        Assert.Equal(HttpStatusCode.Unauthorized, (await actor.Client.PostAsJsonAsync("/api/auth/login",
            new { actor.Email, Password = "wrong-password" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await actor.Client.PostAsJsonAsync("/api/auth/register",
            new { Name = "Duplicate", actor.Email, Password = "Test-Password-123" })).StatusCode);

        using var scope = app.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<ContextDb>();
        var configuration = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
        var jwtOptions = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>>()
            .Get(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme);
        var signingKey = Assert.IsType<Microsoft.IdentityModel.Tokens.SymmetricSecurityKey>(jwtOptions.TokenValidationParameters.IssuerSigningKey);
        Assert.True(signingKey.Key.SequenceEqual(System.Text.Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!)),
            "JWT generator and bearer validation use different keys.");
        var user = await database.Users.SingleAsync();
        Assert.NotEqual("Test-Password-123", user.Password_Hash);
        Assert.NotEqual(PasswordVerificationResult.Failed,
            new PasswordHasher<User>().VerifyHashedPassword(user, user.Password_Hash, "Test-Password-123"));
        Assert.DoesNotContain("password", login.GetProperty("user").ToString().ToLowerInvariant());
    }

    [Theory]
    [InlineData("schedule")]
    [InlineData("expenses")]
    [InlineData("repayments")]
    [InlineData("balances")]
    public async Task FinancialAndScheduleEndpointsRequireMembership(string resource)
    {
        using var app = new ApiFactory();
        var group = await GroupAsync(app);
        var url = $"/api/trips/{group.TripId}/{resource}";
        Assert.Equal(HttpStatusCode.Forbidden, (await group.Outsider.Client.GetAsync(url)).StatusCode);
        using var anonymous = app.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(url)).StatusCode);
        await OkAsync(await group.Participant.Client.GetAsync(url));
    }

    [Fact]
    public async Task ScheduleChecksApprovalOverlapsPermissionsAndTripDates()
    {
        using var app = new ApiFactory();
        var group = await GroupAsync(app);
        var url = $"/api/trips/{group.TripId}/schedule";
        var proposals = $"/api/trips/{group.TripId}/proposals";
        var proposal = await JsonAsync(await group.Participant.Client.PostAsJsonAsync(proposals,
            new ProposalRequest { Title = "Музей", PlannedCost = 500 }));
        var proposalId = proposal.GetProperty("id").GetInt32();
        var request = new ScheduleItemRequest
        {
            Title = "Музей",
            ProposalId = proposalId,
            StartAt = DateTimeOffset.Parse("2026-10-20T13:00:00+03:00"),
            EndAt = DateTimeOffset.Parse("2026-10-20T14:00:00+03:00")
        };
        Assert.Equal(HttpStatusCode.BadRequest, (await group.Organizer.Client.PostAsJsonAsync(url, request)).StatusCode);
        await OkAsync(await group.Organizer.Client.PatchAsJsonAsync($"{proposals}/{proposalId}/status", new { Status = "Approved" }));
        var first = await JsonAsync(await group.Organizer.Client.PostAsJsonAsync(url, request));
        var itemId = first.GetProperty("id").GetInt32();
        Assert.Equal(10, first.GetProperty("startAt").GetDateTimeOffset().Hour);
        Assert.Equal(TimeSpan.Zero, first.GetProperty("startAt").GetDateTimeOffset().Offset);

        Assert.Equal(HttpStatusCode.Forbidden, (await group.Participant.Client.PostAsJsonAsync(url, request)).StatusCode);
        request.StartAt = DateTimeOffset.Parse("2026-10-20T10:30:00Z");
        request.EndAt = DateTimeOffset.Parse("2026-10-20T11:30:00Z");
        Assert.Equal(HttpStatusCode.Conflict, (await group.Organizer.Client.PostAsJsonAsync(url, request)).StatusCode);
        request.StartAt = DateTimeOffset.Parse("2026-10-20T11:00:00Z");
        request.EndAt = DateTimeOffset.Parse("2026-10-20T12:00:00Z");
        var second = await JsonAsync(await group.Organizer.Client.PostAsJsonAsync(url, request));
        await OkAsync(await group.Organizer.Client.PutAsJsonAsync($"{url}/{second.GetProperty("id").GetInt32()}", request));

        Assert.Equal(2, (await JsonAsync(await group.Participant.Client.GetAsync(url + "?date=2026-10-20"))).GetArrayLength());
        Assert.Equal(0, (await JsonAsync(await group.Participant.Client.GetAsync(url + "?date=2026-10-21"))).GetArrayLength());
        Assert.Equal(HttpStatusCode.Conflict, (await group.Organizer.Client.PutAsJsonAsync($"/api/trips/{group.TripId}",
            TripBody(start: "2026-10-21"))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await group.Organizer.Client.PatchAsJsonAsync(
            $"{proposals}/{proposalId}/status", new { Status = "Rejected" })).StatusCode);

        await OkAsync(await group.Participant.Client.DeleteAsync($"{proposals}/{proposalId}"));
        var detached = await JsonAsync(await group.Organizer.Client.GetAsync(url));
        Assert.All(detached.EnumerateArray(), item => Assert.Equal(System.Text.Json.JsonValueKind.Null, item.GetProperty("proposalId").ValueKind));
        await OkAsync(await group.Organizer.Client.DeleteAsync($"{url}/{itemId}"));
        Assert.Equal(HttpStatusCode.NotFound, (await group.Organizer.Client.DeleteAsync($"{url}/{itemId}")).StatusCode);
    }

    [Theory]
    [InlineData("2026-10-20T10:00:00Z", "2026-10-20T10:00:00Z")]
    [InlineData("2026-10-20T11:00:00Z", "2026-10-20T10:00:00Z")]
    [InlineData("2026-10-19T23:00:00Z", "2026-10-20T01:00:00Z")]
    [InlineData("2026-10-22T23:00:00Z", "2026-10-23T01:00:00Z")]
    public async Task ScheduleRejectsInvalidOrOutOfTripIntervals(string start, string end)
    {
        using var app = new ApiFactory();
        var group = await GroupAsync(app);
        var response = await group.Organizer.Client.PostAsJsonAsync($"/api/trips/{group.TripId}/schedule",
            new { Title = "Событие", StartAt = start, EndAt = end });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task OvernightScheduleIsVisibleOnBothDaysAndConflictsOnUpdate()
    {
        using var app = new ApiFactory();
        var group = await GroupAsync(app);
        var url = $"/api/trips/{group.TripId}/schedule";
        await JsonAsync(await group.Organizer.Client.PostAsJsonAsync(url,
            new { Title = "Ночная прогулка", StartAt = "2026-10-20T23:00:00Z", EndAt = "2026-10-21T01:00:00Z" }));
        var second = await JsonAsync(await group.Organizer.Client.PostAsJsonAsync(url,
            new { Title = "Отдых", StartAt = "2026-10-21T01:00:00Z", EndAt = "2026-10-21T02:00:00Z" }));
        Assert.Equal(1, (await JsonAsync(await group.Organizer.Client.GetAsync(url + "?date=2026-10-20"))).GetArrayLength());
        Assert.Equal(2, (await JsonAsync(await group.Organizer.Client.GetAsync(url + "?date=2026-10-21"))).GetArrayLength());
        Assert.Equal(HttpStatusCode.Conflict, (await group.Organizer.Client.PutAsJsonAsync(
            $"{url}/{second.GetProperty("id").GetInt32()}",
            new { Title = "Отдых", StartAt = "2026-10-21T00:30:00Z", EndAt = "2026-10-21T02:00:00Z" })).StatusCode);

        var otherTrip = await JsonAsync(await group.Outsider.Client.PostAsJsonAsync("/api/trips", TripBody("Другая поездка")));
        var otherTripId = otherTrip.GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.NotFound, (await group.Outsider.Client.DeleteAsync(
            $"/api/trips/{otherTripId}/schedule/{second.GetProperty("id").GetInt32()}")).StatusCode);
    }

    [Fact]
    public async Task EqualExpensesAndRepaymentsBringBalancesToZero()
    {
        using var app = new ApiFactory();
        var group = await GroupAsync(app);
        var expenseUrl = $"/api/trips/{group.TripId}/expenses";
        var request = EqualExpense(group, 100m);
        var expense = await JsonAsync(await group.Organizer.Client.PostAsJsonAsync(expenseUrl, request));
        var shares = expense.GetProperty("shares").EnumerateArray().ToDictionary(
            share => share.GetProperty("userId").GetInt32(), share => share.GetProperty("amount").GetDecimal());
        Assert.Equal(100m, shares.Values.Sum());
        Assert.Equal(33.34m, shares[group.Organizer.Id]);
        Assert.Equal(33.33m, shares[group.Participant.Id]);
        var balances = await BalancesAsync(group);
        Assert.Equal(66.66m, balances[group.Organizer.Id]);
        Assert.Equal(-33.33m, balances[group.Participant.Id]);

        var repaymentUrl = $"/api/trips/{group.TripId}/repayments";
        var overpayment = Repayment(group.Participant.Id, group.Organizer.Id, 33.34m);
        Assert.Equal(HttpStatusCode.BadRequest, (await group.Participant.Client.PostAsJsonAsync(repaymentUrl, overpayment)).StatusCode);
        var first = await JsonAsync(await group.Participant.Client.PostAsJsonAsync(repaymentUrl,
            Repayment(group.Participant.Id, group.Organizer.Id, 33.33m)));
        Assert.Equal(HttpStatusCode.Forbidden, (await group.Participant.Client.PostAsJsonAsync(repaymentUrl,
            Repayment(group.Third.Id, group.Organizer.Id, 33.33m))).StatusCode);
        await JsonAsync(await group.Third.Client.PostAsJsonAsync(repaymentUrl,
            Repayment(group.Third.Id, group.Organizer.Id, 33.33m)));
        Assert.All((await BalancesAsync(group)).Values, amount => Assert.Equal(0m, amount));

        var list = await JsonAsync(await group.Organizer.Client.GetAsync(repaymentUrl));
        Assert.Equal(2, list.GetArrayLength());
        Assert.Contains(list.EnumerateArray(), repayment => repayment.GetProperty("fromName").GetString() == "participant"
            && repayment.GetProperty("toName").GetString() == "organizer");

        var repaymentId = first.GetProperty("id").GetInt32();
        await JsonAsync(await group.Participant.Client.PutAsJsonAsync($"{repaymentUrl}/{repaymentId}",
            Repayment(group.Participant.Id, group.Organizer.Id, 10m)));
        Assert.Equal(-23.33m, (await BalancesAsync(group))[group.Participant.Id]);
        await OkAsync(await group.Participant.Client.DeleteAsync($"{repaymentUrl}/{repaymentId}"));
        Assert.Equal(-33.33m, (await BalancesAsync(group))[group.Participant.Id]);
    }

    [Fact]
    public async Task ExpenseUpdateReplacesSharesAndDeleteRecalculatesBalance()
    {
        using var app = new ApiFactory();
        var group = await GroupAsync(app);
        var url = $"/api/trips/{group.TripId}/expenses";
        var request = EqualExpense(group, 90m);
        var first = await JsonAsync(await group.Organizer.Client.PostAsJsonAsync(url, request));
        var id = first.GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.Forbidden, (await group.Participant.Client.PutAsJsonAsync($"{url}/{id}", request)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await group.Participant.Client.DeleteAsync($"{url}/{id}")).StatusCode);

        var edited = new ExpenseRequest
        {
            Title = "Исправленный расход", PayerUserId = group.Organizer.Id, Amount = 70m,
            OccurredOn = new DateOnly(2026, 10, 19), SplitMode = "Custom",
            Shares = [
                new ExpenseShareRequest { UserId = group.Participant.Id, Amount = 20m },
                new ExpenseShareRequest { UserId = group.Third.Id, Amount = 50m }
            ]
        };
        await JsonAsync(await group.Organizer.Client.PutAsJsonAsync($"{url}/{id}", edited));
        var balances = await BalancesAsync(group);
        Assert.Equal(70m, balances[group.Organizer.Id]);
        Assert.Equal(-20m, balances[group.Participant.Id]);
        Assert.Equal(-50m, balances[group.Third.Id]);
        var list = await JsonAsync(await group.Participant.Client.GetAsync(url));
        Assert.Equal(2, list[0].GetProperty("shares").GetArrayLength());
        Assert.Equal("organizer", list[0].GetProperty("payerName").GetString());
        await OkAsync(await group.Organizer.Client.DeleteAsync($"{url}/{id}"));
        Assert.All((await BalancesAsync(group)).Values, amount => Assert.Equal(0m, amount));
        using var scope = app.Services.CreateScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<ContextDb>().ExpenseShares.ToListAsync());
    }

    [Fact]
    public async Task ExpenseValidationDoesNotPartiallyChangeData()
    {
        using var app = new ApiFactory();
        var group = await GroupAsync(app);
        var url = $"/api/trips/{group.TripId}/expenses";
        var request = EqualExpense(group, 12m);
        var expense = await JsonAsync(await group.Organizer.Client.PostAsJsonAsync(url, request));
        var id = expense.GetProperty("id").GetInt32();

        request.ParticipantUserIds = [group.Participant.Id, group.Participant.Id];
        Assert.Equal(HttpStatusCode.BadRequest, (await group.Organizer.Client.PutAsJsonAsync($"{url}/{id}", request)).StatusCode);
        request.ParticipantUserIds = [group.Outsider.Id];
        Assert.Equal(HttpStatusCode.BadRequest, (await group.Organizer.Client.PutAsJsonAsync($"{url}/{id}", request)).StatusCode);
        request.ParticipantUserIds = [];
        request.SplitMode = "Custom";
        request.Shares = [new ExpenseShareRequest { UserId = group.Participant.Id, Amount = 11.99m }];
        Assert.Equal(HttpStatusCode.BadRequest, (await group.Organizer.Client.PutAsJsonAsync($"{url}/{id}", request)).StatusCode);
        request.Shares = [new ExpenseShareRequest { UserId = group.Participant.Id, Amount = 12m }];
        request.Amount = 12.001m;
        Assert.Equal(HttpStatusCode.BadRequest, (await group.Organizer.Client.PutAsJsonAsync($"{url}/{id}", request)).StatusCode);
        var list = await JsonAsync(await group.Organizer.Client.GetAsync(url));
        Assert.Equal(12m, list[0].GetProperty("amount").GetDecimal());
        Assert.Equal(3, list[0].GetProperty("shares").GetArrayLength());
        Assert.Equal(8m, (await BalancesAsync(group))[group.Organizer.Id]);
        Assert.Equal(HttpStatusCode.Forbidden, (await group.Participant.Client.PostAsJsonAsync(url, EqualExpense(group, 12m))).StatusCode);
    }

    [Fact]
    public async Task RepaymentsRejectSelfOutsidersInvalidAmountsAndUnauthorizedEdits()
    {
        using var app = new ApiFactory();
        var group = await GroupAsync(app);
        await JsonAsync(await group.Organizer.Client.PostAsJsonAsync($"/api/trips/{group.TripId}/expenses", EqualExpense(group, 90m)));
        var url = $"/api/trips/{group.TripId}/repayments";
        foreach (var request in new[]
        {
            Repayment(group.Participant.Id, group.Participant.Id, 10m),
            Repayment(group.Participant.Id, group.Outsider.Id, 10m),
            Repayment(group.Organizer.Id, group.Participant.Id, 10m),
            Repayment(group.Participant.Id, group.Organizer.Id, 0m),
            Repayment(group.Participant.Id, group.Organizer.Id, 0.001m)
        })
            Assert.Equal(HttpStatusCode.BadRequest, (await group.Organizer.Client.PostAsJsonAsync(url, request)).StatusCode);

        var repayment = await JsonAsync(await group.Participant.Client.PostAsJsonAsync(url,
            Repayment(group.Participant.Id, group.Organizer.Id, 10m)));
        var id = repayment.GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.Forbidden, (await group.Third.Client.PutAsJsonAsync($"{url}/{id}",
            Repayment(group.Third.Id, group.Organizer.Id, 10m))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await group.Third.Client.DeleteAsync($"{url}/{id}")).StatusCode);
        await JsonAsync(await group.Organizer.Client.PutAsJsonAsync($"{url}/{id}",
            Repayment(group.Participant.Id, group.Organizer.Id, 30m)));
        Assert.Equal(0m, (await BalancesAsync(group))[group.Participant.Id]);
    }

    [Fact]
    public async Task TripAndProposalCrudPreserveRolesVotesAndBudget()
    {
        using var app = new ApiFactory();
        var group = await GroupAsync(app);
        var tripUrl = $"/api/trips/{group.TripId}";
        Assert.Equal(HttpStatusCode.Forbidden, (await group.Participant.Client.PutAsJsonAsync(tripUrl, TripBody("Новое имя"))).StatusCode);
        await JsonAsync(await group.Organizer.Client.PutAsJsonAsync(tripUrl, TripBody("Новое имя")));
        Assert.Equal("Новое имя", (await JsonAsync(await group.Organizer.Client.GetAsync(tripUrl))).GetProperty("trip").GetProperty("name").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await group.Organizer.Client.PutAsJsonAsync(tripUrl,
            TripBody(start: "0001-01-01"))).StatusCode);

        var url = tripUrl + "/proposals";
        var proposal = await JsonAsync(await group.Participant.Client.PostAsJsonAsync(url,
            new ProposalRequest { Title = "Первое", PlannedCost = 20m }));
        var id = proposal.GetProperty("id").GetInt32();
        var request = new ProposalRequest { Title = "Исправлено", PlannedCost = 42m };
        Assert.Equal(HttpStatusCode.Forbidden, (await group.Third.Client.PutAsJsonAsync($"{url}/{id}", request)).StatusCode);
        await JsonAsync(await group.Participant.Client.PutAsJsonAsync($"{url}/{id}", request));
        await OkAsync(await group.Organizer.Client.PutAsJsonAsync($"{url}/{id}/vote", new VoteRequest { Value = 1 }));
        await OkAsync(await group.Organizer.Client.PutAsJsonAsync($"{url}/{id}/vote", new VoteRequest { Value = -1 }));
        var items = await JsonAsync(await group.Participant.Client.GetAsync(url));
        Assert.Equal(-1, items[0].GetProperty("score").GetInt32());
        Assert.Equal(HttpStatusCode.Conflict, (await group.Participant.Client.PutAsJsonAsync($"{url}/{id}", request)).StatusCode);
        await OkAsync(await group.Organizer.Client.PatchAsJsonAsync($"{url}/{id}/status", new { Status = "Approved" }));
        Assert.Equal(HttpStatusCode.BadRequest, (await group.Third.Client.PutAsJsonAsync($"{url}/{id}/vote", new VoteRequest { Value = 1 })).StatusCode);
        Assert.Equal(42m, (await JsonAsync(await group.Organizer.Client.GetAsync(url + "/budget"))).GetProperty("total").GetDecimal());
        Assert.Equal(HttpStatusCode.Forbidden, (await group.Third.Client.DeleteAsync($"{url}/{id}")).StatusCode);
        await OkAsync(await group.Organizer.Client.DeleteAsync($"{url}/{id}"));
        Assert.Equal(0m, (await JsonAsync(await group.Organizer.Client.GetAsync(url + "/budget"))).GetProperty("total").GetDecimal());
        using var scope = app.Services.CreateScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<ContextDb>().Votes.ToListAsync());
    }

    [Fact]
    public async Task DeletingTripRemovesItsDataButKeepsUsers()
    {
        using var app = new ApiFactory();
        var group = await GroupAsync(app);
        var url = $"/api/trips/{group.TripId}";
        await JsonAsync(await group.Organizer.Client.PostAsJsonAsync(url + "/expenses", EqualExpense(group, 90m)));
        await JsonAsync(await group.Participant.Client.PostAsJsonAsync(url + "/repayments",
            Repayment(group.Participant.Id, group.Organizer.Id, 10m)));
        await JsonAsync(await group.Participant.Client.PostAsJsonAsync(url + "/proposals", new ProposalRequest { Title = "Идея" }));
        await JsonAsync(await group.Organizer.Client.PostAsJsonAsync(url + "/schedule",
            new { Title = "Прогулка", StartAt = "2026-10-20T10:00:00Z", EndAt = "2026-10-20T11:00:00Z" }));
        Assert.Equal(HttpStatusCode.Forbidden, (await group.Participant.Client.DeleteAsync(url)).StatusCode);
        await OkAsync(await group.Organizer.Client.DeleteAsync(url));
        using var scope = app.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<ContextDb>();
        Assert.Empty(await database.Trips.ToListAsync());
        Assert.Empty(await database.TripMembers.ToListAsync());
        Assert.Empty(await database.Invitations.ToListAsync());
        Assert.Empty(await database.Proposals.ToListAsync());
        Assert.Empty(await database.ScheduleItems.ToListAsync());
        Assert.Empty(await database.Expenses.ToListAsync());
        Assert.Empty(await database.ExpenseShares.ToListAsync());
        Assert.Empty(await database.Repayments.ToListAsync());
        Assert.Equal(4, await database.Users.CountAsync());
    }

    [Theory]
    [InlineData("0", false)]
    [InlineData("-1", false)]
    [InlineData("0.001", false)]
    [InlineData("9999999999.99", true)]
    [InlineData("10000000000", false)]
    [InlineData("0.01", true)]
    public void MoneyValidationMatchesNumericPrecision(string value, bool expected)
    {
        Assert.Equal(expected, MoneyHelper.IsValid(decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Fact]
    public void EqualSplitPreservesEveryCentAndIsIndependentOfInputOrder()
    {
        var first = MoneyHelper.SplitEqually(0.01m, [3, 1, 2]);
        var second = MoneyHelper.SplitEqually(0.01m, [2, 3, 1]);
        Assert.Equal(0.01m, first.Values.Sum());
        Assert.Equal(0.01m, first[1]);
        Assert.Equal(0m, first[2]);
        Assert.Equal(first.OrderBy(pair => pair.Key), second.OrderBy(pair => pair.Key));
        Assert.Throws<ArgumentException>(() => MoneyHelper.SplitEqually(1m, []));
        Assert.Throws<ArgumentException>(() => MoneyHelper.SplitEqually(1m, [1, 1]));
    }

    private static ExpenseRequest EqualExpense(TravelGroup group, decimal amount)
    {
        return new ExpenseRequest
        {
            Title = "Общий расход", PayerUserId = group.Organizer.Id, Amount = amount,
            OccurredOn = new DateOnly(2026, 10, 20),
            ParticipantUserIds = [group.Organizer.Id, group.Participant.Id, group.Third.Id]
        };
    }

    private static RepaymentRequest Repayment(int from, int to, decimal amount)
    {
        return new RepaymentRequest { FromUserId = from, ToUserId = to, Amount = amount, PaidOn = new DateOnly(2026, 10, 21) };
    }
}

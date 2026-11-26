using System.Net;
using System.Net.Http.Json;
using System.Text;
using PdfSharp.Pdf.IO;
using TravelPlanner.Api.Responses;
using TravelPlanner.Api.UniversalMethod;
using Xunit;
using static TravelPlanner.Api.Tests.ApiScenario;

namespace TravelPlanner.Api.Tests;

public class CompletionTests
{
    [Fact]
    public async Task PdfRequiresTripMembership()
    {
        using var app = new ApiFactory();
        var group = await GroupAsync(app);
        var url = $"/api/trips/{group.TripId}/export/pdf";
        Assert.Equal(HttpStatusCode.Forbidden, (await group.Outsider.Client.GetAsync(url)).StatusCode);
        using var anonymous = app.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(url)).StatusCode);
        var response = await group.Participant.Client.GetAsync(url);
        await OkAsync(response);
        Assert.Contains($"trip-{group.TripId}.pdf", response.Content.Headers.ContentDisposition!.ToString());
    }

    [Fact]
    public async Task MovingOntoOccupiedTimeIsRejectedWithoutChangingEvent()
    {
        using var app = new ApiFactory();
        var group = await GroupAsync(app);
        var url = $"/api/trips/{group.TripId}/schedule";
        var first = await JsonAsync(await group.Organizer.Client.PostAsJsonAsync(url, new
        { Title = "Первое", StartAt = "2026-10-20T10:00:00Z", EndAt = "2026-10-20T11:00:00Z" }));
        await OkAsync(await group.Organizer.Client.PostAsJsonAsync(url, new
        { Title = "Второе", StartAt = "2026-10-21T10:00:00Z", EndAt = "2026-10-21T11:00:00Z" }));
        var id = first.GetProperty("id").GetInt32();
        var response = await group.Organizer.Client.PutAsJsonAsync(url + $"/{id}", new
        { Title = "Первое", StartAt = "2026-10-21T10:00:00Z", EndAt = "2026-10-21T11:00:00Z" });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var events = await JsonAsync(await group.Participant.Client.GetAsync(url));
        Assert.Equal(DateTime.Parse("2026-10-20T10:00:00Z").ToUniversalTime(), events.EnumerateArray().Single(value => value.GetProperty("id").GetInt32() == id).GetProperty("startAt").GetDateTime());
        Assert.Equal(HttpStatusCode.Forbidden, (await group.Participant.Client.DeleteAsync(url + $"/{id}")).StatusCode);
    }

    [Fact]
    public async Task PdfIsReadableAndLongTextContinuesOnNewPages()
    {
        using var app = new ApiFactory();
        var group = await GroupAsync(app);
        var url = $"/api/trips/{group.TripId}";
        for (var day = 20; day <= 22; day++)
            await OkAsync(await group.Organizer.Client.PostAsJsonAsync(url + "/schedule", new
            { Title = "Прогулка по старому городу " + new string('Я', 150), StartAt = $"2026-10-{day}T10:00:00Z", EndAt = $"2026-10-{day}T11:00:00Z" }));
        for (var index = 0; index < 12; index++)
            await OkAsync(await group.Participant.Client.PostAsJsonAsync(url + "/places", new { Title = "Место " + index, Note = new string('Ю', 950), Latitude = 55.79, Longitude = 49.12 }));
        var response = await group.Participant.Client.GetAsync(url + "/export/pdf");
        await OkAsync(response);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType!.MediaType);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.StartsWith("%PDF-", Encoding.ASCII.GetString(bytes.Take(8).ToArray()));
        using var stream = new MemoryStream(bytes);
        using var pdf = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
        Assert.True(pdf.PageCount > 1);
        Assert.Equal("Казань", pdf.Info.Title);
    }

    [Fact]
    public void SettlementFindsBetterPlanThanGreedyAndDoesNotMutateBalances()
    {
        var members = Members(-8, -7, -6, 9, 8, 4);
        var before = members.Select(value => value.Balance).ToArray();
        var result = SettlementHelper.Calculate(members);
        Assert.True(result.IsOptimal);
        Assert.Equal(4, result.Transfers.Count);
        Assert.Equal(before, members.Select(value => value.Balance).ToArray());
        VerifyPlan(members, result);
    }

    [Fact]
    public void SettlementMatchesIndependentSubsetSolverForSmallGroups()
    {
        var random = new Random(20261029);
        for (var iteration = 0; iteration < 60; iteration++)
        {
            var values = Enumerable.Range(0, 7).Select(_ => random.Next(-12, 13)).ToList();
            values.Add(-values.Sum());
            var members = Members(values.Where(value => value != 0).Select(value => (decimal)value / 100).ToArray());
            var plan = SettlementHelper.Calculate(members);
            Assert.True(plan.IsOptimal);
            Assert.Equal(Minimum(members.Select(value => value.Balance).ToArray()), plan.Transfers.Count);
            VerifyPlan(members, plan);
        }
    }

    [Fact]
    public void LargeGroupGetsValidPlanWithoutFalseOptimalityClaim()
    {
        var members = Members(Enumerable.Repeat(new decimal[] { -8, -7, -6, 9, 8, 4 }, 3).SelectMany(value => value).ToArray());
        var plan = SettlementHelper.Calculate(members);
        Assert.False(plan.IsOptimal);
        VerifyPlan(members, plan);
    }

    [Fact]
    public async Task RecommendedRepaymentsCanBeRecordedAndCloseAllBalances()
    {
        using var app = new ApiFactory();
        var group = await GroupAsync(app);
        var url = $"/api/trips/{group.TripId}";
        await OkAsync(await group.Organizer.Client.PostAsJsonAsync(url + "/expenses", new
        { Title = "Билеты", PayerUserId = group.Organizer.Id, Amount = 100m, OccurredOn = "2026-10-20", SplitMode = "Equal", ParticipantUserIds = new[] { group.Organizer.Id, group.Participant.Id, group.Third.Id } }));
        var balances = await JsonAsync(await group.Participant.Client.GetAsync(url + "/balances"));
        Assert.True(balances.GetProperty("settlement").GetProperty("isOptimal").GetBoolean());
        foreach (var transfer in balances.GetProperty("settlement").GetProperty("transfers").EnumerateArray())
            await OkAsync(await group.Organizer.Client.PostAsJsonAsync(url + "/repayments", new
            { FromUserId = transfer.GetProperty("fromUserId").GetInt32(), ToUserId = transfer.GetProperty("toUserId").GetInt32(), Amount = transfer.GetProperty("amount").GetDecimal(), PaidOn = "2026-10-21" }));
        var closed = await JsonAsync(await group.Participant.Client.GetAsync(url + "/balances"));
        Assert.Empty(closed.GetProperty("settlement").GetProperty("transfers").EnumerateArray());
        Assert.All(closed.GetProperty("members").EnumerateArray(), value => Assert.Equal(0m, value.GetProperty("balance").GetDecimal()));
    }

    private static List<BalanceResponse> Members(params decimal[] balances) => balances.Select((value, index) => new BalanceResponse { UserId = index + 1, Name = "Участник " + index, Paid = Math.Max(0, value), Share = Math.Max(0, -value) }).ToList();
    private static void VerifyPlan(List<BalanceResponse> members, SettlementResponse result)
    {
        var amounts = members.ToDictionary(value => value.UserId, value => value.Balance);
        foreach (var transfer in result.Transfers)
        {
            Assert.True(transfer.Amount > 0 && transfer.Amount <= -amounts[transfer.FromUserId] && transfer.Amount <= amounts[transfer.ToUserId]);
            amounts[transfer.FromUserId] += transfer.Amount; amounts[transfer.ToUserId] -= transfer.Amount;
        }
        Assert.All(amounts.Values, value => Assert.Equal(0m, value));
    }
    private static int Minimum(decimal[] values)
    {
        var size = 1 << values.Length;
        var sums = new decimal[size];
        var groups = new int[size];
        for (var mask = 1; mask < size; mask++)
        {
            for (var bit = 0; bit < values.Length; bit++) if ((mask & (1 << bit)) != 0) { sums[mask] = sums[mask ^ (1 << bit)] + values[bit]; break; }
            for (var bit = 0; bit < values.Length; bit++) if ((mask & (1 << bit)) != 0) groups[mask] = Math.Max(groups[mask], groups[mask ^ (1 << bit)] + (sums[mask] == 0 ? 1 : 0));
        }
        return values.Length - groups[^1];
    }
}

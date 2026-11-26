using System.Net.Http.Json;
using TravelPlanner.Api.Requests;
using Xunit;
using static TravelPlanner.Api.Tests.ApiScenario;

namespace TravelPlanner.Api.Tests;

public class ProposalViewTests
{
    [Fact]
    public async Task VoteCountMustNotBeConfusedWithZeroScore()
    {
        using var app = new ApiFactory();
        var group = await GroupAsync(app);
        var url = $"/api/trips/{group.TripId}/proposals";
        var proposal = await JsonAsync(await group.Participant.Client.PostAsJsonAsync(url,
            new ProposalRequest { Title = "Музей" }));
        var id = proposal.GetProperty("id").GetInt32();
        var before = await JsonAsync(await group.Organizer.Client.GetAsync(url));
        Assert.Equal(0, before[0].GetProperty("voteCount").GetInt32());
        await OkAsync(await group.Participant.Client.PutAsJsonAsync($"{url}/{id}/vote", new { Value = 1 }));
        await OkAsync(await group.Organizer.Client.PutAsJsonAsync($"{url}/{id}/vote", new { Value = -1 }));
        var after = await JsonAsync(await group.Organizer.Client.GetAsync(url));
        Assert.Equal(0, after[0].GetProperty("score").GetInt32());
        Assert.Equal(2, after[0].GetProperty("voteCount").GetInt32());
    }
}

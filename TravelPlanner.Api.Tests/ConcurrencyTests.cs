using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using TravelPlanner.Api.DatabaseContext;
using TravelPlanner.Api.Requests;
using Xunit;
using static TravelPlanner.Api.Tests.ApiScenario;

namespace TravelPlanner.Api.Tests;

public class ConcurrencyTests
{
    [PostgresFact]
    public async Task ConcurrentRoleChangesMustKeepAtLeastOneOrganizer()
    {
        var barrier = new ReadBarrier(command => command.Contains("count(*)", StringComparison.OrdinalIgnoreCase)
            && command.Contains("\"TripMembers\""), twoReaders: true);
        using var app = new ApiFactory { CommandInterceptor = barrier };
        var group = await GroupAsync(app);
        var roleUrl = $"/api/trips/{group.TripId}/members";
        await OkAsync(await group.Organizer.Client.PatchAsJsonAsync($"{roleUrl}/{group.Participant.Id}/role", "Organizer"));
        barrier.Armed = true;

        var responses = await Task.WhenAll(
            group.Organizer.Client.PatchAsJsonAsync($"{roleUrl}/{group.Participant.Id}/role", "Participant"),
            group.Participant.Client.PatchAsJsonAsync($"{roleUrl}/{group.Organizer.Id}/role", "Participant"));
        barrier.Armed = false;

        using var scope = app.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<ContextDb>();
        var organizers = await database.TripMembers.CountAsync(member => member.Trip_Id_Fk == group.TripId && member.Role == "Organizer");
        Assert.True(organizers >= 1,
            $"Both demotions returned {string.Join(", ", responses.Select(response => (int)response.StatusCode))}; remaining organizers: {organizers}.");
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
    }

    [PostgresTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ClosingVotingMustPreventAnInFlightVote(bool alreadyVoted)
    {
        var barrier = new ReadBarrier(command => command.Contains("FROM \"Proposals\""), twoReaders: false);
        using var app = new ApiFactory { CommandInterceptor = barrier };
        var group = await GroupAsync(app);
        var proposal = await JsonAsync(await group.Participant.Client.PostAsJsonAsync(
            $"/api/trips/{group.TripId}/proposals", new ProposalRequest { Title = "Audit proposal" }));
        var proposalId = proposal.GetProperty("id").GetInt32();
        var url = $"/api/trips/{group.TripId}/proposals/{proposalId}";
        if (alreadyVoted)
            await OkAsync(await group.Participant.Client.PutAsJsonAsync(url + "/vote", new { Value = -1 }));
        barrier.Armed = true;
        var voteTask = group.Participant.Client.PutAsJsonAsync(url + "/vote", new { Value = 1 });
        try
        {
            await barrier.Entered.WaitAsync(TimeSpan.FromSeconds(10));
            await OkAsync(await group.Organizer.Client.PatchAsJsonAsync(url + "/status", new { Status = "Approved" }));
        }
        finally
        {
            barrier.Release();
        }

        var voteResponse = await voteTask;
        barrier.Armed = false;
        using var scope = app.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<ContextDb>();
        var closed = (await database.Proposals.FindAsync(proposalId))!.Voting_Closed;
        var votes = await database.Votes.Where(vote => vote.Proposal_Id_Fk == proposalId).ToListAsync();
        Assert.Equal(alreadyVoted ? 1 : 0, votes.Count);
        if (alreadyVoted) Assert.Equal(-1, votes[0].Value);
        Assert.True(closed);
        Assert.Equal(HttpStatusCode.Conflict, voteResponse.StatusCode);
    }

    [PostgresFact]
    public async Task SimultaneousVotesByOneUserMustNotCreateDuplicates()
    {
        var barrier = new ReadBarrier(command => command.Contains("FROM \"Proposals\""), twoReaders: true);
        using var app = new ApiFactory { CommandInterceptor = barrier };
        var group = await GroupAsync(app);
        var proposal = await JsonAsync(await group.Participant.Client.PostAsJsonAsync(
            $"/api/trips/{group.TripId}/proposals", new ProposalRequest { Title = "Concurrent votes" }));
        var proposalId = proposal.GetProperty("id").GetInt32();
        var url = $"/api/trips/{group.TripId}/proposals/{proposalId}/vote";
        barrier.Armed = true;

        var responses = await Task.WhenAll(
            group.Participant.Client.PutAsJsonAsync(url, new { Value = 1 }),
            group.Participant.Client.PutAsJsonAsync(url, new { Value = -1 }));
        barrier.Armed = false;

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
        using var scope = app.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<ContextDb>();
        var vote = Assert.Single(await database.Votes.Where(value => value.Proposal_Id_Fk == proposalId).ToListAsync());
        Assert.Equal(group.Participant.Id, vote.User_Id_Fk);

        await OkAsync(await group.Participant.Client.PutAsJsonAsync(url, new { Value = -1 }));
        await database.Entry(vote).ReloadAsync();
        Assert.Equal(-1, vote.Value);
        Assert.Equal(1, await database.Votes.CountAsync(value => value.Proposal_Id_Fk == proposalId));
    }

    private sealed class ReadBarrier(Func<string, bool> matches, bool twoReaders) : DbCommandInterceptor
    {
        private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _arrivals;
        public bool Armed { get; set; }
        public Task Entered => _entered.Task;
        public void Release() => _release.TrySetResult();

        public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command,
            CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
        {
            if (!Armed || !matches(command.CommandText)) return result;
            var number = Interlocked.Increment(ref _arrivals);
            if (twoReaders)
            {
                if (number == 2) _release.TrySetResult();
                await _release.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            }
            else if (number == 1)
            {
                _entered.TrySetResult();
                await _release.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            }
            return result;
        }
    }
}

public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TRAVELPLANNER_TEST_POSTGRES")))
            Skip = "Для проверки одновременных запросов задайте TRAVELPLANNER_TEST_POSTGRES на отдельную тестовую PostgreSQL.";
    }
}

public sealed class PostgresTheoryAttribute : TheoryAttribute
{
    public PostgresTheoryAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TRAVELPLANNER_TEST_POSTGRES")))
            Skip = "Для проверки одновременных запросов задайте TRAVELPLANNER_TEST_POSTGRES на отдельную тестовую PostgreSQL.";
    }
}

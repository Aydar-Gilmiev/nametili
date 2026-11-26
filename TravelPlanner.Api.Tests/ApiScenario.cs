using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace TravelPlanner.Api.Tests;

public record Actor(HttpClient Client, int Id, string Email);
public record TravelGroup(Actor Organizer, Actor Participant, Actor Third, Actor Outsider, int TripId);

public static class ApiScenario
{
    public static async Task<Actor> RegisterAsync(ApiFactory app, string name)
    {
        var client = app.CreateClient();
        var email = name + "@example.com";
        var json = await JsonAsync(await client.PostAsJsonAsync("/api/auth/register",
            new { Name = name, Email = email, Password = "Test-Password-123" }));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.GetProperty("token").GetString());
        return new Actor(client, json.GetProperty("user").GetProperty("id").GetInt32(), email);
    }

    public static async Task<TravelGroup> GroupAsync(ApiFactory app)
    {
        var organizer = await RegisterAsync(app, "organizer");
        var participant = await RegisterAsync(app, "participant");
        var third = await RegisterAsync(app, "third");
        var outsider = await RegisterAsync(app, "outsider");
        var trip = await JsonAsync(await organizer.Client.PostAsJsonAsync("/api/trips", TripBody()));
        var tripId = trip.GetProperty("id").GetInt32();

        foreach (var actor in new[] { participant, third })
        {
            await OkAsync(await organizer.Client.PostAsJsonAsync($"/api/trips/{tripId}/invitations",
                new { actor.Email, Role = "Participant" }));
            var invitations = await JsonAsync(await actor.Client.GetAsync("/api/invitations/mine"));
            var invitationId = invitations[0].GetProperty("id").GetInt32();
            await OkAsync(await actor.Client.PostAsync($"/api/invitations/{invitationId}/accept", null));
        }

        return new TravelGroup(organizer, participant, third, outsider, tripId);
    }

    public static object TripBody(string name = "Казань", string start = "2026-10-20", string end = "2026-10-22")
    {
        return new { Name = name, Destination = "Казань", StartDate = start, EndDate = end };
    }

    public static async Task<JsonElement> JsonAsync(HttpResponseMessage response)
    {
        await OkAsync(response);
        var text = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(text);
        return document.RootElement.Clone();
    }

    public static async Task OkAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"HTTP {(int)response.StatusCode}: {content}; {string.Join(", ", response.Headers.WwwAuthenticate)}");
    }

    public static async Task<Dictionary<int, decimal>> BalancesAsync(TravelGroup group)
    {
        var json = await JsonAsync(await group.Organizer.Client.GetAsync($"/api/trips/{group.TripId}/balances"));
        var balances = json.GetProperty("members").EnumerateArray().ToDictionary(
            member => member.GetProperty("userId").GetInt32(),
            member => member.GetProperty("balance").GetDecimal());
        Assert.Equal(0m, balances.Values.Sum());
        return balances;
    }
}

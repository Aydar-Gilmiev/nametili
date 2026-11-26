using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using TravelPlanner.Api.CustomAttributes;
using TravelPlanner.Api.DatabaseContext;
using Xunit;
using static TravelPlanner.Api.Tests.ApiScenario;

namespace TravelPlanner.Api.Tests;

public class RoleAuthorizationTests
{
    [Fact]
    public async Task OrganizerRoleDoesNotCarryOverToOtherTrips()
    {
        using var app = new ApiFactory();
        var group = await GroupAsync(app);
        var second = await JsonAsync(await group.Participant.Client.PostAsJsonAsync("/api/trips", TripBody("Вторая поездка")));
        var secondId = second.GetProperty("id").GetInt32();
        await OkAsync(await group.Participant.Client.PostAsJsonAsync($"/api/trips/{secondId}/invitations",
            new { group.Organizer.Email, Role = "Participant" }));
        var invitations = await JsonAsync(await group.Organizer.Client.GetAsync("/api/invitations/mine"));
        await OkAsync(await group.Organizer.Client.PostAsync(
            $"/api/invitations/{invitations[0].GetProperty("id").GetInt32()}/accept", null));

        await OkAsync(await group.Organizer.Client.GetAsync($"/api/trips/{secondId}"));
        var denied = await group.Organizer.Client.PostAsJsonAsync($"/api/trips/{secondId}/schedule", ScheduleBody());
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        using var error = System.Text.Json.JsonDocument.Parse(await denied.Content.ReadAsStringAsync());
        Assert.Equal("Нет прав для этой поездки", error.RootElement.GetProperty("error").GetString());

        await JsonAsync(await group.Participant.Client.PostAsJsonAsync($"/api/trips/{secondId}/schedule", ScheduleBody()));
        await JsonAsync(await group.Organizer.Client.PostAsJsonAsync($"/api/trips/{group.TripId}/schedule", ScheduleBody()));
        Assert.Equal(HttpStatusCode.Forbidden, (await group.Participant.Client.PostAsJsonAsync(
            $"/api/trips/{group.TripId}/schedule", ScheduleBody())).StatusCode);
    }

    [Fact]
    public async Task CurrentRoleIsReadFromDatabaseWithoutReplacingToken()
    {
        using var app = new ApiFactory();
        var group = await GroupAsync(app);
        var scheduleUrl = $"/api/trips/{group.TripId}/schedule";
        var roleUrl = $"/api/trips/{group.TripId}/members/{group.Participant.Id}/role";
        Assert.Equal(HttpStatusCode.Forbidden,
            (await group.Participant.Client.PostAsJsonAsync(scheduleUrl, ScheduleBody())).StatusCode);

        await OkAsync(await group.Organizer.Client.PatchAsJsonAsync(roleUrl, "Organizer"));
        await JsonAsync(await group.Participant.Client.PostAsJsonAsync(scheduleUrl, ScheduleBody()));
        await OkAsync(await group.Organizer.Client.PatchAsJsonAsync(roleUrl, "Participant"));

        var denied = await group.Participant.Client.PostAsJsonAsync(scheduleUrl, ScheduleBody());
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        using var deniedJson = System.Text.Json.JsonDocument.Parse(await denied.Content.ReadAsStringAsync());
        Assert.Equal("Нет прав для этой поездки", deniedJson.RootElement.GetProperty("error").GetString());
        Assert.Equal(HttpStatusCode.Forbidden,
            (await group.Participant.Client.PatchAsJsonAsync(roleUrl, "Organizer")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await group.Organizer.Client.PatchAsJsonAsync(
            $"/api/trips/{group.TripId}/members/{group.Organizer.Id}/role", "Participant")).StatusCode);
    }

    [Fact]
    public async Task TokenOfDeletedUserIsRejectedBeforeAction()
    {
        using var app = new ApiFactory();
        var actor = await RegisterAsync(app, "removed");
        using (var scope = app.Services.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<ContextDb>();
            var user = await database.Users.SingleAsync(value => value.User_Id == actor.Id);
            database.Users.Remove(user);
            await database.SaveChangesAsync();
        }

        var response = await actor.Client.GetAsync("/api/trips");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using var error = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Пользователь не найден", error.RootElement.GetProperty("error").GetString());
    }

    [Theory]
    [InlineData("expired")]
    [InlineData("tampered")]
    [InlineData("missingUserClaim")]
    [InlineData("unknownUser")]
    public async Task AttributeDoesNotBypassJwtOrUserValidation(string scenario)
    {
        using var app = new ApiFactory();
        var actor = await RegisterAsync(app, "tokenowner");
        using var scope = app.Services.CreateScope();
        var key = scope.ServiceProvider.GetRequiredService<IConfiguration>()["Jwt:Key"]!;
        var claims = scenario == "missingUserClaim" ? Array.Empty<Claim>() :
            new[] { new Claim(ClaimTypes.NameIdentifier, scenario == "unknownUser" ? "999999" : actor.Id.ToString()) };
        var expires = scenario == "expired" ? DateTime.UtcNow.AddMinutes(-1) : DateTime.UtcNow.AddMinutes(5);
        var token = new JwtSecurityToken(claims: claims, expires: expires,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));
        var encoded = new JwtSecurityTokenHandler().WriteToken(token);
        if (scenario == "tampered")
        {
            var pieces = encoded.Split('.');
            pieces[2] = (pieces[2][0] == 'A' ? "B" : "A") + pieces[2][1..];
            encoded = string.Join(".", pieces);
        }

        using var client = app.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", encoded);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/trips")).StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-an-id")]
    [InlineData("0")]
    public async Task RoleRequirementWithoutValidTripCannotFallBackToLoginOnly(string? tripId)
    {
        using var app = new ApiFactory();
        var actor = await RegisterAsync(app, "filteruser");
        using var scope = app.Services.CreateScope();
        var httpContext = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, actor.Id.ToString())], "Test"));
        var route = new RouteData();
        if (tripId != null) route.Values["tripId"] = tripId;
        var actionContext = new ActionContext(httpContext, route, new ActionDescriptor(), new ModelStateDictionary());
        var filters = new List<IFilterMetadata>();
        var controller = new object();
        var context = new ActionExecutingContext(actionContext, filters, new Dictionary<string, object?>(), controller);
        var nextCalled = false;

        await new RoleAuthorizedAttribute("Organizer").OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(actionContext, filters, controller));
        });

        Assert.False(nextCalled);
        Assert.Equal(403, Assert.IsType<JsonResult>(context.Result).StatusCode);
    }

    private static object ScheduleBody()
    {
        return new { Title = "Прогулка", StartAt = "2026-10-20T10:00:00Z", EndAt = "2026-10-20T11:00:00Z" };
    }
}

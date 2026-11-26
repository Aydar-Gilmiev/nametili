using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TravelPlanner.Api.DatabaseContext;
using TravelPlanner.Api.UniversalMethod;
using Xunit;
using static TravelPlanner.Api.Tests.ApiScenario;

namespace TravelPlanner.Api.Tests;

public class EmailValidationTests
{
    [Theory]
    [InlineData("@")]
    [InlineData("user@")]
    [InlineData("@example.com")]
    [InlineData("user@@example.com")]
    [InlineData("user name@example.com")]
    [InlineData("Name <user@example.com>")]
    [InlineData("user\n@example.com")]
    public async Task RegistrationMustRejectInvalidEmail(string email)
    {
        using var app = new ApiFactory();
        using var client = app.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { Name = "Test", Email = email, Password = "Test-Password-123" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var scope = app.Services.CreateScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<ContextDb>().Users.ToListAsync());
    }

    [Fact]
    public async Task InvitationMustRejectInvalidEmail()
    {
        using var app = new ApiFactory();
        var organizer = await RegisterAsync(app, "organizer");
        var trip = await JsonAsync(await organizer.Client.PostAsJsonAsync("/api/trips", TripBody()));
        var tripId = trip.GetProperty("id").GetInt32();
        var response = await organizer.Client.PostAsJsonAsync($"/api/trips/{tripId}/invitations",
            new { Email = "@", Role = "Participant" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var scope = app.Services.CreateScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<ContextDb>().Invitations.ToListAsync());
    }

    [Fact]
    public async Task RegistrationMustNormalizeValidEmailAndAllowLogin()
    {
        using var app = new ApiFactory();
        using var client = app.CreateClient();
        var registered = await JsonAsync(await client.PostAsJsonAsync("/api/auth/register",
            new { Name = "Test", Email = "  User+trip@Example.com  ", Password = "Test-Password-123" }));
        Assert.Equal("user+trip@example.com", registered.GetProperty("user").GetProperty("email").GetString());
        await OkAsync(await client.PostAsJsonAsync("/api/auth/login",
            new { Email = "USER+trip@example.com", Password = "Test-Password-123" }));
    }

    [Fact]
    public void EmailMustNotExceedMaximumLength()
    {
        Assert.False(EmailHelper.IsValid(new string('a', 243) + "@example.com"));
    }
}

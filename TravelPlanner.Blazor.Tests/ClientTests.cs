using System.Net;
using System.Text;
using Microsoft.JSInterop;
using TravelPlanner.Blazor.Models;
using TravelPlanner.Blazor.Services;
using Xunit;
using Client = TravelPlanner.Blazor.ApiRequest.ApiRequest;

namespace TravelPlanner.Blazor.Tests;

public class ClientTests
{
    [Fact]
    public async Task SignInCanBeRestoredAndLogoutRemovesSavedData()
    {
        var storage = new BrowserStorage();
        var current = new CurrentUser(storage);
        await current.SignIn(Session());
        var restored = new CurrentUser(storage);
        await restored.Load();
        Assert.Equal(7, restored.User?.Id);
        Assert.Equal("test-token", restored.Token);
        await restored.Logout();
        Assert.Null(restored.User);
        Assert.Empty(restored.Token);
        Assert.Empty(storage.Values);
    }

    [Fact]
    public async Task CorruptedSavedUserDoesNotCrashStartup()
    {
        var storage = new BrowserStorage();
        storage.Values["travel_token"] = "test-token";
        storage.Values["travel_user"] = "broken json";
        var current = new CurrentUser(storage);
        await current.Load();
        Assert.Null(current.User);
        Assert.Empty(current.Token);
    }

    [Fact]
    public async Task GetTripsSendsBearerAndReadsApiJson()
    {
        var current = new CurrentUser(new BrowserStorage());
        await current.SignIn(Session());
        var handler = new ResponseHandler(HttpStatusCode.OK, "[{\"id\":2,\"name\":\"Казань\",\"role\":\"Organizer\"}]");
        using var http = Http(handler);
        var result = await new Client(http, current).GetTrips();
        Assert.True(result.status);
        Assert.Equal("Казань", Assert.Single(result.data!).Name);
        Assert.Equal("Bearer test-token", handler.Authorization);
        Assert.Equal("/api/trips", handler.Url);
    }

    [Fact]
    public async Task UnauthorizedResponseClearsCurrentUser()
    {
        var current = new CurrentUser(new BrowserStorage());
        await current.SignIn(Session());
        using var http = Http(new ResponseHandler(HttpStatusCode.Unauthorized, ""));
        var result = await new Client(http, current).GetTrips();
        Assert.False(result.status);
        Assert.Equal(401, result.StatusCode);
        Assert.Null(current.User);
    }

    [Fact]
    public async Task BadLoginDoesNotSendSavedToken()
    {
        var current = new CurrentUser(new BrowserStorage());
        await current.SignIn(Session());
        var handler = new ResponseHandler(HttpStatusCode.Unauthorized, "");
        using var http = Http(handler);
        var result = await new Client(http, current).Authorize(new AuthRequest());
        Assert.False(result.status);
        Assert.Null(handler.Authorization);
        Assert.Equal("Неверная почта или пароль.", result.message);
    }

    [Fact]
    public async Task ConflictMessageAndEmptySuccessBodyAreHandled()
    {
        var current = new CurrentUser(new BrowserStorage());
        using var failed = Http(new ResponseHandler(HttpStatusCode.Conflict, "\"Голосование закрыто.\""));
        var error = await new Client(failed, current).Vote(1, 2, 1);
        Assert.False(error.status);
        Assert.Equal("Голосование закрыто.", error.message);
        using var successful = Http(new ResponseHandler(HttpStatusCode.OK, ""));
        Assert.True((await new Client(successful, current).Vote(1, 2, 1)).status);
    }

    [Fact]
    public async Task ChangeRoleSendsJsonStringNotObject()
    {
        var handler = new ResponseHandler(HttpStatusCode.OK, "");
        using var http = Http(handler);
        var result = await new Client(http, new CurrentUser(new BrowserStorage())).ChangeRole(1, 7, "Participant");
        Assert.True(result.status);
        Assert.Equal("PATCH", handler.Method);
        Assert.Equal("/api/trips/1/members/7/role", handler.Url);
        Assert.Equal("\"Participant\"", handler.Body);
    }

    [Fact]
    public async Task InvalidResponseJsonBecomesReadableError()
    {
        using var http = Http(new ResponseHandler(HttpStatusCode.OK, "not json"));
        var result = await new Client(http, new CurrentUser(new BrowserStorage())).GetTrips();
        Assert.False(result.status);
        Assert.Equal("Не удалось прочитать ответ API.", result.message);
    }

    [Fact]
    public async Task ValidationProblemIsShownWithoutRawJson()
    {
        using var http = Http(new ResponseHandler(HttpStatusCode.BadRequest, "{\"errors\":{\"Email\":[\"Проверьте почту.\"]}}"));
        var result = await new Client(http, new CurrentUser(new BrowserStorage())).Register(new RegRequest());
        Assert.False(result.status);
        Assert.Equal("Проверьте почту.", result.message);
    }

    [Fact]
    public async Task ScheduleSendsUtcTimesAndConflictDoesNotBecomeSuccess()
    {
        var handler = new ResponseHandler(HttpStatusCode.Conflict, "\"Время уже занято.\"");
        using var http = Http(handler);
        var result = await new Client(http, new CurrentUser(new BrowserStorage())).UpdateScheduleItem(3, 4,
            new ScheduleRequest { Title = "Музей", StartAt = new DateTimeOffset(2026, 11, 1, 10, 0, 0, TimeSpan.Zero), EndAt = new DateTimeOffset(2026, 11, 1, 11, 0, 0, TimeSpan.Zero) });
        Assert.Equal("PUT", handler.Method);
        Assert.Equal("/api/trips/3/schedule/4", handler.Url);
        Assert.Contains("2026-11-01T10:00:00", handler.Body);
        Assert.False(result.status);
        Assert.Equal("Время уже занято.", result.message);
    }

    [Fact]
    public async Task ExpenseAndRepaymentMethodsUseExistingApiContracts()
    {
        var handler = new ResponseHandler(HttpStatusCode.OK, "");
        using var http = Http(handler);
        var client = new Client(http, new CurrentUser(new BrowserStorage()));
        Assert.True((await client.AddExpense(2, new ExpenseRequest { ProposalId = 15, SplitMode = "Custom", Shares = new() { new ExpenseShare { UserId = 7, Amount = 10.01m } } })).status);
        Assert.Equal("/api/trips/2/expenses", handler.Url);
        Assert.Contains("10.01", handler.Body);
        Assert.Contains("\"proposalId\":15", handler.Body);
        Assert.True((await client.UpdateRepayment(2, 6, new Repayment { FromUserId = 7, ToUserId = 8, Amount = 10.01m })).status);
        Assert.Equal("/api/trips/2/repayments/6", handler.Url);
        Assert.Equal("PUT", handler.Method);
        Assert.True((await client.DeleteExpense(2, 9)).status);
        Assert.Equal("DELETE", handler.Method);
    }

    [Fact]
    public async Task ExportUsesAuthenticatedRequestAndPreservesFileBytes()
    {
        var current = new CurrentUser(new BrowserStorage());
        await current.SignIn(Session());
        var handler = new ResponseHandler(HttpStatusCode.OK, "Русский маршрут");
        using var http = Http(handler);
        var result = await new Client(http, current).ExportTrip(9);
        Assert.True(result.status);
        Assert.Equal(Encoding.UTF8.GetBytes("Русский маршрут"), result.data);
        Assert.Equal("Bearer test-token", handler.Authorization);
        Assert.Equal("/api/trips/9/export/pdf", handler.Url);
    }

    [Fact]
    public async Task ExpiredExportClearsSessionAndDoesNotDownloadError()
    {
        var current = new CurrentUser(new BrowserStorage());
        await current.SignIn(Session());
        using var http = Http(new ResponseHandler(HttpStatusCode.Unauthorized, ""));
        var result = await new Client(http, current).ExportTrip(9);
        Assert.False(result.status);
        Assert.Null(result.data);
        Assert.Null(current.User);
    }

    private static AuthData Session() => new() { Token = "test-token", User = new User { Id = 7, Name = "Test", Email = "test@example.com" } };
    private static HttpClient Http(ResponseHandler handler) => new(handler) { BaseAddress = new Uri("http://localhost/") };

    private sealed class ResponseHandler(HttpStatusCode status, string content) : HttpMessageHandler
    {
        public string? Authorization { get; private set; }
        public string? Url { get; private set; }
        public string? Method { get; private set; }
        public string? Body { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Authorization = request.Headers.Authorization?.ToString();
            Url = request.RequestUri?.AbsolutePath;
            Method = request.Method.Method;
            Body = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status) { Content = new StringContent(content, Encoding.UTF8, "application/json") };
        }
    }

    private sealed class BrowserStorage : IJSRuntime
    {
        public Dictionary<string, string> Values { get; } = new();
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => InvokeAsync<TValue>(identifier, CancellationToken.None, args);
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            var key = (string)args![0]!;
            if (identifier == "localStorage.getItem") return ValueTask.FromResult((TValue)(object?)Values.GetValueOrDefault(key)!);
            if (identifier == "localStorage.setItem") Values[key] = (string)args[1]!;
            if (identifier == "localStorage.removeItem") Values.Remove(key);
            return ValueTask.FromResult(default(TValue)!);
        }
    }
}

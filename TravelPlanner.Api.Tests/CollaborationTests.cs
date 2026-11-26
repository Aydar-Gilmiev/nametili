using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TravelPlanner.Api.DatabaseContext;
using TravelPlanner.Api.Models.Entities;
using TravelPlanner.Api.Responses;
using Xunit;
using static TravelPlanner.Api.Tests.ApiScenario;

namespace TravelPlanner.Api.Tests;

public class CollaborationTests
{
    private static byte[] Png() => Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aZ1cAAAAASUVORK5CYII=");

    [Theory]
    [InlineData("messages")]
    [InlineData("places")]
    [InlineData("photos")]
    public async Task NewResourcesRequireTripMembership(string resource)
    {
        using var app = new ApiFactory();
        var group = await GroupAsync(app);
        var path = $"/api/trips/{group.TripId}/{resource}";
        using var anonymous = app.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await group.Outsider.Client.GetAsync(path)).StatusCode);
        await OkAsync(await group.Participant.Client.GetAsync(path));
    }

    [Fact]
    public async Task ChatDeliversToOtherParticipantAndKeepsHistory()
    {
        using var app = new ApiFactory();
        var group = await GroupAsync(app);
        await using var first = Connection(app, group.Organizer, group.TripId);
        await using var second = Connection(app, group.Participant, group.TripId);
        var received = new TaskCompletionSource<ChatMessageResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        second.On<ChatMessageResponse>("MessageReceived", message => { if (message.Text == "Встречаемся у вокзала") received.TrySetResult(message); });
        await first.StartAsync(); await second.StartAsync();
        await second.InvokeAsync<ChatMessageResponse>("SendMessage", "Готов обсуждать");
        var sent = await first.InvokeAsync<ChatMessageResponse>("SendMessage", "Встречаемся у вокзала");
        var message = await received.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(sent.Id, message.Id);
        Assert.Equal(group.Organizer.Id, message.UserId);
        var history = await JsonAsync(await group.Participant.Client.GetAsync($"/api/trips/{group.TripId}/messages"));
        Assert.Equal(2, history.GetArrayLength());
        Assert.Equal(message.Text, history[1].GetProperty("text").GetString());
        await Assert.ThrowsAsync<HubException>(() => first.InvokeAsync<ChatMessageResponse>("SendMessage", " "));
        await Assert.ThrowsAsync<HubException>(() => first.InvokeAsync<ChatMessageResponse>("SendMessage", new string('x', 2001)));
    }

    [Fact]
    public async Task HubRejectsAnonymousAndOutsiderAndRechecksMembership()
    {
        using var app = new ApiFactory();
        var group = await GroupAsync(app);
        await using var anonymous = Connection(app, null, group.TripId);
        await Assert.ThrowsAnyAsync<Exception>(() => anonymous.StartAsync());
        await using var outsider = Connection(app, group.Outsider, group.TripId);
        await Assert.ThrowsAnyAsync<Exception>(async () => { await outsider.StartAsync(); await outsider.InvokeAsync<ChatMessageResponse>("SendMessage", "Нет доступа"); });
        await using var member = Connection(app, group.Participant, group.TripId);
        await member.StartAsync();
        await member.InvokeAsync<ChatMessageResponse>("SendMessage", "До выхода");
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ContextDb>();
            db.TripMembers.Remove(await db.TripMembers.SingleAsync(value => value.Trip_Id_Fk == group.TripId && value.User_Id_Fk == group.Participant.Id));
            await db.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<HubException>(() => member.InvokeAsync<ChatMessageResponse>("SendMessage", "После выхода"));
        Assert.Single((await JsonAsync(await group.Organizer.Client.GetAsync($"/api/trips/{group.TripId}/messages"))).EnumerateArray());
    }

    [Fact]
    public async Task DifferentTripsDoNotShareChatGroups()
    {
        using var app = new ApiFactory();
        var group = await GroupAsync(app);
        var trip = await JsonAsync(await group.Outsider.Client.PostAsJsonAsync("/api/trips", TripBody("Другая поездка")));
        var otherId = trip.GetProperty("id").GetInt32();
        await using var own = Connection(app, group.Organizer, group.TripId);
        await using var other = Connection(app, group.Outsider, otherId);
        var received = new List<string>();
        var ownReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        other.On<ChatMessageResponse>("MessageReceived", value => { lock (received) received.Add(value.Text); if (value.Text == "Другая группа") ownReceived.TrySetResult(); });
        await own.StartAsync(); await other.StartAsync();
        await other.InvokeAsync<ChatMessageResponse>("SendMessage", "Другая группа");
        await ownReceived.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await own.InvokeAsync<ChatMessageResponse>("SendMessage", "Секрет нашей поездки");
        await other.InvokeAsync<ChatMessageResponse>("SendMessage", "Проверка завершена");
        // В каждой группе порядок доставки одного отправителя сохраняется; чужая запись не появляется и в истории.
        var history = await JsonAsync(await group.Outsider.Client.GetAsync($"/api/trips/{otherId}/messages"));
        Assert.Equal(2, history.GetArrayLength());
        lock (received) Assert.DoesNotContain("Секрет нашей поездки", received);
    }

    [Fact]
    public async Task HistoryIsPagedByIdWithoutDuplicates()
    {
        using var app = new ApiFactory();
        var group = await GroupAsync(app);
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ContextDb>();
            db.ChatMessages.AddRange(Enumerable.Range(1, 55).Select(id => new ChatMessage { Trip_Id_Fk = group.TripId, User_Id_Fk = group.Organizer.Id, Text = id.ToString(), Created_At = DateTime.UtcNow }));
            await db.SaveChangesAsync();
        }
        var recent = await JsonAsync(await group.Participant.Client.GetAsync($"/api/trips/{group.TripId}/messages"));
        Assert.Equal(50, recent.GetArrayLength());
        var older = await JsonAsync(await group.Participant.Client.GetAsync($"/api/trips/{group.TripId}/messages?beforeId={recent[0].GetProperty("id")}"));
        Assert.Equal(5, older.GetArrayLength());
        Assert.Equal(55, recent.EnumerateArray().Concat(older.EnumerateArray()).Select(value => value.GetProperty("id").GetInt32()).Distinct().Count());
    }

    [Fact]
    public async Task CommentsCheckAuthorRoleAndProposalTrip()
    {
        using var app = new ApiFactory();
        var group = await GroupAsync(app);
        var proposal = await JsonAsync(await group.Organizer.Client.PostAsJsonAsync($"/api/trips/{group.TripId}/proposals", new { Title = "Музей", PlannedCost = 0 }));
        var proposalId = proposal.GetProperty("id").GetInt32();
        var path = $"/api/trips/{group.TripId}/proposals/{proposalId}/comments";
        var comment = await JsonAsync(await group.Participant.Client.PostAsJsonAsync(path, new { Text = "Пойдём утром" }));
        var id = comment.GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.Forbidden, (await group.Outsider.Client.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await group.Third.Client.DeleteAsync($"{path}/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await group.Organizer.Client.PutAsJsonAsync($"{path}/{id}", new { Text = "Чужой текст" })).StatusCode);
        await OkAsync(await group.Participant.Client.PutAsJsonAsync($"{path}/{id}", new { Text = "Лучше после обеда" }));
        Assert.Equal("Лучше после обеда", (await JsonAsync(await group.Third.Client.GetAsync(path)))[0].GetProperty("text").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await group.Participant.Client.PostAsJsonAsync(path, new { Text = "  " })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await group.Participant.Client.PostAsJsonAsync(path, new { Text = new string('x', 2001) })).StatusCode);
        var otherTrip = await JsonAsync(await group.Organizer.Client.PostAsJsonAsync("/api/trips", TripBody("Ещё поездка")));
        var wrong = $"/api/trips/{otherTrip.GetProperty("id")}/proposals/{proposalId}/comments";
        Assert.Equal(HttpStatusCode.NotFound, (await group.Organizer.Client.GetAsync(wrong)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await group.Organizer.Client.DeleteAsync($"{wrong}/{id}")).StatusCode);
        await OkAsync(await group.Organizer.Client.DeleteAsync($"{path}/{id}"));
    }

    [Fact]
    public async Task PlacesValidateCoordinatesAndProtectOtherAuthors()
    {
        using var app = new ApiFactory();
        var group = await GroupAsync(app);
        var path = $"/api/trips/{group.TripId}/places";
        var created = await JsonAsync(await group.Participant.Client.PostAsJsonAsync(path, new { Title = "Кремль", Note = "Встречаемся здесь", Latitude = 55.798, Longitude = 49.106 }));
        var id = created.GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.BadRequest, (await group.Participant.Client.PostAsJsonAsync(path, new { Title = "Ошибка", Latitude = 91, Longitude = 49 })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await group.Third.Client.DeleteAsync($"{path}/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await group.Third.Client.PutAsJsonAsync($"{path}/{id}", new { Title = "Нельзя", Latitude = 0, Longitude = 0 })).StatusCode);
        await OkAsync(await group.Organizer.Client.PutAsJsonAsync($"{path}/{id}", new { Title = "Вход в Кремль", Latitude = 55.798, Longitude = 49.106 }));
        Assert.Equal("Вход в Кремль", (await JsonAsync(await group.Participant.Client.GetAsync(path)))[0].GetProperty("title").GetString());
        await OkAsync(await group.Participant.Client.DeleteAsync($"{path}/{id}"));
    }

    [Fact]
    public async Task PhotosArePrivateValidateContentAndCanBeDeletedByOrganizer()
    {
        using var app = new ApiFactory();
        var group = await GroupAsync(app);
        var path = $"/api/trips/{group.TripId}/photos";
        using var upload = PhotoForm(Png(), "Фотография");
        var photo = await JsonAsync(await group.Participant.Client.PostAsync(path, upload));
        var id = photo.GetProperty("id").GetInt32();
        var contentPath = $"{path}/{id}/content";
        Assert.Equal(HttpStatusCode.Forbidden, (await group.Outsider.Client.GetAsync(contentPath)).StatusCode);
        using var anonymous = app.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(contentPath)).StatusCode);
        using var content = await group.Third.Client.GetAsync(contentPath);
        await OkAsync(content);
        Assert.Equal("image/png", content.Content.Headers.ContentType?.MediaType);
        Assert.Equal(Png(), await content.Content.ReadAsByteArrayAsync());
        Assert.True(content.Headers.CacheControl?.NoStore);
        Assert.False((await JsonAsync(await group.Third.Client.GetAsync(path)))[0].TryGetProperty("image_Data", out _));
        Assert.Equal(HttpStatusCode.Forbidden, (await group.Third.Client.DeleteAsync($"{path}/{id}")).StatusCode);
        using var fake = PhotoForm(Encoding.UTF8.GetBytes("<svg><script>alert(1)</script></svg>"), "Поддельное фото");
        Assert.Equal(HttpStatusCode.BadRequest, (await group.Participant.Client.PostAsync(path, fake)).StatusCode);
        using var tooLongCaption = PhotoForm(Png(), new string('x', 301));
        Assert.Equal(HttpStatusCode.BadRequest, (await group.Participant.Client.PostAsync(path, tooLongCaption)).StatusCode);
        await OkAsync(await group.Organizer.Client.DeleteAsync($"{path}/{id}"));
        Assert.Equal(HttpStatusCode.NotFound, (await group.Participant.Client.GetAsync(contentPath)).StatusCode);
    }

    [Fact]
    public async Task DeletingTripCascadesAllNewData()
    {
        using var app = new ApiFactory();
        var group = await GroupAsync(app);
        var path = $"/api/trips/{group.TripId}";
        await using var chat = Connection(app, group.Organizer, group.TripId);
        await chat.StartAsync(); await chat.InvokeAsync<ChatMessageResponse>("SendMessage", "До удаления");
        var proposal = await JsonAsync(await group.Organizer.Client.PostAsJsonAsync(path + "/proposals", new { Title = "Место", PlannedCost = 0 }));
        await OkAsync(await group.Participant.Client.PostAsJsonAsync($"{path}/proposals/{proposal.GetProperty("id")}/comments", new { Text = "Комментарий" }));
        await OkAsync(await group.Participant.Client.PostAsJsonAsync(path + "/places", new { Title = "Место", Latitude = 55, Longitude = 49 }));
        using var form = PhotoForm(Png(), "Фото");
        await OkAsync(await group.Participant.Client.PostAsync(path + "/photos", form));
        await OkAsync(await group.Organizer.Client.DeleteAsync(path));
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ContextDb>();
        Assert.False(await db.ChatMessages.AnyAsync()); Assert.False(await db.ProposalComments.AnyAsync());
        Assert.False(await db.TripPlaces.AnyAsync()); Assert.False(await db.TripPhotos.AnyAsync());
        Assert.Equal(4, await db.Users.CountAsync());
        await Assert.ThrowsAsync<HubException>(() => chat.InvokeAsync<ChatMessageResponse>("SendMessage", "После удаления"));
    }

    private static HubConnection Connection(ApiFactory app, Actor? actor, int tripId) => new HubConnectionBuilder()
        .WithUrl($"http://localhost/hubs/trip-chat?tripId={tripId}", options =>
        {
            options.Transports = HttpTransportType.LongPolling;
            options.HttpMessageHandlerFactory = _ => app.Server.CreateHandler();
            options.AccessTokenProvider = () => Task.FromResult(actor?.Client.DefaultRequestHeaders.Authorization?.Parameter);
        }).Build();

    [Fact]
    public async Task PhotoLimitsAndWrongTripCannotBeBypassed()
    {
        using var app = new ApiFactory();
        var group = await GroupAsync(app);
        var path = $"/api/trips/{group.TripId}/photos";
        using var oversized = PhotoForm(new byte[5 * 1024 * 1024 + 1], "Большой файл");
        var oversizedResult = await group.Participant.Client.PostAsync(path, oversized);
        Assert.True(oversizedResult.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.RequestEntityTooLarge);
        using var valid = PhotoForm(Png(), "Фото");
        var photo = await JsonAsync(await group.Participant.Client.PostAsync(path, valid));
        var trip = await JsonAsync(await group.Organizer.Client.PostAsJsonAsync("/api/trips", TripBody("Другая")));
        var wrong = $"/api/trips/{trip.GetProperty("id")}/photos/{photo.GetProperty("id")}";
        Assert.Equal(HttpStatusCode.NotFound, (await group.Organizer.Client.GetAsync(wrong + "/content")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await group.Organizer.Client.DeleteAsync(wrong)).StatusCode);
        var hugeDimensions = Png();
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(hugeDimensions.AsSpan(16, 4), 9000);
        using var invalid = PhotoForm(hugeDimensions, "Неверные размеры");
        Assert.Equal(HttpStatusCode.BadRequest, (await group.Participant.Client.PostAsync(path, invalid)).StatusCode);
    }

    [Fact]
    public async Task SwaggerCanDescribeNewEndpoints()
    {
        using var app = new ApiFactory();
        using var client = app.CreateClient();
        var schema = await JsonAsync(await client.GetAsync("/swagger/v1/swagger.json"));
        Assert.True(schema.GetProperty("paths").TryGetProperty("/api/trips/{tripId}/photos", out _));
        Assert.True(schema.GetProperty("paths").TryGetProperty("/api/trips/{tripId}/places", out _));
    }

    private static MultipartFormDataContent PhotoForm(byte[] data, string caption)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(data);
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        form.Add(file, "file", "untrusted-name.jpg");
        form.Add(new StringContent(caption), "caption");
        return form;
    }
}

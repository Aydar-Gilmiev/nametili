using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TravelPlanner.Blazor.Models;
using TravelPlanner.Blazor.Services;

namespace TravelPlanner.Blazor.ApiRequest;

public class ApiRequest
{
    private readonly HttpClient _httpClient;
    private readonly CurrentUser _currentUser;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ApiRequest(HttpClient httpClient, CurrentUser currentUser)
    {
        _httpClient = httpClient;
        _currentUser = currentUser;
    }

    public Task<ApiResult<AuthData>> Authorize(AuthRequest request) =>
        Send<AuthData>(HttpMethod.Post, "api/auth/login", request, false);

    public Task<ApiResult<AuthData>> Register(RegRequest request) =>
        Send<AuthData>(HttpMethod.Post, "api/auth/register", request, false);

    public Task<ApiResult<List<Trip>>> GetTrips() => Send<List<Trip>>(HttpMethod.Get, "api/trips");

    public Task<ApiResult<TripDetailsData>> GetTrip(int tripId) =>
        Send<TripDetailsData>(HttpMethod.Get, $"api/trips/{tripId}");

    public Task<ApiResult<Trip>> AddTrip(TripRequest request) =>
        Send<Trip>(HttpMethod.Post, "api/trips", request);

    public Task<ApiStatus> UpdateTrip(int tripId, TripRequest request) =>
        SendStatus(HttpMethod.Put, $"api/trips/{tripId}", request);

    public Task<ApiStatus> DeleteTrip(int tripId) => SendStatus(HttpMethod.Delete, $"api/trips/{tripId}");

    public Task<ApiResult<List<Invitation>>> GetInvitations() =>
        Send<List<Invitation>>(HttpMethod.Get, "api/invitations/mine");

    public Task<ApiStatus> AcceptInvitation(int invitationId) =>
        SendStatus(HttpMethod.Post, $"api/invitations/{invitationId}/accept");

    public Task<ApiStatus> Invite(int tripId, InvitationRequest request) =>
        SendStatus(HttpMethod.Post, $"api/trips/{tripId}/invitations", request);

    public Task<ApiStatus> ChangeRole(int tripId, int userId, string role) =>
        SendStatus(HttpMethod.Patch, $"api/trips/{tripId}/members/{userId}/role", role);

    public Task<ApiResult<List<Proposal>>> GetProposals(int tripId) =>
        Send<List<Proposal>>(HttpMethod.Get, $"api/trips/{tripId}/proposals");

    public Task<ApiStatus> AddProposal(int tripId, ProposalRequest request) =>
        SendStatus(HttpMethod.Post, $"api/trips/{tripId}/proposals", request);

    public Task<ApiStatus> UpdateProposal(int tripId, int proposalId, ProposalRequest request) =>
        SendStatus(HttpMethod.Put, $"api/trips/{tripId}/proposals/{proposalId}", request);

    public Task<ApiStatus> DeleteProposal(int tripId, int proposalId) =>
        SendStatus(HttpMethod.Delete, $"api/trips/{tripId}/proposals/{proposalId}");

    public Task<ApiStatus> Vote(int tripId, int proposalId, int value) =>
        SendStatus(HttpMethod.Put, $"api/trips/{tripId}/proposals/{proposalId}/vote", new { Value = value });

    public Task<ApiStatus> SetProposalStatus(int tripId, int proposalId, string status) =>
        SendStatus(HttpMethod.Patch, $"api/trips/{tripId}/proposals/{proposalId}/status", new { Status = status });

    public Task<ApiResult<Budget>> GetBudget(int tripId) =>
        Send<Budget>(HttpMethod.Get, $"api/trips/{tripId}/proposals/budget");

    public Task<ApiResult<List<ScheduleItem>>> GetSchedule(int tripId) => Send<List<ScheduleItem>>(HttpMethod.Get, $"api/trips/{tripId}/schedule");
    public Task<ApiStatus> AddScheduleItem(int tripId, ScheduleRequest request) => SendStatus(HttpMethod.Post, $"api/trips/{tripId}/schedule", request);
    public Task<ApiStatus> UpdateScheduleItem(int tripId, int id, ScheduleRequest request) => SendStatus(HttpMethod.Put, $"api/trips/{tripId}/schedule/{id}", request);
    public Task<ApiStatus> DeleteScheduleItem(int tripId, int id) => SendStatus(HttpMethod.Delete, $"api/trips/{tripId}/schedule/{id}");
    public Task<ApiResult<List<Expense>>> GetExpenses(int tripId) => Send<List<Expense>>(HttpMethod.Get, $"api/trips/{tripId}/expenses");
    public Task<ApiStatus> AddExpense(int tripId, ExpenseRequest request) => SendStatus(HttpMethod.Post, $"api/trips/{tripId}/expenses", request);
    public Task<ApiStatus> UpdateExpense(int tripId, int id, ExpenseRequest request) => SendStatus(HttpMethod.Put, $"api/trips/{tripId}/expenses/{id}", request);
    public Task<ApiStatus> DeleteExpense(int tripId, int id) => SendStatus(HttpMethod.Delete, $"api/trips/{tripId}/expenses/{id}");
    public Task<ApiResult<Balances>> GetBalances(int tripId) => Send<Balances>(HttpMethod.Get, $"api/trips/{tripId}/balances");
    public Task<ApiResult<List<Repayment>>> GetRepayments(int tripId) => Send<List<Repayment>>(HttpMethod.Get, $"api/trips/{tripId}/repayments");
    public Task<ApiStatus> AddRepayment(int tripId, Repayment request) => SendStatus(HttpMethod.Post, $"api/trips/{tripId}/repayments", request);
    public Task<ApiStatus> UpdateRepayment(int tripId, int id, Repayment request) => SendStatus(HttpMethod.Put, $"api/trips/{tripId}/repayments/{id}", request);
    public Task<ApiStatus> DeleteRepayment(int tripId, int id) => SendStatus(HttpMethod.Delete, $"api/trips/{tripId}/repayments/{id}");

    public async Task<ApiResult<byte[]>> ExportTrip(int tripId)
    {
        try
        {
            using var response = await SendRequest(HttpMethod.Get, $"api/trips/{tripId}/export/pdf", null, true);
            if (!response.IsSuccessStatusCode) return new ApiResult<byte[]> { message = await ErrorMessage(response, true) };
            return new ApiResult<byte[]> { status = true, data = await response.Content.ReadAsByteArrayAsync() };
        }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException)
        { return new ApiResult<byte[]> { message = "Не удалось скачать файл. Проверьте подключение и повторите попытку." }; }
    }

    public Task<ApiResult<List<ChatMessage>>> GetMessages(int tripId, int? beforeId = null) =>
        Send<List<ChatMessage>>(HttpMethod.Get, $"api/trips/{tripId}/messages?beforeId={beforeId}");

    public Task<ApiResult<List<ChatMessage>>> GetComments(int tripId, int proposalId, int? beforeId = null) =>
        Send<List<ChatMessage>>(HttpMethod.Get, $"api/trips/{tripId}/proposals/{proposalId}/comments?beforeId={beforeId}");
    public Task<ApiStatus> AddComment(int tripId, int proposalId, string text) =>
        SendStatus(HttpMethod.Post, $"api/trips/{tripId}/proposals/{proposalId}/comments", new { Text = text });
    public Task<ApiStatus> UpdateComment(int tripId, int proposalId, int id, string text) =>
        SendStatus(HttpMethod.Put, $"api/trips/{tripId}/proposals/{proposalId}/comments/{id}", new { Text = text });
    public Task<ApiStatus> DeleteComment(int tripId, int proposalId, int id) =>
        SendStatus(HttpMethod.Delete, $"api/trips/{tripId}/proposals/{proposalId}/comments/{id}");

    public Task<ApiResult<List<TripPlace>>> GetPlaces(int tripId) => Send<List<TripPlace>>(HttpMethod.Get, $"api/trips/{tripId}/places");
    public Task<ApiStatus> AddPlace(int tripId, TripPlace place) => SendStatus(HttpMethod.Post, $"api/trips/{tripId}/places", place);
    public Task<ApiStatus> UpdatePlace(int tripId, TripPlace place) => SendStatus(HttpMethod.Put, $"api/trips/{tripId}/places/{place.Id}", place);
    public Task<ApiStatus> DeletePlace(int tripId, int id) => SendStatus(HttpMethod.Delete, $"api/trips/{tripId}/places/{id}");
    public Task<ApiResult<List<TripPhoto>>> GetPhotos(int tripId, int? beforeId = null) =>
        Send<List<TripPhoto>>(HttpMethod.Get, $"api/trips/{tripId}/photos?beforeId={beforeId}");
    public Task<ApiStatus> DeletePhoto(int tripId, int id) => SendStatus(HttpMethod.Delete, $"api/trips/{tripId}/photos/{id}");

    public async Task<ApiStatus> UploadPhoto(int tripId, Microsoft.AspNetCore.Components.Forms.IBrowserFile file, string caption)
    {
        if (file.Size > 5 * 1024 * 1024) return new ApiStatus { message = "Фотография должна быть не больше 5 МБ." };
        try
        {
            using var form = new MultipartFormDataContent();
            using var stream = file.OpenReadStream(5 * 1024 * 1024);
            using var content = new StreamContent(stream);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            form.Add(content, "file", "photo");
            form.Add(new StringContent(caption), "caption");
            using var request = new HttpRequestMessage(HttpMethod.Post, $"api/trips/{tripId}/photos") { Content = form };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _currentUser.Token);
            using var response = await _httpClient.SendAsync(request);
            return new ApiStatus { status = response.IsSuccessStatusCode, message = response.IsSuccessStatusCode ? "" : await ErrorMessage(response, true) };
        }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException or IOException)
        { return new ApiStatus { message = "Не удалось загрузить фотографию. Проверьте подключение и размер файла." }; }
    }

    public async Task<ApiResult<byte[]>> GetPhotoContent(int tripId, int id)
    {
        try
        {
            using var response = await SendRequest(HttpMethod.Get, $"api/trips/{tripId}/photos/{id}/content", null, true);
            if (!response.IsSuccessStatusCode) return new ApiResult<byte[]> { message = await ErrorMessage(response, true) };
            return new ApiResult<byte[]> { status = true, data = await response.Content.ReadAsByteArrayAsync(), message = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg" };
        }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException)
        { return new ApiResult<byte[]> { message = "Не удалось открыть фотографию." }; }
    }

    private async Task<HttpResponseMessage> SendRequest(HttpMethod method, string url, object? data, bool authorized)
    {
        using var request = new HttpRequestMessage(method, url);
        if (authorized && !string.IsNullOrWhiteSpace(_currentUser.Token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _currentUser.Token);
        if (data != null) request.Content = JsonContent.Create(data);
        return await _httpClient.SendAsync(request);
    }

    private async Task<ApiResult<T>> Send<T>(HttpMethod method, string url, object? data = null, bool authorized = true)
    {
        try
        {
            using var response = await SendRequest(method, url, data, authorized);
            if (!response.IsSuccessStatusCode)
                return new ApiResult<T> { message = await ErrorMessage(response, authorized), StatusCode = (int)response.StatusCode };

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<T>(content, _jsonOptions);
            if (result == null) return new ApiResult<T> { message = "API вернул пустой ответ." };
            return new ApiResult<T> { status = true, data = result, StatusCode = (int)response.StatusCode };
        }
        catch (HttpRequestException) { return new ApiResult<T> { message = "Не удалось подключиться к API. Проверьте, что сервер запущен." }; }
        catch (TaskCanceledException) { return new ApiResult<T> { message = "Сервер долго не отвечает. Повторите попытку." }; }
        catch (JsonException) { return new ApiResult<T> { message = "Не удалось прочитать ответ API." }; }
    }

    private async Task<ApiStatus> SendStatus(HttpMethod method, string url, object? data = null)
    {
        try
        {
            using var response = await SendRequest(method, url, data, true);
            return new ApiStatus
            {
                status = response.IsSuccessStatusCode,
                StatusCode = (int)response.StatusCode,
                message = response.IsSuccessStatusCode ? "" : await ErrorMessage(response, true)
            };
        }
        catch (HttpRequestException) { return new ApiStatus { message = "Не удалось подключиться к API. Проверьте, что сервер запущен." }; }
        catch (TaskCanceledException) { return new ApiStatus { message = "Сервер долго не отвечает. Повторите попытку." }; }
    }

    private async Task<string> ErrorMessage(HttpResponseMessage response, bool authorized)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            if (authorized) await _currentUser.Logout();
            return authorized ? "Сессия закончилась. Войдите снова." : "Неверная почта или пароль.";
        }
        if (response.StatusCode == HttpStatusCode.Forbidden) return "У вас нет прав на это действие.";
        if (response.StatusCode == HttpStatusCode.NotFound) return "Запись не найдена или больше не доступна.";
        if ((int)response.StatusCode >= 500) return "Ошибка на сервере. Повторите попытку позже.";

        var text = await response.Content.ReadAsStringAsync();
        if (!string.IsNullOrWhiteSpace(text))
        {
            try
            {
                using var json = JsonDocument.Parse(text);
                if (json.RootElement.ValueKind == JsonValueKind.String) return json.RootElement.GetString() ?? "Ошибка запроса.";
                if (json.RootElement.ValueKind == JsonValueKind.Object && json.RootElement.TryGetProperty("errors", out var errors))
                    return string.Join(" ", errors.EnumerateObject().SelectMany(field => field.Value.EnumerateArray()).Select(value => value.GetString()));
            }
            catch (JsonException) { if (!text.TrimStart().StartsWith('<')) return text; }
        }
        return response.StatusCode == HttpStatusCode.Conflict
            ? "Данные изменились или возник конфликт. Обновите страницу и повторите действие."
            : "Проверьте заполненные поля.";
    }
}

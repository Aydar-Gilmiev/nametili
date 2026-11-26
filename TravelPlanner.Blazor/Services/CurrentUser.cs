using System.Text.Json;
using Microsoft.JSInterop;
using TravelPlanner.Blazor.Models;

namespace TravelPlanner.Blazor.Services;

public class CurrentUser
{
    private readonly IJSRuntime _js;
    public User? User { get; private set; }
    public string Token { get; private set; } = "";
    public event Action? Changed;

    public CurrentUser(IJSRuntime js)
    {
        _js = js;
    }

    public async Task Load()
    {
        try
        {
            var token = await _js.InvokeAsync<string?>("localStorage.getItem", "travel_token");
            var json = await _js.InvokeAsync<string?>("localStorage.getItem", "travel_user");
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(json)) return;
            var user = JsonSerializer.Deserialize<User>(json);
            if (user == null || user.Id <= 0) return;
            User = user;
            Token = token;
        }
        catch (JSException) { }
        catch (JsonException) { }
    }

    public async Task SignIn(AuthData data)
    {
        User = data.User;
        Token = data.Token;
        try
        {
            await _js.InvokeVoidAsync("localStorage.setItem", "travel_token", Token);
            await _js.InvokeVoidAsync("localStorage.setItem", "travel_user", JsonSerializer.Serialize(User));
        }
        catch (JSException) { }
        Changed?.Invoke();
    }

    public async Task Logout()
    {
        User = null;
        Token = "";
        try
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", "travel_token");
            await _js.InvokeVoidAsync("localStorage.removeItem", "travel_user");
        }
        catch (JSException) { }
        Changed?.Invoke();
    }
}

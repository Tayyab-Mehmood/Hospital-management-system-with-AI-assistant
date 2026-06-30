using Microsoft.JSInterop;

namespace LifePulse.Blazor;

public class CustomAuthStateProvider
{
    private readonly IJSRuntime _js;

    public event Action? OnCurrentUserStateChanged;

    public bool IsAuthenticated { get; private set; } = false;
    public string Username { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;
    public int UserId { get; private set; } = 0;
    public bool SessionRestored { get; private set; } = false;

    public CustomAuthStateProvider(IJSRuntime js)
    {
        _js = js;
    }

    public void MarkUserAsAuthenticated(string username, string fullName, string role, int userId)
    {
        IsAuthenticated = true;
        Username = username;
        FullName = fullName;
        Role = role;
        UserId = userId;
        NotifyStateChanged();
        _ = SaveSessionAsync();
    }

    public void MarkUserAsLoggedOut()
    {
        IsAuthenticated = false;
        Username = string.Empty;
        FullName = string.Empty;
        Role = string.Empty;
        UserId = 0;
        NotifyStateChanged();
        _ = ClearSessionAsync();
    }

    public async Task TryRestoreSessionAsync()
    {
        if (SessionRestored) return;
        try
        {
            var username = await _js.InvokeAsync<string?>("localStorage.getItem", "lp_username");
            var fullName = await _js.InvokeAsync<string?>("localStorage.getItem", "lp_fullname");
            var role = await _js.InvokeAsync<string?>("localStorage.getItem", "lp_role");
            var userIdStr = await _js.InvokeAsync<string?>("localStorage.getItem", "lp_userid");

            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(role))
            {
                IsAuthenticated = true;
                Username = username;
                FullName = fullName ?? string.Empty;
                Role = role;
                UserId = int.TryParse(userIdStr, out var id) ? id : 0;
                NotifyStateChanged();
            }
        }
        catch { /* JS not ready yet, ignore */ }
        finally
        {
            SessionRestored = true;
        }
    }

    private async Task SaveSessionAsync()
    {
        try
        {
            await _js.InvokeVoidAsync("localStorage.setItem", "lp_username", Username);
            await _js.InvokeVoidAsync("localStorage.setItem", "lp_fullname", FullName);
            await _js.InvokeVoidAsync("localStorage.setItem", "lp_role", Role);
            await _js.InvokeVoidAsync("localStorage.setItem", "lp_userid", UserId.ToString());
        }
        catch { }
    }

    private async Task ClearSessionAsync()
    {
        try
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", "lp_username");
            await _js.InvokeVoidAsync("localStorage.removeItem", "lp_fullname");
            await _js.InvokeVoidAsync("localStorage.removeItem", "lp_role");
            await _js.InvokeVoidAsync("localStorage.removeItem", "lp_userid");
        }
        catch { }
    }

    private void NotifyStateChanged() => OnCurrentUserStateChanged?.Invoke();
}
using System.Net.Http.Json;
using Oscilloscope.Client.Application.Services;

namespace Oscilloscope.Client.Infrastructure.Auth;

public sealed class AuthService(HttpClient http, TokenStore tokenStore) : IAuthService
{
    public bool IsAuthenticated => tokenStore.HasToken;
    public string? AccessToken => tokenStore.AccessToken;

    public async Task LoginAsync(string email, string password, CancellationToken ct)
    {
        var resp = await http.PostAsJsonAsync("/login", new { Email = email, Password = password }, ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"Login failed: {(int)resp.StatusCode} {resp.ReasonPhrase}");

        var body = await resp.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: ct);
        if (string.IsNullOrWhiteSpace(body?.AccessToken))
            throw new InvalidOperationException("Server returned empty access token.");

        tokenStore.Set(body.AccessToken);
    }

    public async Task RegisterAsync(string email, string password, CancellationToken ct)
    {
        var resp = await http.PostAsJsonAsync("/register", new { Email = email, Password = password }, ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"Register failed: {(int)resp.StatusCode} {resp.ReasonPhrase}");

        var body = await resp.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: ct);
        if (string.IsNullOrWhiteSpace(body?.AccessToken))
            throw new InvalidOperationException("Server returned empty access token.");

        tokenStore.Set(body.AccessToken);
    }

    public void Logout() => tokenStore.Clear();

    private sealed class AuthResponse
    {
        public string AccessToken { get; set; } = "";
        public DateTimeOffset ExpiresAtUtc { get; set; }
    }
}

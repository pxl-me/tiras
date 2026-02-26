namespace Oscilloscope.Client.Infrastructure.Auth;

public sealed class TokenStore
{
    public string? AccessToken { get; private set; }
    public bool HasToken => !string.IsNullOrWhiteSpace(AccessToken);

    public void Set(string token) => AccessToken = token;
    public void Clear() => AccessToken = null;
}

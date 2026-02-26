namespace Oscilloscope.Client.Application.Services;

public interface IAuthService
{
    bool IsAuthenticated { get; }
    string? AccessToken { get; }

    Task LoginAsync(string email, string password, CancellationToken ct);
    Task RegisterAsync(string email, string password, CancellationToken ct);

    void Logout();
}

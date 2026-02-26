namespace Oscilloscope.Client.Application.Services;

public interface IExternalAppsService
{
    bool IsServerRunning { get; }
    bool IsEmulatorRunning { get; }

    void StartServer();
    void StopServer();

    void StartEmulator(string? jwt); // jwt = null, якщо не виконано логін
    void StopEmulator();
}

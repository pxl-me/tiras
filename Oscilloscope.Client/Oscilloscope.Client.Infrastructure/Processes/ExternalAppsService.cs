using System.Diagnostics;
using Microsoft.Extensions.Options;
using Oscilloscope.Client.Application.Services;
using Oscilloscope.Client.Infrastructure.Options;
using Oscilloscope.Client.Infrastructure.Utils;

namespace Oscilloscope.Client.Infrastructure.Processes;

public sealed class ExternalAppsService : IExternalAppsService
{
    private readonly BackendOptions _backend;
    private readonly ExternalAppsOptions _apps;

    private Process? _server;
    private Process? _emulator;

    public bool IsServerRunning => _server is { HasExited: false };
    public bool IsEmulatorRunning => _emulator is { HasExited: false };

    public ExternalAppsService(IOptions<BackendOptions> backend, IOptions<ExternalAppsOptions> apps)
    {
        _backend = backend.Value;
        _apps = apps.Value;
    }

    public void StartServer()
    {
        if (IsServerRunning) return;

        var projectPath = ResolveServerProjectPath();
        var args = $"/k dotnet run --project \"{projectPath}\"";

        _server = StartCmd(args);
    }

    public void StopServer() => Stop(ref _server);

    public void StartEmulator(string? jwt)
    {
        if (IsEmulatorRunning) return;

        var projectPath = ResolveEmulatorProjectPath();

        // upd: 5010
        // Підключення до SignalR якщо є jwt для команд
        var extra = $"--udpHost={_apps.UdpHost} --udpPort={_apps.UdpPort}";

        if (!string.IsNullOrWhiteSpace(jwt))
            extra += $" --hubUrl={_backend.HubUrl} --jwt={jwt}";

        var args = $"/k dotnet run --project \"{projectPath}\" -- {extra}";
        _emulator = StartCmd(args);
    }

    public void StopEmulator() => Stop(ref _emulator);

    private string ResolveServerProjectPath()
    {
        if (!string.IsNullOrWhiteSpace(_apps.ServerApiProjectPath))
            return Path.GetFullPath(_apps.ServerApiProjectPath);

        var root = ProjectRootLocator.LocateRootOrThrow();
        return Path.Combine(root, "Oscilloscope.Server", "Oscilloscope.Server.Api", "Oscilloscope.Server.Api.csproj");
    }

    private string ResolveEmulatorProjectPath()
    {
        if (!string.IsNullOrWhiteSpace(_apps.EmulatorProjectPath))
            return Path.GetFullPath(_apps.EmulatorProjectPath);

        var root = ProjectRootLocator.LocateRootOrThrow();
        return Path.Combine(root, "Oscilloscope.Emulator", "Oscilloscope.Emulator", "Oscilloscope.Emulator.csproj");
    }

    private static Process StartCmd(string args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = args,
            UseShellExecute = true,
            CreateNoWindow = false
        };

        return Process.Start(psi) ?? throw new InvalidOperationException("Failed to start process.");
    }

    private static void Stop(ref Process? p)
    {
        if (p is null) return;

        try
        {
            if (!p.HasExited)
                p.Kill(entireProcessTree: true);
        }
        catch
        {
            // ignore
        }
        finally
        {
            try { p.Dispose(); } catch { /* ignore */ }
            p = null;
        }
    }
}

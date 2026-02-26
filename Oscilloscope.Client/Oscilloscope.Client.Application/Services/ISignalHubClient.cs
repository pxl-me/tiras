using Oscilloscope.Client.Application.Models;

namespace Oscilloscope.Client.Application.Services;

public interface ISignalHubClient
{
    bool IsConnected { get; }

    event Action<string>? ConnectionStatusChanged; // Connected/Reconnecting/Disconnected
    event Action<SignalFrame>? FrameReceived;

    event Action<string>? EmulatorStatusReceived;

    Task ConnectAsync(CancellationToken ct);
    Task DisconnectAsync(CancellationToken ct);

    Task SetSignalTypeAsync(SignalType type, CancellationToken ct);
    Task SetModeAsync(EmulatorMode mode, CancellationToken ct);

    Task RequestStatusAsync(CancellationToken ct);
}

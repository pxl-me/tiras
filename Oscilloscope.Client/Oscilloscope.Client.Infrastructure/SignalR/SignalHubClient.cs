using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using Oscilloscope.Client.Application.Models;
using Oscilloscope.Client.Application.Services;
using Oscilloscope.Client.Infrastructure.Auth;
using Oscilloscope.Client.Infrastructure.Options;

namespace Oscilloscope.Client.Infrastructure.SignalR;

public sealed class SignalHubClient : ISignalHubClient
{
    private readonly BackendOptions _backend;
    private readonly TokenStore _token;

    private HubConnection? _conn;
    private CancellationTokenSource? _streamCts;

    public bool IsConnected => _conn?.State == HubConnectionState.Connected;

    public event Action<string>? ConnectionStatusChanged;
    public event Action<SignalFrame>? FrameReceived;
    public event Action<string>? EmulatorStatusReceived;

    public SignalHubClient(IOptions<BackendOptions> backend, TokenStore token)
    {
        _backend = backend.Value;
        _token = token;
    }

    public async Task ConnectAsync(CancellationToken ct)
    {
        if (_conn is not null) return;

        if (!_token.HasToken)
            throw new InvalidOperationException("Not authenticated. Please login first.");

        _conn = new HubConnectionBuilder()
            .WithUrl(_backend.HubUrl, o =>
            {
                o.AccessTokenProvider = () => Task.FromResult(_token.AccessToken)!;
            })
            .WithAutomaticReconnect()
            .Build();

        // Emulator -> Server -> UI
        _conn.On<string>("Status", status =>
        {
            EmulatorStatusReceived?.Invoke(status);
        });

        _conn.Reconnecting += ex =>
        {
            ConnectionStatusChanged?.Invoke("Reconnecting...");
            return Task.CompletedTask;
        };

        _conn.Reconnected += _ =>
        {
            ConnectionStatusChanged?.Invoke("Connected");
            return Task.CompletedTask;
        };

        _conn.Closed += _ =>
        {
            ConnectionStatusChanged?.Invoke("Disconnected");
            return Task.CompletedTask;
        };

        await _conn.StartAsync(ct);
        ConnectionStatusChanged?.Invoke("Connected");

        _streamCts = new CancellationTokenSource();
        _ = Task.Run(() => StreamLoop(_streamCts.Token));
    }

    public async Task DisconnectAsync(CancellationToken ct)
    {
        if (_conn is null) return;

        _streamCts?.Cancel();
        _streamCts = null;

        await _conn.StopAsync(ct);
        await _conn.DisposeAsync();
        _conn = null;

        ConnectionStatusChanged?.Invoke("Disconnected");
    }

    public Task SetSignalTypeAsync(SignalType type, CancellationToken ct)
    {
        if (_conn is null) return Task.CompletedTask;
        return _conn.InvokeAsync("SetSignalType", type.ToString(), ct);
    }

    public Task SetModeAsync(EmulatorMode mode, CancellationToken ct)
    {
        if (_conn is null) return Task.CompletedTask;
        return _conn.InvokeAsync("SetMode", mode.ToString(), ct);
    }

    public Task RequestStatusAsync(CancellationToken ct)
    {
        if (_conn is null) return Task.CompletedTask;
        return _conn.InvokeAsync("RequestStatus", ct);
    }

    private async Task StreamLoop(CancellationToken ct)
    {
        if (_conn is null) return;

        try
        {
            await foreach (var frame in _conn.StreamAsync<SignalFrame>("Stream", ct))
                FrameReceived?.Invoke(frame);
        }
        catch
        {
            // reconnect handled by SignalR (tocheck)
        }
    }
}

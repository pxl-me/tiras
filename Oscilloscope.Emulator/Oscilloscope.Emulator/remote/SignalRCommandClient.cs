using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using Microsoft.AspNetCore.SignalR.Client;
//using Microsoft.Extensions.Logging;
using Oscilloscope.Emulator.models;
using Oscilloscope.Emulator.runtime;

namespace Oscilloscope.Emulator.remote
{
    public sealed class SignalRCommandClient
    {
        private readonly string _hubUrl;
        private readonly string? _jwt;
        private readonly EmulatorState _state;
        private readonly ILogger _log;

        private HubConnection? _conn;

        public SignalRCommandClient(string hubUrl, string? jwt, EmulatorState state, ILogger<SignalRCommandClient> log)
        {
            _hubUrl = hubUrl;
            _jwt = jwt;
            _state = state;
            _log = log;
        }

        public async Task RunAsync(CancellationToken ct)
        {
            _conn = new HubConnectionBuilder()
                .WithUrl(_hubUrl, options =>
                {
                    if (!string.IsNullOrWhiteSpace(_jwt))
                    {
                        options.AccessTokenProvider = () => Task.FromResult(_jwt)!;
                    }
                })
                .WithAutomaticReconnect()
                .Build();

            RegisterHandlers(_conn);

            _conn.Reconnecting += ex =>
            {
                _log.LogWarning(ex, "SignalR reconnecting...");
                return Task.CompletedTask;
            };

            _conn.Reconnected += id =>
            {
                _log.LogInformation("SignalR reconnected. ConnectionId={Id}", id);
                return Task.CompletedTask;
            };

            _conn.Closed += ex =>
            {
                _log.LogWarning(ex, "SignalR closed.");
                return Task.CompletedTask;
            };

            await _conn.StartAsync(ct);
            _log.LogInformation("SignalR connected: {Url}", _hubUrl);

            try
            {
                await Task.Delay(Timeout.Infinite, ct);
            }
            catch (OperationCanceledException)
            {
                // ignore
            }

            await _conn.StopAsync();
            await _conn.DisposeAsync();
        }

        private void RegisterHandlers(HubConnection conn)
        {
            // Сервер може викликати:
            // - SetSignalType("Sine"/"Square"/"Sawtooth") або int
            // - SetMode("Normal"/"Noise"/"CutOff")
            conn.On<string>("SetSignalType", arg =>
            {
                if (Enum.TryParse<SignalType>(arg, ignoreCase: true, out var type))
                {
                    _state.SetSignalType(type);
                    _log.LogInformation("SignalR cmd: SetSignalType -> {Type}", type);
                }
                else
                {
                    _log.LogWarning("SignalR cmd: SetSignalType invalid arg: {Arg}", arg);
                }
            });

            conn.On<int>("SetSignalTypeInt", arg =>
            {
                if (Enum.IsDefined(typeof(SignalType), arg))
                {
                    var type = (SignalType)arg;
                    _state.SetSignalType(type);
                    _log.LogInformation("SignalR cmd: SetSignalTypeInt -> {Type}", type);
                }
                else
                {
                    _log.LogWarning("SignalR cmd: SetSignalTypeInt invalid arg: {Arg}", arg);
                }
            });

            conn.On<string>("SetMode", arg =>
            {
                if (Enum.TryParse<EmulatorMode>(arg, ignoreCase: true, out var mode))
                {
                    _state.SetMode(mode);
                    _log.LogInformation("SignalR cmd: SetMode -> {Mode}", mode);
                }
                else
                {
                    _log.LogWarning("SignalR cmd: SetMode invalid arg: {Arg}", arg);
                }
            });

            conn.On("Ping", () =>
            {
                _log.LogInformation("SignalR cmd: Ping");
            });

            // UI просить статус -> емулятор відповідає через ReportStatus(status)
            conn.On("RequestStatus", async () =>
            {
                try
                {
                    var snap = _state.Snapshot();
                    var status = $"type={snap.SignalType}; mode={snap.Mode}";
                    _log.LogInformation("SignalR cmd: RequestStatus -> {Status}", status);

                    if (conn.State == HubConnectionState.Connected)
                        await conn.InvokeAsync("ReportStatus", status);
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Failed to report status");
                }
            });
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Oscilloscope.Server.Api.Dto;
using Oscilloscope.Server.Application;

namespace Oscilloscope.Server.Api.Hubs
{
    [Authorize]
    public sealed class SignalHub : Hub
    {
        private readonly ISignalStreamBroadcaster _stream;

        public SignalHub(ISignalStreamBroadcaster stream) => _stream = stream;

        // UI -> Server -> Emulator
        // Команди для емулятору підєднаного до цього ж хабу: "SetSignalType", "SetSignalTypeInt", "SetMode".
        // Броад каст команд для всіх клієнтів (в мене він 1).
        public Task SetSignalType(string type) =>
            Clients.All.SendAsync("SetSignalType", type);

        public Task SetSignalTypeInt(int type) =>
            Clients.All.SendAsync("SetSignalTypeInt", type);

        public Task SetMode(string mode) =>
            Clients.All.SendAsync("SetMode", mode);

        public Task Ping() =>
            Clients.All.SendAsync("Ping");

        // UI -> Server -> Emulator: статус ріквест
        public Task RequestStatus() =>
            Clients.All.SendAsync("RequestStatus");

        // Emulator -> Server -> UI: повернути статус
        public Task ReportStatus(string status) =>
            Clients.All.SendAsync("Status", status);

        public async IAsyncEnumerable<SignalFrameDto> Stream(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            await foreach (var f in _stream.Subscribe(ct))
                yield return new SignalFrameDto(f.TimestampUtc, f.Type, f.Samples, f.IsAnomaly);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace Oscilloscope.Emulator.transport
{
    public sealed class UdpSender : IAsyncDisposable
    {
        private readonly UdpClient _client;

        public UdpSender(string host, int port)
        {
            _client = new UdpClient();
            _client.Connect(host, port); // endpoint
        }

        public async Task SendAsync(byte[] payload, CancellationToken ct)
        {
            // UdpClient не має overload з CancellationToken для SendAsync у всіх версіях,
            // тому варіант без прямого скасування:
            await _client.SendAsync(payload, payload.Length);
            ct.ThrowIfCancellationRequested();
        }

        public ValueTask DisposeAsync()
        {
            _client.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}

using Microsoft.Extensions.Logging;
using Oscilloscope.Emulator.models;
using Oscilloscope.Emulator.runtime;
using Oscilloscope.Emulator.signal;
using Oscilloscope.Emulator.transport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oscilloscope.Emulator.runtime
{
    public sealed class UdpStreamingRunner
    {
        private readonly EmulatorState _state;
        private readonly SignalGenerator _generator;
        private readonly UdpPacketSerializer _serializer;
        private readonly UdpSender _sender;
        private readonly int _intervalMs;
        private readonly ILogger _log;

        private readonly Random _rng = new();

        public UdpStreamingRunner(
            EmulatorState state,
            SignalGenerator generator,
            UdpPacketSerializer serializer,
            UdpSender sender,
            int intervalMs,
            ILogger<UdpStreamingRunner> log)
        {
            _state = state;
            _generator = generator;
            _serializer = serializer;
            _sender = sender;
            _intervalMs = intervalMs;
            _log = log;
        }

        public async Task RunAsync(CancellationToken ct)
        {
            var samples = new float[100];
            long n = 0;

            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_intervalMs));

            _log.LogInformation("UDP streaming started: 100 samples/packet, interval {Ms}ms", _intervalMs);

            while (await timer.WaitForNextTickAsync(ct))
            {
                var snapshot = _state.Snapshot();

                _generator.Generate(snapshot.SignalType, samples);

                ApplyMode(snapshot.Mode, samples);

                long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var packet = _serializer.Serialize(snapshot.SignalType, ts, samples);

                await _sender.SendAsync(packet, ct);

                n++;
                if (n % 250 == 0)
                {
                    _log.LogInformation("Sent packets: {Count}. Type={Type}, Mode={Mode}, PacketSize={Size} bytes",
                        n, snapshot.SignalType, snapshot.Mode, packet.Length);
                }
            }
        }

        private void ApplyMode(EmulatorMode mode, float[] samples)
        {
            switch (mode)
            {
                case EmulatorMode.Normal:
                    return;

                case EmulatorMode.CutOff:
                    Array.Clear(samples);
                    return;

                case EmulatorMode.Noise:
                    for (int i = 0; i < samples.Length; i++)
                    {
                        // шум ±10% (мультиплікативний): * [0.9..1.1]
                        var factor = 0.9 + (_rng.NextDouble() * 0.2);
                        samples[i] = (float)(samples[i] * factor);
                    }
                    return;

                default:
                    return;
            }
        }
    }
}

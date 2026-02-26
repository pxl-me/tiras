using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oscilloscope.Emulator.runtime
{
    public sealed class EmulatorSettings
    {
        public string UdpHost { get; init; } = "127.0.0.1";
        public int UdpPort { get; init; } = 5005;

        public string? HubUrl { get; init; } // "http://localhost:5000/hubs/device"
        public string? Jwt { get; init; }    // якщо хаб захищений JWT

        public int SendIntervalMs { get; init; } = 20;  // 50 пакетів/сек
        public double SampleRateHz { get; init; } = 1000;
        public double FrequencyHz { get; init; } = 5;

        public static EmulatorSettings FromArgs(string[] args)
        {
            // парсер: udpHost, udpPort, hubUrl, jwt, intervalMs, sr, freq
            var dict = args
                .Select(a => a.Split('=', 2))
                .Where(p => p.Length == 2 && p[0].StartsWith("--"))
                .ToDictionary(p => p[0].Substring(2), p => p[1], StringComparer.OrdinalIgnoreCase);

            return new EmulatorSettings
            {
                UdpHost = dict.TryGetValue("udpHost", out var h) ? h : "127.0.0.1",
                UdpPort = dict.TryGetValue("udpPort", out var p) && int.TryParse(p, out var pi) ? pi : 5005,
                HubUrl = dict.TryGetValue("hubUrl", out var u) ? u : null,
                Jwt = dict.TryGetValue("jwt", out var j) ? j : null,
                SendIntervalMs = dict.TryGetValue("intervalMs", out var im) && int.TryParse(im, out var imi) ? imi : 20,
                SampleRateHz = dict.TryGetValue("sr", out var sr) && double.TryParse(sr, out var sri) ? sri : 1000,
                FrequencyHz = dict.TryGetValue("freq", out var fr) && double.TryParse(fr, out var fri) ? fri : 5
            };
        }
    }
}

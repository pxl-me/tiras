using Microsoft.Extensions.Logging;
using Oscilloscope.Emulator.models;
using Oscilloscope.Emulator.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oscilloscope.Emulator.runtime
{
    public static class ConsoleCommandLoop
    {
        public static async Task RunAsync(EmulatorState state, ILogger log, Action requestStop, CancellationToken ct)
        {
            PrintHelp(log);

            while (!ct.IsCancellationRequested)
            {
                string? line;
                try
                {
                    line = await Task.Run(() => Console.ReadLine(), ct);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var cmd = line.Trim();

                if (cmd.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
                    cmd.Equals("quit", StringComparison.OrdinalIgnoreCase))
                {
                    requestStop();
                    return;
                }

                if (cmd.Equals("help", StringComparison.OrdinalIgnoreCase))
                {
                    PrintHelp(log);
                    continue;
                }

                if (cmd.Equals("status", StringComparison.OrdinalIgnoreCase))
                {
                    var s = state.Snapshot();
                    log.LogInformation("STATUS: Type={Type}, Mode={Mode}", s.SignalType, s.Mode);
                    continue;
                }

                // type sine|square|saw
                if (cmd.StartsWith("type ", StringComparison.OrdinalIgnoreCase))
                {
                    var arg = cmd.Substring(5).Trim();
                    var type = arg.ToLowerInvariant() switch
                    {
                        "sine" => SignalType.Sine,
                        "square" => SignalType.Square,
                        "saw" or "sawtooth" => SignalType.Sawtooth,
                        _ => (SignalType?)null
                    };

                    if (type is null)
                    {
                        log.LogWarning("Unknown type: {Arg}", arg);
                    }
                    else
                    {
                        state.SetSignalType(type.Value);
                        log.LogInformation("Console cmd: Type -> {Type}", type.Value);
                    }
                    continue;
                }

                // noise on|off
                if (cmd.StartsWith("noise ", StringComparison.OrdinalIgnoreCase))
                {
                    var arg = cmd.Substring(6).Trim().ToLowerInvariant();
                    if (arg == "on")
                    {
                        state.SetMode(EmulatorMode.Noise);
                        log.LogInformation("Console cmd: Mode -> Noise");
                    }
                    else if (arg == "off")
                    {
                        state.SetMode(EmulatorMode.Normal);
                        log.LogInformation("Console cmd: Mode -> Normal");
                    }
                    else
                    {
                        log.LogWarning("Usage: noise on|off");
                    }
                    continue;
                }

                // cut on
                if (cmd.Equals("cut on", StringComparison.OrdinalIgnoreCase))
                {
                    state.SetMode(EmulatorMode.CutOff);
                    log.LogInformation("Console cmd: Mode -> CutOff");
                    continue;
                }

                // restore
                if (cmd.Equals("restore", StringComparison.OrdinalIgnoreCase))
                {
                    state.SetMode(EmulatorMode.Normal);
                    log.LogInformation("Console cmd: Mode -> Normal");
                    continue;
                }

                log.LogWarning("Unknown command. Type 'help'.");
            }
        }

        private static void PrintHelp(ILogger log)
        {
            log.LogInformation("Commands:");
            log.LogInformation("  help");
            log.LogInformation("  status");
            log.LogInformation("  type sine|square|saw");
            log.LogInformation("  noise on|off");
            log.LogInformation("  cut on");
            log.LogInformation("  restore");
            log.LogInformation("  exit|quit");
        }
    }
}

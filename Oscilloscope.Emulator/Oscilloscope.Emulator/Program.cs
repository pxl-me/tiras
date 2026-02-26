using Microsoft.Extensions.Logging;
using Oscilloscope.Emulator.models;
using Oscilloscope.Emulator.remote;
using Oscilloscope.Emulator.runtime;
using Oscilloscope.Emulator.signal;
using Oscilloscope.Emulator.transport;

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddSimpleConsole(o =>
    {
        o.SingleLine = true;
        o.TimestampFormat = "HH:mm:ss ";
    });
    builder.SetMinimumLevel(LogLevel.Information);
});

var log = loggerFactory.CreateLogger("Emulator");

// ---- simple args parsing ----
var settings = EmulatorSettings.FromArgs(args);

log.LogInformation("Starting emulator. UDP -> {Host}:{Port}, SignalR -> {HubUrl}", settings.UdpHost, settings.UdpPort, settings.HubUrl ?? "(disabled)");
if (!string.IsNullOrWhiteSpace(settings.Jwt))
    log.LogInformation("JWT provided (len={Len})", settings.Jwt.Length);

// ---- core components ----
var state = new EmulatorState();
var generator = new SignalGenerator(sampleRateHz: settings.SampleRateHz, frequencyHz: settings.FrequencyHz);
var serializer = new UdpPacketSerializer();
var udpSender = new UdpSender(settings.UdpHost, settings.UdpPort);

SignalRCommandClient? signalR = null;
if (!string.IsNullOrWhiteSpace(settings.HubUrl))
{
    signalR = new SignalRCommandClient(settings.HubUrl!, settings.Jwt, state, loggerFactory.CreateLogger<SignalRCommandClient>());
}

var runner = new UdpStreamingRunner(
    state,
    generator,
    serializer,
    udpSender,
    settings.SendIntervalMs,
    loggerFactory.CreateLogger<UdpStreamingRunner>());

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

// ---- start ----
var tasks = new List<Task>();

tasks.Add(runner.RunAsync(cts.Token));

if (signalR != null)
    tasks.Add(signalR.RunAsync(cts.Token));

tasks.Add(ConsoleCommandLoop.RunAsync(state, log, () => cts.Cancel(), cts.Token));

await Task.WhenAll(tasks);

// ---- cleanup ----
await udpSender.DisposeAsync();
log.LogInformation("Emulator stopped.");
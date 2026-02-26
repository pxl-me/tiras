using System.Text.Json;
using Microsoft.Extensions.Logging;
using Oscilloscope.Server.Domain;

namespace Oscilloscope.Server.Application;

public sealed class SignalProcessingService
{
    private readonly IUdpPacketDeserializer _deserializer;
    private readonly ISignalAnalyzer _analyzer;
    private readonly IAnomalyWriter _anomalyWriter;
    private readonly ISignalStreamBroadcaster _broadcaster;
    private readonly ILogger<SignalProcessingService> _log;

    public SignalProcessingService(
        IUdpPacketDeserializer deserializer,
        ISignalAnalyzer analyzer,
        IAnomalyWriter anomalyWriter,
        ISignalStreamBroadcaster broadcaster,
        ILogger<SignalProcessingService> log)
    {
        _deserializer = deserializer;
        _analyzer = analyzer;
        _anomalyWriter = anomalyWriter;
        _broadcaster = broadcaster;
        _log = log;
    }

    public async Task ProcessAsync(ReadOnlyMemory<byte> datagram, CancellationToken ct)
    {
        var packet = _deserializer.Deserialize(datagram.Span);
        var res = _analyzer.Analyze(packet.Type, packet.Samples);

        var frame = new SignalFrame(packet.Type, packet.TimestampUtc, packet.Samples, res.Mae, res.IsAnomaly);
        _broadcaster.Publish(frame);

        if (!res.IsAnomaly) return;

        var rawJson = JsonSerializer.Serialize(new
        {
            packet.TimestampUtc,
            packet.Type,
            Samples = packet.Samples
        });

        await _anomalyWriter.SaveAsync(new AnomalyRecord
        {
            TimestampUtc = packet.TimestampUtc,
            Type = packet.Type,
            Mae = res.Mae,
            RawJson = rawJson
        }, ct);

        _log.LogWarning("(!) ANOMALY DETECTED: Type={Type}, MAE={Mae:F4}", packet.Type, res.Mae);

        _log.LogInformation("Frame: {Type} samples={N} mae={Mae:F3} anomaly={Anom}",
            packet.Type, packet.Samples.Length, res.Mae, res.IsAnomaly);
    }
}

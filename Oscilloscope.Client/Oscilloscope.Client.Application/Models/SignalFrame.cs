namespace Oscilloscope.Client.Application.Models;

public sealed record SignalFrame(
    DateTimeOffset TimestampUtc,
    SignalType Type,
    float[] Samples,
    bool IsAnomaly
);

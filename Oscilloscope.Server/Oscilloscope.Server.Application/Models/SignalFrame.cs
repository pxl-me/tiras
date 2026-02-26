using Oscilloscope.Server.Domain;

namespace Oscilloscope.Server.Application;

public sealed record SignalFrame(
    SignalType Type,
    DateTimeOffset TimestampUtc,
    float[] Samples,
    double Mae,
    bool IsAnomaly
);

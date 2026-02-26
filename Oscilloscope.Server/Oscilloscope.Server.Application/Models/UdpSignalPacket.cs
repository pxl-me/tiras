using Oscilloscope.Server.Domain;

namespace Oscilloscope.Server.Application;

public sealed record UdpSignalPacket(
    SignalType Type,
    DateTimeOffset TimestampUtc,
    float[] Samples
);

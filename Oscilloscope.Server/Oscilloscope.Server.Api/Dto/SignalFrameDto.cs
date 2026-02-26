using Oscilloscope.Server.Domain;

namespace Oscilloscope.Server.Api.Dto
{
    public sealed record SignalFrameDto(
        DateTimeOffset TimestampUtc,
        SignalType Type,
        float[] Samples,
        bool IsAnomaly
    );
}

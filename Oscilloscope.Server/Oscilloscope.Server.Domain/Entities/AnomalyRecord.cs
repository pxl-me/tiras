namespace Oscilloscope.Server.Domain;

public sealed class AnomalyRecord
{
    public long Id { get; set; }
    public DateTimeOffset TimestampUtc { get; set; }
    public SignalType Type { get; set; }
    public double Mae { get; set; }
    public string RawJson { get; set; } = "{}";
}

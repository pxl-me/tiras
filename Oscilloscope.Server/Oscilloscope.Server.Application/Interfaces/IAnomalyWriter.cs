using Oscilloscope.Server.Domain;

namespace Oscilloscope.Server.Application;

public interface IAnomalyWriter
{
    Task SaveAsync(AnomalyRecord record, CancellationToken ct);
}

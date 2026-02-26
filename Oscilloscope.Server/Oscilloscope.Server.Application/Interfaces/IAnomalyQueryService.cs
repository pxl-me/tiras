using Oscilloscope.Server.Domain;

namespace Oscilloscope.Server.Application;

public interface IAnomalyQueryService
{
    Task<IReadOnlyList<AnomalyRecord>> GetLatestAsync(int take, CancellationToken ct);
}

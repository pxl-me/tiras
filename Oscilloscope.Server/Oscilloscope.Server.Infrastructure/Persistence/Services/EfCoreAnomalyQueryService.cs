using Microsoft.EntityFrameworkCore;
using Oscilloscope.Server.Application;
using Oscilloscope.Server.Domain;

namespace Oscilloscope.Server.Infrastructure;

public sealed class EfCoreAnomalyQueryService : IAnomalyQueryService
{
    private readonly AppDbContext _db;

    public EfCoreAnomalyQueryService(AppDbContext db)
        => _db = db;

    public async Task<IReadOnlyList<AnomalyRecord>> GetLatestAsync(int take, CancellationToken ct)
    {
        return await _db.Anomalies
            .OrderByDescending(x => x.TimestampUtc)
            .Take(take)
            .ToListAsync(ct);
    }
}
using Oscilloscope.Server.Application;
using Oscilloscope.Server.Domain;

namespace Oscilloscope.Server.Infrastructure;

public sealed class EfCoreAnomalyWriter : IAnomalyWriter
{
    private readonly AppDbContext _db;

    public EfCoreAnomalyWriter(AppDbContext db) => _db = db;

    public async Task SaveAsync(AnomalyRecord record, CancellationToken ct)
    {
        _db.Anomalies.Add(record);
        await _db.SaveChangesAsync(ct);
    }
}
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oscilloscope.Server.Application;

namespace Oscilloscope.Server.Infrastructure;

public sealed class UdpIngestHostedService : BackgroundService
{
    private readonly UdpOptions _opt;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UdpIngestHostedService> _log;

    public UdpIngestHostedService(
        IOptions<UdpOptions> opt,
        IServiceScopeFactory scopeFactory,
        ILogger<UdpIngestHostedService> log)
    {
        _opt = opt.Value;
        _scopeFactory = scopeFactory;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var udp = new UdpClient(_opt.Port);
        _log.LogInformation("UDP listener started on port {Port}", _opt.Port);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var res = await udp.ReceiveAsync(stoppingToken);

                using var scope = _scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<SignalProcessingService>();

                await processor.ProcessAsync(res.Buffer, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "UDP ingest error");
            }
        }
    }
}
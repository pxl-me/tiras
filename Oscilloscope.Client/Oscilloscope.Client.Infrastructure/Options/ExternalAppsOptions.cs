namespace Oscilloscope.Client.Infrastructure.Options;

public sealed class ExternalAppsOptions
{
    // (tofix)
    public string? ServerApiProjectPath { get; set; }
    public string? EmulatorProjectPath { get; set; }

    public string UdpHost { get; set; } = "127.0.0.1";
    public int UdpPort { get; set; } = 5010;
}

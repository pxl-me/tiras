namespace Oscilloscope.Client.Infrastructure.Options;

public sealed class BackendOptions
{
    public string BaseUrl { get; set; } = "http://localhost:5070";
    public string HubUrl { get; set; } = "http://localhost:5070/hubs/signal";
}

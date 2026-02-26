using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Oscilloscope.Client.Application.Services;
using Oscilloscope.Client.Infrastructure.Auth;
using Oscilloscope.Client.Infrastructure.Options;
using Oscilloscope.Client.Infrastructure.Processes;
using Oscilloscope.Client.Infrastructure.SignalR;

namespace Oscilloscope.Client.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOscilloscopeClientInfrastructure(this IServiceCollection services, IConfiguration cfg)
    {
        services.Configure<BackendOptions>(cfg.GetSection("Backend"));
        services.Configure<ExternalAppsOptions>(cfg.GetSection("ExternalApps"));

        services.AddSingleton<TokenStore>();

        var baseUrl = cfg.GetSection("Backend").GetValue<string>("BaseUrl") ?? "http://localhost:5070";

        services.AddHttpClient<IAuthService, AuthService>(c =>
        {
            c.BaseAddress = new Uri(baseUrl);
        });

        services.AddSingleton<ISignalHubClient, SignalHubClient>();
        services.AddSingleton<IExternalAppsService, ExternalAppsService>();

        return services;
    }
}

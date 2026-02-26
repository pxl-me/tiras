using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Oscilloscope.Client.Infrastructure;
using Oscilloscope.Client.Ui.ViewModels;

namespace Oscilloscope.Client.Ui;

// NOTE: inside namespace "Oscilloscope.Client.Ui" the identifier "Application" can be resolved
// as the sibling namespace "Oscilloscope.Client.Application" (because both are under "Oscilloscope.Client").
// Fully qualify the WPF Application type to avoid CS0118.
public partial class App : System.Windows.Application
{
    private IHost? _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(cfg =>
            {
                cfg.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((ctx, services) =>
            {
                services.AddOscilloscopeClientInfrastructure(ctx.Configuration);

                services.AddSingleton<MainViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();

        _host.Start();

        var wnd = _host.Services.GetRequiredService<MainWindow>();
        wnd.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        base.OnExit(e);
    }
}

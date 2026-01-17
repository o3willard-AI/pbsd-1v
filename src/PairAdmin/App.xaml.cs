using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Windows;
using PairAdmin.UI;

namespace PairAdmin;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IHost? _host;

    /// <summary>
    /// Gets the current service provider
    /// </summary>
    public static IServiceProvider? Services { get; private set; }

    /// <summary>
    /// Application startup
    /// </summary>
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Build the host
        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                // Register all services here
                ConfigureServices(context, services);
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.AddDebug();
            })
            .Build();

        Services = _host.Services;

        // Start the host
        await _host.StartAsync();

        // Show main window
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    /// <summary>
    /// Application exit
    /// </summary>
    protected override async void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);

        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
    }

    /// <summary>
    /// Configure dependency injection services
    /// </summary>
    private void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        // Configuration
        services.AddSingleton(context.Configuration);
        services.Configure<WindowStateOptions>(context.Configuration.GetSection("WindowState"));

        // Logging
        services.AddLogging();

        // Window state manager
        services.AddSingleton<WindowStateManager>();

        // Services will be registered in their respective project files
        // This is a placeholder for Task 1.1

        // Register main window
        services.AddSingleton<MainWindow>();
    }
}

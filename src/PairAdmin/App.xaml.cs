using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Windows;
using System.Diagnostics;
using PairAdmin.UI;
using PairAdmin.LLMGateway;
using PairAdmin.LLMGateway.Providers;
using PairAdmin.LLMGateway.Models;
using PairAdmin.Chat;
using PairAdmin.Context;

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
        // Set up global exception handling ASAP
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        base.OnStartup(e);

        Console.WriteLine("=== PAIRADMIN STARTUP BEGIN === (BUILD: Jan 22 2025 1:00 PM)");
        Console.WriteLine("🔥 ENGINE: ACTIVE & WEAPONIZED FOR DEEP CRASH DIAGNOSTICS 🔥");
        Console.WriteLine("📊 MONITORING: Enhanced Chat Processing & Exception Capture");

        // Build the host
        Console.WriteLine("Building DI container...");
        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                ConfigureServices(context, services);
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Debug);
                logging.AddDebug();
            })
            .Build();

        Services = _host.Services;
        Console.WriteLine("DI container built, starting host...");

        // Start the host
        await _host.StartAsync();
        Console.WriteLine("Host started successfully");

        // Configure LLM providers
        Console.WriteLine("Configuring LLM providers...");
        var llmSection = _host.Services.GetRequiredService<IConfiguration>().GetSection("LLM");
        var llmGateway = _host.Services.GetRequiredService<PairAdmin.LLMGateway.LLMGateway>();

        var openaiProvider = _host.Services.GetRequiredService<OpenAIProvider>();
        var geminiProvider = _host.Services.GetRequiredService<GeminiProvider>();
        var openrouterProvider = _host.Services.GetRequiredService<OpenRouterProvider>();
        var ollamaProvider = _host.Services.GetRequiredService<OllamaProvider>();

        try {
            // Configure each provider if settings exist
            var openaiConfig = new {
                ProviderId = "openai",
                Model = llmSection.GetSection("OpenAI").GetValue<string>("Model") ?? "gpt-3.5-turbo",
                ApiKey = llmSection.GetSection("OpenAI").GetValue<string>("ApiKey"),
                BaseUrl = llmSection.GetSection("OpenAI").GetValue<string>("BaseUrl", "https://api.openai.com/v1")
            };
            openaiProvider.Configure(new ProviderConfiguration {
                ProviderId = openaiConfig.ProviderId,
                Model = openaiConfig.Model,
                ApiKey = openaiConfig.ApiKey,
                BaseUrl = openaiConfig.BaseUrl
            });

            var geminiConfig = new {
                ProviderId = "gemini",
                Model = llmSection.GetSection("Google").GetValue<string>("Model") ?? "gemini-pro",
                ApiKey = llmSection.GetSection("Google").GetValue<string>("ApiKey"),
                BaseUrl = llmSection.GetSection("Google").GetValue<string>("BaseUrl", "https://generativelanguage.googleapis.com/v1")
            };
            geminiProvider.Configure(new ProviderConfiguration {
                ProviderId = geminiConfig.ProviderId,
                Model = geminiConfig.Model,
                ApiKey = geminiConfig.ApiKey,
                BaseUrl = geminiConfig.BaseUrl
            });

            var openrouterConfig = new {
                ProviderId = "openrouter",
                Model = llmSection.GetSection("OpenRouter").GetValue<string>("Model") ?? "openrouter/auto",
                ApiKey = llmSection.GetSection("OpenRouter").GetValue<string>("ApiKey"),
                BaseUrl = llmSection.GetSection("OpenRouter").GetValue<string>("BaseUrl", "https://openrouter.ai/api/v1")
            };
            openrouterProvider.Configure(new ProviderConfiguration {
                ProviderId = openrouterConfig.ProviderId,
                Model = openrouterConfig.Model,
                ApiKey = openrouterConfig.ApiKey,
                BaseUrl = openrouterConfig.BaseUrl
            });

            var ollamaConfig = new {
                ProviderId = "ollama",
                Model = llmSection.GetSection("Ollama").GetValue<string>("Model") ?? "llama3",
                BaseUrl = llmSection.GetSection("Ollama").GetValue<string>("BaseUrl", "http://localhost:11434")
            };
            ollamaProvider.Configure(new ProviderConfiguration {
                ProviderId = ollamaConfig.ProviderId,
                Model = ollamaConfig.Model,
                BaseUrl = ollamaConfig.BaseUrl
            });

            // Register providers with gateway
            llmGateway.RegisterProvider(openaiProvider);
            llmGateway.RegisterProvider(geminiProvider);
            llmGateway.RegisterProvider(openrouterProvider);
            llmGateway.RegisterProvider(ollamaProvider);

            // Set default active provider
            llmGateway.SetActiveProvider("openrouter");

            Console.WriteLine("=== LLM PROVIDERS CONFIGURED SUCCESSFULLY ===");

        } catch (Exception ex) {
            Console.WriteLine($"Failed to configure LLM providers: {ex.Message}");
            // Continue with limited functionality
        }

        // Show main window
        Console.WriteLine("Creating MainWindow...");
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        Console.WriteLine("MainWindow created, showing...");
        mainWindow.Show();
        Console.WriteLine("=== PAIRADMIN STARTUP COMPLETE ===");
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

        // LLM Gateway
        services.AddSingleton<PairAdmin.LLMGateway.LLMGateway>();
        services.AddSingleton<OpenAIProvider>();
        services.AddSingleton<GeminiProvider>();
        services.AddSingleton<OpenRouterProvider>();
        services.AddSingleton<OllamaProvider>();

        // Context
        services.AddSingleton<IContextProvider, ContextManager>();

        // Register main window
        services.AddSingleton<MainWindow>();
    }
}

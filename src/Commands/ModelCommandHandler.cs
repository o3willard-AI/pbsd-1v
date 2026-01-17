using System.Text;
using Microsoft.Extensions.Logging;

namespace PairAdmin.Commands;

public class ModelCommandHandler : CommandHandlerBase
{
    private readonly LLMGateway.LLMGateway _gateway;
    private readonly Configuration.SettingsProvider _settingsProvider;

    public ModelCommandHandler(
        LLMGateway.LLMGateway gateway,
        Configuration.SettingsProvider settingsProvider,
        ILogger<ModelCommandHandler>? logger = null)
        : base(logger)
    {
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        _settingsProvider = settingsProvider ?? throw new ArgumentNullException(nameof(settingsProvider));
    }

    public override CommandMetadata Metadata => new()
    {
        Name = "model",
        Description = "Manages the LLM model and provider",
        Syntax = "/model [show|<model>|--list|--provider <provider-id>]",
        Examples =
        [
            "/model",
            "/model gpt-4",
            "/model gpt-3.5-turbo",
            "/model --list",
            "/model --provider openai"
        ],
        Category = "Configuration",
        IsAvailable = true,
        MinArguments = 0,
        MaxArguments = 1,
        Aliases = ["m", "model"]
    };

    public override bool CanExecute(CommandContext context)
    {
        return context.LLMGateway != null;
    }

    public override CommandResult Execute(CommandContext context, ParsedCommand command)
    {
        if (command.Arguments.Count == 0 && command.Flags.Count == 0)
        {
            return ShowCurrentModel();
        }

        if (command.Flags.TryGetValue("list", out var listValue))
        {
            return ListModels();
        }

        if (command.Flags.TryGetValue("provider", out var providerId))
        {
            return SetProvider(providerId);
        }

        if (command.Arguments.Count > 0)
        {
            var model = command.Arguments[0];
            return SetModel(model);
        }

        return InvalidArguments("/model [show|<model>|--list|--provider <provider-id>]");
    }

    private CommandResult ShowCurrentModel()
    {
        var activeProvider = _gateway.ActiveProvider;

        if (activeProvider == null)
        {
            return new CommandResult
            {
                IsSuccess = false,
                Response = "## No Active Provider\n\nNo LLM provider is currently configured. Use `/model --provider <id>` to set a provider.",
                Status = CommandStatus.NotAvailable,
                CancelSend = true
            };
        }

        var providerInfo = activeProvider.GetProviderInfo();
        var config = _gateway.GetActiveConfiguration();
        var currentModel = config?.Model ?? providerInfo.DefaultModel;

        var sb = new StringBuilder();

        sb.AppendLine("## Current Model");
        sb.AppendLine();
        sb.AppendLine($"**Provider:** {providerInfo.Name} ({providerInfo.Id})");
        sb.AppendLine($"**Model:** {currentModel}");
        sb.AppendLine($"**Max Context:** {providerInfo.GetMaxContextForModel(currentModel):N0} tokens");
        sb.AppendLine($"**Streaming:** {(providerInfo.SupportsStreaming ? "Yes" : "No")}");
        sb.AppendLine();

        sb.AppendLine("**Available Models:**");
        foreach (var model in providerInfo.SupportedModels.Take(10))
        {
            var marker = model.Equals(currentModel, StringComparison.OrdinalIgnoreCase) ? " ✓" : "";
            sb.AppendLine($"- {model}{marker}");
        }

        if (providerInfo.SupportedModels.Count > 10)
        {
            sb.AppendLine($"- ... and {providerInfo.SupportedModels.Count - 10} more");
        }

        sb.AppendLine();
        sb.AppendLine("Use `/model --list` to see all providers and models.");

        return Success(sb.ToString());
    }

    private CommandResult ListModels()
    {
        var providers = _gateway.GetAllProvidersInfo();

        if (providers.Count == 0)
        {
            return Error("No LLM providers are registered.");
        }

        var sb = new StringBuilder();

        sb.AppendLine("## Available Models");
        sb.AppendLine();

        foreach (var provider in providers)
        {
            sb.AppendLine($"### {provider.Name} ({provider.Id})");
            sb.AppendLine($"_{provider.Description}_");
            sb.AppendLine();
            sb.AppendLine("**Models:**");
            foreach (var model in provider.SupportedModels)
            {
                sb.AppendLine($"- {model}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("Use `/model gpt-4` to switch to a specific model.");
        sb.AppendLine("Use `/model --provider openai` to switch providers.");

        return Success(sb.ToString());
    }

    private CommandResult SetModel(string model)
    {
        var activeProvider = _gateway.ActiveProvider;

        if (activeProvider == null)
        {
            return new CommandResult
            {
                IsSuccess = false,
                Response = "## No Active Provider\n\nNo LLM provider is currently configured. Use `/model --provider <id>` to set a provider first.",
                Status = CommandStatus.NotAvailable,
                CancelSend = true
            };
        }

        var providerInfo = activeProvider.GetProviderInfo();

        if (!providerInfo.SupportsModel(model))
        {
            var sb = new StringBuilder();
            sb.AppendLine($"## Model Not Supported");
            sb.AppendLine();
            sb.AppendLine($"Model '{model}' is not supported by provider '{providerInfo.Name}'.");
            sb.AppendLine();
            sb.AppendLine("**Supported models:**");
            foreach (var m in providerInfo.SupportedModels)
            {
                sb.AppendLine($"- {m}");
            }
            return Error(sb.ToString());
        }

        try
        {
            var config = _gateway.GetActiveConfiguration();
            if (config != null)
            {
                config.Model = model;
                _gateway.ConfigureProvider(config);
            }

            _logger.LogInformation("Set model to {Model}", model);

            var sb = new StringBuilder();
            sb.AppendLine("## Model Updated");
            sb.AppendLine();
            sb.AppendLine($"Model set to **{model}**.");
            sb.AppendLine($"Max context: {providerInfo.GetMaxContextForModel(model):N0} tokens");

            return Success(sb.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set model");
            return Error(ex.Message);
        }
    }

    private CommandResult SetProvider(string providerId)
    {
        var providerIds = _gateway.GetProviderIds();

        if (!providerIds.Contains(providerId, StringComparer.OrdinalIgnoreCase))
        {
            var sb = new StringBuilder();
            sb.AppendLine($"## Provider Not Found");
            sb.AppendLine();
            sb.AppendLine($"Provider '{providerId}' is not registered.");
            sb.AppendLine();
            sb.AppendLine("**Available providers:**");
            foreach (var id in providerIds)
            {
                var info = _gateway.GetAllProvidersInfo().FirstOrDefault(p => p.Id == id);
                var name = info?.Name ?? id;
                sb.AppendLine($"- {name} ({id})");
            }
            return Error(sb.ToString());
        }

        try
        {
            _gateway.SetActiveProvider(providerId);
            _logger.LogInformation("Set active provider to {ProviderId}", providerId);

            var providerInfo = _gateway.GetAllProvidersInfo()
                .FirstOrDefault(p => p.Id.Equals(providerId, StringComparison.OrdinalIgnoreCase));

            var sb = new StringBuilder();
            sb.AppendLine("## Provider Updated");
            sb.AppendLine();
            sb.AppendLine($"Active provider set to **{providerInfo?.Name ?? providerId}**.");
            sb.AppendLine($"Default model: {providerInfo?.DefaultModel ?? "unknown"}");

            if (providerInfo?.SupportedModels.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("**Available models:**");
                foreach (var model in providerInfo.SupportedModels.Take(5))
                {
                    sb.AppendLine($"- {model}");
                }
                if (providerInfo.SupportedModels.Count > 5)
                {
                    sb.AppendLine($"- ... and {providerInfo.SupportedModels.Count - 5} more");
                }
            }

            return Success(sb.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set provider");
            return Error(ex.Message);
        }
    }
}

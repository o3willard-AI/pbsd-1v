using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace PairAdmin.Commands;

public class StatusCommandHandler : CommandHandlerBase
{
    private readonly LLMGateway.LLMGateway _gateway;
    private readonly DateTime _startTime;

    public StatusCommandHandler(
        LLMGateway.LLMGateway gateway,
        ILogger<StatusCommandHandler>? logger = null)
        : base(logger)
    {
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        _startTime = DateTime.Now;
    }

    public override CommandMetadata Metadata => new()
    {
        Name = "status",
        Description = "Shows system status and diagnostics",
        Syntax = "/status",
        Examples = ["/status"],
        Category = "Utility",
        IsAvailable = true,
        MinArguments = 0,
        MaxArguments = 0,
        Aliases = ["stats", "info"]
    };

    public override CommandResult Execute(CommandContext context, ParsedCommand command)
    {
        return ShowStatus(context);
    }

    private CommandResult ShowStatus(CommandContext context)
    {
        var sb = new StringBuilder();

        sb.AppendLine("## System Status");
        sb.AppendLine();

        sb.AppendLine("### Runtime");
        sb.AppendLine($"**Uptime:** {GetUptime()}");
        sb.AppendLine($"**Started:** {_startTime:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();

        sb.AppendLine("### Messages");
        sb.AppendLine($"**Total Messages:** {context.Messages.Count}");
        sb.AppendLine($"**User Messages:** {context.Messages.Count(m => m.Role == "user")}");
        sb.AppendLine($"**Assistant Messages:** {context.Messages.Count(m => m.Role == "assistant")}");
        sb.AppendLine();

        sb.AppendLine("### Context");
        if (context.ContextManager != null)
        {
            sb.AppendLine($"**Max Lines:** {context.ContextManager.MaxLines}");
            sb.AppendLine($"**Current Lines:** {context.ContextManager.CurrentLines}");
            sb.AppendLine($"**Current Tokens:** {context.ContextManager.CurrentTokens:N0}");
            sb.AppendLine($"**Policy:** {context.ContextManager.Policy}");
        }
        else
        {
            sb.AppendLine("_Context manager not available_");
        }
        sb.AppendLine();

        sb.AppendLine("### LLM Provider");
        var activeProvider = _gateway.ActiveProvider;
        if (activeProvider != null)
        {
            var providerInfo = activeProvider.GetProviderInfo();
            sb.AppendLine($"**Provider:** {providerInfo.Name}");
            sb.AppendLine($"**Model:** {_gateway.GetActiveConfiguration()?.Model ?? providerInfo.DefaultModel}");
            sb.AppendLine($"**Max Context:** {providerInfo.GetMaxContextForModel(providerInfo.DefaultModel):N0} tokens");
            sb.AppendLine($"**Streaming:** {(providerInfo.SupportsStreaming ? "Yes" : "No")}");
        }
        else
        {
            sb.AppendLine("_No active provider_");
        }
        sb.AppendLine();

        sb.AppendLine("### Memory");
        sb.AppendLine($"**Working Set:** {GetMemoryUsage():N0} KB");
        sb.AppendLine($"**GC Total:** {GC.GetTotalMemory(false):N0} bytes");
        sb.AppendLine();

        sb.AppendLine("### Registered Providers");
        var providerIds = _gateway.GetProviderIds();
        foreach (var id in providerIds)
        {
            var info = _gateway.GetAllProvidersInfo().FirstOrDefault(p => p.Id == id);
            var name = info?.Name ?? id;
            var marker = id == _gateway.ActiveProviderId ? " (active)" : "";
            sb.AppendLine($"- {name}{marker}");
        }

        if (providerIds.Count == 0)
        {
            sb.AppendLine("_No providers registered_");
        }

        return Success(sb.ToString());
    }

    private string GetUptime()
    {
        var elapsed = DateTime.Now - _startTime;
        var parts = new List<string>();

        if (elapsed.Days > 0)
        {
            parts.Add($"{elapsed.Days}d");
        }

        if (elapsed.Hours > 0)
        {
            parts.Add($"{elapsed.Hours}h");
        }

        if (elapsed.Minutes > 0)
        {
            parts.Add($"{elapsed.Minutes}m");
        }

        parts.Add($"{elapsed.Seconds}s");

        return string.Join(" ", parts);
    }

    private long GetMemoryUsage()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            return process.WorkingSet64 / 1024;
        }
        catch
        {
            return 0;
        }
    }
}

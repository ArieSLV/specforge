// specforge MCP host (ITEM-002). Wires stderr logging + a manual DI container, registers the Core
// services and the IMcpTool implementations, and runs the MCP stdio loop dispatching to the tools.
using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

using Specforge.Core.Diagnostics;
using Specforge.Core.Exceptions;
using Specforge.Core.Skills;
using Specforge.Mcp.Errors;
using Specforge.Mcp.Hosting;
using Specforge.Mcp.Tools;
using Specforge.Mcp.Tools.Decisions;
using Specforge.Mcp.Tools.Items;
using Specforge.Mcp.Tools.Ledger;
using Specforge.Mcp.Tools.Validation;

ServiceCollection services = new();
services.AddSpecforgeStderrLogging();
services.AddSpecforgeCore();
services.AddSingleton(new SpecforgeWorkingDirectory(Directory.GetCurrentDirectory()));
services.AddSingleton<ToolExceptionMapper>();
services.AddSingleton<IEnvelopeRenderer, EnvelopeRenderer>();
services.AddSingleton<IMcpTool, ListPackagesTool>();
services.AddSingleton<IMcpTool, UsePackageTool>();
services.AddSingleton<IMcpTool, InfoTool>();
services.AddSingleton<IMcpTool, InstallSkillsTool>();
services.AddSingleton<IMcpTool, InitTool>();
services.AddSingleton<IMcpTool, ListDecisionsTool>();
services.AddSingleton<IMcpTool, GetDecisionTool>();
services.AddSingleton<IMcpTool, CreateDecisionTool>();
services.AddSingleton<IMcpTool, SetDecisionStatusTool>();
services.AddSingleton<IMcpTool, DeleteDecisionTool>();
services.AddSingleton<IMcpTool, ListItemsTool>();
services.AddSingleton<IMcpTool, GetItemTool>();
services.AddSingleton<IMcpTool, CreateItemTool>();
services.AddSingleton<IMcpTool, SetItemStatusTool>();
services.AddSingleton<IMcpTool, DeleteItemTool>();
services.AddSingleton<IMcpTool, AppendHistoryTool>();
services.AddSingleton<IMcpTool, AppendReviewTool>();
services.AddSingleton<IMcpTool, AppendCommitTool>();
services.AddSingleton<IMcpTool, DeleteReviewTool>();
services.AddSingleton<IMcpTool, DeleteCommitTool>();
services.AddSingleton<IMcpTool, ValidateTool>();

await using ServiceProvider provider = services.BuildServiceProvider();

Dictionary<string, IMcpTool> tools = provider.GetServices<IMcpTool>().ToDictionary(t => t.Name, t => t);
IEnvelopeRenderer renderer = provider.GetRequiredService<IEnvelopeRenderer>();
BinaryInfo binaryInfo = provider.GetRequiredService<BinaryInfo>();
ILoggerFactory loggerFactory = provider.GetRequiredService<ILoggerFactory>();

// ITEM-004 (DEC-005): validate the embedded skill catalog before the stdio loop accepts traffic.
// A broken embedded SKILL.md fails the server start with a clear stderr log; an empty catalog is fine.
ILogger startupLogger = loggerFactory.CreateLogger("Specforge.Mcp.Startup");
try
{
    int skillCount = provider.GetRequiredService<SkillCatalogValidator>().Validate();
    startupLogger.LogInformation("Embedded skill catalog validated: {SkillCount} skill(s).", skillCount);
}
catch (SpecforgeEmbeddedSkillNotFoundException ex)
{
    startupLogger.LogError(ex, "Embedded skill catalog validation failed for resource {ResourceName}.", ex.ResourceName);
    return 1;
}

List<Tool> advertisedTools = [.. tools.Values.Select(t => new Tool { Name = t.Name, InputSchema = t.InputSchema })];

McpServerOptions options = new()
{
    ServerInfo = new Implementation { Name = "specforge", Version = binaryInfo.Version },
    Capabilities = new ServerCapabilities { Tools = new ToolsCapability() },
    Handlers = new McpServerHandlers
    {
        ListToolsHandler = (_, _) => ValueTask.FromResult(new ListToolsResult { Tools = advertisedTools }),
        CallToolHandler = async (context, cancellationToken) =>
        {
            string name = context.Params?.Name ?? string.Empty;
            if (!tools.TryGetValue(name, out IMcpTool? tool))
            {
                return ErrorResult(EnvelopeRenderer.InvalidArgument("name", name, "a known tool name", "call a tool from the advertised list"));
            }

            try
            {
                JsonElement args = ArgumentsToElement(context.Params?.Arguments);
                ToolResult result = await tool.InvokeAsync(args, cancellationToken).ConfigureAwait(false);
                return result switch
                {
                    ToolResult.Success success => SuccessResult(success.Payload),
                    ToolResult.Failure failure => ErrorResult(failure.Envelope),
                    _ => ErrorResult(renderer.Render(new InvalidOperationException("unrecognized tool result"))),
                };
            }
#pragma warning disable CA1031 // host boundary: every unhandled exception becomes a structured envelope.
            catch (Exception ex)
            {
                return ErrorResult(renderer.Render(ex));
            }
#pragma warning restore CA1031
        },
    },
};

await using StdioServerTransport transport = new("specforge", loggerFactory);
await using McpServer server = McpServer.Create(transport, options, loggerFactory, provider);
await server.RunAsync().ConfigureAwait(false);
return 0;

static CallToolResult SuccessResult(JsonElement payload) => new()
{
    Content = [new TextContentBlock { Text = payload.GetRawText() }],
};

static CallToolResult ErrorResult(McpErrorEnvelope envelope) => new()
{
    IsError = true,
    Content = [new TextContentBlock { Text = envelope.ToJson() }],
};

static JsonElement ArgumentsToElement(IDictionary<string, JsonElement>? arguments) =>
    JsonSerializer.SerializeToElement(arguments ?? new Dictionary<string, JsonElement>());

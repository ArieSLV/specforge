// specforge MCP host (ITEM-002). Wires stderr logging + a manual DI container, registers the Core
// services and the IMcpTool implementations, and runs the MCP stdio loop dispatching to the tools.
using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

using Specforge.Core.Diagnostics;
using Specforge.Mcp.Hosting;
using Specforge.Mcp.Tools;

ServiceCollection services = new();
services.AddSpecforgeStderrLogging();
services.AddSpecforgeCore();
services.AddSingleton(new SpecforgeWorkingDirectory(Directory.GetCurrentDirectory()));
services.AddSingleton<ToolExceptionMapper>();
services.AddSingleton<IMcpTool, ListPackagesTool>();
services.AddSingleton<IMcpTool, UsePackageTool>();
services.AddSingleton<IMcpTool, InfoTool>();

await using ServiceProvider provider = services.BuildServiceProvider();

Dictionary<string, IMcpTool> tools = provider.GetServices<IMcpTool>().ToDictionary(t => t.Name, t => t);
ToolExceptionMapper mapper = provider.GetRequiredService<ToolExceptionMapper>();
BinaryInfo binaryInfo = provider.GetRequiredService<BinaryInfo>();
ILoggerFactory loggerFactory = provider.GetRequiredService<ILoggerFactory>();

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
                return ErrorResult(new McpErrorEnvelope(
                    "specforge.tool.invalid_argument",
                    $"unknown tool '{name}'",
                    "call a tool from the advertised list",
                    JsonSerializer.SerializeToElement(new { argument = "name", given = name, expected = "a known tool name" })));
            }

            try
            {
                JsonElement args = ArgumentsToElement(context.Params?.Arguments);
                ToolResult result = await tool.InvokeAsync(args, cancellationToken).ConfigureAwait(false);
                return result switch
                {
                    ToolResult.Success success => SuccessResult(success.Payload),
                    ToolResult.Failure failure => ErrorResult(failure.Envelope),
                    _ => ErrorResult(mapper.Map(new InvalidOperationException("unrecognized tool result"))),
                };
            }
#pragma warning disable CA1031 // host boundary: every unhandled exception becomes a structured envelope.
            catch (Exception ex)
            {
                return ErrorResult(mapper.Map(ex));
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

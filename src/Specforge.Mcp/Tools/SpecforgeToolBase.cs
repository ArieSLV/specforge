using System.Text.Json;

using Specforge.Core.Configuration;
using Specforge.Mcp.Hosting;

namespace Specforge.Mcp.Tools;

/// <summary>
/// Shared base for tools that need a loaded config. Centralizes the once-per-session
/// discovery + load + cache step so every tool body stays focused on its own logic.
/// </summary>
public abstract class SpecforgeToolBase(ConfigLoader loader, SessionState session, SpecforgeWorkingDirectory workingDirectory) : IMcpTool
{
    /// <summary>The process-local session state (loaded config + active selection).</summary>
    protected SessionState Session => session;

    /// <summary>The directory config discovery walks up from.</summary>
    protected string StartDirectory => workingDirectory.Path;

    public abstract string Name { get; }

    public abstract JsonElement InputSchema { get; }

    public abstract Task<ToolResult> InvokeAsync(JsonElement args, CancellationToken ct);

    /// <summary>Loads and caches the config on first use; returns the cached config thereafter.</summary>
    protected async Task<SpecforgeConfig> EnsureLoadedAsync(CancellationToken ct)
    {
        if (session.Config is null)
        {
            SpecforgeConfig config = await loader.LoadAsync(workingDirectory.Path, ct).ConfigureAwait(false);
            session.Initialize(config);
        }

        return session.Config!;
    }

    /// <summary>Reads a non-empty string argument; returns false (and empty) if absent or not a string.</summary>
    protected static bool TryGetString(JsonElement args, string name, out string value)
    {
        if (args.ValueKind == JsonValueKind.Object
            && args.TryGetProperty(name, out JsonElement element)
            && element.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(element.GetString()))
        {
            value = element.GetString()!;
            return true;
        }

        value = string.Empty;
        return false;
    }

    /// <summary>Reads an optional string argument, or <see langword="null"/> if absent.</summary>
    protected static string? GetOptionalString(JsonElement args, string name) =>
        TryGetString(args, name, out string value) ? value : null;

    /// <summary>Reads a boolean argument; true only when present and JSON <c>true</c>.</summary>
    protected static bool GetBool(JsonElement args, string name) =>
        args.ValueKind == JsonValueKind.Object && args.TryGetProperty(name, out JsonElement element) && element.ValueKind == JsonValueKind.True;

    /// <summary>Builds a <c>specforge.tool.invalid_argument</c> failure result.</summary>
    protected static ToolResult InvalidArgument(string argument, string expected) => ToolResult.Fail(new McpErrorEnvelope(
        "specforge.tool.invalid_argument",
        $"argument '{argument}' is invalid; expected {expected}.",
        "correct the argument value",
        JsonSerializer.SerializeToElement(new { argument, expected })));
}

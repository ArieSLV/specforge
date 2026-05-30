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

    /// <summary>
    /// Builds a <c>specforge.id.not_found</c> failure result (ITEM-009 inline emission). ITEM-011's
    /// audit later formalizes this code in the DEC-007 catalog and reroutes it through a typed exception.
    /// </summary>
    protected static ToolResult IdNotFound(string id) => ToolResult.Fail(new McpErrorEnvelope(
        "specforge.id.not_found",
        $"no ledger row found for id '{id}'.",
        "verify the id by reading the relevant ledger file or via get_decision/get_item",
        JsonSerializer.SerializeToElement(new { id })));

    /// <summary>Builds the <c>confirm:true</c>-required failure result shared by the delete tools (DEC-007 Delete Semantics).</summary>
    protected static ToolResult ConfirmRequired() => ToolResult.Fail(new McpErrorEnvelope(
        "specforge.tool.invalid_argument",
        "argument 'confirm' is invalid; expected true.",
        "pass confirm=true to perform the delete; pass dryRun=true to preview",
        JsonSerializer.SerializeToElement(new { argument = "confirm", expected = "true" })));

    /// <summary>Maps a decision/item target (bare or ART form) to its <c>ART-*</c> artifact id.</summary>
    protected static string ToArtifactId(string target) =>
        target.StartsWith("ART-", StringComparison.Ordinal) ? target : $"ART-{target}";

    /// <summary>Strips the leading <c>ART-</c> from an artifact id (e.g. <c>ART-ITEM-007</c> → <c>ITEM-007</c>).</summary>
    protected static string BareTarget(string artifactId) =>
        artifactId.StartsWith("ART-", StringComparison.Ordinal) ? artifactId["ART-".Length..] : artifactId;
}

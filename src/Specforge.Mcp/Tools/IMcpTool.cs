using System.Text.Json;

namespace Specforge.Mcp.Tools;

/// <summary>
/// Uniform shape for every specforge MCP tool (ITEM-002). Every later tool-bearing item adds a new
/// implementation; the host (<c>Program.cs</c>) registers them and dispatches by <see cref="Name"/>.
/// Implementations are SDK-agnostic so they can be invoked in-proc by tests.
/// </summary>
public interface IMcpTool
{
    /// <summary>The MCP tool name (<c>verb_noun</c> snake_case, DEC-007).</summary>
    string Name { get; }

    /// <summary>The JSON Schema advertised for this tool's arguments (DEC-007 schema richness).</summary>
    JsonElement InputSchema { get; }

    /// <summary>Executes the tool against parsed <paramref name="args"/>, returning a success or failure result.</summary>
    Task<ToolResult> InvokeAsync(JsonElement args, CancellationToken ct);
}

using System.Text.Json;

namespace Specforge.Mcp.Tools;

/// <summary>
/// Discriminated union of a tool outcome: a JSON <see cref="Success"/> payload or a structured
/// <see cref="Failure"/> envelope (DEC-007). The host renders either into the MCP tool result.
/// </summary>
public abstract record ToolResult
{
    private ToolResult()
    {
    }

    /// <summary>A successful invocation carrying a JSON payload.</summary>
    public sealed record Success(JsonElement Payload) : ToolResult;

    /// <summary>A failed invocation carrying the error envelope to surface.</summary>
    public sealed record Failure(McpErrorEnvelope Envelope) : ToolResult;

    /// <summary>Wraps a pre-built <see cref="JsonElement"/> payload.</summary>
    public static ToolResult Ok(JsonElement payload) => new Success(payload);

    /// <summary>Serializes <paramref name="payload"/> to a JSON payload (camelCase by literal property naming).</summary>
    public static ToolResult Ok(object payload) => new Success(JsonSerializer.SerializeToElement(payload));

    /// <summary>Wraps an error envelope.</summary>
    public static ToolResult Fail(McpErrorEnvelope envelope) => new Failure(envelope);
}

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Specforge.Mcp.Tools;

/// <summary>
/// The structured error payload every tool surfaces (DEC-007 §"Error Envelope"):
/// <c>{ "error": { code, message, suggestion?, data? } }</c>.
/// </summary>
/// <param name="Code">Stable dotted code the AI dispatches on.</param>
/// <param name="Message">Single-paragraph human/AI-readable summary.</param>
/// <param name="Suggestion">Actionable next step, when a deterministic one exists.</param>
/// <param name="Data">Code-specific machine-readable detail, when present.</param>
public sealed record McpErrorEnvelope(string Code, string Message, string? Suggestion, JsonElement? Data)
{
    /// <summary>Renders the canonical <c>{ "error": { ... } }</c> JSON document.</summary>
    public string ToJson()
    {
        JsonObject error = new()
        {
            ["code"] = Code,
            ["message"] = Message,
        };

        if (Suggestion is not null)
        {
            error["suggestion"] = Suggestion;
        }

        if (Data is { } data)
        {
            error["data"] = JsonNode.Parse(data.GetRawText());
        }

        return new JsonObject { ["error"] = error }.ToJsonString();
    }
}

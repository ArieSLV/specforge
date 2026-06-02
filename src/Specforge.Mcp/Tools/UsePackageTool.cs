using System.Text.Json;

using Specforge.Core.Configuration;
using Specforge.Mcp.Hosting;

namespace Specforge.Mcp.Tools;

/// <summary>
/// <c>use_package</c> (DEC-002/DEC-007): set the session's active package by name. Returns the
/// previous and new selection. Throws <c>specforge.package.unknown</c> for a bogus name (mapped by the host).
/// </summary>
public sealed class UsePackageTool(ConfigLoader loader, SessionState session, SpecforgeWorkingDirectory workingDirectory)
    : SpecforgeToolBase(loader, session, workingDirectory)
{
    private static readonly JsonElement Schema = JsonSerializer.Deserialize<JsonElement>(
        """
        {
          "type": "object",
          "description": "Set the session's active package by name (DEC-002). Switching mid-session is allowed; updates in-process state only.",
          "required": ["name"],
          "properties": {
            "name": {
              "type": "string",
              "minLength": 1,
              "description": "A package name as reported by list_packages.",
              "examples": ["specforge-mvp"]
            }
          },
          "additionalProperties": false
        }
        """);

    public override string Name => "use_package";

    public override JsonElement InputSchema => Schema;

    public override async Task<ToolResult> InvokeAsync(JsonElement args, CancellationToken ct)
    {
        if (args.ValueKind != JsonValueKind.Object
            || !args.TryGetProperty("name", out JsonElement nameElement)
            || nameElement.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(nameElement.GetString()))
        {
            return ToolResult.Fail(Errors.EnvelopeRenderer.InvalidArgument("name", given: null, "a non-empty string"));
        }

        string name = nameElement.GetString()!;
        await EnsureLoadedAsync(ct).ConfigureAwait(false);

        string? previous = Session.ActivePackageName;
        Session.Select(name);
        return ToolResult.Ok(new { previous, current = name });
    }
}

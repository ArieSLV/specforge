using System.Text.Json;

using Specforge.Core.Configuration;
using Specforge.Core.Documents;
using Specforge.Mcp.Hosting;

namespace Specforge.Mcp.Tools.Decisions;

/// <summary><c>create_decision</c> (DEC-007): allocate DEC-NNN, write the file, add the ART row, append history.</summary>
public sealed class CreateDecisionTool(ConfigLoader loader, SessionState session, SpecforgeWorkingDirectory workingDirectory, DecisionService service)
    : SpecforgeToolBase(loader, session, workingDirectory)
{
    private static readonly JsonElement Schema = JsonSerializer.Deserialize<JsonElement>(
        """
        {
          "type": "object",
          "description": "Create a decision record in the active package: allocates the next DEC-NNN, writes the file, adds the ART-DEC-NNN artifact row, and appends a Created history event.",
          "required": ["title"],
          "properties": {
            "title": { "type": "string", "minLength": 1, "maxLength": 120, "description": "Plain title; slugified for the filename.", "examples": ["Schema Versioning Policy"] },
            "status": { "type": "string", "enum": ["Draft", "Draft for user review"], "default": "Draft", "description": "Initial status; Approved and later are rejected." },
            "dryRun": { "type": "boolean", "default": false, "description": "Return the planned id/path/ledger writes without touching the filesystem." }
          },
          "additionalProperties": false
        }
        """);

    public override string Name => "create_decision";

    public override JsonElement InputSchema => Schema;

    public override async Task<ToolResult> InvokeAsync(JsonElement args, CancellationToken ct)
    {
        if (!TryGetString(args, "title", out string title))
        {
            return InvalidArgument("title", "a non-empty string");
        }

        await EnsureLoadedAsync(ct).ConfigureAwait(false);
        CreateDecisionResult result = await service.CreateAsync(title, GetOptionalString(args, "status"), GetBool(args, "dryRun"), ct).ConfigureAwait(false);
        return ToolResult.Ok(new
        {
            id = result.Id,
            slug = result.Slug,
            decisionPath = result.DecisionPath,
            artifactId = result.ArtifactId,
            dryRun = result.DryRun,
            plannedWrites = result.PlannedWrites,
        });
    }
}

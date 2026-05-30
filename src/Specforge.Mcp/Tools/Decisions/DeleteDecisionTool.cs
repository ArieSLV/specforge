using System.Text.Json;

using Specforge.Core.Configuration;
using Specforge.Core.Documents;
using Specforge.Mcp.Hosting;

namespace Specforge.Mcp.Tools.Decisions;

/// <summary><c>delete_decision</c> (DEC-007 Delete Semantics): pre-Approved-only; removes file + ART row + cascades REV rows + appends Deleted history.</summary>
public sealed class DeleteDecisionTool(ConfigLoader loader, SessionState session, SpecforgeWorkingDirectory workingDirectory, DecisionService service)
    : SpecforgeToolBase(loader, session, workingDirectory)
{
    private static readonly JsonElement Schema = JsonSerializer.Deserialize<JsonElement>(
        """
        {
          "type": "object",
          "description": "Delete a pre-Approved decision: removes the file, the ART-DEC-NNN row, and cascades to its REV rows; appends a Deleted history event. The number is tombstoned (never reused). Requires confirm:true.",
          "required": ["id", "confirm"],
          "properties": {
            "id": { "type": "string", "description": "Decision identifier (bare or qualified)." },
            "confirm": { "type": "boolean", "description": "Must be true to perform the delete." },
            "dryRun": { "type": "boolean", "default": false, "description": "Preview the cascade without writing." }
          },
          "additionalProperties": false
        }
        """);

    public override string Name => "delete_decision";

    public override JsonElement InputSchema => Schema;

    public override async Task<ToolResult> InvokeAsync(JsonElement args, CancellationToken ct)
    {
        if (!TryGetString(args, "id", out string id))
        {
            return InvalidArgument("id", "a decision identifier");
        }

        await EnsureLoadedAsync(ct).ConfigureAwait(false);
        DeleteDecisionResult result = await service.DeleteAsync(id, GetBool(args, "confirm"), GetBool(args, "dryRun"), ct).ConfigureAwait(false);
        return ToolResult.Ok(new
        {
            id = result.Id,
            decisionPath = result.DecisionPath,
            artifactId = result.ArtifactId,
            removedReviewRows = result.RemovedReviewRows,
            dryRun = result.DryRun,
        });
    }
}

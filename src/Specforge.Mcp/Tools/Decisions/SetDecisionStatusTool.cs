using System.Text.Json;

using Specforge.Core.Configuration;
using Specforge.Core.Documents;
using Specforge.Mcp.Hosting;

namespace Specforge.Mcp.Tools.Decisions;

/// <summary><c>set_decision_status</c> (DEC-007): validate + apply a lifecycle transition; update file + ART row + history (+ REV on approval).</summary>
public sealed class SetDecisionStatusTool(ConfigLoader loader, SessionState session, SpecforgeWorkingDirectory workingDirectory, DecisionService service)
    : SpecforgeToolBase(loader, session, workingDirectory)
{
    private static readonly JsonElement Schema = JsonSerializer.Deserialize<JsonElement>(
        """
        {
          "type": "object",
          "description": "Change a decision's status (validated against the lifecycle state machine). Updates the file, the ART row, and appends history; on Approved with reviewer+notes also appends a REV row.",
          "required": ["id", "status"],
          "properties": {
            "id": { "type": "string", "description": "Decision identifier (bare or qualified)." },
            "status": { "type": "string", "description": "Target status.", "examples": ["Draft for user review", "Approved", "Superseded", "Withdrawn"] },
            "reviewer": { "type": "string", "description": "Reviewer name; with notes on an Approved transition, appends a REV row." },
            "notes": { "type": "string", "description": "Review notes; with reviewer on Approved, appends a REV row." },
            "dryRun": { "type": "boolean", "default": false }
          },
          "additionalProperties": false
        }
        """);

    public override string Name => "set_decision_status";

    public override JsonElement InputSchema => Schema;

    public override async Task<ToolResult> InvokeAsync(JsonElement args, CancellationToken ct)
    {
        if (!TryGetString(args, "id", out string id))
        {
            return InvalidArgument("id", "a decision identifier");
        }

        if (!TryGetString(args, "status", out string status))
        {
            return InvalidArgument("status", "a target status string");
        }

        await EnsureLoadedAsync(ct).ConfigureAwait(false);
        SetDecisionStatusResult result = await service.SetStatusAsync(id, status, GetOptionalString(args, "reviewer"), GetOptionalString(args, "notes"), GetBool(args, "dryRun"), ct).ConfigureAwait(false);
        return ToolResult.Ok(new
        {
            id = result.Id,
            previousStatus = result.PreviousStatus,
            newStatus = result.NewStatus,
            reviewId = result.ReviewId,
            dryRun = result.DryRun,
        });
    }
}

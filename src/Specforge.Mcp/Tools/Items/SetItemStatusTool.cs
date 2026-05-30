using System.Text.Json;

using Specforge.Core.Configuration;
using Specforge.Core.Documents;
using Specforge.Mcp.Hosting;

namespace Specforge.Mcp.Tools.Items;

/// <summary><c>set_item_status</c> (DEC-007): validate + apply a lifecycle transition; update file + ART row (items.md) + history (+ REV on approval).</summary>
public sealed class SetItemStatusTool(ConfigLoader loader, SessionState session, SpecforgeWorkingDirectory workingDirectory, ItemService service)
    : SpecforgeToolBase(loader, session, workingDirectory)
{
    private static readonly JsonElement Schema = JsonSerializer.Deserialize<JsonElement>(
        """
        {
          "type": "object",
          "description": "Change an item's status (validated against the lifecycle state machine). Updates the file, the ART-ITEM row, and appends history; on Approved with reviewer+notes also appends a REV row. Does not enforce required-section completeness (that is validate's job).",
          "required": ["id", "status"],
          "properties": {
            "id": { "type": "string", "description": "Item identifier (bare or qualified)." },
            "status": { "type": "string", "description": "Target status.", "examples": ["Draft for user review", "Approved", "Superseded", "Withdrawn"] },
            "reviewer": { "type": "string", "description": "Reviewer name; with notes on Approved, appends a REV row." },
            "notes": { "type": "string", "description": "Review notes; with reviewer on Approved, appends a REV row." },
            "dryRun": { "type": "boolean", "default": false }
          },
          "additionalProperties": false
        }
        """);

    public override string Name => "set_item_status";

    public override JsonElement InputSchema => Schema;

    public override async Task<ToolResult> InvokeAsync(JsonElement args, CancellationToken ct)
    {
        if (!TryGetString(args, "id", out string id))
        {
            return InvalidArgument("id", "an item identifier");
        }

        if (!TryGetString(args, "status", out string status))
        {
            return InvalidArgument("status", "a target status string");
        }

        await EnsureLoadedAsync(ct).ConfigureAwait(false);
        SetItemStatusResult result = await service.SetStatusAsync(id, status, GetOptionalString(args, "reviewer"), GetOptionalString(args, "notes"), GetBool(args, "dryRun"), ct).ConfigureAwait(false);
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

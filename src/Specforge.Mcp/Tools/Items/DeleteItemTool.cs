using System.Text.Json;

using Specforge.Core.Configuration;
using Specforge.Core.Documents;
using Specforge.Mcp.Hosting;

namespace Specforge.Mcp.Tools.Items;

/// <summary><c>delete_item</c> (DEC-007 Delete Semantics): pre-Approved-only; removes file + ART-ITEM row + cascades REV rows + appends Deleted history.</summary>
public sealed class DeleteItemTool(ConfigLoader loader, SessionState session, SpecforgeWorkingDirectory workingDirectory, ItemService service)
    : SpecforgeToolBase(loader, session, workingDirectory)
{
    private static readonly JsonElement Schema = JsonSerializer.Deserialize<JsonElement>(
        """
        {
          "type": "object",
          "description": "Delete a pre-Approved item: removes the file, the ART-ITEM-NNN row, and cascades to its REV rows; appends a Deleted history event. The number is tombstoned. Requires confirm:true.",
          "required": ["id", "confirm"],
          "properties": {
            "id": { "type": "string", "description": "Item identifier (bare or qualified)." },
            "confirm": { "type": "boolean", "description": "Must be true to perform the delete." },
            "dryRun": { "type": "boolean", "default": false, "description": "Preview the cascade without writing." }
          },
          "additionalProperties": false
        }
        """);

    public override string Name => "delete_item";

    public override JsonElement InputSchema => Schema;

    public override async Task<ToolResult> InvokeAsync(JsonElement args, CancellationToken ct)
    {
        if (!TryGetString(args, "id", out string id))
        {
            return InvalidArgument("id", "an item identifier");
        }

        await EnsureLoadedAsync(ct).ConfigureAwait(false);
        DeleteItemResult result = await service.DeleteAsync(id, GetBool(args, "confirm"), GetBool(args, "dryRun"), ct).ConfigureAwait(false);
        return ToolResult.Ok(new
        {
            id = result.Id,
            itemPath = result.ItemPath,
            artifactId = result.ArtifactId,
            removedReviewRows = result.RemovedReviewRows,
            dryRun = result.DryRun,
        });
    }
}

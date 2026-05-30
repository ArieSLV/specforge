using System.Text.Json;

using Specforge.Core.Configuration;
using Specforge.Core.Documents;
using Specforge.Mcp.Hosting;

namespace Specforge.Mcp.Tools.Items;

/// <summary><c>get_item</c> (DEC-007): return one item's parsed structure + raw markdown + missingRequiredSections.</summary>
public sealed class GetItemTool(ConfigLoader loader, SessionState session, SpecforgeWorkingDirectory workingDirectory, ItemService service)
    : SpecforgeToolBase(loader, session, workingDirectory)
{
    private static readonly JsonElement Schema = JsonSerializer.Deserialize<JsonElement>(
        """
        {
          "type": "object",
          "description": "Return one item spec by id (bare ITEM-NNN or qualified <package>/ITEM-NNN). Returns metadata + section map + missingRequiredSections + raw markdown.",
          "required": ["id"],
          "properties": { "id": { "type": "string", "pattern": "^([a-z0-9._-]+/)?[A-Z]{2,6}-[0-9]{3}$", "description": "Item identifier." } },
          "additionalProperties": false
        }
        """);

    public override string Name => "get_item";

    public override JsonElement InputSchema => Schema;

    public override async Task<ToolResult> InvokeAsync(JsonElement args, CancellationToken ct)
    {
        if (!TryGetString(args, "id", out string id))
        {
            return InvalidArgument("id", "an item identifier");
        }

        await EnsureLoadedAsync(ct).ConfigureAwait(false);
        ItemDocument document = await service.GetAsync(id, ct).ConfigureAwait(false);
        return ToolResult.Ok(new
        {
            id = document.Id,
            title = document.Title,
            status = document.Status,
            reviewOwner = document.ReviewOwner,
            dependsOn = document.DependsOn,
            updatesLedgerRows = document.UpdatesLedgerRows,
            sections = document.Sections,
            missingRequiredSections = document.MissingRequiredSections,
            rawMarkdown = document.RawMarkdown,
            path = document.AbsolutePath,
        });
    }
}

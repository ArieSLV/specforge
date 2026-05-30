using System.Text.Json;

using Specforge.Core.Configuration;
using Specforge.Core.Documents;
using Specforge.Mcp.Hosting;

namespace Specforge.Mcp.Tools.Items;

/// <summary><c>list_items</c> (DEC-007): enumerate item specs in the active package, optional status filter.</summary>
public sealed class ListItemsTool(ConfigLoader loader, SessionState session, SpecforgeWorkingDirectory workingDirectory, ItemService service)
    : SpecforgeToolBase(loader, session, workingDirectory)
{
    private static readonly JsonElement Schema = JsonSerializer.Deserialize<JsonElement>(
        """
        {
          "type": "object",
          "description": "Enumerate item specs in the active package. Read-only.",
          "properties": { "status": { "type": "string", "description": "Optional exact status to filter by." } },
          "additionalProperties": false
        }
        """);

    public override string Name => "list_items";

    public override JsonElement InputSchema => Schema;

    public override async Task<ToolResult> InvokeAsync(JsonElement args, CancellationToken ct)
    {
        await EnsureLoadedAsync(ct).ConfigureAwait(false);
        IReadOnlyList<ItemSummary> items = await service.ListAsync(GetOptionalString(args, "status"), ct).ConfigureAwait(false);
        return ToolResult.Ok(new { items = items.Select(i => new { id = i.Id, title = i.Title, status = i.Status, path = i.Path }) });
    }
}

using System.Text.Json;

using Specforge.Core.Configuration;
using Specforge.Core.Documents;
using Specforge.Mcp.Hosting;

namespace Specforge.Mcp.Tools.Items;

/// <summary><c>create_item</c> (DEC-007): allocate ITEM-NNN, write the file, add the ART-ITEM-NNN row (items.md), append history.</summary>
public sealed class CreateItemTool(ConfigLoader loader, SessionState session, SpecforgeWorkingDirectory workingDirectory, ItemService service)
    : SpecforgeToolBase(loader, session, workingDirectory)
{
    private static readonly JsonElement Schema = JsonSerializer.Deserialize<JsonElement>(
        """
        {
          "type": "object",
          "description": "Create an item spec in the active package: allocates the next ITEM-NNN, writes the file from the item template, adds the ART-ITEM-NNN row to items.md, and appends a Created history event.",
          "required": ["title"],
          "properties": {
            "title": { "type": "string", "minLength": 1, "maxLength": 120, "description": "Plain title; slugified for the filename." },
            "status": { "type": "string", "enum": ["Placeholder", "Draft", "Draft for user review"], "default": "Draft", "description": "Initial status; Approved and later are rejected." },
            "dependsOn": { "type": "array", "items": { "type": "string" }, "description": "Identifiers this item depends on (validated for shape/kind; existence is not checked at create time)." },
            "dryRun": { "type": "boolean", "default": false }
          },
          "additionalProperties": false
        }
        """);

    public override string Name => "create_item";

    public override JsonElement InputSchema => Schema;

    public override async Task<ToolResult> InvokeAsync(JsonElement args, CancellationToken ct)
    {
        if (!TryGetString(args, "title", out string title))
        {
            return InvalidArgument("title", "a non-empty string");
        }

        IReadOnlyList<string>? dependsOn = null;
        if (args.ValueKind == JsonValueKind.Object && args.TryGetProperty("dependsOn", out JsonElement deps) && deps.ValueKind == JsonValueKind.Array)
        {
            dependsOn = [.. deps.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()!)];
        }

        await EnsureLoadedAsync(ct).ConfigureAwait(false);
        CreateItemResult result = await service.CreateAsync(title, GetOptionalString(args, "status"), dependsOn, GetBool(args, "dryRun"), ct).ConfigureAwait(false);
        return ToolResult.Ok(new
        {
            id = result.Id,
            slug = result.Slug,
            itemPath = result.ItemPath,
            artifactId = result.ArtifactId,
            dryRun = result.DryRun,
            plannedWrites = result.PlannedWrites,
        });
    }
}

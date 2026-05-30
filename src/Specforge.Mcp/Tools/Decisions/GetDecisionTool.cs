using System.Text.Json;

using Specforge.Core.Configuration;
using Specforge.Core.Documents;
using Specforge.Mcp.Hosting;

namespace Specforge.Mcp.Tools.Decisions;

/// <summary><c>get_decision</c> (DEC-007): return one decision's parsed structure + raw markdown.</summary>
public sealed class GetDecisionTool(ConfigLoader loader, SessionState session, SpecforgeWorkingDirectory workingDirectory, DecisionService service)
    : SpecforgeToolBase(loader, session, workingDirectory)
{
    private static readonly JsonElement Schema = JsonSerializer.Deserialize<JsonElement>(
        """
        {
          "type": "object",
          "description": "Return one decision record by id (bare DEC-NNN or qualified <package>/DEC-NNN). Returns metadata + section map + raw markdown.",
          "required": ["id"],
          "properties": { "id": { "type": "string", "pattern": "^([a-z0-9._-]+/)?[A-Z]{2,6}-[0-9]{3}$", "description": "Decision identifier." } },
          "additionalProperties": false
        }
        """);

    public override string Name => "get_decision";

    public override JsonElement InputSchema => Schema;

    public override async Task<ToolResult> InvokeAsync(JsonElement args, CancellationToken ct)
    {
        if (!TryGetString(args, "id", out string id))
        {
            return InvalidArgument("id", "a decision identifier");
        }

        await EnsureLoadedAsync(ct).ConfigureAwait(false);
        DecisionDocument document = await service.GetAsync(id, ct).ConfigureAwait(false);
        return ToolResult.Ok(new
        {
            id = document.Id,
            title = document.Title,
            status = document.Status,
            date = document.Date,
            owner = document.Owner,
            reviewOwner = document.ReviewOwner,
            supersedes = document.Supersedes,
            covers = document.Covers,
            amends = document.Amends,
            sections = document.Sections,
            rawMarkdown = document.RawMarkdown,
            path = document.AbsolutePath,
        });
    }
}

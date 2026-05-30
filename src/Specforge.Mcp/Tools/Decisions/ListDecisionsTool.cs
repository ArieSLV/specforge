using System.Text.Json;

using Specforge.Core.Configuration;
using Specforge.Core.Documents;
using Specforge.Mcp.Hosting;

namespace Specforge.Mcp.Tools.Decisions;

/// <summary><c>list_decisions</c> (DEC-007): enumerate decisions in the active package, optional status filter.</summary>
public sealed class ListDecisionsTool(ConfigLoader loader, SessionState session, SpecforgeWorkingDirectory workingDirectory, DecisionService service)
    : SpecforgeToolBase(loader, session, workingDirectory)
{
    private static readonly JsonElement Schema = JsonSerializer.Deserialize<JsonElement>(
        """
        {
          "type": "object",
          "description": "Enumerate decision records in the active package. Read-only.",
          "properties": { "status": { "type": "string", "description": "Optional exact status to filter by (e.g. Approved)." } },
          "additionalProperties": false
        }
        """);

    public override string Name => "list_decisions";

    public override JsonElement InputSchema => Schema;

    public override async Task<ToolResult> InvokeAsync(JsonElement args, CancellationToken ct)
    {
        await EnsureLoadedAsync(ct).ConfigureAwait(false);
        string? status = GetOptionalString(args, "status");
        IReadOnlyList<DecisionSummary> decisions = await service.ListAsync(status, ct).ConfigureAwait(false);
        return ToolResult.Ok(new { decisions = decisions.Select(d => new { id = d.Id, title = d.Title, status = d.Status, path = d.Path }) });
    }
}

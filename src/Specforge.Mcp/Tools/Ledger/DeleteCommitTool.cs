using System.Text.Json;

using Specforge.Core.Configuration;
using Specforge.Core.Exceptions;
using Specforge.Core.Identifiers;
using Specforge.Core.Ledger;
using Specforge.Core.Validation;
using Specforge.Mcp.Hosting;

namespace Specforge.Mcp.Tools.Ledger;

/// <summary><c>delete_commit</c> (DEC-007 Delete Semantics): remove a CMT row + append a Deleted history event. No lifecycle gate.</summary>
public sealed class DeleteCommitTool(
    ConfigLoader loader,
    SessionState session,
    SpecforgeWorkingDirectory workingDirectory,
    ICommitLedgerService commits,
    IHistoryLedgerService history)
    : SpecforgeToolBase(loader, session, workingDirectory)
{
    private static readonly JsonElement Schema = JsonSerializer.Deserialize<JsonElement>(
        """
        {
          "type": "object",
          "description": "Delete a commit row by its sequential id (CMT-NNN) and append a Deleted history event. Commits have no lifecycle state, so the only gate is confirm:true. The number is tombstoned (never reused); the next append_commit advances past it.",
          "required": ["id", "confirm"],
          "properties": {
            "id": { "type": "string", "description": "The commit id.", "examples": ["CMT-008"] },
            "confirm": { "type": "boolean", "description": "Must be true to perform the delete." },
            "dryRun": { "type": "boolean", "default": false, "description": "Preview the removal without writing." }
          },
          "additionalProperties": false
        }
        """);

    public override string Name => "delete_commit";

    public override JsonElement InputSchema => Schema;

    public override async Task<ToolResult> InvokeAsync(JsonElement args, CancellationToken ct)
    {
        if (!TryGetString(args, "id", out string id))
        {
            return InvalidArgument("id", "a commit id (e.g., CMT-008)");
        }

        if (!IdParser.TryParse(id, out ParsedIdentifier? parsed)
            || parsed is not AtomicIdentifier atomic
            || !string.Equals(atomic.Kind, "CMT", StringComparison.Ordinal))
        {
            return InvalidArgument("id", "a commit id (e.g., CMT-008)");
        }

        if (!GetBool(args, "confirm"))
        {
            return ConfirmRequired();
        }

        await EnsureLoadedAsync(ct).ConfigureAwait(false);

        CommitRow? row = await commits.FindByLedgerIdAsync(id, ct).ConfigureAwait(false);
        if (row is null)
        {
            throw new SpecforgeIdNotFoundException(id);
        }

        bool dryRun = GetBool(args, "dryRun");
        if (!dryRun)
        {
            await commits.RemoveAsync(id, ct).ConfigureAwait(false);
            await history.AppendAsync(row.Linked, "Deleted", TombstoneDetailFormat.Format(id, $"was {row.CommitSha}; deleted via delete_commit"), ct).ConfigureAwait(false);
        }

        return ToolResult.Ok(new
        {
            id,
            target = row.Linked,
            gitRef = row.CommitSha,
            dryRun,
            plannedWrites = new[]
            {
                $"{LedgerPaths.LedgerFile(Session, "commits.md")} (-{id})",
                $"{LedgerPaths.LedgerFile(Session, "history.md")} (+Deleted)",
            },
        });
    }
}

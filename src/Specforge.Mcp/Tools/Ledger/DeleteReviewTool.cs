using System.Text.Json;

using Specforge.Core.Configuration;
using Specforge.Core.Exceptions;
using Specforge.Core.Identifiers;
using Specforge.Core.Ledger;
using Specforge.Core.Validation;
using Specforge.Mcp.Hosting;

namespace Specforge.Mcp.Tools.Ledger;

/// <summary><c>delete_review</c> (DEC-007 Delete Semantics): remove a REV row + append a Deleted history event. No lifecycle gate.</summary>
public sealed class DeleteReviewTool(
    ConfigLoader loader,
    SessionState session,
    SpecforgeWorkingDirectory workingDirectory,
    IReviewLedgerService reviews,
    IHistoryLedgerService history)
    : SpecforgeToolBase(loader, session, workingDirectory)
{
    private static readonly JsonElement Schema = JsonSerializer.Deserialize<JsonElement>(
        """
        {
          "type": "object",
          "description": "Delete a review row by its composite id (REV-<target>-NNN) and append a Deleted history event. Reviews have no lifecycle state, so the only gate is confirm:true. The number is tombstoned (never reused); the next append_review for the same target advances past it.",
          "required": ["id", "confirm"],
          "properties": {
            "id": { "type": "string", "description": "The composite review id.", "examples": ["REV-DEC-001-002", "REV-ITEM-007-001"] },
            "confirm": { "type": "boolean", "description": "Must be true to perform the delete." },
            "dryRun": { "type": "boolean", "default": false, "description": "Preview the removal without writing." }
          },
          "additionalProperties": false
        }
        """);

    public override string Name => "delete_review";

    public override JsonElement InputSchema => Schema;

    public override async Task<ToolResult> InvokeAsync(JsonElement args, CancellationToken ct)
    {
        if (!TryGetString(args, "id", out string id))
        {
            return InvalidArgument("id", "a composite review id (e.g., REV-DEC-001-002)");
        }

        if (!IdParser.TryParse(id, out ParsedIdentifier? parsed) || parsed is not CompositeReviewIdentifier review)
        {
            return InvalidArgument("id", "a composite review id (e.g., REV-DEC-001-002)");
        }

        if (!GetBool(args, "confirm"))
        {
            return ConfirmRequired();
        }

        await EnsureLoadedAsync(ct).ConfigureAwait(false);

        string bareTarget = $"{review.Target.Kind}-{review.Target.Number:D3}";
        string artifactTarget = $"ART-{bareTarget}";

        // The review's Target column may be stored bare (runtime rows) or as the ART form (hand-authored
        // rows); accept either when verifying the composite id exists.
        IEnumerable<ReviewRow> candidates =
            (await reviews.FindByTargetAsync(bareTarget, ct).ConfigureAwait(false))
            .Concat(await reviews.FindByTargetAsync(artifactTarget, ct).ConfigureAwait(false));
        if (!candidates.Any(r => string.Equals(r.LedgerId, id, StringComparison.Ordinal)))
        {
            throw new SpecforgeIdNotFoundException(id);
        }

        bool dryRun = GetBool(args, "dryRun");
        if (!dryRun)
        {
            await reviews.RemoveAsync(id, ct).ConfigureAwait(false);
            await history.AppendAsync(artifactTarget, "Deleted", TombstoneDetailFormat.Format(id, "deleted via delete_review"), ct).ConfigureAwait(false);
        }

        return ToolResult.Ok(new
        {
            id,
            target = artifactTarget,
            dryRun,
            plannedWrites = new[]
            {
                $"{LedgerPaths.LedgerFile(Session, "reviews.md")} (-{id})",
                $"{LedgerPaths.LedgerFile(Session, "history.md")} (+Deleted)",
            },
        });
    }
}

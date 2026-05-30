using System.Text.Json;

using Specforge.Core.Configuration;
using Specforge.Core.Ledger;
using Specforge.Mcp.Hosting;

namespace Specforge.Mcp.Tools.Ledger;

/// <summary><c>append_review</c> (DEC-007): append a REV row for an existing decision/item + a Reviewed history event.</summary>
public sealed class AppendReviewTool(
    ConfigLoader loader,
    SessionState session,
    SpecforgeWorkingDirectory workingDirectory,
    IReviewLedgerService reviews,
    IArtifactLedgerService artifacts,
    IHistoryLedgerService history)
    : SpecforgeToolBase(loader, session, workingDirectory)
{
    private static readonly JsonElement Schema = JsonSerializer.Deserialize<JsonElement>(
        """
        {
          "type": "object",
          "description": "Append a review (REV-<target>-NNN) for an existing decision or item, plus a Reviewed history event. The target points at the artifact's ART-DEC-NNN / ART-ITEM-NNN row (the bare DEC-NNN / ITEM-NNN form is also accepted). The outcome is free-form.",
          "required": ["target", "reviewer", "outcome"],
          "properties": {
            "target": { "type": "string", "description": "The reviewed artifact id.", "examples": ["ART-DEC-001", "ART-ITEM-007", "ITEM-007"] },
            "reviewer": { "type": "string", "minLength": 1, "description": "Who performed the review.", "examples": ["User"] },
            "outcome": { "type": "string", "minLength": 1, "description": "Review outcome (free-form).", "examples": ["Approved", "Requested changes", "Refined", "Withdrawn"] },
            "notes": { "type": "string", "description": "Optional free-form review notes." },
            "dryRun": { "type": "boolean", "default": false, "description": "Return the planned writes without touching the filesystem." }
          },
          "additionalProperties": false
        }
        """);

    public override string Name => "append_review";

    public override JsonElement InputSchema => Schema;

    public override async Task<ToolResult> InvokeAsync(JsonElement args, CancellationToken ct)
    {
        if (!TryGetString(args, "target", out string target))
        {
            return InvalidArgument("target", "an existing decision/item id (e.g., ART-DEC-001 or ART-ITEM-007)");
        }

        if (!TryGetString(args, "reviewer", out string reviewer))
        {
            return InvalidArgument("reviewer", "a non-empty string");
        }

        if (!TryGetString(args, "outcome", out string outcome))
        {
            return InvalidArgument("outcome", "a non-empty string");
        }

        await EnsureLoadedAsync(ct).ConfigureAwait(false);

        string artifactId = ToArtifactId(target);
        string bareTarget = BareTarget(artifactId);
        if (await artifacts.FindByLedgerIdAsync(artifactId, ct).ConfigureAwait(false) is null)
        {
            return IdNotFound(target);
        }

        string? notes = GetOptionalString(args, "notes");
        bool dryRun = GetBool(args, "dryRun");
        string? reviewId = null;
        if (!dryRun)
        {
            reviewId = await reviews.AppendAsync(bareTarget, reviewer, outcome, notes, ct).ConfigureAwait(false);
            await history.AppendAsync(artifactId, "Reviewed", outcome, ct).ConfigureAwait(false);
        }

        return ToolResult.Ok(new
        {
            target = artifactId,
            reviewId,
            reviewer,
            outcome,
            dryRun,
            plannedWrites = new[]
            {
                $"{LedgerPaths.LedgerFile(Session, "reviews.md")} (+REV-{bareTarget}-NNN)",
                $"{LedgerPaths.LedgerFile(Session, "history.md")} (+Reviewed)",
            },
        });
    }
}

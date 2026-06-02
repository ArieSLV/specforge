using System.Globalization;
using System.Text.Json;

using Specforge.Core.Configuration;
using Specforge.Core.Exceptions;
using Specforge.Core.Ledger;
using Specforge.Mcp.Hosting;

namespace Specforge.Mcp.Tools.Ledger;

/// <summary><c>append_commit</c> (DEC-007): append a CMT row + an Implemented history event; first call promotes the commits ledger.</summary>
public sealed class AppendCommitTool(
    ConfigLoader loader,
    SessionState session,
    SpecforgeWorkingDirectory workingDirectory,
    ICommitLedgerService commits,
    IArtifactLedgerService artifacts,
    IHistoryLedgerService history)
    : SpecforgeToolBase(loader, session, workingDirectory)
{
    private static readonly JsonElement Schema = JsonSerializer.Deserialize<JsonElement>(
        """
        {
          "type": "object",
          "description": "Record an implementation/documentation commit: allocates the next CMT-NNN, writes a row to commits.md linking the commit to an existing artifact, and appends an Implemented history event. The first append per package promotes the commits ledger from Placeholder to Draft. specforge does not verify the git ref exists.",
          "required": ["target", "gitRef", "detail"],
          "properties": {
            "target": { "type": "string", "description": "The artifact the commit implements/documents.", "examples": ["ART-ITEM-009", "ITEM-009", "ART-DEC-007"] },
            "gitRef": { "type": "string", "minLength": 1, "maxLength": 64, "description": "A commit SHA, tag, or branch ref (no whitespace).", "examples": ["986efba"] },
            "detail": { "type": "string", "minLength": 1, "description": "Single-line summary of the commit's effect on the target." },
            "date": { "type": "string", "description": "Optional ISO date (yyyy-MM-dd); defaults to today (UTC). Useful for backfilling historic commits.", "examples": ["2026-05-30"] },
            "dryRun": { "type": "boolean", "default": false, "description": "Return the planned writes (including the allocated CMT id) without touching the filesystem." }
          },
          "additionalProperties": false
        }
        """);

    public override string Name => "append_commit";

    public override JsonElement InputSchema => Schema;

    public override async Task<ToolResult> InvokeAsync(JsonElement args, CancellationToken ct)
    {
        if (!TryGetString(args, "target", out string target))
        {
            return InvalidArgument("target", "an existing artifact id (e.g., ART-ITEM-009)");
        }

        if (!TryGetString(args, "gitRef", out string gitRef))
        {
            return InvalidArgument("gitRef", "a non-empty string");
        }

        if (gitRef.Length > 64 || gitRef.AsSpan().ContainsAny(WhitespaceChars))
        {
            return InvalidArgument("gitRef", "a non-whitespace ref of at most 64 characters");
        }

        if (!TryGetString(args, "detail", out string detail))
        {
            return InvalidArgument("detail", "a non-empty string");
        }

        string? date = GetOptionalString(args, "date");
        if (date is not null && !DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            return InvalidArgument("date", "an ISO date (yyyy-MM-dd)");
        }

        await EnsureLoadedAsync(ct).ConfigureAwait(false);

        string artifactId = ToArtifactId(target);
        if (await artifacts.FindByLedgerIdAsync(artifactId, ct).ConfigureAwait(false) is null)
        {
            throw new SpecforgeIdNotFoundException(target);
        }

        bool dryRun = GetBool(args, "dryRun");
        CommitAppendResult result = await commits.AppendAsync(artifactId, gitRef, detail, date, dryRun, ct).ConfigureAwait(false);

        if (!dryRun)
        {
            await history.AppendAsync(artifactId, "Implemented", gitRef, ct).ConfigureAwait(false);
            if (result.PromotedCommitsLedger)
            {
                await history.AppendAsync(CommitLedgerService.CommitsLedgerArtifactId, "Draft", "commits ledger promoted Placeholder → Draft (first append_commit)", ct).ConfigureAwait(false);
            }
        }

        List<string> plannedWrites =
        [
            $"{LedgerPaths.LedgerFile(Session, "commits.md")} (+{result.Id})",
            $"{LedgerPaths.LedgerFile(Session, "history.md")} (+Implemented)",
        ];
        if (result.PromotedCommitsLedger)
        {
            plannedWrites.Add($"{LedgerPaths.LedgerFile(Session, "artifacts.md")} ({CommitLedgerService.CommitsLedgerArtifactId} Placeholder → Draft)");
            plannedWrites.Add($"{LedgerPaths.LedgerFile(Session, "history.md")} (+Draft promotion event)");
        }

        return ToolResult.Ok(new
        {
            id = result.Id,
            target = artifactId,
            gitRef,
            promotedCommitsLedger = result.PromotedCommitsLedger,
            dryRun,
            plannedWrites,
        });
    }

    private static readonly char[] WhitespaceChars = [' ', '\t', '\n', '\r', '\f', '\v'];
}

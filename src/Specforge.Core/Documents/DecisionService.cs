using System.Globalization;

using Specforge.Core.Configuration;
using Specforge.Core.Exceptions;
using Specforge.Core.Identifiers;
using Specforge.Core.Ledger;
using Specforge.Core.Lifecycle;

namespace Specforge.Core.Documents;

/// <summary>
/// Spec-graph operations on decision records (ITEM-007): list / get / create / set-status / delete,
/// each maintaining the cross-file invariants (decision file + artifacts row + history + reviews).
/// </summary>
public sealed class DecisionService(
    SessionState session,
    IIdAllocator allocator,
    IArtifactLedgerService artifacts,
    IHistoryLedgerService history,
    IReviewLedgerService reviews,
    LifecycleStateMachine lifecycle,
    DecisionFileWriter writer)
{
    private const string DefaultOwner = "AI assistant (drafted)";
    private const string DefaultReviewOwner = "User";

    public async Task<IReadOnlyList<DecisionSummary>> ListAsync(string? statusFilter, CancellationToken ct)
    {
        string directory = LedgerPaths.DecisionsDirectory(session);
        if (!Directory.Exists(directory))
        {
            return [];
        }

        List<DecisionSummary> summaries = [];
        foreach (string file in Directory.EnumerateFiles(directory, "DEC-*.md").OrderBy(f => f, StringComparer.Ordinal))
        {
            DecisionDocument? document = await TryParseAsync(file, ct).ConfigureAwait(false);
            if (document is null)
            {
                continue;
            }

            if (statusFilter is null || string.Equals(document.Status, statusFilter, StringComparison.Ordinal))
            {
                summaries.Add(new DecisionSummary(document.Id, document.Title, document.Status, file));
            }
        }

        return summaries;
    }

    public async Task<DecisionDocument> GetAsync(string id, CancellationToken ct)
    {
        string bareId = BareId(id);
        string path = ResolvePath(bareId) ?? throw NotFound(id);
        string text = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
        return DecisionDocumentParser.Parse(text, path);
    }

    public async Task<CreateDecisionResult> CreateAsync(string title, string? status, bool dryRun, CancellationToken ct)
    {
        SpecforgePackageConfig package = session.RequireActivePackage();
        string effectiveStatus = status ?? LifecycleState.Draft;
        if (effectiveStatus is not (LifecycleState.Draft or LifecycleState.DraftForUserReview))
        {
            throw new SpecforgeInvalidArgumentException("status", $"{LifecycleState.Draft} | {LifecycleState.DraftForUserReview}", "a new decision cannot start as Approved or later", effectiveStatus);
        }

        string slug = TitleSlug.FromTitle(title);
        int number = await allocator.NextNumberAsync("DEC", package.Name, ct).ConfigureAwait(false);
        string id = $"DEC-{number:D3}";
        string directory = LedgerPaths.DecisionsDirectory(session);

        if (Directory.Exists(directory) && Directory.EnumerateFiles(directory, $"DEC-*-{slug}.md").Any())
        {
            throw new SpecforgeInvalidArgumentException("title", "a title whose slug is unique within the package", "amend the title to disambiguate", slug);
        }

        string decisionPath = Path.Combine(directory, $"{id}-{slug}.md");
        string artifactId = $"ART-{id}";
        string date = Today();
        IReadOnlyList<string> plannedWrites = [decisionPath, $"{LedgerPaths.LedgerFile(session, "artifacts.md")} (+{artifactId})", $"{LedgerPaths.LedgerFile(session, "history.md")} (+Created)"];

        if (dryRun)
        {
            return new CreateDecisionResult(id, slug, decisionPath, artifactId, true, plannedWrites);
        }

        Directory.CreateDirectory(directory);
        string content = writer.Render(id, slug, title, effectiveStatus, date, DefaultOwner, DefaultReviewOwner);
        await File.WriteAllTextAsync(decisionPath, content, ct).ConfigureAwait(false);

        await artifacts.AppendAsync(new ArtifactRow
        {
            LedgerId = artifactId,
            Artifact = ToRelative(decisionPath),
            Status = effectiveStatus,
            Owner = DefaultReviewOwner,
            LastUpdate = $"{date}: created via create_decision",
            NextAction = "Author body sections",
        }, ct).ConfigureAwait(false);

        await history.AppendAsync(artifactId, "Created", $"create_decision: {title}", ct).ConfigureAwait(false);

        return new CreateDecisionResult(id, slug, decisionPath, artifactId, false, plannedWrites);
    }

    public async Task<SetDecisionStatusResult> SetStatusAsync(string id, string status, string? reviewer, string? notes, bool dryRun, CancellationToken ct)
    {
        string bareId = BareId(id);
        string path = ResolvePath(bareId) ?? throw NotFound(id);
        string text = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
        DecisionDocument document = DecisionDocumentParser.Parse(text, path);

        if (!lifecycle.IsValidTransition(document.Status, status))
        {
            string allowed = string.Join(", ", lifecycle.AllowedTransitions(document.Status));
            throw new SpecforgeInvalidArgumentException("status", $"a valid transition from '{document.Status}' (allowed: {(allowed.Length == 0 ? "none" : allowed)})", "choose an allowed next status", status);
        }

        string artifactId = $"ART-{bareId}";
        string? reviewId = null;

        if (!dryRun)
        {
            await File.WriteAllTextAsync(path, ReplaceStatusLine(text, status), ct).ConfigureAwait(false);
            await artifacts.UpdateAsync(artifactId, row =>
            {
                row.Status = status;
                row.LastUpdate = $"{Today()}: set_decision_status → {status}";
            }, ct).ConfigureAwait(false);

            string detail = reviewer is null ? $"set_decision_status → {status}" : $"set_decision_status → {status} by {reviewer}{(notes is null ? string.Empty : $": {notes}")}";
            await history.AppendAsync(artifactId, status, detail, ct).ConfigureAwait(false);

            if (string.Equals(status, LifecycleState.Approved, StringComparison.Ordinal) && reviewer is not null && notes is not null)
            {
                reviewId = await reviews.AppendAsync(bareId, reviewer, "Approved", notes, ct).ConfigureAwait(false);
            }
        }

        return new SetDecisionStatusResult(bareId, document.Status, status, reviewId, dryRun);
    }

    public async Task<DeleteDecisionResult> DeleteAsync(string id, bool confirm, bool dryRun, CancellationToken ct)
    {
        if (!confirm)
        {
            throw new SpecforgeInvalidArgumentException("confirm", "true", "pass confirm=true to perform the delete; pass dryRun=true to preview");
        }

        string bareId = BareId(id);
        string path = ResolvePath(bareId) ?? throw NotFound(id);
        string text = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
        DecisionDocument document = DecisionDocumentParser.Parse(text, path);

        if (!LifecycleState.PreApproved.Contains(document.Status, StringComparer.Ordinal))
        {
            throw new SpecforgeDeleteForbiddenException(bareId, document.Status, LifecycleState.PreApproved);
        }

        string artifactId = $"ART-{bareId}";
        IReadOnlyList<ReviewRow> targetingReviews = await reviews.FindByTargetAsync(bareId, ct).ConfigureAwait(false);

        if (dryRun)
        {
            return new DeleteDecisionResult(bareId, path, artifactId, targetingReviews.Count, true);
        }

        File.Delete(path);
        await artifacts.RemoveAsync(artifactId, ct).ConfigureAwait(false);
        int removed = await reviews.RemoveByTargetAsync(bareId, ct).ConfigureAwait(false);
        await history.AppendAsync(artifactId, "Deleted", $"delete_decision: tombstoned {bareId}; removed {removed} review row(s); number not reused", ct).ConfigureAwait(false);

        return new DeleteDecisionResult(bareId, path, artifactId, removed, false);
    }

    private static string Today() => DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string ReplaceStatusLine(string text, string newStatus)
    {
        string[] lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].TrimStart().StartsWith("Status:", StringComparison.Ordinal))
            {
                lines[i] = $"Status: {newStatus}";
                break;
            }
        }

        return string.Join('\n', lines);
    }

    private static string BareId(string id)
    {
        int slash = id.IndexOf('/', StringComparison.Ordinal);
        return slash >= 0 ? id[(slash + 1)..] : id;
    }

    private string? ResolvePath(string bareId)
    {
        string directory = LedgerPaths.DecisionsDirectory(session);
        if (!Directory.Exists(directory))
        {
            return null;
        }

        return Directory.EnumerateFiles(directory, $"{bareId}-*.md").OrderBy(f => f, StringComparer.Ordinal).FirstOrDefault()
            ?? (File.Exists(Path.Combine(directory, $"{bareId}.md")) ? Path.Combine(directory, $"{bareId}.md") : null);
    }

    private string ToRelative(string absolutePath)
    {
        string configDirectory = Path.GetDirectoryName(session.Config!.ConfigPath) ?? ".";
        return Path.GetRelativePath(configDirectory, absolutePath).Replace('\\', '/');
    }

    private async Task<DecisionDocument?> TryParseAsync(string file, CancellationToken ct)
    {
        try
        {
            return DecisionDocumentParser.Parse(await File.ReadAllTextAsync(file, ct).ConfigureAwait(false), file);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static SpecforgeInvalidArgumentException NotFound(string id) =>
        new("id", "an existing decision identifier", "check the id with list_decisions", id);
}

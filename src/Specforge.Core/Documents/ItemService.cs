using System.Globalization;

using Specforge.Core.Configuration;
using Specforge.Core.Exceptions;
using Specforge.Core.Identifiers;
using Specforge.Core.Ledger;
using Specforge.Core.Lifecycle;

namespace Specforge.Core.Documents;

/// <summary>
/// Spec-graph operations on item specs (ITEM-008): list / get / create / set-status / delete. Mirrors
/// <see cref="DecisionService"/>; <c>ART-ITEM-*</c> rows route to <c>ledger/items.md</c> via the
/// artifact ledger service.
/// </summary>
public sealed class ItemService(
    SessionState session,
    IIdAllocator allocator,
    IArtifactLedgerService artifacts,
    IHistoryLedgerService history,
    IReviewLedgerService reviews,
    LifecycleStateMachine lifecycle,
    ItemFileWriter writer)
{
    public async Task<IReadOnlyList<ItemSummary>> ListAsync(string? statusFilter, CancellationToken ct)
    {
        string directory = LedgerPaths.ItemsDirectory(session);
        if (!Directory.Exists(directory))
        {
            return [];
        }

        List<ItemSummary> summaries = [];
        foreach (string file in Directory.EnumerateFiles(directory, "ITEM-*.md").OrderBy(f => f, StringComparer.Ordinal))
        {
            ItemDocument? document = await TryParseAsync(file, ct).ConfigureAwait(false);
            if (document is null)
            {
                continue;
            }

            if (statusFilter is null || string.Equals(document.Status, statusFilter, StringComparison.Ordinal))
            {
                summaries.Add(new ItemSummary(document.Id, document.Title, document.Status, file));
            }
        }

        return summaries;
    }

    public async Task<ItemDocument> GetAsync(string id, CancellationToken ct)
    {
        string path = ResolvePath(BareId(id)) ?? throw NotFound(id);
        return ItemDocumentParser.Parse(await File.ReadAllTextAsync(path, ct).ConfigureAwait(false), path);
    }

    public async Task<CreateItemResult> CreateAsync(string title, string? status, IReadOnlyList<string>? dependsOn, bool dryRun, CancellationToken ct)
    {
        SpecforgePackageConfig package = session.RequireActivePackage();
        string effectiveStatus = status ?? LifecycleState.Draft;
        if (effectiveStatus is not (LifecycleState.Placeholder or LifecycleState.Draft or LifecycleState.DraftForUserReview))
        {
            throw new SpecforgeInvalidArgumentException("status", $"{LifecycleState.Placeholder} | {LifecycleState.Draft} | {LifecycleState.DraftForUserReview}", "a new item cannot start as Approved or later", effectiveStatus);
        }

        if (dependsOn is { Count: > 0 })
        {
            KindRegistry registry = KindRegistry.FromConfig(session.Config!, package.Name);
            ValidationContext context = ValidationContext.FromConfig(session.Config!);
            foreach (string dependency in dependsOn)
            {
                IdValidator.Validate(dependency, registry, context);
            }
        }

        string slug = TitleSlug.FromTitle(title);
        int number = await allocator.NextNumberAsync("ITEM", package.Name, ct).ConfigureAwait(false);
        string id = $"ITEM-{number:D3}";
        string directory = LedgerPaths.ItemsDirectory(session);

        if (Directory.Exists(directory) && Directory.EnumerateFiles(directory, $"ITEM-*-{slug}.md").Any())
        {
            throw new SpecforgeInvalidArgumentException("title", "a title whose slug is unique within the package", "amend the title to disambiguate", slug);
        }

        string itemPath = Path.Combine(directory, $"{id}-{slug}.md");
        string artifactId = $"ART-{id}";
        IReadOnlyList<string> plannedWrites = [itemPath, $"{LedgerPaths.LedgerFile(session, "items.md")} (+{artifactId})", $"{LedgerPaths.LedgerFile(session, "history.md")} (+Created)"];

        if (dryRun)
        {
            return new CreateItemResult(id, slug, itemPath, artifactId, true, plannedWrites);
        }

        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(itemPath, writer.Render(id, slug, title, effectiveStatus, dependsOn), ct).ConfigureAwait(false);

        await artifacts.AppendAsync(new ArtifactRow
        {
            LedgerId = artifactId,
            Artifact = ToRelative(itemPath),
            Status = effectiveStatus,
            DependsOn = dependsOn is { Count: > 0 } ? string.Join(", ", dependsOn) : string.Empty,
            Owner = "User",
            LastUpdate = $"{Today()}: created via create_item",
            NextAction = "Author body sections",
        }, ct).ConfigureAwait(false);

        await history.AppendAsync(artifactId, "Created", $"create_item: {title}", ct).ConfigureAwait(false);

        return new CreateItemResult(id, slug, itemPath, artifactId, false, plannedWrites);
    }

    public async Task<SetItemStatusResult> SetStatusAsync(string id, string status, string? reviewer, string? notes, bool dryRun, CancellationToken ct)
    {
        string bareId = BareId(id);
        string path = ResolvePath(bareId) ?? throw NotFound(id);
        string text = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
        ItemDocument document = ItemDocumentParser.Parse(text, path);

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
                row.LastUpdate = $"{Today()}: set_item_status → {status}";
            }, ct).ConfigureAwait(false);

            string detail = reviewer is null ? $"set_item_status → {status}" : $"set_item_status → {status} by {reviewer}{(notes is null ? string.Empty : $": {notes}")}";
            await history.AppendAsync(artifactId, status, detail, ct).ConfigureAwait(false);

            if (string.Equals(status, LifecycleState.Approved, StringComparison.Ordinal) && reviewer is not null && notes is not null)
            {
                reviewId = await reviews.AppendAsync(bareId, reviewer, "Approved", notes, ct).ConfigureAwait(false);
            }
        }

        return new SetItemStatusResult(bareId, document.Status, status, reviewId, dryRun);
    }

    public async Task<DeleteItemResult> DeleteAsync(string id, bool confirm, bool dryRun, CancellationToken ct)
    {
        if (!confirm)
        {
            throw new SpecforgeInvalidArgumentException("confirm", "true", "pass confirm=true to perform the delete; pass dryRun=true to preview");
        }

        string bareId = BareId(id);
        string path = ResolvePath(bareId) ?? throw NotFound(id);
        ItemDocument document = ItemDocumentParser.Parse(await File.ReadAllTextAsync(path, ct).ConfigureAwait(false), path);

        if (!LifecycleState.PreApproved.Contains(document.Status, StringComparer.Ordinal))
        {
            throw new SpecforgeDeleteForbiddenException(bareId, document.Status, LifecycleState.PreApproved);
        }

        string artifactId = $"ART-{bareId}";
        IReadOnlyList<ReviewRow> targeting = await reviews.FindByTargetAsync(bareId, ct).ConfigureAwait(false);

        if (dryRun)
        {
            return new DeleteItemResult(bareId, path, artifactId, targeting.Count, true);
        }

        File.Delete(path);
        await artifacts.RemoveAsync(artifactId, ct).ConfigureAwait(false);
        int removed = await reviews.RemoveByTargetAsync(bareId, ct).ConfigureAwait(false);
        await history.AppendAsync(artifactId, "Deleted", $"delete_item: tombstoned {bareId}; removed {removed} review row(s); number not reused", ct).ConfigureAwait(false);

        return new DeleteItemResult(bareId, path, artifactId, removed, false);
    }

    private static string Today() => DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string BareId(string id)
    {
        int slash = id.IndexOf('/', StringComparison.Ordinal);
        return slash >= 0 ? id[(slash + 1)..] : id;
    }

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

    private string? ResolvePath(string bareId)
    {
        string directory = LedgerPaths.ItemsDirectory(session);
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

    private async Task<ItemDocument?> TryParseAsync(string file, CancellationToken ct)
    {
        try
        {
            return ItemDocumentParser.Parse(await File.ReadAllTextAsync(file, ct).ConfigureAwait(false), file);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static SpecforgeInvalidArgumentException NotFound(string id) =>
        new("id", "an existing item identifier", "check the id with list_items", id);
}

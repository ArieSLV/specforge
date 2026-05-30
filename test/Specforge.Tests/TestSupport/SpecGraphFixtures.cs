using System.Text;

using Specforge.Core.Documents;
using Specforge.Core.Identifiers;
using Specforge.Core.Ledger;
using Specforge.Core.Lifecycle;
using Specforge.Core.Validation;

namespace Specforge.Tests.TestSupport;

/// <summary>Seeds fixture decision/item files + ledger rows and builds the ITEM-010 validators over a <see cref="PackageWorkspace"/>.</summary>
internal static class SpecGraphFixtures
{
    public static TombstoneAwareIdAllocator Allocator(PackageWorkspace ws) => new(new IdAllocator(new LedgerReader(), ws.Session), ws.Session);

    public static IdsAspectValidator Ids(PackageWorkspace ws) => new(ws.Session, Allocator(ws));

    public static LinksAspectValidator Links(PackageWorkspace ws) => new(ws.Session, Allocator(ws));

    public static LifecycleAspectValidator Lifecycle(PackageWorkspace ws) => new(ws.Session, new ArtifactLedgerService(ws.Session), new LifecycleStateMachine());

    public static ImpactCoverageAspectValidator Impact(PackageWorkspace ws) => new(ws.Session);

    public static ValidationService Service(PackageWorkspace ws) => new(Ids(ws), Links(ws), Lifecycle(ws), Impact(ws));

    public static Task WriteDecisionAsync(PackageWorkspace ws, string id, string status, string? supersedes = null)
    {
        StringBuilder sb = new();
        sb.Append($"# {id} - Fixture Decision\n\n");
        sb.Append($"Status: {status}\n");
        sb.Append("Review owner: User\n");
        if (supersedes is not null)
        {
            sb.Append($"Supersedes: {supersedes}\n");
        }

        sb.Append("\n## Context\n\nfixture body\n");
        return File.WriteAllTextAsync(Path.Combine(ws.DecisionsDirectory, $"{id}-FIXTURE.md"), sb.ToString());
    }

    public static Task WriteItemAsync(
        PackageWorkspace ws,
        string id,
        string status,
        IReadOnlyList<string>? dependsOn = null,
        IReadOnlyList<string>? omitSections = null,
        IReadOnlyList<string>? blankSections = null)
    {
        omitSections ??= [];
        blankSections ??= [];
        StringBuilder sb = new();
        sb.Append($"# {id} - Fixture Item\n\n");
        sb.Append($"Status: {status}\n");
        sb.Append("Review owner: User\n");
        if (dependsOn is { Count: > 0 })
        {
            sb.Append($"Depends on: {string.Join(", ", dependsOn)}\n");
        }

        foreach (string name in RequiredItemSections.Names)
        {
            if (omitSections.Contains(name, StringComparer.Ordinal))
            {
                continue;
            }

            sb.Append($"\n## {name}\n\n");
            sb.Append(blankSections.Contains(name, StringComparer.Ordinal) ? "   \n" : "fixture body\n");
        }

        return File.WriteAllTextAsync(Path.Combine(ws.ItemsDirectory, $"{id}-FIXTURE.md"), sb.ToString());
    }

    /// <summary>Appends an artifact row (routes ART-ITEM-* → items.md, others → artifacts.md).</summary>
    public static Task AddArtifactAsync(PackageWorkspace ws, string ledgerId, string status, string dependsOn = "")
        => new ArtifactLedgerService(ws.Session).AppendAsync(
            new ArtifactRow { LedgerId = ledgerId, Status = status, DependsOn = dependsOn }, CancellationToken.None);

    /// <summary>Appends a Deleted history event with the canonical tombstone-detail format.</summary>
    public static Task AddTombstoneAsync(PackageWorkspace ws, string deletedId)
        => new HistoryLedgerService(ws.Session).AppendAsync(
            "(multiple)", "Deleted", TombstoneDetailFormat.Format(deletedId, "fixture tombstone"), CancellationToken.None);
}

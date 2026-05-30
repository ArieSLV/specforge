using Specforge.Core.Configuration;
using Specforge.Core.Exceptions;
using Specforge.Core.Identifiers;
using Specforge.Core.Ledger;

namespace Specforge.Core.Validation;

/// <summary>
/// Default <see cref="IIdsAspectValidator"/> (ITEM-010): every identifier across the spec graph must be
/// well-formed, use a registered kind, be unique, and leave no unexplained gap in its kind's sequence.
/// </summary>
public sealed class IdsAspectValidator(SessionState session, TombstoneAwareIdAllocator allocator) : IIdsAspectValidator
{
    private static readonly (string File, string Prefix)[] AtomicMirrorKinds =
    [
        ("artifacts.md", "ART-DEC-"),
        ("items.md", "ART-ITEM-"),
    ];

    public async Task<IReadOnlyList<ValidationFinding>> ValidateAsync(CancellationToken ct)
    {
        SpecforgeConfig config = session.Config ?? throw new InvalidOperationException("Configuration is not loaded.");
        SpecforgePackageConfig package = session.RequireActivePackage();
        KindRegistry registry = KindRegistry.FromConfig(config, package.Name);
        ValidationContext context = ValidationContext.FromConfig(config);

        List<ValidationFinding> findings = [];

        try
        {
            KindRegistry.ValidateExtraKinds(package.ExtraKinds);
        }
        catch (SpecforgeReservedKindException ex)
        {
            findings.Add(new ValidationFinding(ValidationAspect.Ids, ValidationSeverity.Error, null, ex.Message, "remove the reserved kind from extraKinds"));
        }

        // Collect every primary identifier: ledger row ids + decision/item file ids.
        List<(string Id, string Source)> primary = [];
        foreach (string file in new[] { "artifacts.md", "items.md", "reviews.md", "commits.md" })
        {
            LedgerTable table = await SpecGraphScan.TableAsync(session, file, ct).ConfigureAwait(false);
            foreach (string id in SpecGraphScan.RowIds(table))
            {
                primary.Add((id, $"ledger/{file}"));
            }
        }

        foreach ((string Id, string Source) entry in await DecisionItemFileIdsAsync(ct).ConfigureAwait(false))
        {
            primary.Add(entry);
        }

        foreach ((string id, string source) in primary)
        {
            try
            {
                IdValidator.Validate(id, registry, context);
            }
            catch (SpecforgeInvalidIdentifierException)
            {
                findings.Add(new ValidationFinding(ValidationAspect.Ids, ValidationSeverity.Error, id,
                    $"identifier '{id}' (in {source}) is not a valid specforge identifier or uses an unregistered kind.",
                    "correct the identifier, or register the kind via the package's extraKinds"));
            }
        }

        foreach (IGrouping<string, (string Id, string Source)> group in primary.GroupBy(p => p.Id, StringComparer.Ordinal).Where(g => g.Count() > 1))
        {
            findings.Add(new ValidationFinding(ValidationAspect.Ids, ValidationSeverity.Error, group.Key,
                $"identifier '{group.Key}' is defined {group.Count()} times ({string.Join(", ", group.Select(x => x.Source).Distinct())}).",
                "ensure every identifier is defined exactly once"));
        }

        // Unexplained-gap checks for the globally-sequential atomic kinds (DEC/ITEM via ART mirror rows, CMT direct).
        foreach ((string file, string prefix) in AtomicMirrorKinds)
        {
            string kind = prefix["ART-".Length..].TrimEnd('-');
            LedgerTable table = await SpecGraphScan.TableAsync(session, file, ct).ConfigureAwait(false);
            await AddGapFindingsAsync(kind, SpecGraphScan.SeqsWithPrefix(SpecGraphScan.RowIds(table), prefix), findings, ct).ConfigureAwait(false);
        }

        LedgerTable commits = await SpecGraphScan.TableAsync(session, "commits.md", ct).ConfigureAwait(false);
        await AddGapFindingsAsync("CMT", SpecGraphScan.SeqsWithPrefix(SpecGraphScan.RowIds(commits), "CMT-"), findings, ct).ConfigureAwait(false);

        return findings;
    }

    private async Task AddGapFindingsAsync(string kind, IReadOnlyList<int> observedSeqs, List<ValidationFinding> findings, CancellationToken ct)
    {
        foreach (int gap in await allocator.VerifyGapsAsync(kind, observedSeqs, ct).ConfigureAwait(false))
        {
            string missingId = $"{kind}-{gap:D3}";
            findings.Add(new ValidationFinding(ValidationAspect.Ids, ValidationSeverity.Error, missingId,
                $"sequence gap: '{missingId}' is missing with no tombstone in history.md (numbers are never reused, so a gap must be explained by a Deleted event).",
                "append the missing tombstone history event, or investigate the lost identifier"));
        }
    }

    private async Task<IReadOnlyList<(string Id, string Source)>> DecisionItemFileIdsAsync(CancellationToken ct)
    {
        List<(string, string)> ids = [];
        foreach (Documents.DecisionDocument d in await SpecGraphScan.DecisionsAsync(session, ct).ConfigureAwait(false))
        {
            ids.Add((d.Id, "decisions/"));
        }

        foreach (Documents.ItemDocument i in await SpecGraphScan.ItemsAsync(session, ct).ConfigureAwait(false))
        {
            ids.Add((i.Id, "items/"));
        }

        return ids;
    }
}

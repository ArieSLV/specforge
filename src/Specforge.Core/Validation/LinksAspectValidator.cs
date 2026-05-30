using Specforge.Core.Configuration;
using Specforge.Core.Documents;
using Specforge.Core.Identifiers;
using Specforge.Core.Ledger;

namespace Specforge.Core.Validation;

/// <summary>
/// Default <see cref="ILinksAspectValidator"/> (ITEM-010): every cross-reference must resolve to an
/// existing artifact (or a tombstoned one). Severity splits per the item: decision Supersedes/Covers/Amends
/// and REV/CMT targets are errors; Depends on references are warnings (they may legitimately predate targets).
/// </summary>
public sealed class LinksAspectValidator(SessionState session, TombstoneAwareIdAllocator allocator) : ILinksAspectValidator
{
    public async Task<IReadOnlyList<ValidationFinding>> ValidateAsync(CancellationToken ct)
    {
        LedgerTable artifacts = await SpecGraphScan.TableAsync(session, "artifacts.md", ct).ConfigureAwait(false);
        LedgerTable items = await SpecGraphScan.TableAsync(session, "items.md", ct).ConfigureAwait(false);
        LedgerTable reviews = await SpecGraphScan.TableAsync(session, "reviews.md", ct).ConfigureAwait(false);
        LedgerTable commits = await SpecGraphScan.TableAsync(session, "commits.md", ct).ConfigureAwait(false);
        IReadOnlyList<DecisionDocument> decisions = await SpecGraphScan.DecisionsAsync(session, ct).ConfigureAwait(false);
        IReadOnlyList<ItemDocument> itemDocs = await SpecGraphScan.ItemsAsync(session, ct).ConfigureAwait(false);

        HashSet<string> universe = new(StringComparer.Ordinal);
        foreach (string id in SpecGraphScan.RowIds(artifacts).Concat(SpecGraphScan.RowIds(items)).Concat(SpecGraphScan.RowIds(reviews)).Concat(SpecGraphScan.RowIds(commits)))
        {
            universe.Add(id);
        }

        foreach (DecisionDocument d in decisions)
        {
            universe.Add(d.Id);
        }

        foreach (ItemDocument i in itemDocs)
        {
            universe.Add(i.Id);
        }

        IReadOnlyCollection<string> tombstoned = await allocator.ReadTombstonedIdsAsync(ct).ConfigureAwait(false);
        bool Resolves(string id) => universe.Contains(id) || tombstoned.Contains(id);

        List<ValidationFinding> findings = [];

        foreach (DecisionDocument d in decisions)
        {
            foreach (string referenced in ExtractIds(d.Supersedes).Concat(ExtractIds(d.Covers)).Concat(ExtractIds(d.Amends)))
            {
                if (!Resolves(referenced))
                {
                    findings.Add(new ValidationFinding(ValidationAspect.Links, ValidationSeverity.Error, d.Id,
                        $"decision {d.Id} references '{referenced}' (Supersedes/Covers/Amends) which does not exist.",
                        "correct the reference or restore the target"));
                }
            }
        }

        foreach (ItemDocument i in itemDocs)
        {
            foreach (string dependency in i.DependsOn)
            {
                if (IdParser.TryParse(dependency, out _) && !Resolves(dependency))
                {
                    findings.Add(new ValidationFinding(ValidationAspect.Links, ValidationSeverity.Warning, i.Id,
                        $"item {i.Id} depends on '{dependency}' which does not exist yet.",
                        "create the dependency, or remove the reference if obsolete"));
                }
            }
        }

        AddTargetColumnFindings(reviews, columnIndex: 1, ValidationSeverity.Error, "review", Resolves, findings);
        AddTargetColumnFindings(commits, columnIndex: 2, ValidationSeverity.Error, "commit", Resolves, findings);
        AddDependsOnFindings(artifacts, Resolves, findings);
        AddDependsOnFindings(items, Resolves, findings);

        return findings;
    }

    private static void AddTargetColumnFindings(LedgerTable table, int columnIndex, string severity, string rowKind, Func<string, bool> resolves, List<ValidationFinding> findings)
    {
        foreach (LedgerRow row in table.Rows)
        {
            if (row.Cells.Count <= columnIndex)
            {
                continue;
            }

            string target = LedgerRow.Normalize(row.Cells[columnIndex]);
            if (target.Length > 0 && IdParser.TryParse(target, out _) && !resolves(target))
            {
                findings.Add(new ValidationFinding(ValidationAspect.Links, severity, row.LedgerId,
                    $"{rowKind} row {row.LedgerId} targets '{target}' which does not exist.",
                    "correct the target reference"));
            }
        }
    }

    private static void AddDependsOnFindings(LedgerTable table, Func<string, bool> resolves, List<ValidationFinding> findings)
    {
        const int dependsOnColumn = 3;
        foreach (LedgerRow row in table.Rows)
        {
            if (row.Cells.Count <= dependsOnColumn)
            {
                continue;
            }

            foreach (string referenced in ExtractIds(row.Cells[dependsOnColumn]))
            {
                if (!resolves(referenced))
                {
                    findings.Add(new ValidationFinding(ValidationAspect.Links, ValidationSeverity.Warning, row.LedgerId,
                        $"artifact {row.LedgerId} depends on '{referenced}' which does not exist.",
                        "create the dependency, or remove the reference if obsolete"));
                }
            }
        }
    }

    private static readonly string[] FillerTokens = ["none", "None", "n/a", "N/A", "-", "–", "and", "&", "or"];

    /// <summary>
    /// Extracts the referenced ids from a field — but ONLY when the field is genuinely a reference list
    /// (every token is a valid id or a filler word like "none"/"-"). If any token is a non-id, non-filler
    /// word, the field is treated as prose (e.g. DEC-007's <c>Covers:</c> sentence) and yields nothing.
    /// </summary>
    private static IReadOnlyList<string> ExtractIds(string? field)
    {
        if (string.IsNullOrWhiteSpace(field))
        {
            return [];
        }

        List<string> ids = [];
        foreach (string raw in field.Split([',', ' ', '\t', '\n', '`', '(', ')', ';', '/'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string token = raw.Trim().Trim('`', ',', '.', ';', ':');
            if (token.Length == 0 || FillerTokens.Contains(token, StringComparer.Ordinal))
            {
                continue;
            }

            if (IdParser.TryParse(token, out _))
            {
                ids.Add(token);
                continue;
            }

            return []; // a non-id, non-filler token => the field is prose, not a reference list.
        }

        return ids;
    }
}

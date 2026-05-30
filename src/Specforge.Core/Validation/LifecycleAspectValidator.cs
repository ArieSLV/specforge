using Specforge.Core.Configuration;
using Specforge.Core.Documents;
using Specforge.Core.Ledger;
using Specforge.Core.Lifecycle;

namespace Specforge.Core.Validation;

/// <summary>
/// Default <see cref="ILifecycleAspectValidator"/> (ITEM-010): file <c>Status:</c> must match the ledger
/// row's Status (error on mismatch), be a recognized lifecycle state (error if not), and an Approved
/// artifact without any review row is flagged (warning).
/// </summary>
public sealed class LifecycleAspectValidator(SessionState session, IArtifactLedgerService artifacts, LifecycleStateMachine lifecycle) : ILifecycleAspectValidator
{
    public async Task<IReadOnlyList<ValidationFinding>> ValidateAsync(CancellationToken ct)
    {
        LedgerTable reviews = await SpecGraphScan.TableAsync(session, "reviews.md", ct).ConfigureAwait(false);
        HashSet<string> reviewTargets = new(StringComparer.Ordinal);
        foreach (LedgerRow row in reviews.Rows)
        {
            if (row.Cells.Count > 1)
            {
                reviewTargets.Add(LedgerRow.Normalize(row.Cells[1]));
            }
        }

        List<ValidationFinding> findings = [];

        foreach (DecisionDocument d in await SpecGraphScan.DecisionsAsync(session, ct).ConfigureAwait(false))
        {
            await CheckAsync(d.Id, d.Status, findings, reviewTargets, ct).ConfigureAwait(false);
        }

        foreach (ItemDocument i in await SpecGraphScan.ItemsAsync(session, ct).ConfigureAwait(false))
        {
            await CheckAsync(i.Id, i.Status, findings, reviewTargets, ct).ConfigureAwait(false);
        }

        return findings;
    }

    private async Task CheckAsync(string bareId, string fileStatus, List<ValidationFinding> findings, HashSet<string> reviewTargets, CancellationToken ct)
    {
        string artifactId = $"ART-{bareId}";

        if (!lifecycle.IsKnownState(fileStatus))
        {
            findings.Add(new ValidationFinding(ValidationAspect.Lifecycle, ValidationSeverity.Error, bareId,
                $"{bareId} has status '{fileStatus}', which is not a recognized lifecycle state.",
                "set the status to a recognized lifecycle state"));
        }

        ArtifactRow? row = await artifacts.FindByLedgerIdAsync(artifactId, ct).ConfigureAwait(false);
        if (row is null)
        {
            findings.Add(new ValidationFinding(ValidationAspect.Lifecycle, ValidationSeverity.Warning, bareId,
                $"{bareId} has no matching ledger row ({artifactId}).",
                "add the artifact row, or remove the orphaned file"));
        }
        else if (!string.Equals(row.Status, fileStatus, StringComparison.Ordinal))
        {
            findings.Add(new ValidationFinding(ValidationAspect.Lifecycle, ValidationSeverity.Error, bareId,
                $"status mismatch for {bareId}: the file says '{fileStatus}' but ledger row {artifactId} says '{row.Status}'.",
                "reconcile the file Status header with the ledger row"));
        }

        if (string.Equals(fileStatus, LifecycleState.Approved, StringComparison.Ordinal)
            && !reviewTargets.Contains(bareId) && !reviewTargets.Contains(artifactId))
        {
            findings.Add(new ValidationFinding(ValidationAspect.Lifecycle, ValidationSeverity.Warning, bareId,
                $"{bareId} is Approved but has no review (REV) row.",
                "append a review via append_review to record the approval evidence"));
        }
    }
}

using Specforge.Core.Configuration;
using Specforge.Core.Documents;
using Specforge.Core.Lifecycle;

namespace Specforge.Core.Validation;

/// <summary>
/// Default <see cref="IImpactCoverageAspectValidator"/> (ITEM-010): an Approved item missing any required
/// section is an error (the Approved gate); an in-flight (Draft / Draft for user review) item missing a
/// section is informational. A present-but-whitespace-only section counts as missing.
/// </summary>
public sealed class ImpactCoverageAspectValidator(SessionState session) : IImpactCoverageAspectValidator
{
    public async Task<IReadOnlyList<ValidationFinding>> ValidateAsync(CancellationToken ct)
    {
        List<ValidationFinding> findings = [];

        foreach (ItemDocument item in await SpecGraphScan.ItemsAsync(session, ct).ConfigureAwait(false))
        {
            List<string> missing =
            [
                .. RequiredItemSections.Names.Where(name =>
                    !item.Sections.TryGetValue(name, out string? body) || string.IsNullOrWhiteSpace(body)),
            ];

            if (missing.Count == 0)
            {
                continue;
            }

            string? severity = item.Status switch
            {
                LifecycleState.Approved => ValidationSeverity.Error,
                LifecycleState.Draft or LifecycleState.DraftForUserReview => ValidationSeverity.Info,
                _ => null,
            };

            if (severity is null)
            {
                continue;
            }

            string detail = string.Join(", ", missing);
            findings.Add(new ValidationFinding(ValidationAspect.ImpactCoverage, severity, item.Id,
                severity == ValidationSeverity.Error
                    ? $"Approved item {item.Id} is missing required section(s): {detail}."
                    : $"in-flight item {item.Id} is missing required section(s): {detail}.",
                "author the missing section(s) before approving"));
        }

        return findings;
    }
}

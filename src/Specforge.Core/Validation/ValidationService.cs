using Specforge.Core.Exceptions;

namespace Specforge.Core.Validation;

/// <summary>
/// Default <see cref="IValidationService"/> (ITEM-010): runs the requested aspect(s) and aggregates the
/// findings. For <c>all</c> it runs every aspect in declared order and never short-circuits on errors.
/// </summary>
public sealed class ValidationService(
    IIdsAspectValidator ids,
    ILinksAspectValidator links,
    ILifecycleAspectValidator lifecycle,
    IImpactCoverageAspectValidator impactCoverage) : IValidationService
{
    public async Task<ValidationResult> ValidateAsync(string aspect, CancellationToken ct)
    {
        List<ValidationFinding> findings = [];

        switch (aspect)
        {
            case ValidationAspect.Ids:
                findings.AddRange(await ids.ValidateAsync(ct).ConfigureAwait(false));
                break;
            case ValidationAspect.Links:
                findings.AddRange(await links.ValidateAsync(ct).ConfigureAwait(false));
                break;
            case ValidationAspect.Lifecycle:
                findings.AddRange(await lifecycle.ValidateAsync(ct).ConfigureAwait(false));
                break;
            case ValidationAspect.ImpactCoverage:
                findings.AddRange(await impactCoverage.ValidateAsync(ct).ConfigureAwait(false));
                break;
            case ValidationAspect.All:
                // Declared order; every aspect runs (no short-circuit on errors).
                findings.AddRange(await ids.ValidateAsync(ct).ConfigureAwait(false));
                findings.AddRange(await links.ValidateAsync(ct).ConfigureAwait(false));
                findings.AddRange(await lifecycle.ValidateAsync(ct).ConfigureAwait(false));
                findings.AddRange(await impactCoverage.ValidateAsync(ct).ConfigureAwait(false));
                break;
            default:
                throw new SpecforgeInvalidArgumentException("aspect", string.Join(" | ", ValidationAspect.Accepted), "choose a valid aspect", aspect);
        }

        return ValidationResult.From(aspect, findings);
    }
}

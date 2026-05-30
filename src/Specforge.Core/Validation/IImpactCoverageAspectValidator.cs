namespace Specforge.Core.Validation;

/// <summary>Validates required-section coverage on item specs (ITEM-010 §impact-coverage): Approved gate vs in-flight info.</summary>
public interface IImpactCoverageAspectValidator
{
    Task<IReadOnlyList<ValidationFinding>> ValidateAsync(CancellationToken ct);
}

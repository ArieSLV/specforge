namespace Specforge.Core.Validation;

/// <summary>Validates lifecycle consistency (ITEM-010 §lifecycle): file-vs-ledger status, Approved-without-review, unknown states.</summary>
public interface ILifecycleAspectValidator
{
    Task<IReadOnlyList<ValidationFinding>> ValidateAsync(CancellationToken ct);
}

namespace Specforge.Core.Validation;

/// <summary>Validates identifier integrity (ITEM-010 §ids): format, registered kinds, duplicates, unexplained gaps.</summary>
public interface IIdsAspectValidator
{
    Task<IReadOnlyList<ValidationFinding>> ValidateAsync(CancellationToken ct);
}

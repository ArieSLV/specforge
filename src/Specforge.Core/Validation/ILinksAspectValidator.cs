namespace Specforge.Core.Validation;

/// <summary>Validates cross-references (ITEM-010 §links): Supersedes/Covers/Amends, Depends on, REV/CMT/ART targets.</summary>
public interface ILinksAspectValidator
{
    Task<IReadOnlyList<ValidationFinding>> ValidateAsync(CancellationToken ct);
}

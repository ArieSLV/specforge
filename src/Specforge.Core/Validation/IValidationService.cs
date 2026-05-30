namespace Specforge.Core.Validation;

/// <summary>Runs the aspect validators (ITEM-010) and aggregates their findings; never short-circuits on errors.</summary>
public interface IValidationService
{
    /// <summary>Validates <paramref name="aspect"/> (one of <see cref="ValidationAspect.Accepted"/>); <c>all</c> runs every aspect in declared order.</summary>
    Task<ValidationResult> ValidateAsync(string aspect, CancellationToken ct);
}

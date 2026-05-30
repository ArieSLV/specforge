namespace Specforge.Core.Validation;

/// <summary>The aggregate result of a <c>validate</c> call (ITEM-010): the ordered findings + per-severity counts.</summary>
/// <param name="AspectRequested">Echoes the requested aspect (<c>ids</c>/<c>links</c>/.../<c>all</c>).</param>
/// <param name="Findings">Ordered by aspect (declared order), then severity (error, warning, info), then target.</param>
public sealed record ValidationResult(
    string AspectRequested,
    IReadOnlyList<ValidationFinding> Findings,
    int ErrorCount,
    int WarningCount,
    int InfoCount)
{
    /// <summary>Builds a result from an unordered finding list, sorting it and computing the counts.</summary>
    public static ValidationResult From(string aspectRequested, IEnumerable<ValidationFinding> findings)
    {
        List<ValidationFinding> ordered =
        [
            .. findings.OrderBy(f => AspectRank(f.Aspect))
                       .ThenBy(f => SeverityRank(f.Severity))
                       .ThenBy(f => f.Target ?? string.Empty, StringComparer.Ordinal)
                       .ThenBy(f => f.Message, StringComparer.Ordinal),
        ];

        return new ValidationResult(
            aspectRequested,
            ordered,
            ordered.Count(f => string.Equals(f.Severity, ValidationSeverity.Error, StringComparison.Ordinal)),
            ordered.Count(f => string.Equals(f.Severity, ValidationSeverity.Warning, StringComparison.Ordinal)),
            ordered.Count(f => string.Equals(f.Severity, ValidationSeverity.Info, StringComparison.Ordinal)));
    }

    private static int AspectRank(string aspect)
    {
        int index = ValidationAspect.Concrete.ToList().IndexOf(aspect);
        return index < 0 ? int.MaxValue : index;
    }

    private static int SeverityRank(string severity) => severity switch
    {
        ValidationSeverity.Error => 0,
        ValidationSeverity.Warning => 1,
        ValidationSeverity.Info => 2,
        _ => 3,
    };
}

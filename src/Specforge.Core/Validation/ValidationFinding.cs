namespace Specforge.Core.Validation;

/// <summary>A single spec-graph consistency observation (ITEM-010). <paramref name="Target"/> is null for package-wide findings.</summary>
/// <param name="Aspect"><c>ids</c> | <c>links</c> | <c>lifecycle</c> | <c>impact-coverage</c>.</param>
/// <param name="Severity"><c>error</c> | <c>warning</c> | <c>info</c>.</param>
/// <param name="Target">Bare or qualified LedgerId, or null for package-wide.</param>
/// <param name="Message">Human/AI-readable description.</param>
/// <param name="Suggestion">Actionable next step, when one exists.</param>
public sealed record ValidationFinding(
    string Aspect,
    string Severity,
    string? Target,
    string Message,
    string? Suggestion);

/// <summary>The validation aspect names (ITEM-010).</summary>
public static class ValidationAspect
{
    public const string Ids = "ids";
    public const string Links = "links";
    public const string Lifecycle = "lifecycle";
    public const string ImpactCoverage = "impact-coverage";
    public const string All = "all";

    /// <summary>The four concrete aspects in declared (canonical) order; <see cref="All"/> runs each.</summary>
    public static IReadOnlyList<string> Concrete { get; } = [Ids, Links, Lifecycle, ImpactCoverage];

    /// <summary>Every accepted <c>aspect</c> argument value.</summary>
    public static IReadOnlyList<string> Accepted { get; } = [Ids, Links, Lifecycle, ImpactCoverage, All];
}

/// <summary>The severity names (ITEM-010), ordered most-to-least severe.</summary>
public static class ValidationSeverity
{
    public const string Error = "error";
    public const string Warning = "warning";
    public const string Info = "info";
}

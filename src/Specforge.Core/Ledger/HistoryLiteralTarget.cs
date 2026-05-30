namespace Specforge.Core.Ledger;

/// <summary>
/// The closed set of non-LedgerId literal targets the History table accepts for project-wide events
/// (ITEM-009). MVP accepts exactly two: <c>(milestone)</c> for milestone events and <c>(multiple)</c>
/// for events affecting several artifacts. Extending this set is a future DEC change, not a config knob.
/// </summary>
public static class HistoryLiteralTarget
{
    /// <summary>The exact literal target strings allowed in the History <c>LedgerId</c>/target column.</summary>
    public static IReadOnlyList<string> AcceptedLiterals { get; } = ["(milestone)", "(multiple)"];

    /// <summary>True if <paramref name="value"/> is one of the accepted literal targets (ordinal match).</summary>
    public static bool IsLiteralTarget(string? value) =>
        value is not null && AcceptedLiterals.Contains(value, StringComparer.Ordinal);
}

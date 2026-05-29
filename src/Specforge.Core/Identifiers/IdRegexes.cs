using System.Text.RegularExpressions;

namespace Specforge.Core.Identifiers;

/// <summary>
/// Compiled, source-generated regexes for the identifier forms in DEC-004 §"Identifier Form" and
/// §"Reserved Kinds and Validation".
/// </summary>
public static partial class IdRegexes
{
    /// <summary>Atomic: <c>^[A-Z]{2,6}-[0-9]{3}$</c>.</summary>
    [GeneratedRegex("^[A-Z]{2,6}-[0-9]{3}$")]
    public static partial Regex Atomic();

    /// <summary>Composite review: <c>^REV-[A-Z]{2,6}-[0-9]{3}-[0-9]{3}$</c>.</summary>
    [GeneratedRegex("^REV-[A-Z]{2,6}-[0-9]{3}-[0-9]{3}$")]
    public static partial Regex CompositeReview();

    /// <summary>Descriptive artifact: <c>^ART-[A-Z][A-Z0-9-]*[A-Z0-9]$</c>.</summary>
    [GeneratedRegex("^ART-[A-Z][A-Z0-9-]*[A-Z0-9]$")]
    public static partial Regex DescriptiveArt();

    /// <summary>Package-name token for the qualified form (non-empty, no slash).</summary>
    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._-]*$")]
    public static partial Regex PackageName();
}

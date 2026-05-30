namespace Specforge.Core.Exceptions;

/// <summary>
/// A tool argument (or a file an argument points at) is malformed — e.g. an imbalanced specforge
/// tagged block in <c>CLAUDE.md</c> (DEC-008). Maps to <c>specforge.tool.invalid_argument</c>. Carrier
/// type so deep Core code can surface the code without each tool re-deriving the envelope.
/// </summary>
public sealed class SpecforgeInvalidArgumentException : SpecforgeException
{
    public SpecforgeInvalidArgumentException(string argument, string expected, string suggestion, string? given = null)
        : base("specforge.tool.invalid_argument", $"Invalid argument '{argument}': expected {expected}.")
    {
        Argument = argument;
        Expected = expected;
        Suggestion = suggestion;
        Given = given;
    }

    /// <summary>The offending argument name (or file path).</summary>
    public string Argument { get; }

    /// <summary>What was expected.</summary>
    public string Expected { get; }

    /// <summary>Actionable next step.</summary>
    public string Suggestion { get; }

    /// <summary>The offending value, when short enough to surface.</summary>
    public string? Given { get; }
}

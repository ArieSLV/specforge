namespace Specforge.Core.Exceptions;

/// <summary>
/// An identifier string does not match any recognized form, names an unknown kind, or (for a
/// qualified reference) names an unknown package (DEC-004). Maps to <c>specforge.id.invalid</c>.
/// </summary>
public sealed class SpecforgeInvalidIdentifierException : SpecforgeException
{
    public SpecforgeInvalidIdentifierException(string given, IReadOnlyList<string> expectedPatterns)
        : base(Diagnostics.SpecforgeErrorCode.IdInvalid, $"Identifier '{given}' is not a valid specforge identifier.")
    {
        Given = given;
        ExpectedPatterns = expectedPatterns;
    }

    /// <summary>The offending input.</summary>
    public string Given { get; }

    /// <summary>The patterns the input was expected to match.</summary>
    public IReadOnlyList<string> ExpectedPatterns { get; }
}

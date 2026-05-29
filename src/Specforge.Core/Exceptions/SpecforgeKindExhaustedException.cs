namespace Specforge.Core.Exceptions;

/// <summary>
/// Allocating the next number for a kind would exceed the 3-digit ceiling of 999 (DEC-004 —
/// number exhaustion is a hard error forcing a package split, not a width bump).
/// Maps to <c>specforge.id.kind_exhausted</c>.
/// </summary>
public sealed class SpecforgeKindExhaustedException : SpecforgeException
{
    public SpecforgeKindExhaustedException(string kind, string package)
        : base("specforge.id.kind_exhausted", $"Kind '{kind}' in package '{package}' has reached the 999-identifier limit.")
    {
        Kind = kind;
        Package = package;
    }

    /// <summary>The exhausted kind.</summary>
    public string Kind { get; }

    /// <summary>The package whose kind counter is exhausted.</summary>
    public string Package { get; }
}

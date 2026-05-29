namespace Specforge.Core.Exceptions;

/// <summary>
/// A package's <c>extraKinds</c> array contains a reserved core kind (DEC-004 — core kinds may not
/// be redeclared). Maps to <c>specforge.id.kind_reserved</c>.
/// </summary>
public sealed class SpecforgeReservedKindException : SpecforgeException
{
    public SpecforgeReservedKindException(string given, IReadOnlyList<string> reservedKinds)
        : base("specforge.id.kind_reserved", $"Kind '{given}' is reserved and cannot appear in a package's extraKinds.")
    {
        Given = given;
        ReservedKinds = reservedKinds;
    }

    /// <summary>The offending kind from <c>extraKinds</c>.</summary>
    public string Given { get; }

    /// <summary>The reserved core kinds that may not be redeclared.</summary>
    public IReadOnlyList<string> ReservedKinds { get; }
}

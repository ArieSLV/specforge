namespace Specforge.Core.Configuration;

/// <summary>
/// One <c>packages[]</c> entry from <c>.specforge.json</c> (DEC-002 schema v1, DEC-004 <c>extraKinds</c>).
/// Paths are stored relative to the config file directory; resolution to absolute is lazy.
/// </summary>
/// <param name="Name">Package identifier used for selection and cross-package references.</param>
/// <param name="Path">Package directory path, relative to the config file.</param>
/// <param name="ExtraKinds">Per-package additional ID kinds (DEC-004); empty when absent.</param>
public sealed record SpecforgePackageConfig(string Name, string Path, IReadOnlyList<string> ExtraKinds)
{
    /// <summary>Shared empty list for entries without an <c>extraKinds</c> field.</summary>
    public static IReadOnlyList<string> NoExtraKinds { get; } = [];
}

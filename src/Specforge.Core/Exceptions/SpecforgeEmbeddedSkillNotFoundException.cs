namespace Specforge.Core.Exceptions;

/// <summary>
/// The embedded skill catalog is broken: a <c>SKILL.md</c> has missing/malformed frontmatter, a
/// name that disagrees with its directory, or a referenced resource is unreadable (DEC-005). Indicates
/// a build problem, surfaced at startup. Maps to <c>specforge.skills.catalog_missing</c>.
/// </summary>
public sealed class SpecforgeEmbeddedSkillNotFoundException : SpecforgeException
{
    public SpecforgeEmbeddedSkillNotFoundException(string resourceName, string message, Exception? innerException = null)
        : base("specforge.skills.catalog_missing", message, innerException)
    {
        ResourceName = resourceName;
    }

    /// <summary>The logical name of the offending embedded resource.</summary>
    public string ResourceName { get; }
}

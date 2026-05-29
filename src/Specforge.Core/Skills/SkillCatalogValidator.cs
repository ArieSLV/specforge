namespace Specforge.Core.Skills;

/// <summary>
/// One-shot startup check (DEC-005 / ITEM-004): forces full catalog enumeration so a broken embedded
/// <c>SKILL.md</c> fails the server start rather than surfacing later. An empty catalog is valid.
/// </summary>
public sealed class SkillCatalogValidator(IEmbeddedSkillCatalog catalog)
{
    /// <summary>
    /// Enumerates (and thereby validates) the catalog. Throws
    /// <see cref="Exceptions.SpecforgeEmbeddedSkillNotFoundException"/> on the first broken skill;
    /// returns the number of valid skills on success.
    /// </summary>
    public int Validate() => catalog.GetSkills().Count;
}

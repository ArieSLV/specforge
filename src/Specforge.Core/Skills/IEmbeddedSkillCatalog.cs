namespace Specforge.Core.Skills;

/// <summary>
/// Enumerates the skills embedded in the binary at build time (DEC-005). Parsing + validation happen
/// during enumeration, so a successful <see cref="GetSkills"/> means the catalog is well-formed.
/// </summary>
public interface IEmbeddedSkillCatalog
{
    /// <summary>Returns every embedded skill, ordered alphabetically by <see cref="EmbeddedSkill.Name"/>.</summary>
    IReadOnlyList<EmbeddedSkill> GetSkills();
}

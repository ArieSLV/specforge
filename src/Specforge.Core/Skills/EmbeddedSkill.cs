namespace Specforge.Core.Skills;

/// <summary>
/// One embedded skill: its validated frontmatter plus the supporting files that travel with it
/// (DEC-005). Built by <see cref="IEmbeddedSkillCatalog"/>.
/// </summary>
/// <param name="Name">Kebab-case skill name; equals the skill's directory segment.</param>
/// <param name="Description">One-sentence description (≤ 200 chars).</param>
/// <param name="ExtraFrontmatter">Any non-required frontmatter keys, passed through unvalidated.</param>
/// <param name="SkillMarkdown">The skill's own <c>SKILL.md</c> file (ITEM-005 needs it to install the skill).</param>
/// <param name="Files">Supporting files in the directory (excludes <c>SKILL.md</c> and <c>.gitkeep</c>).</param>
public sealed record EmbeddedSkill(
    string Name,
    string Description,
    IReadOnlyDictionary<string, object> ExtraFrontmatter,
    EmbeddedSkillFile SkillMarkdown,
    IReadOnlyList<EmbeddedSkillFile> Files);

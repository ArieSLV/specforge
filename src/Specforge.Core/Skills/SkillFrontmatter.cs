namespace Specforge.Core.Skills;

/// <summary>
/// Parsed YAML frontmatter of a <c>SKILL.md</c> (DEC-005 canonical format). <c>name</c> and
/// <c>description</c> are required; any other keys are carried in <see cref="Extras"/> unvalidated.
/// </summary>
/// <param name="Name">Required kebab-case name.</param>
/// <param name="Description">Required one-sentence description (≤ 200 chars).</param>
/// <param name="Extras">All other frontmatter keys.</param>
public sealed record SkillFrontmatter(
    string Name,
    string Description,
    IReadOnlyDictionary<string, object> Extras);

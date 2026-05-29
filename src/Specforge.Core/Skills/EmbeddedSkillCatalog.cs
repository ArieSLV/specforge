using System.Reflection;
using System.Text.RegularExpressions;

using Specforge.Core.Exceptions;

namespace Specforge.Core.Skills;

/// <summary>
/// Default <see cref="IEmbeddedSkillCatalog"/> backed by an assembly's manifest resources (DEC-005).
/// Groups resources by skill directory, parses each <c>SKILL.md</c>, enforces the
/// name-equals-directory + kebab-case rules, and returns the catalog ordered by name.
/// </summary>
public sealed partial class EmbeddedSkillCatalog(Assembly assembly, string resourcePrefix = "specforge.skills/") : IEmbeddedSkillCatalog
{
    [GeneratedRegex("^[a-z][a-z0-9-]*[a-z0-9]$")]
    private static partial Regex KebabName();

    public IReadOnlyList<EmbeddedSkill> GetSkills()
    {
        Dictionary<string, List<(string Resource, string RelativeFile)>> byDirectory = new(StringComparer.Ordinal);
        foreach (string resource in assembly.GetManifestResourceNames())
        {
            // MSBuild's %(RecursiveDir) emits the platform separator ('\' on Windows), so logical names
            // look like "specforge.skills/draft-decision\SKILL.md". Normalize to '/' for structural
            // parsing while keeping the ORIGINAL name for GetManifestResourceStream.
            string normalized = resource.Replace('\\', '/');
            if (!normalized.StartsWith(resourcePrefix, StringComparison.Ordinal))
            {
                continue;
            }

            string relative = normalized[resourcePrefix.Length..];
            int slash = relative.IndexOf('/', StringComparison.Ordinal);
            if (slash < 0)
            {
                continue; // file directly under the prefix root — not part of any skill
            }

            string directory = relative[..slash];
            string fileName = relative[(slash + 1)..];
            if (string.Equals(Path.GetFileName(fileName), ".gitkeep", StringComparison.Ordinal))
            {
                continue;
            }

            if (!byDirectory.TryGetValue(directory, out List<(string Resource, string RelativeFile)>? entries))
            {
                entries = [];
                byDirectory[directory] = entries;
            }

            entries.Add((resource, fileName));
        }

        List<EmbeddedSkill> skills = [];
        foreach ((string directory, List<(string Resource, string RelativeFile)> entries) in byDirectory)
        {
            string skillLogicalName = $"{resourcePrefix}{directory}/SKILL.md";
            (string Resource, string RelativeFile) skillEntry = entries.FirstOrDefault(e => string.Equals(e.RelativeFile, "SKILL.md", StringComparison.Ordinal));
            if (skillEntry.Resource is null)
            {
                throw new SpecforgeEmbeddedSkillNotFoundException(skillLogicalName, $"Skill directory '{directory}' has no SKILL.md.");
            }

            SkillFrontmatter frontmatter = SkillFrontmatterParser.Parse(ReadResource(skillEntry.Resource), skillLogicalName);

            if (!string.Equals(frontmatter.Name, directory, StringComparison.Ordinal))
            {
                throw new SpecforgeEmbeddedSkillNotFoundException(
                    skillLogicalName,
                    $"SKILL.md declares name '{frontmatter.Name}' but lives in directory '{directory}'.");
            }

            if (!KebabName().IsMatch(frontmatter.Name))
            {
                throw new SpecforgeEmbeddedSkillNotFoundException(
                    skillLogicalName,
                    $"Skill name '{frontmatter.Name}' is not kebab-case (^[a-z][a-z0-9-]*[a-z0-9]$).");
            }

            string skillResource = skillEntry.Resource;
            EmbeddedSkillFile skillMarkdown = new(skillLogicalName, () => OpenResource(skillResource));

            List<EmbeddedSkillFile> files = entries
                .Where(e => !string.Equals(e.RelativeFile, "SKILL.md", StringComparison.Ordinal))
                .OrderBy(e => e.RelativeFile, StringComparer.Ordinal)
                .Select(e => new EmbeddedSkillFile($"{resourcePrefix}{directory}/{e.RelativeFile}", () => OpenResource(e.Resource)))
                .ToList();

            skills.Add(new EmbeddedSkill(frontmatter.Name, frontmatter.Description, frontmatter.Extras, skillMarkdown, files));
        }

        return [.. skills.OrderBy(s => s.Name, StringComparer.Ordinal)];
    }

    private Stream OpenResource(string resource) =>
        assembly.GetManifestResourceStream(resource)
            ?? throw new SpecforgeEmbeddedSkillNotFoundException(resource, $"Embedded resource '{resource}' could not be opened.");

    private string ReadResource(string resource)
    {
        using Stream stream = OpenResource(resource);
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }
}

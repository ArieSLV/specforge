using Specforge.Core.Exceptions;

using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Specforge.Core.Skills;

/// <summary>
/// Extracts and validates the YAML frontmatter from a <c>SKILL.md</c> (DEC-005). Strict on the two
/// required keys (<c>name</c>, <c>description</c> ≤ 200 chars); tolerant of any other keys.
/// </summary>
public static class SkillFrontmatterParser
{
    /// <summary>Maximum allowed <c>description</c> length (DEC-005).</summary>
    public const int MaxDescriptionLength = 200;

    private static readonly IDeserializer Deserializer = new DeserializerBuilder().Build();

    /// <summary>
    /// Parses <paramref name="source"/> (full <c>SKILL.md</c> text). Throws
    /// <see cref="SpecforgeEmbeddedSkillNotFoundException"/> (carrying <paramref name="resourceName"/>)
    /// on any frontmatter problem.
    /// </summary>
    public static SkillFrontmatter Parse(string source, string? resourceName = null)
    {
        string name = resourceName ?? "(frontmatter)";
        if (string.IsNullOrEmpty(source))
        {
            throw new SpecforgeEmbeddedSkillNotFoundException(name, "SKILL.md is empty; expected YAML frontmatter.");
        }

        string[] lines = source.Replace("\r\n", "\n", StringComparison.Ordinal).TrimStart('﻿').Split('\n');
        if (lines.Length == 0 || lines[0].Trim() != "---")
        {
            throw new SpecforgeEmbeddedSkillNotFoundException(name, "SKILL.md is missing the opening '---' frontmatter delimiter.");
        }

        int end = -1;
        for (int i = 1; i < lines.Length; i++)
        {
            if (lines[i].Trim() == "---")
            {
                end = i;
                break;
            }
        }

        if (end < 0)
        {
            throw new SpecforgeEmbeddedSkillNotFoundException(name, "SKILL.md frontmatter is missing the closing '---' delimiter.");
        }

        string yaml = string.Join('\n', lines[1..end]);

        Dictionary<string, object>? map;
        try
        {
            map = Deserializer.Deserialize<Dictionary<string, object>>(yaml);
        }
        catch (YamlException ex)
        {
            throw new SpecforgeEmbeddedSkillNotFoundException(name, $"SKILL.md frontmatter is not valid YAML: {ex.Message}", ex);
        }

        map ??= [];
        string skillName = RequireString(map, "name", name);
        string description = RequireString(map, "description", name);
        if (description.Length > MaxDescriptionLength)
        {
            throw new SpecforgeEmbeddedSkillNotFoundException(name, $"SKILL.md description is {description.Length} chars; the maximum is {MaxDescriptionLength}.");
        }

        Dictionary<string, object> extras = map
            .Where(kv => kv.Key is not ("name" or "description"))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        return new SkillFrontmatter(skillName, description, extras);
    }

    private static string RequireString(Dictionary<string, object> map, string key, string resourceName)
    {
        if (!map.TryGetValue(key, out object? value) || value is null)
        {
            throw new SpecforgeEmbeddedSkillNotFoundException(resourceName, $"SKILL.md frontmatter is missing required key '{key}'.");
        }

        string text = value.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new SpecforgeEmbeddedSkillNotFoundException(resourceName, $"SKILL.md frontmatter key '{key}' must be non-empty.");
        }

        return text;
    }
}

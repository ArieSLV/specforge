using System.Text.RegularExpressions;

using Xunit;

namespace Specforge.Tests.Skills;

/// <summary>
/// Static drift guards over the authored SKILL.md bodies (ITEM-012): every backticked tool name is in
/// the DEC-007 catalog, every shared-doc/template path resolves, and every cross-skill reference names a
/// catalog skill. Reads the source files from <c>spec/skills/</c> in the dogfood repo.
/// </summary>
public class SkillContentReferenceTests
{
    // DEC-007 MVP tool catalog (21). Hard-coded per the spec's fallback (no reflective tool registry exists).
    private static readonly HashSet<string> Tools = new(StringComparer.Ordinal)
    {
        "list_packages", "use_package", "info", "install_skills", "init",
        "list_decisions", "get_decision", "create_decision", "set_decision_status", "delete_decision",
        "list_items", "get_item", "create_item", "set_item_status", "delete_item",
        "append_history", "append_review", "append_commit", "delete_review", "delete_commit",
        "validate",
    };

    private static readonly string[] Skills =
    [
        "draft-decision", "review-decision", "draft-item", "review-item",
        "impact-assessment", "validate-spec-graph", "adopt-existing-project",
    ];

    private static readonly Regex Backtick = new("`([^`\n]+)`");
    private static readonly Regex ToolShape = new("^[a-z][a-z_]+[a-z]$");
    private static readonly Regex SharedPath = new(@"spec/(?:shared|templates)/[a-z_]+\.md");

    public static TheoryData<string> AllSkills()
    {
        TheoryData<string> data = [];
        foreach (string skill in Skills)
        {
            data.Add(skill);
        }

        return data;
    }

    private static string RepoRoot()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, ".specforge.json")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException(".specforge.json not found by walk-up from the test base directory");
    }

    private static string ReadSkill(string name) =>
        File.ReadAllText(Path.Combine(RepoRoot(), "spec", "skills", name, "SKILL.md"));

    [Theory]
    [MemberData(nameof(AllSkills))]
    public void EveryToolNameMentionedIsRegistered(string skill)
    {
        string body = ReadSkill(skill);
        List<string> offenders = [];
        foreach (Match m in Backtick.Matches(body))
        {
            string token = m.Groups[1].Value.Trim();
            if (Tools.Contains(token))
            {
                continue; // registered tool reference
            }

            if (ToolShape.IsMatch(token) && token.Contains('_', StringComparison.Ordinal) && token.Length is >= 4 and <= 30)
            {
                offenders.Add(token); // tool-shaped but not registered => typo or stale rename
            }
            // otherwise: an ordinary word / id / path / skill name — skip
        }

        Assert.True(offenders.Count == 0, $"{skill}: unregistered tool-shaped identifiers: {string.Join(", ", offenders)}");
    }

    [Theory]
    [MemberData(nameof(AllSkills))]
    public void EverySharedDocReferenceResolves(string skill)
    {
        string root = RepoRoot();
        string body = ReadSkill(skill);
        foreach (Match m in SharedPath.Matches(body))
        {
            string rel = m.Value.Replace('/', Path.DirectorySeparatorChar);
            Assert.True(File.Exists(Path.Combine(root, rel)), $"{skill} references missing shared doc: {m.Value}");
        }
    }

    [Theory]
    [MemberData(nameof(AllSkills))]
    public void EveryCrossSkillReferenceIsValid(string skill)
    {
        string body = ReadSkill(skill);
        int start = body.IndexOf("## Cross-skill references", StringComparison.Ordinal);
        Assert.True(start >= 0, $"{skill} has no '## Cross-skill references' section");

        int next = body.IndexOf("\n## ", start + 1, StringComparison.Ordinal);
        string section = next >= 0 ? body[start..next] : body[start..];

        foreach (Match m in Backtick.Matches(section))
        {
            string token = m.Groups[1].Value.Trim();
            // Cross-skill references are kebab skill names; ignore any incidental non-skill backticks.
            if (token.Contains('-', StringComparison.Ordinal))
            {
                Assert.Contains(token, Skills);
            }
        }
    }

    [Fact]
    public void EverySkillFileExists()
    {
        string root = RepoRoot();
        foreach (string skill in Skills)
        {
            Assert.True(File.Exists(Path.Combine(root, "spec", "skills", skill, "SKILL.md")), $"missing SKILL.md for {skill}");
        }
    }
}

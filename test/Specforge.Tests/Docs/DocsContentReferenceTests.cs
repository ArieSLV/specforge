using System.Text.RegularExpressions;

using Xunit;

namespace Specforge.Tests.Docs;

/// <summary>
/// Static drift guards over the user-facing docs (ITEM-013): every backticked tool name resolves to the
/// DEC-007 21-tool catalog, every backticked skill name resolves to the 7-skill catalog (ITEM-012), and
/// every project-level file path is spelled canonically. Reads <c>README.md</c> and
/// <c>docs/getting-started.md</c> from the dogfood repo root. Same two-phase identifier-extraction approach
/// as <c>SkillContentReferenceTests</c>; only single-backtick inline spans are inspected, so fenced code
/// blocks (the <c>.mcp.json</c> / <c>agents/openai.yaml</c> snippets) and multi-token spans are excluded by
/// construction.
/// </summary>
public class DocsContentReferenceTests
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

    // ITEM-012 skill catalog (7; amended from six on 2026-05-28).
    private static readonly HashSet<string> Skills = new(StringComparer.Ordinal)
    {
        "draft-decision", "review-decision", "draft-item", "review-item",
        "impact-assessment", "validate-spec-graph", "adopt-existing-project",
    };

    // (family, canonical): an inline-code token matching the family must equal the canonical spelling exactly.
    // Each family is a tight closed-set spell-check over one project-level output; it ignores every other path.
    private static readonly (Regex Family, string Canonical)[] PathFamilies =
    [
        (new Regex(@"^\.?specforge\.(json|jsonc|config|cfg|yaml|yml|toml|ini)$", RegexOptions.IgnoreCase), ".specforge.json"),
        (new Regex(@"^spec_?forge\.(md|markdown|txt)$", RegexOptions.IgnoreCase), "SPECFORGE.md"),
        (new Regex(@"^claude\.(md|markdown|txt)$", RegexOptions.IgnoreCase), "CLAUDE.md"),
        (new Regex(@"^agents?\.(md|markdown|txt)$", RegexOptions.IgnoreCase), "AGENTS.md"),
        (new Regex(@"^agents?/openai\.(yaml|yml)$", RegexOptions.IgnoreCase), "agents/openai.yaml"),
        (new Regex(@"^~/\.claude/skills?/?$", RegexOptions.IgnoreCase), "~/.claude/skills/"),
        (new Regex(@"^~/\.agents/skills?/?$", RegexOptions.IgnoreCase), "~/.agents/skills/"),
    ];

    private static readonly Regex Backtick = new("`([^`\n]+)`");
    private static readonly Regex ToolShape = new("^[a-z][a-z_]+[a-z]$");
    private static readonly Regex SkillShape = new("^[a-z][a-z-]+[a-z]$");

    private static readonly string[] DocPaths = ["README.md", Path.Combine("docs", "getting-started.md")];

    public static TheoryData<string> AllDocs()
    {
        TheoryData<string> data = [];
        foreach (string doc in DocPaths)
        {
            data.Add(doc);
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

    private static string ReadDoc(string relativePath) =>
        File.ReadAllText(Path.Combine(RepoRoot(), relativePath));

    // Inline-code tokens only: the contents of single-backtick spans. Fenced blocks have no surrounding
    // single backticks, and multi-token spans carry spaces/pipes that fail every shape/family regex below.
    private static IEnumerable<string> InlineTokens(string body)
    {
        foreach (Match m in Backtick.Matches(body))
        {
            yield return m.Groups[1].Value.Trim();
        }
    }

    [Theory]
    [MemberData(nameof(AllDocs))]
    public void EveryToolNameMentionedIsRegistered(string doc)
    {
        string body = ReadDoc(doc);
        List<string> offenders = [];
        foreach (string token in InlineTokens(body))
        {
            if (Tools.Contains(token))
            {
                continue; // registered tool reference
            }

            if (ToolShape.IsMatch(token) && token.Contains('_', StringComparison.Ordinal) && token.Length is >= 4 and <= 30)
            {
                offenders.Add(token); // tool-shaped (underscore) but not registered => typo or stale rename
            }
            // otherwise: an ordinary word / arg name / path / skill name — skip
        }

        Assert.True(offenders.Count == 0, $"{doc}: unregistered tool-shaped identifiers: {string.Join(", ", offenders)}");
    }

    [Theory]
    [MemberData(nameof(AllDocs))]
    public void EverySkillNameMentionedIsRegistered(string doc)
    {
        string body = ReadDoc(doc);
        List<string> offenders = [];
        foreach (string token in InlineTokens(body))
        {
            if (Skills.Contains(token))
            {
                continue; // catalog skill reference
            }

            if (SkillShape.IsMatch(token) && token.Contains('-', StringComparison.Ordinal) && token.Length is >= 6 and <= 30)
            {
                offenders.Add(token); // skill-shaped (hyphen) but not in the 7-skill catalog => typo or stale rename
            }
        }

        Assert.True(offenders.Count == 0, $"{doc}: skill-shaped identifiers not in the 7-skill catalog: {string.Join(", ", offenders)}");
    }

    [Theory]
    [MemberData(nameof(AllDocs))]
    public void EveryFilePathMentionedIsCanonical(string doc)
    {
        string body = ReadDoc(doc);
        List<string> offenders = [];
        foreach (string token in InlineTokens(body))
        {
            foreach ((Regex family, string canonical) in PathFamilies)
            {
                if (family.IsMatch(token) && !string.Equals(token, canonical, StringComparison.Ordinal))
                {
                    offenders.Add($"{token} (expected {canonical})");
                }
            }
        }

        Assert.True(offenders.Count == 0, $"{doc}: non-canonical project-level file paths: {string.Join("; ", offenders)}");
    }

    [Fact]
    public void BothDocsExistAndAreNonEmpty()
    {
        string root = RepoRoot();
        foreach (string doc in DocPaths)
        {
            string path = Path.Combine(root, doc);
            Assert.True(File.Exists(path), $"missing doc: {doc}");
            Assert.True(new FileInfo(path).Length > 0, $"empty doc: {doc}");
        }
    }

    [Fact]
    public void AllSevenSkillsAreReferencedAcrossTheDocs()
    {
        string combined = string.Concat(DocPaths.Select(ReadDoc));
        HashSet<string> mentioned = new(StringComparer.Ordinal);
        foreach (string token in InlineTokens(combined))
        {
            if (Skills.Contains(token))
            {
                mentioned.Add(token);
            }
        }

        List<string> missing = [.. Skills.Except(mentioned)];
        Assert.True(missing.Count == 0, $"skills never referenced in the docs: {string.Join(", ", missing)}");
    }
}

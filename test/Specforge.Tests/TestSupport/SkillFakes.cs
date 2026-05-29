using System.Text;

using Specforge.Core.Skills;

namespace Specforge.Tests.TestSupport;

/// <summary>In-memory skill catalog doubles for the installer tests (no embedded resources / no real home dir).</summary>
public static class SkillFakes
{
    public static EmbeddedSkill Skill(string name, string body = "body", params (string Rel, string Content)[] extras)
    {
        EmbeddedSkillFile markdown = new($"specforge.skills/{name}/SKILL.md", () => new MemoryStream(Encoding.UTF8.GetBytes(body)));
        List<EmbeddedSkillFile> files = [.. extras.Select(e => new EmbeddedSkillFile($"specforge.skills/{name}/{e.Rel}", () => new MemoryStream(Encoding.UTF8.GetBytes(e.Content))))];
        return new EmbeddedSkill(name, "A test skill.", new Dictionary<string, object>(), markdown, files);
    }

    public sealed class FakeCatalog(IReadOnlyList<EmbeddedSkill> skills) : IEmbeddedSkillCatalog
    {
        public IReadOnlyList<EmbeddedSkill> GetSkills() => skills;
    }
}

/// <summary>Redirects both agents' install roots to throwaway temp directories.</summary>
public sealed class TempSkillTargetResolver : ISkillInstallTargetResolver, IDisposable
{
    public TempSkillTargetResolver()
    {
        string baseDir = Path.Combine(Path.GetTempPath(), "specforge-tests", Guid.NewGuid().ToString("n"));
        ClaudeRoot = Path.Combine(baseDir, "claude");
        CodexRoot = Path.Combine(baseDir, "codex");
    }

    public string ClaudeRoot { get; }

    public string CodexRoot { get; }

    public string Resolve(SkillInstallAgent agent) => agent switch
    {
        SkillInstallAgent.ClaudeCode => ClaudeRoot,
        SkillInstallAgent.Codex => CodexRoot,
        _ => throw new ArgumentOutOfRangeException(nameof(agent), agent, "concrete agent expected"),
    };

    public void Dispose()
    {
        foreach (string directory in new[] { ClaudeRoot, CodexRoot })
        {
            try
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, recursive: true);
                }
            }
            catch (IOException)
            {
                // best-effort cleanup
            }
        }
    }
}

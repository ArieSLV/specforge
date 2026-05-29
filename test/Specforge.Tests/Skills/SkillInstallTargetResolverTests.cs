using Specforge.Core.Skills;

using Xunit;

namespace Specforge.Tests.Skills;

public class SkillInstallTargetResolverTests
{
    private static readonly SkillInstallTargetResolver Resolver = new();

    [Fact]
    public void Resolve_ClaudeCode_EndsWithClaudeSkills()
    {
        string path = Resolver.Resolve(SkillInstallAgent.ClaudeCode);
        Assert.EndsWith(Path.Combine(".claude", "skills"), path, StringComparison.Ordinal);
    }

    [Fact]
    public void Resolve_Codex_EndsWithAgentsSkills()
    {
        string path = Resolver.Resolve(SkillInstallAgent.Codex);
        Assert.EndsWith(Path.Combine(".agents", "skills"), path, StringComparison.Ordinal);
    }

    [Fact]
    public void Resolve_All_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Resolver.Resolve(SkillInstallAgent.All));
}

using Microsoft.Extensions.Logging.Abstractions;

using Specforge.Core.Diagnostics;
using Specforge.Core.Skills;
using Specforge.Tests.TestSupport;

using Xunit;

namespace Specforge.Tests.Skills;

public class SkillInstallerTests
{
    private static SkillInstaller InstallerFor(ISkillInstallTargetResolver resolver, params EmbeddedSkill[] skills) =>
        new(new SkillFakes.FakeCatalog(skills), resolver, new BinaryInfo(), NullLogger<SkillInstaller>.Instance);

    [Fact]
    public async Task HappyPath_WritesSkillMarkdownForBothAgents()
    {
        using TempSkillTargetResolver resolver = new();
        SkillInstaller installer = InstallerFor(resolver, SkillFakes.Skill("alpha"));

        SkillInstallResult result = await installer.InstallAsync(SkillInstallAgent.All, dryRun: false, CancellationToken.None);

        Assert.True(result.Agents.ContainsKey("claude-code"));
        Assert.True(result.Agents.ContainsKey("codex"));
        Assert.Equal(1, result.Agents["claude-code"].WrittenCount);
        Assert.True(File.Exists(Path.Combine(resolver.ClaudeRoot, "alpha", "SKILL.md")));
        Assert.True(File.Exists(Path.Combine(resolver.CodexRoot, "alpha", "SKILL.md")));
    }

    [Fact]
    public async Task WritesSupportingFiles()
    {
        using TempSkillTargetResolver resolver = new();
        SkillInstaller installer = InstallerFor(resolver, SkillFakes.Skill("beta", "body", ("helpers/note.txt", "hi")));

        SkillInstallResult result = await installer.InstallAsync(SkillInstallAgent.ClaudeCode, dryRun: false, CancellationToken.None);

        Assert.Equal(2, result.Agents["claude-code"].WrittenCount);
        Assert.True(File.Exists(Path.Combine(resolver.ClaudeRoot, "beta", "SKILL.md")));
        Assert.True(File.Exists(Path.Combine(resolver.ClaudeRoot, "beta", "helpers", "note.txt")));
    }

    [Fact]
    public async Task SingleAgent_OmitsOtherAgent()
    {
        using TempSkillTargetResolver resolver = new();
        SkillInstaller installer = InstallerFor(resolver, SkillFakes.Skill("alpha"));

        SkillInstallResult result = await installer.InstallAsync(SkillInstallAgent.ClaudeCode, dryRun: false, CancellationToken.None);

        Assert.True(result.Agents.ContainsKey("claude-code"));
        Assert.False(result.Agents.ContainsKey("codex"));
    }

    [Fact]
    public async Task EmptyCatalog_ReturnsZeroCountsAndWritesNothing()
    {
        using TempSkillTargetResolver resolver = new();
        SkillInstaller installer = InstallerFor(resolver);

        SkillInstallResult result = await installer.InstallAsync(SkillInstallAgent.All, dryRun: false, CancellationToken.None);

        Assert.Equal(0, result.Agents["claude-code"].WrittenCount);
        Assert.Equal(0, result.Agents["codex"].WrittenCount);
        Assert.False(Directory.Exists(resolver.ClaudeRoot));
    }

    [Fact]
    public async Task DryRun_PlansButWritesNothing()
    {
        using TempSkillTargetResolver resolver = new();
        SkillInstaller installer = InstallerFor(resolver, SkillFakes.Skill("alpha"));

        SkillInstallResult result = await installer.InstallAsync(SkillInstallAgent.All, dryRun: true, CancellationToken.None);

        Assert.Equal(1, result.Agents["claude-code"].WrittenCount);
        Assert.False(Directory.Exists(resolver.ClaudeRoot));
        Assert.False(Directory.Exists(resolver.CodexRoot));
    }

    [Fact]
    public async Task OverwriteDetection_ReportsExistingPath()
    {
        using TempSkillTargetResolver resolver = new();
        string existing = Path.Combine(resolver.ClaudeRoot, "alpha", "SKILL.md");
        Directory.CreateDirectory(Path.GetDirectoryName(existing)!);
        await File.WriteAllTextAsync(existing, "old");
        SkillInstaller installer = InstallerFor(resolver, SkillFakes.Skill("alpha"));

        SkillInstallResult result = await installer.InstallAsync(SkillInstallAgent.ClaudeCode, dryRun: false, CancellationToken.None);

        Assert.Contains(existing, result.Agents["claude-code"].OverwrittenPaths);
    }
}

using Microsoft.Extensions.Logging.Abstractions;

using Specforge.Core.Diagnostics;
using Specforge.Core.Exceptions;
using Specforge.Core.Skills;
using Specforge.Tests.TestSupport;

using Xunit;

namespace Specforge.Tests.Skills;

public class PartialFailureTests
{
    // ClaudeCode -> a writable temp dir; Codex -> a path *under an existing file*, so Directory.CreateDirectory
    // throws and the Codex agent fails after ClaudeCode already succeeded.
    private sealed class OneAgentUnwritableResolver : ISkillInstallTargetResolver, IDisposable
    {
        private readonly string _baseDir;

        public OneAgentUnwritableResolver()
        {
            _baseDir = Path.Combine(Path.GetTempPath(), "specforge-tests", Guid.NewGuid().ToString("n"));
            Directory.CreateDirectory(_baseDir);
            ClaudeRoot = Path.Combine(_baseDir, "claude");
            string blockingFile = Path.Combine(_baseDir, "codex-is-a-file");
            File.WriteAllText(blockingFile, "not a directory");
            CodexRoot = Path.Combine(blockingFile, "skills"); // cannot create a directory under a file
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
            try
            {
                if (Directory.Exists(_baseDir))
                {
                    Directory.Delete(_baseDir, recursive: true);
                }
            }
            catch (IOException)
            {
                // best-effort
            }
        }
    }

    [Fact]
    public async Task OneAgentFails_PreservesOtherAgentAndReportsPartialState()
    {
        using OneAgentUnwritableResolver resolver = new();
        SkillInstaller installer = new(
            new SkillFakes.FakeCatalog([SkillFakes.Skill("alpha")]),
            resolver,
            new BinaryInfo(),
            NullLogger<SkillInstaller>.Instance);

        SpecforgeSkillInstallException ex = await Assert.ThrowsAsync<SpecforgeSkillInstallException>(
            () => installer.InstallAsync(SkillInstallAgent.All, dryRun: false, CancellationToken.None));

        Assert.Equal("codex", ex.Agent);
        // ClaudeCode ran first and its write survived (no rollback).
        Assert.Equal(1, ex.PartialResult.Agents["claude-code"].WrittenCount);
        Assert.True(File.Exists(Path.Combine(resolver.ClaudeRoot, "alpha", "SKILL.md")));
        // Codex aborted before writing anything.
        Assert.Equal(0, ex.PartialResult.Agents["codex"].WrittenCount);
    }
}

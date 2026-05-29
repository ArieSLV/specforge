using Microsoft.Extensions.Logging;

using Specforge.Core.Diagnostics;
using Specforge.Core.Exceptions;

namespace Specforge.Core.Skills;

/// <summary>
/// Default <see cref="ISkillInstaller"/> (DEC-005): for each targeted agent, write every embedded
/// skill's files byte-for-byte to <c>&lt;agentRoot&gt;\&lt;skill&gt;\&lt;relative&gt;</c>. Always overwrites; no
/// rollback on partial failure.
/// </summary>
public sealed class SkillInstaller(
    IEmbeddedSkillCatalog catalog,
    ISkillInstallTargetResolver resolver,
    BinaryInfo binaryInfo,
    ILogger<SkillInstaller> logger) : ISkillInstaller
{
    public async Task<SkillInstallResult> InstallAsync(SkillInstallAgent agent, bool dryRun, CancellationToken ct)
    {
        IReadOnlyList<SkillInstallAgent> targets = agent == SkillInstallAgent.All
            ? [SkillInstallAgent.ClaudeCode, SkillInstallAgent.Codex]
            : [agent];

        IReadOnlyList<EmbeddedSkill> skills = catalog.GetSkills();
        Dictionary<string, SkillInstallAgentResult> agentResults = new(StringComparer.Ordinal);

        foreach (SkillInstallAgent target in targets)
        {
            string agentName = AgentName(target);
            string root = resolver.Resolve(target);
            List<string> written = [];
            List<string> overwritten = [];

            foreach (EmbeddedSkill skill in skills)
            {
                string skillDirectory = Path.Combine(root, skill.Name);
                foreach (EmbeddedSkillFile file in EnumerateFiles(skill))
                {
                    ct.ThrowIfCancellationRequested();
                    string relative = RelativePath(file.LogicalPath, skill.Name);
                    string targetPath = Path.Combine(skillDirectory, relative);
                    bool exists = File.Exists(targetPath);

                    if (!dryRun)
                    {
                        try
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
                            await using Stream source = file.OpenStream();
                            await using FileStream destination = File.Create(targetPath);
                            await source.CopyToAsync(destination, ct).ConfigureAwait(false);
                        }
                        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                        {
                            agentResults[agentName] = new SkillInstallAgentResult(written.Count, written, overwritten);
                            SkillInstallResult partial = new(agentResults, binaryInfo.Version);
                            logger.LogError(ex, "install_skills failed writing {Path} for agent {Agent}.", targetPath, agentName);
                            throw new SpecforgeSkillInstallException(agentName, targetPath, ex.GetType(), ex.Message, partial);
                        }
                    }

                    written.Add(targetPath);
                    if (exists)
                    {
                        overwritten.Add(targetPath);
                    }
                }
            }

            agentResults[agentName] = new SkillInstallAgentResult(written.Count, written, overwritten);
        }

        return new SkillInstallResult(agentResults, binaryInfo.Version);
    }

    private static IEnumerable<EmbeddedSkillFile> EnumerateFiles(EmbeddedSkill skill)
    {
        yield return skill.SkillMarkdown;
        foreach (EmbeddedSkillFile file in skill.Files)
        {
            yield return file;
        }
    }

    // Logical paths are "<prefix>/<skill-name>/<relative>"; strip up to and including "/<skill-name>/".
    // Falls back to the bare filename if the marker is absent.
    private static string RelativePath(string logicalPath, string skillName)
    {
        string marker = $"/{skillName}/";
        int index = logicalPath.IndexOf(marker, StringComparison.Ordinal);
        string relative = index >= 0 ? logicalPath[(index + marker.Length)..] : Path.GetFileName(logicalPath);
        return relative.Replace('/', Path.DirectorySeparatorChar);
    }

    private static string AgentName(SkillInstallAgent agent) => agent switch
    {
        SkillInstallAgent.ClaudeCode => "claude-code",
        SkillInstallAgent.Codex => "codex",
        _ => throw new ArgumentOutOfRangeException(nameof(agent), agent, "Concrete agent expected."),
    };
}

namespace Specforge.Core.Skills;

/// <summary>
/// Default <see cref="ISkillInstallTargetResolver"/> reading <see cref="Environment.SpecialFolder.UserProfile"/>
/// (DEC-005). Windows-only for MVP (DEC-001).
/// </summary>
public sealed class SkillInstallTargetResolver : ISkillInstallTargetResolver
{
    public string Resolve(SkillInstallAgent agent)
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return agent switch
        {
            SkillInstallAgent.ClaudeCode => Path.Combine(home, ".claude", "skills"),
            SkillInstallAgent.Codex => Path.Combine(home, ".agents", "skills"),
            _ => throw new ArgumentOutOfRangeException(nameof(agent), agent, "Resolve expects a concrete agent (ClaudeCode or Codex), not All."),
        };
    }
}

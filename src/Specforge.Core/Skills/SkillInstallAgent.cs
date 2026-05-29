namespace Specforge.Core.Skills;

/// <summary>The agent an <c>install_skills</c> call targets (DEC-005). <see cref="All"/> is the aggregate.</summary>
public enum SkillInstallAgent
{
    /// <summary>Claude Code — installs to <c>%USERPROFILE%\.claude\skills\</c>.</summary>
    ClaudeCode,

    /// <summary>Codex CLI — installs to <c>%USERPROFILE%\.agents\skills\</c>.</summary>
    Codex,

    /// <summary>Both agents.</summary>
    All,
}

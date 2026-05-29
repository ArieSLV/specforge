namespace Specforge.Core.Skills;

/// <summary>
/// Resolves the user-wide skill directory for a single agent (DEC-005 target paths). DI-swappable so
/// tests redirect installs to temp directories instead of the real home directory.
/// </summary>
public interface ISkillInstallTargetResolver
{
    /// <summary>Returns the absolute skills root for a concrete agent. <see cref="SkillInstallAgent.All"/> is rejected.</summary>
    string Resolve(SkillInstallAgent agent);
}

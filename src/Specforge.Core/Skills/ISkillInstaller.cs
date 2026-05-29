namespace Specforge.Core.Skills;

/// <summary>
/// Installs the embedded skill catalog to the user-wide agent directories (DEC-005). Idempotent
/// always-overwrite. Returns a <see cref="SkillInstallResult"/> on success; throws
/// <see cref="Exceptions.SpecforgeSkillInstallException"/> (with partial-state data) on a write failure.
/// </summary>
public interface ISkillInstaller
{
    Task<SkillInstallResult> InstallAsync(SkillInstallAgent agent, bool dryRun, CancellationToken ct);
}

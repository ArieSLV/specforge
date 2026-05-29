namespace Specforge.Core.Skills;

/// <summary>Per-agent outcome of an <c>install_skills</c> call (DEC-005).</summary>
/// <param name="WrittenCount">Number of files written (or planned, for a dry run).</param>
/// <param name="WrittenPaths">Absolute target paths written (or planned).</param>
/// <param name="OverwrittenPaths">Subset of <paramref name="WrittenPaths"/> that already existed.</param>
public sealed record SkillInstallAgentResult(
    int WrittenCount,
    IReadOnlyList<string> WrittenPaths,
    IReadOnlyList<string> OverwrittenPaths);

namespace Specforge.Core.Skills;

/// <summary>Result of an <c>install_skills</c> call (DEC-005).</summary>
/// <param name="Agents">Per-agent results, keyed by kebab-case agent name (<c>claude-code</c>, <c>codex</c>).</param>
/// <param name="BinaryVersion">The specforge binary version that performed the install (diagnostic only).</param>
public sealed record SkillInstallResult(
    IReadOnlyDictionary<string, SkillInstallAgentResult> Agents,
    string BinaryVersion);

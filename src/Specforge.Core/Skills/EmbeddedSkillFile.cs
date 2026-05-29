namespace Specforge.Core.Skills;

/// <summary>
/// A supporting file inside a skill directory (everything except the <c>SKILL.md</c> itself).
/// </summary>
/// <param name="LogicalPath">The full embedded-resource logical name.</param>
/// <param name="OpenStream">Opens a fresh read stream over the resource on each call.</param>
public sealed record EmbeddedSkillFile(string LogicalPath, Func<Stream> OpenStream);

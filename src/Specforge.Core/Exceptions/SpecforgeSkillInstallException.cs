using Specforge.Core.Skills;

namespace Specforge.Core.Exceptions;

/// <summary>
/// A filesystem write failed during <c>install_skills</c> (DEC-005). Per the no-rollback rule, the
/// already-written files (this run) stay in place; <see cref="PartialResult"/> reports them.
/// Maps to <c>specforge.skills.install_failed</c>.
/// </summary>
public sealed class SpecforgeSkillInstallException : SpecforgeException
{
    public SpecforgeSkillInstallException(string agent, string path, Type innerExceptionType, string innerMessage, SkillInstallResult partialResult)
        : base(Diagnostics.SpecforgeErrorCode.SkillsInstallFailed, $"Skill install failed for agent '{agent}' at '{path}': {innerMessage}")
    {
        Agent = agent;
        Path = path;
        InnerExceptionType = innerExceptionType;
        InnerMessage = innerMessage;
        PartialResult = partialResult;
    }

    /// <summary>The agent whose install failed.</summary>
    public string Agent { get; }

    /// <summary>The target path that could not be written.</summary>
    public string Path { get; }

    /// <summary>The CLR type of the underlying I/O failure.</summary>
    public Type InnerExceptionType { get; }

    /// <summary>The underlying failure's message.</summary>
    public string InnerMessage { get; }

    /// <summary>Successful agents' results plus the failing agent's pre-failure progress.</summary>
    public SkillInstallResult PartialResult { get; }
}

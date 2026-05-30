namespace Specforge.Core.Init;

/// <summary>Which behavioral files <c>init</c> writes (DEC-008 <c>behavioralFiles</c> arg).</summary>
public enum BehavioralFilesScope
{
    /// <summary><c>SPECFORGE.md</c> + both tagged blocks.</summary>
    All,

    /// <summary><c>SPECFORGE.md</c> + the <c>CLAUDE.md</c> block only.</summary>
    ClaudeOnly,

    /// <summary><c>SPECFORGE.md</c> + the <c>AGENTS.md</c> block only.</summary>
    CodexOnly,

    /// <summary>No behavioral files.</summary>
    None,
}

namespace Specforge.Core.Init;

/// <summary>Full outcome of an <c>init</c> run, with per-area breakdown plus aggregate lists.</summary>
/// <param name="Config">The <c>.specforge.json</c> write.</param>
/// <param name="Behavioral">SPECFORGE.md + tagged-block writes.</param>
/// <param name="Scaffold">Package-skeleton + shared/templates copy writes.</param>
/// <param name="DryRun">Whether this was a planning-only run.</param>
public sealed record InitResult(
    InitSectionResult Config,
    InitSectionResult Behavioral,
    InitSectionResult Scaffold,
    bool DryRun)
{
    /// <summary>All written (or planned) paths across every area.</summary>
    public IReadOnlyList<string> WrittenPaths => [.. Config.WrittenPaths, .. Behavioral.WrittenPaths, .. Scaffold.WrittenPaths];

    /// <summary>All overwritten paths across every area.</summary>
    public IReadOnlyList<string> OverwrittenPaths => [.. Config.OverwrittenPaths, .. Behavioral.OverwrittenPaths, .. Scaffold.OverwrittenPaths];

    /// <summary>All skipped paths across every area.</summary>
    public IReadOnlyList<string> SkippedPaths => [.. Config.SkippedPaths, .. Behavioral.SkippedPaths, .. Scaffold.SkippedPaths];
}

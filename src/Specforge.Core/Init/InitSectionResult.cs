namespace Specforge.Core.Init;

/// <summary>Outcome of one <c>init</c> area (config / behavioral / scaffold).</summary>
/// <param name="WrittenPaths">Paths written (or planned, for a dry run).</param>
/// <param name="OverwrittenPaths">Subset of written paths that already existed.</param>
/// <param name="SkippedPaths">Paths intentionally left untouched (e.g. non-destructive scaffold).</param>
public sealed record InitSectionResult(
    IReadOnlyList<string> WrittenPaths,
    IReadOnlyList<string> OverwrittenPaths,
    IReadOnlyList<string> SkippedPaths)
{
    /// <summary>An empty section.</summary>
    public static InitSectionResult Empty { get; } = new([], [], []);

    /// <summary>Number of paths written (or planned).</summary>
    public int WrittenCount => WrittenPaths.Count;
}

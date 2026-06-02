namespace Specforge.Core.Exceptions;

/// <summary>
/// No <c>.specforge.json</c> was found on or above the search directory (DEC-002 discovery flow).
/// Maps to <c>specforge.config.not_found</c>.
/// </summary>
public sealed class SpecforgeConfigNotFoundException : SpecforgeException
{
    public SpecforgeConfigNotFoundException(string searchedPath)
        : base(Diagnostics.SpecforgeErrorCode.ConfigNotFound,
            $"No .specforge.json found on or above '{searchedPath}'.")
    {
        SearchedPath = searchedPath;
    }

    /// <summary>The absolute directory the walk-up search started from.</summary>
    public string SearchedPath { get; }
}

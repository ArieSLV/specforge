namespace Specforge.Core.Configuration;

/// <summary>
/// DEC-002 discovery: walk up from a start directory to the first <c>.specforge.json</c>.
/// Symlinks are followed by the OS; loops are bounded by a max-depth cutoff.
/// </summary>
public static class ConfigDiscovery
{
    /// <summary>The fixed config file name (DEC-002).</summary>
    public const string ConfigFileName = ".specforge.json";

    /// <summary>Maximum ancestor levels walked before giving up (loop / pathological-tree guard).</summary>
    public const int MaxDepth = 64;

    /// <summary>
    /// Returns the absolute path to the first <see cref="ConfigFileName"/> found on or above
    /// <paramref name="startDir"/>, or <see langword="null"/> if none exists up to the filesystem root
    /// (or the <see cref="MaxDepth"/> cutoff).
    /// </summary>
    public static Task<string?> FindConfigAsync(string startDir, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(startDir);

        DirectoryInfo? dir = new(Path.GetFullPath(startDir));
        for (int depth = 0; dir is not null && depth < MaxDepth; depth++)
        {
            ct.ThrowIfCancellationRequested();
            string candidate = Path.Combine(dir.FullName, ConfigFileName);
            if (File.Exists(candidate))
            {
                return Task.FromResult<string?>(candidate);
            }

            dir = dir.Parent;
        }

        return Task.FromResult<string?>(null);
    }
}

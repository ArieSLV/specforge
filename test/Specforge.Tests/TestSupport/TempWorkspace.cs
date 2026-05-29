namespace Specforge.Tests.TestSupport;

/// <summary>
/// A throwaway temp directory for filesystem-touching tests (discovery, tool cwd). Deleted on dispose.
/// </summary>
public sealed class TempWorkspace : IDisposable
{
    public TempWorkspace()
    {
        Root = Path.Combine(Path.GetTempPath(), "specforge-tests", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(Root);
    }

    /// <summary>Absolute path to the workspace root.</summary>
    public string Root { get; }

    /// <summary>Writes <c>.specforge.json</c> at the root and returns its absolute path.</summary>
    public string WriteConfig(string json)
    {
        string path = Path.Combine(Root, ".specforge.json");
        File.WriteAllText(path, json);
        return path;
    }

    /// <summary>Creates (and returns) a nested subdirectory under the root.</summary>
    public string CreateSubdir(params string[] segments)
    {
        string path = Path.Combine([Root, .. segments]);
        Directory.CreateDirectory(path);
        return path;
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
        catch (IOException)
        {
            // Best-effort cleanup; a locked file should not fail the test.
        }
    }
}

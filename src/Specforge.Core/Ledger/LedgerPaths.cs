using Specforge.Core.Configuration;

namespace Specforge.Core.Ledger;

/// <summary>Resolves the active package's directories from the loaded session config.</summary>
public static class LedgerPaths
{
    /// <summary>Absolute path to the active package's directory.</summary>
    public static string PackageDirectory(SessionState session)
    {
        ArgumentNullException.ThrowIfNull(session);
        SpecforgePackageConfig package = session.RequireActivePackage();
        string configDirectory = Path.GetDirectoryName(session.Config!.ConfigPath) ?? ".";
        return Path.Combine(configDirectory, package.Path);
    }

    /// <summary>Absolute path to a file under the active package's <c>ledger/</c> directory.</summary>
    public static string LedgerFile(SessionState session, string fileName) =>
        Path.Combine(PackageDirectory(session), "ledger", fileName);

    /// <summary>Absolute path to the active package's <c>decisions/</c> directory.</summary>
    public static string DecisionsDirectory(SessionState session) =>
        Path.Combine(PackageDirectory(session), "decisions");

    /// <summary>Absolute path to the active package's <c>items/</c> directory.</summary>
    public static string ItemsDirectory(SessionState session) =>
        Path.Combine(PackageDirectory(session), "items");
}

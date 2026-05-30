using Specforge.Core.Configuration;

namespace Specforge.Tests.TestSupport;

/// <summary>
/// A temp single-package workspace (config + decisions/ + items/ + seeded ledger files) with a
/// <see cref="SessionState"/> pre-initialized to it, for spec-graph service/tool tests.
/// </summary>
public sealed class PackageWorkspace : IDisposable
{
    public PackageWorkspace(IReadOnlyList<string>? extraKinds = null)
    {
        Root = Path.Combine(Path.GetTempPath(), "specforge-tests", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(DecisionsDirectory);
        Directory.CreateDirectory(ItemsDirectory);
        Directory.CreateDirectory(LedgerDirectory);

        File.WriteAllText(Path.Combine(LedgerDirectory, "artifacts.md"),
            "# Artifact Ledger\n\nStatus: Draft\n\n| LedgerId | Artifact | Status | Depends on | Owner / reviewer | Last update | Next action | Blocking question |\n|---|---|---|---|---|---|---|---|\n");
        File.WriteAllText(Path.Combine(LedgerDirectory, "items.md"),
            "# Item Ledger\n\nStatus: Draft\n\n| LedgerId | Item Spec | Status | Depends on | Owner / reviewer | Last update | Next action | Blocking question |\n|---|---|---|---|---|---|---|---|\n");
        File.WriteAllText(Path.Combine(LedgerDirectory, "history.md"),
            "# History Ledger\n\nStatus: Draft\n\n| Date | LedgerId | Event | Detail |\n|---|---|---|---|\n");
        File.WriteAllText(Path.Combine(LedgerDirectory, "reviews.md"),
            "# Review Ledger\n\nStatus: Draft\n\n| LedgerId | Target | Reviewer | Outcome | Date | Notes |\n|---|---|---|---|---|---|\n");
        File.WriteAllText(Path.Combine(LedgerDirectory, "commits.md"),
            "# Commit Ledger\n\nStatus: Draft\n\n| LedgerId | Commit SHA | Linked artifacts/items | Branch | Date | Note |\n|---|---|---|---|---|---|\n");

        SpecforgePackageConfig package = new("p", "spec/packages/p", extraKinds ?? SpecforgePackageConfig.NoExtraKinds);
        SpecforgeConfig config = new(1, "spec/shared", "spec/templates", [package], Path.Combine(Root, ".specforge.json"));
        Session = new SessionState();
        Session.Initialize(config);
    }

    public string Root { get; }

    public SessionState Session { get; }

    public string PackageDirectory => Path.Combine(Root, "spec", "packages", "p");

    public string DecisionsDirectory => Path.Combine(PackageDirectory, "decisions");

    public string ItemsDirectory => Path.Combine(PackageDirectory, "items");

    public string LedgerDirectory => Path.Combine(PackageDirectory, "ledger");

    public Task<string> ReadLedgerAsync(string fileName) => File.ReadAllTextAsync(Path.Combine(LedgerDirectory, fileName));

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
            // best-effort
        }
    }
}

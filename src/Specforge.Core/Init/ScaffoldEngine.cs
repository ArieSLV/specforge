namespace Specforge.Core.Init;

/// <summary>
/// Scaffold mode (DEC-002 "generate a conforming layout"): creates package directory skeletons and
/// copies embedded canonical shared/templates content. Non-destructive — existing files are skipped.
/// </summary>
public sealed class ScaffoldEngine(IEmbeddedTemplateCatalog templateCatalog)
{
    public async Task<InitSectionResult> RunAsync(InitOptions options, CancellationToken ct)
    {
        List<string> written = [];
        List<string> skipped = [];

        foreach (InitPackageInput package in options.Packages)
        {
            string packageRoot = Combine(options.Root, package.Path);
            foreach ((string relative, string content) in PackageSkeleton(package.Name))
            {
                await WriteIfAbsentAsync(Path.Combine(packageRoot, relative.Replace('/', Path.DirectorySeparatorChar)), content, options.DryRun, written, skipped, ct).ConfigureAwait(false);
            }
        }

        await CopyAsync(templateCatalog.GetShared(), Combine(options.Root, options.Shared), options.DryRun, written, skipped, ct).ConfigureAwait(false);
        await CopyAsync(templateCatalog.GetTemplates(), Combine(options.Root, options.Templates), options.DryRun, written, skipped, ct).ConfigureAwait(false);

        return new InitSectionResult(written, [], skipped);
    }

    private static string Combine(string root, string relative) =>
        Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));

    private static IEnumerable<(string Relative, string Content)> PackageSkeleton(string name)
    {
        yield return ("README.md", $"# {name}\n\nStatus: Draft\n\nSpec package managed by specforge.\n");
        yield return ("work_plan.md", $"# {name} Work Plan\n\nStatus: Draft\n");
        yield return ("work_ledger.md", $"# {name} Work Ledger\n\nStatus: Draft\n");
        yield return ("decisions/README.md", "# Decisions\n\nStatus: Draft\n\nDecision records (DEC-NNN) for this package.\n");
        yield return ("items/README.md", "# Items\n\nStatus: Draft\n\nItem specs (ITEM-NNN) for this package.\n");
        yield return ("ledger/README.md", "# Ledger\n\nStatus: Draft\n");
        yield return ("ledger/artifacts.md", "# Artifact Ledger\n\nStatus: Draft\n\n| LedgerId | Artifact | Status | Depends on | Owner / reviewer | Last update | Next action | Blocking question |\n|---|---|---|---|---|---|---|---|\n");
        yield return ("ledger/items.md", "# Item Ledger\n\nStatus: Draft\n");
        yield return ("ledger/commits.md", "# Commit Ledger\n\nStatus: Draft\n\n| LedgerId | Commit SHA | Linked artifacts/items | Branch | Date | Note |\n|---|---|---|---|---|---|\n");
        yield return ("ledger/reviews.md", "# Review Ledger\n\nStatus: Draft\n\n| LedgerId | Target | Reviewer | Outcome | Date | Notes |\n|---|---|---|---|---|---|\n");
        yield return ("ledger/history.md", "# History Ledger\n\nStatus: Draft\n\n| Date | LedgerId | Event | Detail |\n|---|---|---|---|\n");
    }

    private static async Task WriteIfAbsentAsync(string target, string content, bool dryRun, List<string> written, List<string> skipped, CancellationToken ct)
    {
        if (File.Exists(target))
        {
            skipped.Add(target);
            return;
        }

        written.Add(target);
        if (!dryRun)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            await File.WriteAllTextAsync(target, content, ct).ConfigureAwait(false);
        }
    }

    private static async Task CopyAsync(IReadOnlyList<EmbeddedTemplate> templates, string destinationRoot, bool dryRun, List<string> written, List<string> skipped, CancellationToken ct)
    {
        foreach (EmbeddedTemplate template in templates)
        {
            string target = Path.Combine(destinationRoot, template.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(target))
            {
                skipped.Add(target);
                continue;
            }

            written.Add(target);
            if (!dryRun)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                await using Stream source = template.OpenStream();
                await using FileStream destination = File.Create(target);
                await source.CopyToAsync(destination, ct).ConfigureAwait(false);
            }
        }
    }
}

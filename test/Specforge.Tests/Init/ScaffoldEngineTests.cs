using Specforge.Core.Init;
using Specforge.Tests.TestSupport;

using Xunit;

namespace Specforge.Tests.Init;

public class ScaffoldEngineTests
{
    private static InitOptions Options(string root, IEmbeddedTemplateCatalog _) =>
        new(root, [new InitPackageInput("pkg", "spec/packages/pkg")], Scaffold: true);

    [Fact]
    public async Task Run_FreshDir_CreatesPackageSkeleton()
    {
        using TempWorkspace ws = new();
        ScaffoldEngine engine = new(FakeTemplateCatalog.Empty);

        await engine.RunAsync(Options(ws.Root, FakeTemplateCatalog.Empty), CancellationToken.None);

        string packageRoot = Path.Combine(ws.Root, "spec", "packages", "pkg");
        Assert.True(File.Exists(Path.Combine(packageRoot, "README.md")));
        Assert.True(File.Exists(Path.Combine(packageRoot, "decisions", "README.md")));
        Assert.True(File.Exists(Path.Combine(packageRoot, "ledger", "artifacts.md")));
    }

    [Fact]
    public async Task Run_NonDestructive_SkipsExistingFile()
    {
        using TempWorkspace ws = new();
        string packageRoot = Path.Combine(ws.Root, "spec", "packages", "pkg");
        Directory.CreateDirectory(packageRoot);
        string readme = Path.Combine(packageRoot, "README.md");
        await File.WriteAllTextAsync(readme, "USER CONTENT");
        ScaffoldEngine engine = new(FakeTemplateCatalog.Empty);

        InitSectionResult result = await engine.RunAsync(Options(ws.Root, FakeTemplateCatalog.Empty), CancellationToken.None);

        Assert.Contains(readme, result.SkippedPaths);
        Assert.Equal("USER CONTENT", await File.ReadAllTextAsync(readme));
    }

    [Fact]
    public async Task Run_CopiesSharedTemplates()
    {
        using TempWorkspace ws = new();
        FakeTemplateCatalog catalog = new(
            [FakeTemplateCatalog.Template("specforge.shared/", "glossary.md", "# Glossary")],
            [FakeTemplateCatalog.Template("specforge.templates/", "item_spec.md", "# Item template")]);
        ScaffoldEngine engine = new(catalog);

        await engine.RunAsync(new InitOptions(ws.Root, [new InitPackageInput("pkg", "spec/packages/pkg")], Scaffold: true), CancellationToken.None);

        Assert.True(File.Exists(Path.Combine(ws.Root, "spec", "shared", "glossary.md")));
        Assert.True(File.Exists(Path.Combine(ws.Root, "spec", "templates", "item_spec.md")));
    }

    [Fact]
    public async Task Run_DryRun_WritesNothing()
    {
        using TempWorkspace ws = new();
        ScaffoldEngine engine = new(FakeTemplateCatalog.Empty);

        InitSectionResult result = await engine.RunAsync(
            new InitOptions(ws.Root, [new InitPackageInput("pkg", "spec/packages/pkg")], Scaffold: true, DryRun: true),
            CancellationToken.None);

        Assert.NotEmpty(result.WrittenPaths);
        Assert.False(Directory.Exists(Path.Combine(ws.Root, "spec", "packages", "pkg")));
    }
}

using Specforge.Core.Diagnostics;
using Specforge.Core.Init;
using Specforge.Tests.TestSupport;

using Xunit;

namespace Specforge.Tests.Init;

public class InitServiceTests
{
    private static InitService Service() => new(
        new ConfigWriter(),
        new SpecforgeMdGenerator(new BinaryInfo()),
        new TaggedBlockMerger(),
        new CodexOpenaiYamlWriter(),
        new ScaffoldEngine(FakeTemplateCatalog.Empty));

    private static InitOptions Options(
        string root,
        BehavioralFilesScope scope = BehavioralFilesScope.All,
        bool scaffold = false,
        bool dryRun = false,
        IReadOnlyList<InitPackageInput>? packages = null) =>
        new(root, packages ?? [new InitPackageInput("specforge-mvp", "spec/packages/specforge-mvp")], BehavioralFiles: scope, Scaffold: scaffold, DryRun: dryRun);

    [Fact]
    public async Task Run_All_WritesEveryFile()
    {
        using TempWorkspace ws = new();
        await Service().RunAsync(Options(ws.Root), CancellationToken.None);

        Assert.True(File.Exists(Path.Combine(ws.Root, ".specforge.json")));
        Assert.True(File.Exists(Path.Combine(ws.Root, "SPECFORGE.md")));
        Assert.True(File.Exists(Path.Combine(ws.Root, "CLAUDE.md")));
        Assert.True(File.Exists(Path.Combine(ws.Root, "AGENTS.md")));
        Assert.True(File.Exists(Path.Combine(ws.Root, "agents", "openai.yaml")));
    }

    [Fact]
    public async Task Run_None_WritesConfigAndYamlOnly()
    {
        using TempWorkspace ws = new();
        await Service().RunAsync(Options(ws.Root, BehavioralFilesScope.None), CancellationToken.None);

        Assert.True(File.Exists(Path.Combine(ws.Root, ".specforge.json")));
        Assert.True(File.Exists(Path.Combine(ws.Root, "agents", "openai.yaml")));
        Assert.False(File.Exists(Path.Combine(ws.Root, "SPECFORGE.md")));
        Assert.False(File.Exists(Path.Combine(ws.Root, "CLAUDE.md")));
        Assert.False(File.Exists(Path.Combine(ws.Root, "AGENTS.md")));
    }

    [Fact]
    public async Task Run_ClaudeOnly_OmitsAgentsMd()
    {
        using TempWorkspace ws = new();
        await Service().RunAsync(Options(ws.Root, BehavioralFilesScope.ClaudeOnly), CancellationToken.None);

        Assert.True(File.Exists(Path.Combine(ws.Root, "SPECFORGE.md")));
        Assert.True(File.Exists(Path.Combine(ws.Root, "CLAUDE.md")));
        Assert.False(File.Exists(Path.Combine(ws.Root, "AGENTS.md")));
    }

    [Fact]
    public async Task Run_DryRun_WritesNothing()
    {
        using TempWorkspace ws = new();
        InitResult result = await Service().RunAsync(Options(ws.Root, dryRun: true), CancellationToken.None);

        Assert.NotEmpty(result.WrittenPaths);
        Assert.False(File.Exists(Path.Combine(ws.Root, ".specforge.json")));
        Assert.False(File.Exists(Path.Combine(ws.Root, "SPECFORGE.md")));
    }

    [Fact]
    public async Task Run_Scaffold_CreatesPackageSkeleton()
    {
        using TempWorkspace ws = new();
        await Service().RunAsync(Options(ws.Root, scaffold: true), CancellationToken.None);

        Assert.True(File.Exists(Path.Combine(ws.Root, "spec", "packages", "specforge-mvp", "README.md")));
    }

    [Fact]
    public async Task Run_ReInit_ReplacesPackages()
    {
        using TempWorkspace ws = new();
        await Service().RunAsync(Options(ws.Root, packages: [new InitPackageInput("alpha", "spec/packages/alpha"), new InitPackageInput("beta", "spec/packages/beta")]), CancellationToken.None);
        await Service().RunAsync(Options(ws.Root, packages: [new InitPackageInput("gamma", "spec/packages/gamma")]), CancellationToken.None);

        string json = await File.ReadAllTextAsync(Path.Combine(ws.Root, ".specforge.json"));
        Assert.Contains("gamma", json, StringComparison.Ordinal);
        Assert.DoesNotContain("alpha", json, StringComparison.Ordinal);
        Assert.DoesNotContain("beta", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Run_Idempotent_SecondRunByteEqual()
    {
        using TempWorkspace ws = new();
        InitService service = Service();

        await service.RunAsync(Options(ws.Root), CancellationToken.None);
        string config1 = await File.ReadAllTextAsync(Path.Combine(ws.Root, ".specforge.json"));
        string md1 = await File.ReadAllTextAsync(Path.Combine(ws.Root, "SPECFORGE.md"));
        string claude1 = await File.ReadAllTextAsync(Path.Combine(ws.Root, "CLAUDE.md"));

        await service.RunAsync(Options(ws.Root), CancellationToken.None);

        Assert.Equal(config1, await File.ReadAllTextAsync(Path.Combine(ws.Root, ".specforge.json")));
        Assert.Equal(md1, await File.ReadAllTextAsync(Path.Combine(ws.Root, "SPECFORGE.md")));
        Assert.Equal(claude1, await File.ReadAllTextAsync(Path.Combine(ws.Root, "CLAUDE.md")));
    }
}

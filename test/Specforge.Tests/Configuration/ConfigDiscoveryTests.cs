using Specforge.Core.Configuration;
using Specforge.Tests.TestSupport;

using Xunit;

namespace Specforge.Tests.Configuration;

public class ConfigDiscoveryTests
{
    [Fact]
    public async Task FindConfigAsync_WalksUp_FindsConfigFromDeepCwd()
    {
        using TempWorkspace ws = new();
        string expected = ws.WriteConfig(ConfigJson.Single());
        string deepCwd = ws.CreateSubdir("a", "b", "c");

        string? found = await ConfigDiscovery.FindConfigAsync(deepCwd, CancellationToken.None);

        Assert.Equal(Path.GetFullPath(expected), found is null ? null : Path.GetFullPath(found));
    }

    [Fact]
    public async Task FindConfigAsync_ReturnsNull_WhenAbsent()
    {
        using TempWorkspace ws = new();
        string deepCwd = ws.CreateSubdir("x", "y");

        string? found = await ConfigDiscovery.FindConfigAsync(deepCwd, CancellationToken.None);

        Assert.Null(found);
    }

    [Fact]
    public async Task FindConfigAsync_StopsAtMaxDepth()
    {
        using TempWorkspace ws = new();
        ws.WriteConfig(ConfigJson.Single());

        // Construct a path far deeper than the cutoff WITHOUT creating the directories — discovery
        // is pure path walk-up, so the (existing) root config sits beyond MaxDepth and is not found.
        string deep = ws.Root;
        for (int i = 0; i < ConfigDiscovery.MaxDepth + 5; i++)
        {
            deep = Path.Combine(deep, "d");
        }

        string? found = await ConfigDiscovery.FindConfigAsync(deep, CancellationToken.None);

        Assert.Null(found);
    }
}

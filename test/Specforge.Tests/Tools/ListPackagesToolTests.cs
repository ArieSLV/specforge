using System.Text.Json;

using Specforge.Core.Configuration;
using Specforge.Mcp.Hosting;
using Specforge.Mcp.Tools;
using Specforge.Tests.TestSupport;

using Xunit;

namespace Specforge.Tests.Tools;

public class ListPackagesToolTests
{
    private static JsonElement EmptyArgs => JsonSerializer.Deserialize<JsonElement>("{}");

    [Fact]
    public async Task ReturnsPackages()
    {
        using TempWorkspace ws = new();
        ws.WriteConfig(ConfigJson.Single());
        ListPackagesTool tool = new(new ConfigLoader(), new SessionState(), new SpecforgeWorkingDirectory(ws.Root));

        ToolResult result = await tool.InvokeAsync(EmptyArgs, CancellationToken.None);

        JsonElement payload = Assert.IsType<ToolResult.Success>(result).Payload;
        List<JsonElement> packages = [.. payload.GetProperty("packages").EnumerateArray()];
        JsonElement only = Assert.Single(packages);
        Assert.Equal("specforge-mvp", only.GetProperty("name").GetString());
        Assert.Equal("spec/packages/specforge-mvp", only.GetProperty("path").GetString());
    }

    [Fact]
    public async Task EmptyWhenNoConfig()
    {
        using TempWorkspace ws = new();
        ListPackagesTool tool = new(new ConfigLoader(), new SessionState(), new SpecforgeWorkingDirectory(ws.Root));

        ToolResult result = await tool.InvokeAsync(EmptyArgs, CancellationToken.None);

        JsonElement payload = Assert.IsType<ToolResult.Success>(result).Payload;
        Assert.Empty(payload.GetProperty("packages").EnumerateArray());
    }
}

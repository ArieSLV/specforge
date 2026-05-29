using System.Text.Json;

using Specforge.Core.Configuration;
using Specforge.Core.Diagnostics;
using Specforge.Mcp.Hosting;
using Specforge.Mcp.Tools;
using Specforge.Tests.TestSupport;

using Xunit;

namespace Specforge.Tests.Tools;

public class InfoToolTests
{
    private static JsonElement EmptyArgs => JsonSerializer.Deserialize<JsonElement>("{}");

    private static InfoTool ToolFor(string startDir) =>
        new(new ConfigLoader(), new SessionState(), new SpecforgeWorkingDirectory(startDir), new BinaryInfo());

    [Fact]
    public async Task PreConfig_ReturnsNullConfigPath()
    {
        using TempWorkspace ws = new();

        ToolResult result = await ToolFor(ws.Root).InvokeAsync(EmptyArgs, CancellationToken.None);

        JsonElement payload = Assert.IsType<ToolResult.Success>(result).Payload;
        Assert.Equal(JsonValueKind.Null, payload.GetProperty("configPath").ValueKind);
        Assert.Equal(JsonValueKind.Null, payload.GetProperty("activePackage").ValueKind);
        Assert.False(string.IsNullOrEmpty(payload.GetProperty("binaryVersion").GetString()));

        List<int> range = [.. payload.GetProperty("supportedSchemaVersionRange").EnumerateArray().Select(e => e.GetInt32())];
        Assert.Equal(2, range.Count);
        Assert.Equal(1, range[0]);
        Assert.Equal(1, range[1]);
    }

    [Fact]
    public async Task SinglePackage_ReturnsActiveName()
    {
        using TempWorkspace ws = new();
        ws.WriteConfig(ConfigJson.Single());

        ToolResult result = await ToolFor(ws.Root).InvokeAsync(EmptyArgs, CancellationToken.None);

        JsonElement payload = Assert.IsType<ToolResult.Success>(result).Payload;
        Assert.Equal("specforge-mvp", payload.GetProperty("activePackage").GetString());
        Assert.Equal(JsonValueKind.String, payload.GetProperty("configPath").ValueKind);
    }

    [Fact]
    public async Task MultiPackageNoSelection_ActivePackageNull()
    {
        using TempWorkspace ws = new();
        ws.WriteConfig(ConfigJson.TwoPackages());

        ToolResult result = await ToolFor(ws.Root).InvokeAsync(EmptyArgs, CancellationToken.None);

        JsonElement payload = Assert.IsType<ToolResult.Success>(result).Payload;
        Assert.Equal(JsonValueKind.Null, payload.GetProperty("activePackage").ValueKind);
        Assert.Equal(JsonValueKind.String, payload.GetProperty("configPath").ValueKind);
    }
}

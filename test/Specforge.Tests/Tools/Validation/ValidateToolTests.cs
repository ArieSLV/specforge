using System.Text.Json;

using Specforge.Core.Configuration;
using Specforge.Mcp.Hosting;
using Specforge.Mcp.Tools;
using Specforge.Mcp.Tools.Validation;
using Specforge.Tests.TestSupport;

using Xunit;

namespace Specforge.Tests.Tools.Validation;

public class ValidateToolTests
{
    private static ValidateTool ToolFor(PackageWorkspace ws) =>
        new(new ConfigLoader(), ws.Session, new SpecforgeWorkingDirectory(ws.Root), SpecGraphFixtures.Service(ws));

    private static JsonElement Args(string json) => JsonSerializer.Deserialize<JsonElement>(json);

    [Fact]
    public async Task InvalidAspect_ReturnsInvalidArgument()
    {
        using PackageWorkspace ws = new();
        ToolResult result = await ToolFor(ws).InvokeAsync(Args("""{"aspect":"bogus"}"""), CancellationToken.None);

        ToolResult.Failure failure = Assert.IsType<ToolResult.Failure>(result);
        Assert.Equal("specforge.tool.invalid_argument", failure.Envelope.Code);
        Assert.Equal("aspect", failure.Envelope.Data!.Value.GetProperty("argument").GetString());
    }

    [Fact]
    public async Task MissingAspect_ReturnsInvalidArgument()
    {
        using PackageWorkspace ws = new();
        ToolResult result = await ToolFor(ws).InvokeAsync(Args("{}"), CancellationToken.None);

        Assert.Equal("specforge.tool.invalid_argument", Assert.IsType<ToolResult.Failure>(result).Envelope.Code);
    }

    [Fact]
    public async Task CleanGraph_ReturnsSuccessWithZeroErrors()
    {
        using PackageWorkspace ws = new();
        ToolResult result = await ToolFor(ws).InvokeAsync(Args("""{"aspect":"all"}"""), CancellationToken.None);

        JsonElement payload = Assert.IsType<ToolResult.Success>(result).Payload;
        Assert.Equal(0, payload.GetProperty("errorCount").GetInt32());
        Assert.Equal("clean", payload.GetProperty("message").GetString());
    }

    [Fact]
    public async Task SadPath_ReturnsSuccessWithFindings()
    {
        using PackageWorkspace ws = new();
        await SpecGraphFixtures.AddArtifactAsync(ws, "ART-DEC-001", "Approved");
        await SpecGraphFixtures.AddArtifactAsync(ws, "ART-DEC-003", "Approved"); // gap at DEC-002

        ToolResult result = await ToolFor(ws).InvokeAsync(Args("""{"aspect":"ids"}"""), CancellationToken.None);

        JsonElement payload = Assert.IsType<ToolResult.Success>(result).Payload; // findings-as-data: SUCCESS even with errors
        Assert.True(payload.GetProperty("errorCount").GetInt32() >= 1);
        Assert.NotEmpty(payload.GetProperty("findings").EnumerateArray());
    }
}

using System.Text.Json;

using Microsoft.Extensions.Logging.Abstractions;

using Specforge.Core.Configuration;
using Specforge.Core.Exceptions;
using Specforge.Mcp.Errors;
using Specforge.Mcp.Hosting;
using Specforge.Mcp.Tools;
using Specforge.Tests.TestSupport;

using Xunit;

namespace Specforge.Tests.Tools;

public class UsePackageToolTests
{
    [Fact]
    public async Task SetsState()
    {
        using TempWorkspace ws = new();
        ws.WriteConfig(ConfigJson.TwoPackages());
        SessionState session = new();
        UsePackageTool tool = new(new ConfigLoader(), session, new SpecforgeWorkingDirectory(ws.Root));

        ToolResult result = await tool.InvokeAsync(JsonSerializer.Deserialize<JsonElement>("""{"name":"beta"}"""), CancellationToken.None);

        JsonElement payload = Assert.IsType<ToolResult.Success>(result).Payload;
        Assert.Equal("beta", payload.GetProperty("current").GetString());
        Assert.Equal(JsonValueKind.Null, payload.GetProperty("previous").ValueKind);
        Assert.Equal("beta", session.ActivePackageName);
    }

    [Fact]
    public async Task UnknownName_Throws_MapsToUnknownPackageEnvelope()
    {
        using TempWorkspace ws = new();
        ws.WriteConfig(ConfigJson.TwoPackages());
        UsePackageTool tool = new(new ConfigLoader(), new SessionState(), new SpecforgeWorkingDirectory(ws.Root));

        SpecforgeUnknownPackageException ex = await Assert.ThrowsAsync<SpecforgeUnknownPackageException>(
            () => tool.InvokeAsync(JsonSerializer.Deserialize<JsonElement>("""{"name":"missing"}"""), CancellationToken.None));

        Assert.Equal("missing", ex.RequestedName);

        McpErrorEnvelope envelope = new EnvelopeRenderer(new ToolExceptionMapper(), NullLogger<EnvelopeRenderer>.Instance).Render(ex);
        Assert.Equal("specforge.package.unknown", envelope.Code);
    }

    [Fact]
    public async Task MissingName_ReturnsInvalidArgument()
    {
        using TempWorkspace ws = new();
        UsePackageTool tool = new(new ConfigLoader(), new SessionState(), new SpecforgeWorkingDirectory(ws.Root));

        ToolResult result = await tool.InvokeAsync(JsonSerializer.Deserialize<JsonElement>("{}"), CancellationToken.None);

        ToolResult.Failure failure = Assert.IsType<ToolResult.Failure>(result);
        Assert.Equal("specforge.tool.invalid_argument", failure.Envelope.Code);
    }
}

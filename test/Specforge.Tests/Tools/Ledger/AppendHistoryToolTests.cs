using System.Text.Json;

using Specforge.Core.Configuration;
using Specforge.Core.Ledger;
using Specforge.Mcp.Hosting;
using Specforge.Mcp.Tools;
using Specforge.Mcp.Tools.Ledger;
using Specforge.Tests.TestSupport;

using Xunit;

namespace Specforge.Tests.Tools.Ledger;

public class AppendHistoryToolTests
{
    private static AppendHistoryTool ToolFor(PackageWorkspace ws) =>
        new(new ConfigLoader(), ws.Session, new SpecforgeWorkingDirectory(ws.Root), new HistoryLedgerService(ws.Session));

    private static JsonElement Args(string json) => JsonSerializer.Deserialize<JsonElement>(json);

    [Fact]
    public async Task LiteralTarget_WritesRow()
    {
        using PackageWorkspace ws = new();
        ToolResult result = await ToolFor(ws).InvokeAsync(Args("""{"target":"(milestone)","event":"Stage 1 complete","detail":"all items approved"}"""), CancellationToken.None);

        Assert.IsType<ToolResult.Success>(result);
        Assert.Contains("(milestone)", await ws.ReadLedgerAsync("history.md"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task LedgerIdTarget_WritesRow()
    {
        using PackageWorkspace ws = new();
        ToolResult result = await ToolFor(ws).InvokeAsync(Args("""{"target":"ART-DEC-001","event":"Note","detail":"a manual note"}"""), CancellationToken.None);

        Assert.IsType<ToolResult.Success>(result);
        Assert.Contains("ART-DEC-001", await ws.ReadLedgerAsync("history.md"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task InvalidTarget_ReturnsInvalidArgument()
    {
        using PackageWorkspace ws = new();
        ToolResult result = await ToolFor(ws).InvokeAsync(Args("""{"target":"(unknown)","event":"X","detail":"Y"}"""), CancellationToken.None);

        ToolResult.Failure failure = Assert.IsType<ToolResult.Failure>(result);
        Assert.Equal("specforge.tool.invalid_argument", failure.Envelope.Code);
        Assert.Equal("target", failure.Envelope.Data!.Value.GetProperty("argument").GetString());
    }

    [Fact]
    public async Task EventTooLong_ReturnsInvalidArgument()
    {
        using PackageWorkspace ws = new();
        JsonElement args = JsonSerializer.SerializeToElement(new { target = "(milestone)", @event = new string('x', 81), detail = "d" });
        ToolResult result = await ToolFor(ws).InvokeAsync(args, CancellationToken.None);

        Assert.Equal("specforge.tool.invalid_argument", Assert.IsType<ToolResult.Failure>(result).Envelope.Code);
    }

    [Fact]
    public async Task DryRun_DoesNotWrite()
    {
        using PackageWorkspace ws = new();
        ToolResult result = await ToolFor(ws).InvokeAsync(Args("""{"target":"(milestone)","event":"NoWrite","detail":"d","dryRun":true}"""), CancellationToken.None);

        Assert.True(Assert.IsType<ToolResult.Success>(result).Payload.GetProperty("dryRun").GetBoolean());
        Assert.DoesNotContain("NoWrite", await ws.ReadLedgerAsync("history.md"), StringComparison.Ordinal);
    }
}

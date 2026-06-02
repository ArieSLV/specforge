using System.Text.Json;

using Specforge.Core.Configuration;
using Specforge.Core.Exceptions;
using Specforge.Core.Ledger;
using Specforge.Mcp.Hosting;
using Specforge.Mcp.Tools;
using Specforge.Mcp.Tools.Ledger;
using Specforge.Tests.TestSupport;

using Xunit;

namespace Specforge.Tests.Tools.Ledger;

public class AppendReviewToolTests
{
    private static AppendReviewTool ToolFor(PackageWorkspace ws) => new(
        new ConfigLoader(),
        ws.Session,
        new SpecforgeWorkingDirectory(ws.Root),
        new ReviewLedgerService(ws.Session),
        new ArtifactLedgerService(ws.Session),
        new HistoryLedgerService(ws.Session));

    private static JsonElement Args(string json) => JsonSerializer.Deserialize<JsonElement>(json);

    private static Task SeedItemAsync(PackageWorkspace ws) =>
        new ArtifactLedgerService(ws.Session).AppendAsync(new ArtifactRow { LedgerId = "ART-ITEM-007", Status = "Approved" }, CancellationToken.None);

    [Fact]
    public async Task MultiEffect_WritesReviewRowAndHistoryEvent()
    {
        using PackageWorkspace ws = new();
        await SeedItemAsync(ws);

        ToolResult result = await ToolFor(ws).InvokeAsync(Args("""{"target":"ART-ITEM-007","reviewer":"User","outcome":"Refined","notes":"tightened scope"}"""), CancellationToken.None);

        Assert.Equal("REV-ITEM-007-001", Assert.IsType<ToolResult.Success>(result).Payload.GetProperty("reviewId").GetString());
        Assert.Contains("REV-ITEM-007-001", await ws.ReadLedgerAsync("reviews.md"), StringComparison.Ordinal);
        string history = await ws.ReadLedgerAsync("history.md");
        Assert.Contains("Reviewed", history, StringComparison.Ordinal);
        Assert.Contains("Refined", history, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PerTargetSequence_Advances()
    {
        using PackageWorkspace ws = new();
        await SeedItemAsync(ws);
        AppendReviewTool tool = ToolFor(ws);

        await tool.InvokeAsync(Args("""{"target":"ART-ITEM-007","reviewer":"User","outcome":"Approved"}"""), CancellationToken.None);
        ToolResult second = await tool.InvokeAsync(Args("""{"target":"ART-ITEM-007","reviewer":"User","outcome":"Refined"}"""), CancellationToken.None);

        Assert.Equal("REV-ITEM-007-002", Assert.IsType<ToolResult.Success>(second).Payload.GetProperty("reviewId").GetString());
    }

    [Fact]
    public async Task NonExistentTarget_ThrowsIdNotFound()
    {
        using PackageWorkspace ws = new();
        SpecforgeIdNotFoundException ex = await Assert.ThrowsAsync<SpecforgeIdNotFoundException>(
            () => ToolFor(ws).InvokeAsync(Args("""{"target":"ART-ITEM-999","reviewer":"User","outcome":"Approved"}"""), CancellationToken.None));
        Assert.Equal("ART-ITEM-999", ex.Id);
    }

    [Fact]
    public async Task Outcome_IsFreeForm()
    {
        using PackageWorkspace ws = new();
        await SeedItemAsync(ws);
        ToolResult result = await ToolFor(ws).InvokeAsync(Args("""{"target":"ART-ITEM-007","reviewer":"User","outcome":"Looks great, ship it"}"""), CancellationToken.None);

        Assert.IsType<ToolResult.Success>(result);
        Assert.Contains("Looks great, ship it", await ws.ReadLedgerAsync("reviews.md"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task DryRun_DoesNotWrite()
    {
        using PackageWorkspace ws = new();
        await SeedItemAsync(ws);
        ToolResult result = await ToolFor(ws).InvokeAsync(Args("""{"target":"ART-ITEM-007","reviewer":"User","outcome":"Approved","dryRun":true}"""), CancellationToken.None);

        Assert.IsType<ToolResult.Success>(result);
        Assert.DoesNotContain("REV-ITEM-007-001", await ws.ReadLedgerAsync("reviews.md"), StringComparison.Ordinal);
    }
}

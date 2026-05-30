using System.Text.Json;

using Specforge.Core.Configuration;
using Specforge.Core.Ledger;
using Specforge.Mcp.Hosting;
using Specforge.Mcp.Tools;
using Specforge.Mcp.Tools.Ledger;
using Specforge.Tests.TestSupport;

using Xunit;

namespace Specforge.Tests.Tools.Ledger;

public class DeleteReviewToolTests
{
    private static SpecforgeWorkingDirectory Cwd(PackageWorkspace ws) => new(ws.Root);

    private static AppendReviewTool AppendTool(PackageWorkspace ws) => new(
        new ConfigLoader(), ws.Session, Cwd(ws),
        new ReviewLedgerService(ws.Session), new ArtifactLedgerService(ws.Session), new HistoryLedgerService(ws.Session));

    private static DeleteReviewTool DeleteTool(PackageWorkspace ws) => new(
        new ConfigLoader(), ws.Session, Cwd(ws), new ReviewLedgerService(ws.Session), new HistoryLedgerService(ws.Session));

    private static JsonElement Args(string json) => JsonSerializer.Deserialize<JsonElement>(json);

    private static Task SeedItemAsync(PackageWorkspace ws) =>
        new ArtifactLedgerService(ws.Session).AppendAsync(new ArtifactRow { LedgerId = "ART-ITEM-007", Status = "Approved" }, CancellationToken.None);

    private static Task AppendReviewAsync(PackageWorkspace ws, string outcome) =>
        AppendTool(ws).InvokeAsync(Args($$"""{"target":"ART-ITEM-007","reviewer":"User","outcome":"{{outcome}}"}"""), CancellationToken.None);

    [Fact]
    public async Task WithoutConfirm_ReturnsInvalidArgument()
    {
        using PackageWorkspace ws = new();
        ToolResult result = await DeleteTool(ws).InvokeAsync(Args("""{"id":"REV-ITEM-007-001"}"""), CancellationToken.None);

        ToolResult.Failure failure = Assert.IsType<ToolResult.Failure>(result);
        Assert.Equal("specforge.tool.invalid_argument", failure.Envelope.Code);
        Assert.Equal("confirm", failure.Envelope.Data!.Value.GetProperty("argument").GetString());
    }

    [Fact]
    public async Task NonExistent_ReturnsIdNotFound()
    {
        using PackageWorkspace ws = new();
        ToolResult result = await DeleteTool(ws).InvokeAsync(Args("""{"id":"REV-DEC-001-001","confirm":true}"""), CancellationToken.None);

        Assert.Equal("specforge.id.not_found", Assert.IsType<ToolResult.Failure>(result).Envelope.Code);
    }

    [Fact]
    public async Task MalformedId_ReturnsInvalidArgument()
    {
        using PackageWorkspace ws = new();
        ToolResult result = await DeleteTool(ws).InvokeAsync(Args("""{"id":"DEC-001","confirm":true}"""), CancellationToken.None);

        Assert.Equal("specforge.tool.invalid_argument", Assert.IsType<ToolResult.Failure>(result).Envelope.Code);
    }

    [Fact]
    public async Task RemovesRow_AppendsDeletedHistory_NoLifecycleGate()
    {
        using PackageWorkspace ws = new();
        await SeedItemAsync(ws);
        await AppendReviewAsync(ws, "Approved");

        ToolResult result = await DeleteTool(ws).InvokeAsync(Args("""{"id":"REV-ITEM-007-001","confirm":true}"""), CancellationToken.None);

        Assert.IsType<ToolResult.Success>(result);
        Assert.DoesNotContain("REV-ITEM-007-001", await ws.ReadLedgerAsync("reviews.md"), StringComparison.Ordinal);
        string history = await ws.ReadLedgerAsync("history.md");
        Assert.Contains("Deleted", history, StringComparison.Ordinal);
        Assert.Contains("Tombstone REV-ITEM-007-001:", history, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SequenceAdvancesPastTombstone()
    {
        using PackageWorkspace ws = new();
        await SeedItemAsync(ws);
        await AppendReviewAsync(ws, "Approved");  // REV-ITEM-007-001
        await AppendReviewAsync(ws, "Refined");   // REV-ITEM-007-002

        await DeleteTool(ws).InvokeAsync(Args("""{"id":"REV-ITEM-007-001","confirm":true}"""), CancellationToken.None);

        ToolResult third = await AppendTool(ws).InvokeAsync(Args("""{"target":"ART-ITEM-007","reviewer":"User","outcome":"Approved"}"""), CancellationToken.None);
        Assert.Equal("REV-ITEM-007-003", Assert.IsType<ToolResult.Success>(third).Payload.GetProperty("reviewId").GetString());
    }

    [Fact]
    public async Task DryRun_DoesNotRemove()
    {
        using PackageWorkspace ws = new();
        await SeedItemAsync(ws);
        await AppendReviewAsync(ws, "Approved");

        ToolResult result = await DeleteTool(ws).InvokeAsync(Args("""{"id":"REV-ITEM-007-001","confirm":true,"dryRun":true}"""), CancellationToken.None);

        Assert.IsType<ToolResult.Success>(result);
        Assert.Contains("REV-ITEM-007-001", await ws.ReadLedgerAsync("reviews.md"), StringComparison.Ordinal);
    }
}

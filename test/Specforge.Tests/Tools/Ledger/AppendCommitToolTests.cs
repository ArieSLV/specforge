using System.Text.Json;

using Specforge.Core.Configuration;
using Specforge.Core.Ledger;
using Specforge.Core.Lifecycle;
using Specforge.Mcp.Hosting;
using Specforge.Mcp.Tools;
using Specforge.Mcp.Tools.Ledger;
using Specforge.Tests.TestSupport;

using Xunit;

namespace Specforge.Tests.Tools.Ledger;

public class AppendCommitToolTests
{
    private static AppendCommitTool ToolFor(PackageWorkspace ws) => new(
        new ConfigLoader(),
        ws.Session,
        new SpecforgeWorkingDirectory(ws.Root),
        new CommitLedgerService(ws.Session, new ArtifactLedgerService(ws.Session)),
        new ArtifactLedgerService(ws.Session),
        new HistoryLedgerService(ws.Session));

    private static JsonElement Args(string json) => JsonSerializer.Deserialize<JsonElement>(json);

    private static async Task SeedAsync(PackageWorkspace ws, bool withPlaceholderCommitsLedger)
    {
        ArtifactLedgerService artifacts = new(ws.Session);
        await artifacts.AppendAsync(new ArtifactRow { LedgerId = "ART-ITEM-001", Status = "Approved" }, CancellationToken.None);
        await artifacts.AppendAsync(new ArtifactRow { LedgerId = "ART-ITEM-002", Status = "Approved" }, CancellationToken.None);
        if (withPlaceholderCommitsLedger)
        {
            await artifacts.AppendAsync(new ArtifactRow { LedgerId = CommitLedgerService.CommitsLedgerArtifactId, Status = LifecycleState.Placeholder }, CancellationToken.None);
        }
    }

    [Fact]
    public async Task MultiEffect_WritesCommitImplementedEventAndPromotes()
    {
        using PackageWorkspace ws = new();
        await SeedAsync(ws, withPlaceholderCommitsLedger: true);

        ToolResult result = await ToolFor(ws).InvokeAsync(Args("""{"target":"ART-ITEM-001","gitRef":"abc1234","detail":"item one impl"}"""), CancellationToken.None);

        JsonElement payload = Assert.IsType<ToolResult.Success>(result).Payload;
        Assert.Equal("CMT-001", payload.GetProperty("id").GetString());
        Assert.True(payload.GetProperty("promotedCommitsLedger").GetBoolean());

        Assert.Contains("abc1234", await ws.ReadLedgerAsync("commits.md"), StringComparison.Ordinal);
        Assert.Contains("Implemented", await ws.ReadLedgerAsync("history.md"), StringComparison.Ordinal);

        ArtifactRow? commitsLedger = await new ArtifactLedgerService(ws.Session).FindByLedgerIdAsync(CommitLedgerService.CommitsLedgerArtifactId, CancellationToken.None);
        Assert.Equal(LifecycleState.Draft, commitsLedger!.Status);
    }

    [Fact]
    public async Task GlobalSequence_Advances()
    {
        using PackageWorkspace ws = new();
        await SeedAsync(ws, withPlaceholderCommitsLedger: false);
        AppendCommitTool tool = ToolFor(ws);

        await tool.InvokeAsync(Args("""{"target":"ART-ITEM-001","gitRef":"aaa1111","detail":"first"}"""), CancellationToken.None);
        ToolResult second = await tool.InvokeAsync(Args("""{"target":"ART-ITEM-002","gitRef":"bbb2222","detail":"second"}"""), CancellationToken.None);

        Assert.Equal("CMT-002", Assert.IsType<ToolResult.Success>(second).Payload.GetProperty("id").GetString());
    }

    [Fact]
    public async Task PromotionIsIdempotent_OnSubsequentCalls()
    {
        using PackageWorkspace ws = new();
        await SeedAsync(ws, withPlaceholderCommitsLedger: true);
        AppendCommitTool tool = ToolFor(ws);

        ToolResult first = await tool.InvokeAsync(Args("""{"target":"ART-ITEM-001","gitRef":"aaa1111","detail":"first"}"""), CancellationToken.None);
        ToolResult second = await tool.InvokeAsync(Args("""{"target":"ART-ITEM-002","gitRef":"bbb2222","detail":"second"}"""), CancellationToken.None);

        Assert.True(Assert.IsType<ToolResult.Success>(first).Payload.GetProperty("promotedCommitsLedger").GetBoolean());
        Assert.False(Assert.IsType<ToolResult.Success>(second).Payload.GetProperty("promotedCommitsLedger").GetBoolean());
    }

    [Fact]
    public async Task GitRefWithWhitespace_ReturnsInvalidArgument()
    {
        using PackageWorkspace ws = new();
        await SeedAsync(ws, withPlaceholderCommitsLedger: false);

        ToolResult result = await ToolFor(ws).InvokeAsync(Args("""{"target":"ART-ITEM-001","gitRef":"ab cd","detail":"x"}"""), CancellationToken.None);

        ToolResult.Failure failure = Assert.IsType<ToolResult.Failure>(result);
        Assert.Equal("specforge.tool.invalid_argument", failure.Envelope.Code);
        Assert.Equal("gitRef", failure.Envelope.Data!.Value.GetProperty("argument").GetString());
    }

    [Fact]
    public async Task NonExistentTarget_ReturnsIdNotFound()
    {
        using PackageWorkspace ws = new();
        await SeedAsync(ws, withPlaceholderCommitsLedger: false);

        ToolResult result = await ToolFor(ws).InvokeAsync(Args("""{"target":"ART-ITEM-404","gitRef":"abc1234","detail":"x"}"""), CancellationToken.None);

        Assert.Equal("specforge.id.not_found", Assert.IsType<ToolResult.Failure>(result).Envelope.Code);
    }

    [Fact]
    public async Task DryRun_DoesNotWrite()
    {
        using PackageWorkspace ws = new();
        await SeedAsync(ws, withPlaceholderCommitsLedger: true);

        ToolResult result = await ToolFor(ws).InvokeAsync(Args("""{"target":"ART-ITEM-001","gitRef":"abc1234","detail":"x","dryRun":true}"""), CancellationToken.None);

        Assert.Equal("CMT-001", Assert.IsType<ToolResult.Success>(result).Payload.GetProperty("id").GetString());
        Assert.DoesNotContain("abc1234", await ws.ReadLedgerAsync("commits.md"), StringComparison.Ordinal);
        ArtifactRow? commitsLedger = await new ArtifactLedgerService(ws.Session).FindByLedgerIdAsync(CommitLedgerService.CommitsLedgerArtifactId, CancellationToken.None);
        Assert.Equal(LifecycleState.Placeholder, commitsLedger!.Status);
    }
}

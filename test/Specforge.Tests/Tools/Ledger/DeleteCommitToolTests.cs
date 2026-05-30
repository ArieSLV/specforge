using System.Text.Json;

using Specforge.Core.Configuration;
using Specforge.Core.Ledger;
using Specforge.Mcp.Hosting;
using Specforge.Mcp.Tools;
using Specforge.Mcp.Tools.Ledger;
using Specforge.Tests.TestSupport;

using Xunit;

namespace Specforge.Tests.Tools.Ledger;

public class DeleteCommitToolTests
{
    private static SpecforgeWorkingDirectory Cwd(PackageWorkspace ws) => new(ws.Root);

    private static AppendCommitTool AppendTool(PackageWorkspace ws) => new(
        new ConfigLoader(), ws.Session, Cwd(ws),
        new CommitLedgerService(ws.Session, new ArtifactLedgerService(ws.Session)),
        new ArtifactLedgerService(ws.Session), new HistoryLedgerService(ws.Session));

    private static DeleteCommitTool DeleteTool(PackageWorkspace ws) => new(
        new ConfigLoader(), ws.Session, Cwd(ws),
        new CommitLedgerService(ws.Session, new ArtifactLedgerService(ws.Session)), new HistoryLedgerService(ws.Session));

    private static JsonElement Args(string json) => JsonSerializer.Deserialize<JsonElement>(json);

    private static async Task SeedItemsAsync(PackageWorkspace ws)
    {
        // Serial appends: ledger writes are single-session (DEC-003); concurrent writes race the temp file.
        ArtifactLedgerService artifacts = new(ws.Session);
        await artifacts.AppendAsync(new ArtifactRow { LedgerId = "ART-ITEM-001", Status = "Approved" }, CancellationToken.None);
        await artifacts.AppendAsync(new ArtifactRow { LedgerId = "ART-ITEM-002", Status = "Approved" }, CancellationToken.None);
    }

    private static Task AppendCommitAsync(PackageWorkspace ws, string target, string gitRef) =>
        AppendTool(ws).InvokeAsync(Args($$"""{"target":"{{target}}","gitRef":"{{gitRef}}","detail":"impl"}"""), CancellationToken.None);

    [Fact]
    public async Task WithoutConfirm_ReturnsInvalidArgument()
    {
        using PackageWorkspace ws = new();
        ToolResult result = await DeleteTool(ws).InvokeAsync(Args("""{"id":"CMT-001"}"""), CancellationToken.None);

        ToolResult.Failure failure = Assert.IsType<ToolResult.Failure>(result);
        Assert.Equal("specforge.tool.invalid_argument", failure.Envelope.Code);
        Assert.Equal("confirm", failure.Envelope.Data!.Value.GetProperty("argument").GetString());
    }

    [Fact]
    public async Task NonExistent_ReturnsIdNotFound()
    {
        using PackageWorkspace ws = new();
        ToolResult result = await DeleteTool(ws).InvokeAsync(Args("""{"id":"CMT-999","confirm":true}"""), CancellationToken.None);

        Assert.Equal("specforge.id.not_found", Assert.IsType<ToolResult.Failure>(result).Envelope.Code);
    }

    [Fact]
    public async Task MalformedId_ReturnsInvalidArgument()
    {
        using PackageWorkspace ws = new();
        ToolResult result = await DeleteTool(ws).InvokeAsync(Args("""{"id":"REV-DEC-001-001","confirm":true}"""), CancellationToken.None);

        Assert.Equal("specforge.tool.invalid_argument", Assert.IsType<ToolResult.Failure>(result).Envelope.Code);
    }

    [Fact]
    public async Task RemovesRow_AppendsDeletedHistory()
    {
        using PackageWorkspace ws = new();
        await SeedItemsAsync(ws);
        await AppendCommitAsync(ws, "ART-ITEM-001", "abc1234");

        ToolResult result = await DeleteTool(ws).InvokeAsync(Args("""{"id":"CMT-001","confirm":true}"""), CancellationToken.None);

        JsonElement payload = Assert.IsType<ToolResult.Success>(result).Payload;
        Assert.Equal("ART-ITEM-001", payload.GetProperty("target").GetString());
        Assert.DoesNotContain("CMT-001", await ws.ReadLedgerAsync("commits.md"), StringComparison.Ordinal);
        string history = await ws.ReadLedgerAsync("history.md");
        Assert.Contains("Deleted", history, StringComparison.Ordinal);
        Assert.Contains("tombstoned by delete_commit", history, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SequenceAdvancesPastTombstone()
    {
        using PackageWorkspace ws = new();
        await SeedItemsAsync(ws);
        await AppendCommitAsync(ws, "ART-ITEM-001", "aaa1111"); // CMT-001
        await AppendCommitAsync(ws, "ART-ITEM-002", "bbb2222"); // CMT-002

        await DeleteTool(ws).InvokeAsync(Args("""{"id":"CMT-001","confirm":true}"""), CancellationToken.None);

        ToolResult third = await AppendTool(ws).InvokeAsync(Args("""{"target":"ART-ITEM-001","gitRef":"ccc3333","detail":"impl"}"""), CancellationToken.None);
        Assert.Equal("CMT-003", Assert.IsType<ToolResult.Success>(third).Payload.GetProperty("id").GetString());
    }

    [Fact]
    public async Task DryRun_DoesNotRemove()
    {
        using PackageWorkspace ws = new();
        await SeedItemsAsync(ws);
        await AppendCommitAsync(ws, "ART-ITEM-001", "abc1234");

        ToolResult result = await DeleteTool(ws).InvokeAsync(Args("""{"id":"CMT-001","confirm":true,"dryRun":true}"""), CancellationToken.None);

        Assert.IsType<ToolResult.Success>(result);
        Assert.Contains("abc1234", await ws.ReadLedgerAsync("commits.md"), StringComparison.Ordinal);
    }
}

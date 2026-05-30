using Specforge.Core.Ledger;
using Specforge.Tests.TestSupport;

using Xunit;

namespace Specforge.Tests.Ledger;

public class LedgerServicesTests
{
    [Fact]
    public async Task Artifact_AppendFindUpdateRemove()
    {
        using PackageWorkspace ws = new();
        ArtifactLedgerService service = new(ws.Session);

        await service.AppendAsync(new ArtifactRow { LedgerId = "ART-DEC-001", Artifact = "decisions/DEC-001.md", Status = "Draft" }, CancellationToken.None);
        ArtifactRow? found = await service.FindByLedgerIdAsync("ART-DEC-001", CancellationToken.None);
        Assert.NotNull(found);
        Assert.Equal("Draft", found!.Status);

        Assert.True(await service.UpdateAsync("ART-DEC-001", r => r.Status = "Approved", CancellationToken.None));
        Assert.Equal("Approved", (await service.FindByLedgerIdAsync("ART-DEC-001", CancellationToken.None))!.Status);

        Assert.True(await service.RemoveAsync("ART-DEC-001", CancellationToken.None));
        Assert.Null(await service.FindByLedgerIdAsync("ART-DEC-001", CancellationToken.None));
    }

    [Fact]
    public async Task History_AppendsRow()
    {
        using PackageWorkspace ws = new();
        await new HistoryLedgerService(ws.Session).AppendAsync("ART-DEC-001", "Created", "create_decision: Topic", CancellationToken.None);

        string history = await ws.ReadLedgerAsync("history.md");
        Assert.Contains("Created", history, StringComparison.Ordinal);
        Assert.Contains("ART-DEC-001", history, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Review_AllocatesPerTargetSeqAndCascades()
    {
        using PackageWorkspace ws = new();
        ReviewLedgerService service = new(ws.Session);

        Assert.Equal("REV-DEC-001-001", await service.AppendAsync("DEC-001", "User", "Approved", "ok", CancellationToken.None));
        Assert.Equal("REV-DEC-001-002", await service.AppendAsync("DEC-001", "User", "Approved", "again", CancellationToken.None));
        Assert.Equal("REV-DEC-002-001", await service.AppendAsync("DEC-002", "User", "Approved", "other", CancellationToken.None));

        Assert.Equal(2, (await service.FindByTargetAsync("DEC-001", CancellationToken.None)).Count);
        Assert.Equal(2, await service.RemoveByTargetAsync("DEC-001", CancellationToken.None));
        Assert.Empty(await service.FindByTargetAsync("DEC-001", CancellationToken.None));
        Assert.Single(await service.FindByTargetAsync("DEC-002", CancellationToken.None));
    }
}

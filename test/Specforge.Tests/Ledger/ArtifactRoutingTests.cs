using Specforge.Core.Ledger;
using Specforge.Tests.TestSupport;

using Xunit;

namespace Specforge.Tests.Ledger;

public class ArtifactRoutingTests
{
    [Fact]
    public async Task DecisionRow_WritesToArtifactsLedger()
    {
        using PackageWorkspace ws = new();
        await new ArtifactLedgerService(ws.Session).AppendAsync(new ArtifactRow { LedgerId = "ART-DEC-001", Artifact = "decisions/DEC-001.md", Status = "Draft" }, CancellationToken.None);

        Assert.Contains("ART-DEC-001", await ws.ReadLedgerAsync("artifacts.md"), StringComparison.Ordinal);
        Assert.DoesNotContain("ART-DEC-001", await ws.ReadLedgerAsync("items.md"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ItemRow_WritesToItemsLedger()
    {
        using PackageWorkspace ws = new();
        await new ArtifactLedgerService(ws.Session).AppendAsync(new ArtifactRow { LedgerId = "ART-ITEM-001", Artifact = "items/ITEM-001.md", Status = "Draft" }, CancellationToken.None);

        Assert.Contains("ART-ITEM-001", await ws.ReadLedgerAsync("items.md"), StringComparison.Ordinal);
        Assert.DoesNotContain("ART-ITEM-001", await ws.ReadLedgerAsync("artifacts.md"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ItemRow_FindAndRemove_RouteToItemsLedger()
    {
        using PackageWorkspace ws = new();
        ArtifactLedgerService service = new(ws.Session);
        await service.AppendAsync(new ArtifactRow { LedgerId = "ART-ITEM-002", Artifact = "items/ITEM-002.md", Status = "Draft" }, CancellationToken.None);

        Assert.NotNull(await service.FindByLedgerIdAsync("ART-ITEM-002", CancellationToken.None));
        Assert.True(await service.RemoveAsync("ART-ITEM-002", CancellationToken.None));
        Assert.Null(await service.FindByLedgerIdAsync("ART-ITEM-002", CancellationToken.None));
    }
}

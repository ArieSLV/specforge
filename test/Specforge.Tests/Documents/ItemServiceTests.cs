using Specforge.Core.Documents;
using Specforge.Core.Exceptions;
using Specforge.Core.Identifiers;
using Specforge.Core.Ledger;
using Specforge.Core.Lifecycle;
using Specforge.Tests.TestSupport;

using Xunit;

namespace Specforge.Tests.Documents;

public class ItemServiceTests
{
    private static ItemService ServiceFor(PackageWorkspace ws) => new(
        ws.Session,
        new IdAllocator(new LedgerReader(), ws.Session),
        new ArtifactLedgerService(ws.Session),
        new HistoryLedgerService(ws.Session),
        new ReviewLedgerService(ws.Session),
        new LifecycleStateMachine(),
        new ItemFileWriter());

    [Fact]
    public async Task Create_WritesFileArtifactRowInItemsLedgerAndHistory()
    {
        using PackageWorkspace ws = new();
        CreateItemResult result = await ServiceFor(ws).CreateAsync("Sample Item", null, null, false, CancellationToken.None);

        Assert.Equal("ITEM-001", result.Id);
        Assert.True(File.Exists(result.ItemPath));
        Assert.Contains("ART-ITEM-001", await ws.ReadLedgerAsync("items.md"), StringComparison.Ordinal);
        Assert.DoesNotContain("ART-ITEM-001", await ws.ReadLedgerAsync("artifacts.md"), StringComparison.Ordinal);
        Assert.Contains("Created", await ws.ReadLedgerAsync("history.md"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Create_AllocatesSequentially()
    {
        using PackageWorkspace ws = new();
        ItemService service = ServiceFor(ws);
        Assert.Equal("ITEM-001", (await service.CreateAsync("First", null, null, false, CancellationToken.None)).Id);
        Assert.Equal("ITEM-002", (await service.CreateAsync("Second", null, null, false, CancellationToken.None)).Id);
    }

    [Fact]
    public async Task Create_ValidatesDependsOn()
    {
        using PackageWorkspace ws = new();
        ItemService service = ServiceFor(ws);

        await service.CreateAsync("Good Deps", null, ["DEC-001"], false, CancellationToken.None);
        await Assert.ThrowsAsync<SpecforgeInvalidIdentifierException>(
            () => service.CreateAsync("Bad Deps", null, ["dec-001"], false, CancellationToken.None));
    }

    [Fact]
    public async Task Create_InvalidStatus_Throws()
    {
        using PackageWorkspace ws = new();
        await Assert.ThrowsAsync<SpecforgeInvalidArgumentException>(
            () => ServiceFor(ws).CreateAsync("Topic", "Approved", null, false, CancellationToken.None));
    }

    [Fact]
    public async Task SetStatus_DoesNotEnforceRequiredSections()
    {
        using PackageWorkspace ws = new();
        // An item missing most required sections — the setter must still succeed (gate is ITEM-010's job).
        string path = Path.Combine(ws.ItemsDirectory, "ITEM-005-PARTIAL.md");
        await File.WriteAllTextAsync(path, "# ITEM-005-PARTIAL - Partial\n\nStatus: Draft\n\n## Handoff Summary\n\nx\n");

        SetItemStatusResult result = await ServiceFor(ws).SetStatusAsync("ITEM-005", "Draft for user review", null, null, false, CancellationToken.None);

        Assert.Equal("Draft", result.PreviousStatus);
        Assert.Equal("Draft for user review", result.NewStatus);
    }

    [Fact]
    public async Task SetStatus_ApprovalChain_AppendsReview()
    {
        using PackageWorkspace ws = new();
        ItemService service = ServiceFor(ws);
        await service.CreateAsync("Topic", null, null, false, CancellationToken.None);
        await service.SetStatusAsync("ITEM-001", "Draft for user review", null, null, false, CancellationToken.None);

        SetItemStatusResult approved = await service.SetStatusAsync("ITEM-001", "Approved", "User", "ok", false, CancellationToken.None);

        Assert.Equal("REV-ITEM-001-001", approved.ReviewId);
        Assert.Contains("REV-ITEM-001-001", await ws.ReadLedgerAsync("reviews.md"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Delete_PreApproved_RemovesAndCascades()
    {
        using PackageWorkspace ws = new();
        ItemService service = ServiceFor(ws);
        CreateItemResult created = await service.CreateAsync("Topic", null, null, false, CancellationToken.None);
        await new ReviewLedgerService(ws.Session).AppendAsync("ITEM-001", "User", "Changes requested", "fix", CancellationToken.None);

        DeleteItemResult result = await service.DeleteAsync("ITEM-001", confirm: true, dryRun: false, CancellationToken.None);

        Assert.Equal(1, result.RemovedReviewRows);
        Assert.False(File.Exists(created.ItemPath));
        Assert.DoesNotContain("ART-ITEM-001", await ws.ReadLedgerAsync("items.md"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Delete_Approved_Forbidden()
    {
        using PackageWorkspace ws = new();
        ItemService service = ServiceFor(ws);
        await service.CreateAsync("Topic", null, null, false, CancellationToken.None);
        await service.SetStatusAsync("ITEM-001", "Draft for user review", null, null, false, CancellationToken.None);
        await service.SetStatusAsync("ITEM-001", "Approved", null, null, false, CancellationToken.None);

        await Assert.ThrowsAsync<SpecforgeDeleteForbiddenException>(
            () => service.DeleteAsync("ITEM-001", confirm: true, dryRun: false, CancellationToken.None));
    }

    [Fact]
    public async Task Delete_NoConfirm_Throws()
    {
        using PackageWorkspace ws = new();
        ItemService service = ServiceFor(ws);
        await service.CreateAsync("Topic", null, null, false, CancellationToken.None);

        SpecforgeInvalidArgumentException ex = await Assert.ThrowsAsync<SpecforgeInvalidArgumentException>(
            () => service.DeleteAsync("ITEM-001", confirm: false, dryRun: false, CancellationToken.None));
        Assert.Equal("confirm", ex.Argument);
    }
}

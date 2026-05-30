using Specforge.Core.Documents;
using Specforge.Core.Exceptions;
using Specforge.Core.Identifiers;
using Specforge.Core.Ledger;
using Specforge.Core.Lifecycle;
using Specforge.Tests.TestSupport;

using Xunit;

namespace Specforge.Tests.Documents;

public class DecisionServiceTests
{
    private static DecisionService ServiceFor(PackageWorkspace ws) => new(
        ws.Session,
        new IdAllocator(new LedgerReader(), ws.Session),
        new ArtifactLedgerService(ws.Session),
        new HistoryLedgerService(ws.Session),
        new ReviewLedgerService(ws.Session),
        new LifecycleStateMachine(),
        new DecisionFileWriter());

    [Fact]
    public async Task Create_WritesFileArtifactAndHistory()
    {
        using PackageWorkspace ws = new();
        DecisionService service = ServiceFor(ws);

        CreateDecisionResult result = await service.CreateAsync("Test Topic", null, dryRun: false, CancellationToken.None);

        Assert.Equal("DEC-001", result.Id);
        Assert.True(File.Exists(result.DecisionPath));
        ArtifactRow? art = await new ArtifactLedgerService(ws.Session).FindByLedgerIdAsync("ART-DEC-001", CancellationToken.None);
        Assert.NotNull(art);
        Assert.Equal("Draft", art!.Status);
        Assert.Contains("Created", await ws.ReadLedgerAsync("history.md"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Create_AllocatesSequentially()
    {
        using PackageWorkspace ws = new();
        DecisionService service = ServiceFor(ws);

        Assert.Equal("DEC-001", (await service.CreateAsync("First", null, false, CancellationToken.None)).Id);
        Assert.Equal("DEC-002", (await service.CreateAsync("Second", null, false, CancellationToken.None)).Id);
    }

    [Fact]
    public async Task Create_InvalidStatus_Throws()
    {
        using PackageWorkspace ws = new();
        await Assert.ThrowsAsync<SpecforgeInvalidArgumentException>(
            () => ServiceFor(ws).CreateAsync("Topic", "Approved", false, CancellationToken.None));
    }

    [Fact]
    public async Task Create_DryRun_WritesNothing()
    {
        using PackageWorkspace ws = new();
        CreateDecisionResult result = await ServiceFor(ws).CreateAsync("Topic", null, dryRun: true, CancellationToken.None);

        Assert.False(File.Exists(result.DecisionPath));
        Assert.DoesNotContain("Created", await ws.ReadLedgerAsync("history.md"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task SetStatus_ApprovalChain_UpdatesAndAppendsReview()
    {
        using PackageWorkspace ws = new();
        DecisionService service = ServiceFor(ws);
        await service.CreateAsync("Topic", null, false, CancellationToken.None);

        await service.SetStatusAsync("DEC-001", "Draft for user review", null, null, false, CancellationToken.None);
        SetDecisionStatusResult approved = await service.SetStatusAsync("DEC-001", "Approved", "User", "looks good", false, CancellationToken.None);

        Assert.Equal("Draft for user review", approved.PreviousStatus);
        Assert.Equal("REV-DEC-001-001", approved.ReviewId);
        Assert.Contains("Approved", await ws.ReadLedgerAsync("reviews.md"), StringComparison.Ordinal);
        Assert.Equal("Approved", (await new ArtifactLedgerService(ws.Session).FindByLedgerIdAsync("ART-DEC-001", CancellationToken.None))!.Status);
    }

    [Fact]
    public async Task SetStatus_InvalidTransition_Throws()
    {
        using PackageWorkspace ws = new();
        DecisionService service = ServiceFor(ws);
        await service.CreateAsync("Topic", null, false, CancellationToken.None);

        await Assert.ThrowsAsync<SpecforgeInvalidArgumentException>(
            () => service.SetStatusAsync("DEC-001", "Approved", null, null, false, CancellationToken.None));
    }

    [Fact]
    public async Task Delete_PreApproved_RemovesFileArtifactAndCascadesReviews()
    {
        using PackageWorkspace ws = new();
        DecisionService service = ServiceFor(ws);
        CreateDecisionResult created = await service.CreateAsync("Topic", null, false, CancellationToken.None);
        await new ReviewLedgerService(ws.Session).AppendAsync("DEC-001", "User", "Changes requested", "fix", CancellationToken.None);

        DeleteDecisionResult result = await service.DeleteAsync("DEC-001", confirm: true, dryRun: false, CancellationToken.None);

        Assert.Equal(1, result.RemovedReviewRows);
        Assert.False(File.Exists(created.DecisionPath));
        Assert.Null(await new ArtifactLedgerService(ws.Session).FindByLedgerIdAsync("ART-DEC-001", CancellationToken.None));
        Assert.Contains("Deleted", await ws.ReadLedgerAsync("history.md"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Delete_Approved_Forbidden()
    {
        using PackageWorkspace ws = new();
        DecisionService service = ServiceFor(ws);
        await service.CreateAsync("Topic", null, false, CancellationToken.None);
        await service.SetStatusAsync("DEC-001", "Draft for user review", null, null, false, CancellationToken.None);
        await service.SetStatusAsync("DEC-001", "Approved", null, null, false, CancellationToken.None);

        await Assert.ThrowsAsync<SpecforgeDeleteForbiddenException>(
            () => service.DeleteAsync("DEC-001", confirm: true, dryRun: false, CancellationToken.None));
    }

    [Fact]
    public async Task Delete_NoConfirm_Throws()
    {
        using PackageWorkspace ws = new();
        DecisionService service = ServiceFor(ws);
        await service.CreateAsync("Topic", null, false, CancellationToken.None);

        SpecforgeInvalidArgumentException ex = await Assert.ThrowsAsync<SpecforgeInvalidArgumentException>(
            () => service.DeleteAsync("DEC-001", confirm: false, dryRun: false, CancellationToken.None));
        Assert.Equal("confirm", ex.Argument);
    }

    [Fact]
    public async Task Get_NotFound_Throws() =>
        await Assert.ThrowsAsync<SpecforgeInvalidArgumentException>(async () =>
        {
            using PackageWorkspace ws = new();
            await ServiceFor(ws).GetAsync("DEC-404", CancellationToken.None);
        });
}

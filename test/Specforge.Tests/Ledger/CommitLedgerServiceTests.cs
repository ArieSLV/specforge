using Specforge.Core.Ledger;
using Specforge.Core.Lifecycle;
using Specforge.Tests.TestSupport;

using Xunit;

namespace Specforge.Tests.Ledger;

public class CommitLedgerServiceTests
{
    private static CommitLedgerService ServiceFor(PackageWorkspace ws) => new(ws.Session, new ArtifactLedgerService(ws.Session));

    private static async Task SeedCommitsLedgerArtifactAsync(PackageWorkspace ws, string status)
    {
        await new ArtifactLedgerService(ws.Session).AppendAsync(new ArtifactRow
        {
            LedgerId = CommitLedgerService.CommitsLedgerArtifactId,
            Artifact = "spec/packages/p/ledger/commits.md",
            Status = status,
        }, CancellationToken.None);
    }

    [Fact]
    public async Task AppendAsync_AllocatesSequentialGlobalIds()
    {
        using PackageWorkspace ws = new();
        CommitLedgerService commits = ServiceFor(ws);

        CommitAppendResult first = await commits.AppendAsync("ART-ITEM-001", "aaa1111", "first", null, dryRun: false, CancellationToken.None);
        CommitAppendResult second = await commits.AppendAsync("ART-ITEM-002", "bbb2222", "second", null, dryRun: false, CancellationToken.None);

        Assert.Equal("CMT-001", first.Id);
        Assert.Equal("CMT-002", second.Id);
    }

    [Fact]
    public async Task AppendAsync_RoundTripsRowValues()
    {
        using PackageWorkspace ws = new();
        CommitLedgerService commits = ServiceFor(ws);

        await commits.AppendAsync("ART-ITEM-009", "986efba", "ITEM-009 ledger tools", "2026-05-30", dryRun: false, CancellationToken.None);

        CommitRow? row = await commits.FindByLedgerIdAsync("CMT-001", CancellationToken.None);
        Assert.NotNull(row);
        Assert.Equal("CMT-001", row!.LedgerId);
        Assert.Equal("986efba", row.CommitSha);
        Assert.Equal("ART-ITEM-009", row.Linked);
        Assert.Equal("master", row.Branch);
        Assert.Equal("2026-05-30", row.Date);
        Assert.Equal("ITEM-009 ledger tools", row.Note);
    }

    [Fact]
    public async Task AppendAsync_FirstCall_PromotesPlaceholderToDraft()
    {
        using PackageWorkspace ws = new();
        await SeedCommitsLedgerArtifactAsync(ws, LifecycleState.Placeholder);
        CommitLedgerService commits = ServiceFor(ws);

        CommitAppendResult first = await commits.AppendAsync("ART-ITEM-001", "abc1234", "first", null, dryRun: false, CancellationToken.None);
        CommitAppendResult second = await commits.AppendAsync("ART-ITEM-002", "def5678", "second", null, dryRun: false, CancellationToken.None);

        Assert.True(first.PromotedCommitsLedger);
        Assert.False(second.PromotedCommitsLedger);

        ArtifactRow? promoted = await new ArtifactLedgerService(ws.Session).FindByLedgerIdAsync(CommitLedgerService.CommitsLedgerArtifactId, CancellationToken.None);
        Assert.Equal(LifecycleState.Draft, promoted!.Status);
    }

    [Fact]
    public async Task AppendAsync_NoCommitsLedgerArtifact_DoesNotPromote()
    {
        using PackageWorkspace ws = new();
        CommitLedgerService commits = ServiceFor(ws);

        CommitAppendResult result = await commits.AppendAsync("ART-ITEM-001", "abc1234", "first", null, dryRun: false, CancellationToken.None);

        Assert.False(result.PromotedCommitsLedger);
    }

    [Fact]
    public async Task AppendAsync_DryRun_DoesNotWrite()
    {
        using PackageWorkspace ws = new();
        await SeedCommitsLedgerArtifactAsync(ws, LifecycleState.Placeholder);
        CommitLedgerService commits = ServiceFor(ws);

        CommitAppendResult result = await commits.AppendAsync("ART-ITEM-001", "abc1234", "first", null, dryRun: true, CancellationToken.None);

        Assert.Equal("CMT-001", result.Id);
        Assert.True(result.PromotedCommitsLedger); // would promote
        Assert.Null(await commits.FindByLedgerIdAsync("CMT-001", CancellationToken.None));
        ArtifactRow? untouched = await new ArtifactLedgerService(ws.Session).FindByLedgerIdAsync(CommitLedgerService.CommitsLedgerArtifactId, CancellationToken.None);
        Assert.Equal(LifecycleState.Placeholder, untouched!.Status);
    }

    [Fact]
    public async Task RemoveAsync_TombstonesNumber_NextAdvancesPastGap()
    {
        using PackageWorkspace ws = new();
        CommitLedgerService commits = ServiceFor(ws);

        await commits.AppendAsync("ART-ITEM-001", "aaa1111", "first", null, dryRun: false, CancellationToken.None);
        await commits.AppendAsync("ART-ITEM-002", "bbb2222", "second", null, dryRun: false, CancellationToken.None);
        Assert.True(await commits.RemoveAsync("CMT-001", CancellationToken.None));

        CommitAppendResult next = await commits.AppendAsync("ART-ITEM-003", "ccc3333", "third", null, dryRun: false, CancellationToken.None);
        Assert.Equal("CMT-003", next.Id); // 001 tombstoned, 002 remains → max+1 = 003
        Assert.Null(await commits.FindByLedgerIdAsync("CMT-001", CancellationToken.None));
    }
}

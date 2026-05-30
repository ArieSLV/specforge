using Specforge.Core.Validation;
using Specforge.Tests.TestSupport;

using Xunit;

namespace Specforge.Tests.Validation;

public class TombstoneAwareIdAllocatorTests
{
    [Theory]
    [InlineData("Tombstone DEC-005: file removed", "DEC-005")]
    [InlineData("Tombstone REV-ITEM-001-002: removed", "REV-ITEM-001-002")]
    [InlineData("Tombstone CMT-009: was abc123; deleted via delete_commit", "CMT-009")]
    public void TryExtractDeletedId_TombstonePrefix_ReturnsId(string detail, string expected)
    {
        Assert.True(TombstoneDetailFormat.TryExtractDeletedId(detail, out string? id));
        Assert.Equal(expected, id);
    }

    [Theory]
    [InlineData("Foo bar baz")]
    [InlineData("delete_decision: tombstoned DEC-005")]
    [InlineData("")]
    public void TryExtractDeletedId_NonMatching_ReturnsFalse(string detail)
    {
        Assert.False(TombstoneDetailFormat.TryExtractDeletedId(detail, out string? id));
        Assert.Null(id);
    }

    [Fact]
    public void Format_RoundTripsThroughTheParser()
    {
        string detail = TombstoneDetailFormat.Format("ITEM-007", "removed 2 review row(s)");
        Assert.True(TombstoneDetailFormat.TryExtractDeletedId(detail, out string? id));
        Assert.Equal("ITEM-007", id);
    }

    [Fact]
    public async Task VerifyGaps_NoGaps_ReturnsEmpty()
    {
        using PackageWorkspace ws = new();
        Assert.Empty(await SpecGraphFixtures.Allocator(ws).VerifyGapsAsync("DEC", [1, 2, 3], CancellationToken.None));
    }

    [Fact]
    public async Task VerifyGaps_UnexplainedGap_ReturnsTheGap()
    {
        using PackageWorkspace ws = new();
        IReadOnlyList<int> gaps = await SpecGraphFixtures.Allocator(ws).VerifyGapsAsync("DEC", [1, 2, 4], CancellationToken.None);
        Assert.Single(gaps);
        Assert.Equal(3, gaps[0]);
    }

    [Fact]
    public async Task VerifyGaps_GapExplainedByTombstone_ReturnsEmpty()
    {
        using PackageWorkspace ws = new();
        await SpecGraphFixtures.AddTombstoneAsync(ws, "DEC-003");
        Assert.Empty(await SpecGraphFixtures.Allocator(ws).VerifyGapsAsync("DEC", [1, 2, 4], CancellationToken.None));
    }
}

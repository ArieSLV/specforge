using Specforge.Core.Ledger;
using Specforge.Core.Validation;
using Specforge.Tests.TestSupport;

using Xunit;

namespace Specforge.Tests.Validation;

public class ValidationServiceTests
{
    // Seeds one finding per aspect: ids gap (DEC-002), links dangling review (DEC-099),
    // lifecycle status mismatch (DEC-001), impact-coverage Approved-missing-section (ITEM-001).
    private static async Task SeedOnePerAspectAsync(PackageWorkspace ws)
    {
        await SpecGraphFixtures.AddArtifactAsync(ws, "ART-DEC-001", "Approved");
        await SpecGraphFixtures.AddArtifactAsync(ws, "ART-DEC-003", "Approved");      // ids: gap at DEC-002
        await SpecGraphFixtures.WriteDecisionAsync(ws, "DEC-001", "Draft");           // lifecycle: file Draft vs row Approved
        await new ReviewLedgerService(ws.Session).AppendAsync("DEC-099", "User", "Approved", null, CancellationToken.None); // links: dangling REV target
        await SpecGraphFixtures.AddArtifactAsync(ws, "ART-ITEM-001", "Approved");
        await SpecGraphFixtures.WriteItemAsync(ws, "ITEM-001", "Approved", omitSections: ["Done Criteria"]); // impact: Approved missing section
    }

    [Fact]
    public async Task All_RunsEveryAspect_InDeclaredOrder()
    {
        using PackageWorkspace ws = new();
        await SeedOnePerAspectAsync(ws);

        ValidationResult result = await SpecGraphFixtures.Service(ws).ValidateAsync(ValidationAspect.All, CancellationToken.None);

        foreach (string aspect in ValidationAspect.Concrete)
        {
            Assert.Contains(result.Findings, f => f.Aspect == aspect);
        }

        Assert.True(result.ErrorCount >= 4, $"expected >= 4 errors, got {result.ErrorCount}");

        // findings are ordered by aspect rank (ids, links, lifecycle, impact-coverage)
        List<int> ranks = [.. result.Findings.Select(f => ValidationAspect.Concrete.ToList().IndexOf(f.Aspect))];
        for (int i = 1; i < ranks.Count; i++)
        {
            Assert.True(ranks[i] >= ranks[i - 1], "findings are not ordered by aspect");
        }
    }

    [Fact]
    public async Task SingleAspect_RunsOnlyThatAspect()
    {
        using PackageWorkspace ws = new();
        await SeedOnePerAspectAsync(ws);

        ValidationResult result = await SpecGraphFixtures.Service(ws).ValidateAsync(ValidationAspect.Ids, CancellationToken.None);

        Assert.NotEmpty(result.Findings);
        Assert.All(result.Findings, f => Assert.Equal(ValidationAspect.Ids, f.Aspect));
    }

    [Fact]
    public async Task CleanGraph_ReturnsZeroCounts()
    {
        using PackageWorkspace ws = new();

        ValidationResult result = await SpecGraphFixtures.Service(ws).ValidateAsync(ValidationAspect.All, CancellationToken.None);

        Assert.Empty(result.Findings);
        Assert.Equal(0, result.ErrorCount);
        Assert.Equal(0, result.WarningCount);
        Assert.Equal(0, result.InfoCount);
    }
}

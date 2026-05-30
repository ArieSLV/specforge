using Specforge.Core.Validation;
using Specforge.Tests.TestSupport;

using Xunit;

namespace Specforge.Tests.Validation;

public class ImpactCoverageAspectValidatorTests
{
    [Fact]
    public async Task ApprovedItemMissingSection_IsError()
    {
        using PackageWorkspace ws = new();
        await SpecGraphFixtures.WriteItemAsync(ws, "ITEM-001", "Approved", omitSections: ["Done Criteria"]);

        IReadOnlyList<ValidationFinding> findings = await SpecGraphFixtures.Impact(ws).ValidateAsync(CancellationToken.None);

        Assert.Contains(findings, f => f.Severity == ValidationSeverity.Error && f.Target == "ITEM-001" && f.Message.Contains("Done Criteria", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DraftItemMissingSection_IsInfo()
    {
        using PackageWorkspace ws = new();
        await SpecGraphFixtures.WriteItemAsync(ws, "ITEM-001", "Draft", omitSections: ["Done Criteria"]);

        IReadOnlyList<ValidationFinding> findings = await SpecGraphFixtures.Impact(ws).ValidateAsync(CancellationToken.None);

        Assert.Contains(findings, f => f.Severity == ValidationSeverity.Info && f.Target == "ITEM-001");
        Assert.DoesNotContain(findings, f => f.Severity == ValidationSeverity.Error);
    }

    [Fact]
    public async Task EmptyRequiredSection_CountsAsMissing()
    {
        using PackageWorkspace ws = new();
        await SpecGraphFixtures.WriteItemAsync(ws, "ITEM-001", "Approved", blankSections: ["Validation"]);

        IReadOnlyList<ValidationFinding> findings = await SpecGraphFixtures.Impact(ws).ValidateAsync(CancellationToken.None);

        Assert.Contains(findings, f => f.Severity == ValidationSeverity.Error && f.Message.Contains("Validation", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DecisionsAreSkipped()
    {
        using PackageWorkspace ws = new();
        await SpecGraphFixtures.WriteDecisionAsync(ws, "DEC-001", "Approved");

        IReadOnlyList<ValidationFinding> findings = await SpecGraphFixtures.Impact(ws).ValidateAsync(CancellationToken.None);

        Assert.Empty(findings);
    }

    [Fact]
    public async Task CompleteApprovedItem_IsClean()
    {
        using PackageWorkspace ws = new();
        await SpecGraphFixtures.WriteItemAsync(ws, "ITEM-001", "Approved");

        IReadOnlyList<ValidationFinding> findings = await SpecGraphFixtures.Impact(ws).ValidateAsync(CancellationToken.None);

        Assert.Empty(findings);
    }
}

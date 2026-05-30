using Specforge.Core.Ledger;
using Specforge.Core.Validation;
using Specforge.Tests.TestSupport;

using Xunit;

namespace Specforge.Tests.Validation;

public class LinksAspectValidatorTests
{
    [Fact]
    public async Task DanglingSupersedes_IsError()
    {
        using PackageWorkspace ws = new();
        await SpecGraphFixtures.WriteDecisionAsync(ws, "DEC-001", "Approved", supersedes: "DEC-099");

        IReadOnlyList<ValidationFinding> findings = await SpecGraphFixtures.Links(ws).ValidateAsync(CancellationToken.None);

        Assert.Contains(findings, f => f.Severity == ValidationSeverity.Error && f.Message.Contains("DEC-099", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ProseSupersedesCoversField_IsNotTreatedAsReference()
    {
        using PackageWorkspace ws = new();
        // A prose field that merely mentions ids must not be parsed as a reference list.
        await SpecGraphFixtures.WriteDecisionAsync(ws, "DEC-001", "Approved", supersedes: "none; clarifies DEC-099 wording");

        IReadOnlyList<ValidationFinding> findings = await SpecGraphFixtures.Links(ws).ValidateAsync(CancellationToken.None);

        Assert.DoesNotContain(findings, f => f.Message.Contains("DEC-099", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DanglingDependsOn_IsWarning()
    {
        using PackageWorkspace ws = new();
        await SpecGraphFixtures.WriteItemAsync(ws, "ITEM-001", "Draft", dependsOn: ["ITEM-099"]);

        IReadOnlyList<ValidationFinding> findings = await SpecGraphFixtures.Links(ws).ValidateAsync(CancellationToken.None);

        Assert.Contains(findings, f => f.Severity == ValidationSeverity.Warning && f.Message.Contains("ITEM-099", StringComparison.Ordinal));
        Assert.DoesNotContain(findings, f => f.Severity == ValidationSeverity.Error && f.Message.Contains("ITEM-099", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DanglingReviewTarget_IsError()
    {
        using PackageWorkspace ws = new();
        await new ReviewLedgerService(ws.Session).AppendAsync("DEC-099", "User", "Approved", null, CancellationToken.None);

        IReadOnlyList<ValidationFinding> findings = await SpecGraphFixtures.Links(ws).ValidateAsync(CancellationToken.None);

        Assert.Contains(findings, f => f.Severity == ValidationSeverity.Error && f.Message.Contains("review", StringComparison.Ordinal) && f.Message.Contains("DEC-099", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DanglingCommitTarget_IsError()
    {
        using PackageWorkspace ws = new();
        await new CommitLedgerService(ws.Session, new ArtifactLedgerService(ws.Session))
            .AppendAsync("ART-ITEM-099", "abc1234", "fixture", null, dryRun: false, CancellationToken.None);

        IReadOnlyList<ValidationFinding> findings = await SpecGraphFixtures.Links(ws).ValidateAsync(CancellationToken.None);

        Assert.Contains(findings, f => f.Severity == ValidationSeverity.Error && f.Message.Contains("commit", StringComparison.Ordinal) && f.Message.Contains("ART-ITEM-099", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AllReferencesResolve_IsClean()
    {
        using PackageWorkspace ws = new();
        await SpecGraphFixtures.WriteDecisionAsync(ws, "DEC-001", "Approved");
        await SpecGraphFixtures.AddArtifactAsync(ws, "ART-DEC-001", "Approved");
        await SpecGraphFixtures.WriteItemAsync(ws, "ITEM-001", "Draft", dependsOn: ["ART-DEC-001"]);

        IReadOnlyList<ValidationFinding> findings = await SpecGraphFixtures.Links(ws).ValidateAsync(CancellationToken.None);

        Assert.Empty(findings);
    }
}

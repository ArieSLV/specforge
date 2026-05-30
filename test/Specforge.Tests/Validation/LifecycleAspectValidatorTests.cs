using Specforge.Core.Validation;
using Specforge.Tests.TestSupport;

using Xunit;

namespace Specforge.Tests.Validation;

public class LifecycleAspectValidatorTests
{
    [Fact]
    public async Task FileVsLedgerStatusMismatch_IsError()
    {
        using PackageWorkspace ws = new();
        await SpecGraphFixtures.WriteDecisionAsync(ws, "DEC-001", "Draft");
        await SpecGraphFixtures.AddArtifactAsync(ws, "ART-DEC-001", "Approved");

        IReadOnlyList<ValidationFinding> findings = await SpecGraphFixtures.Lifecycle(ws).ValidateAsync(CancellationToken.None);

        Assert.Contains(findings, f => f.Severity == ValidationSeverity.Error && f.Target == "DEC-001" && f.Message.Contains("status mismatch", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ApprovedWithoutReview_IsWarning()
    {
        using PackageWorkspace ws = new();
        await SpecGraphFixtures.WriteItemAsync(ws, "ITEM-001", "Approved");
        await SpecGraphFixtures.AddArtifactAsync(ws, "ART-ITEM-001", "Approved");

        IReadOnlyList<ValidationFinding> findings = await SpecGraphFixtures.Lifecycle(ws).ValidateAsync(CancellationToken.None);

        Assert.Contains(findings, f => f.Severity == ValidationSeverity.Warning && f.Target == "ITEM-001" && f.Message.Contains("Approved", StringComparison.Ordinal));
        Assert.DoesNotContain(findings, f => f.Severity == ValidationSeverity.Error);
    }

    [Fact]
    public async Task UnknownLifecycleState_IsError()
    {
        using PackageWorkspace ws = new();
        await SpecGraphFixtures.WriteDecisionAsync(ws, "DEC-001", "Bogus");
        await SpecGraphFixtures.AddArtifactAsync(ws, "ART-DEC-001", "Bogus");

        IReadOnlyList<ValidationFinding> findings = await SpecGraphFixtures.Lifecycle(ws).ValidateAsync(CancellationToken.None);

        Assert.Contains(findings, f => f.Severity == ValidationSeverity.Error && f.Target == "DEC-001" && f.Message.Contains("not a recognized lifecycle state", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ConsistentApprovedWithReview_IsClean()
    {
        using PackageWorkspace ws = new();
        await SpecGraphFixtures.WriteDecisionAsync(ws, "DEC-001", "Approved");
        await SpecGraphFixtures.AddArtifactAsync(ws, "ART-DEC-001", "Approved");
        await new Core.Ledger.ReviewLedgerService(ws.Session).AppendAsync("DEC-001", "User", "Approved", null, CancellationToken.None);

        IReadOnlyList<ValidationFinding> findings = await SpecGraphFixtures.Lifecycle(ws).ValidateAsync(CancellationToken.None);

        Assert.Empty(findings);
    }
}

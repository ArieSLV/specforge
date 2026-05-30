using Specforge.Core.Validation;
using Specforge.Tests.TestSupport;

using Xunit;

namespace Specforge.Tests.Validation;

public class IdsAspectValidatorTests
{
    private static bool HasError(IReadOnlyList<ValidationFinding> findings, string targetContains) =>
        findings.Any(f => f.Severity == ValidationSeverity.Error && (f.Target?.Contains(targetContains, StringComparison.Ordinal) ?? false));

    [Fact]
    public async Task DuplicateIdentifier_IsError()
    {
        using PackageWorkspace ws = new();
        await SpecGraphFixtures.AddArtifactAsync(ws, "ART-DEC-001", "Approved");
        await SpecGraphFixtures.AddArtifactAsync(ws, "ART-DEC-001", "Approved");

        IReadOnlyList<ValidationFinding> findings = await SpecGraphFixtures.Ids(ws).ValidateAsync(CancellationToken.None);

        Assert.Contains(findings, f => f.Severity == ValidationSeverity.Error && f.Message.Contains("defined 2 times", StringComparison.Ordinal));
    }

    [Fact]
    public async Task UnregisteredKind_IsError()
    {
        using PackageWorkspace ws = new();
        await SpecGraphFixtures.AddArtifactAsync(ws, "FOO-001", "Draft");

        IReadOnlyList<ValidationFinding> findings = await SpecGraphFixtures.Ids(ws).ValidateAsync(CancellationToken.None);

        Assert.True(HasError(findings, "FOO-001"));
    }

    [Fact]
    public async Task MalformedIdentifier_IsError()
    {
        using PackageWorkspace ws = new();
        await SpecGraphFixtures.AddArtifactAsync(ws, "dec-1", "Draft");

        IReadOnlyList<ValidationFinding> findings = await SpecGraphFixtures.Ids(ws).ValidateAsync(CancellationToken.None);

        Assert.True(HasError(findings, "dec-1"));
    }

    [Fact]
    public async Task ReservedExtraKind_IsError()
    {
        using PackageWorkspace ws = new(["DEC"]);

        IReadOnlyList<ValidationFinding> findings = await SpecGraphFixtures.Ids(ws).ValidateAsync(CancellationToken.None);

        Assert.Contains(findings, f => f.Severity == ValidationSeverity.Error && f.Aspect == ValidationAspect.Ids && f.Message.Contains("DEC", StringComparison.Ordinal));
    }

    [Fact]
    public async Task UnexplainedSequenceGap_IsError()
    {
        using PackageWorkspace ws = new();
        await SpecGraphFixtures.AddArtifactAsync(ws, "ART-DEC-001", "Approved");
        await SpecGraphFixtures.AddArtifactAsync(ws, "ART-DEC-003", "Approved");

        IReadOnlyList<ValidationFinding> findings = await SpecGraphFixtures.Ids(ws).ValidateAsync(CancellationToken.None);

        Assert.True(HasError(findings, "DEC-002"));
    }

    [Fact]
    public async Task GapExplainedByTombstone_IsClean()
    {
        using PackageWorkspace ws = new();
        await SpecGraphFixtures.AddArtifactAsync(ws, "ART-DEC-001", "Approved");
        await SpecGraphFixtures.AddArtifactAsync(ws, "ART-DEC-003", "Approved");
        await SpecGraphFixtures.AddTombstoneAsync(ws, "DEC-002");

        IReadOnlyList<ValidationFinding> findings = await SpecGraphFixtures.Ids(ws).ValidateAsync(CancellationToken.None);

        Assert.DoesNotContain(findings, f => f.Target == "DEC-002");
    }
}

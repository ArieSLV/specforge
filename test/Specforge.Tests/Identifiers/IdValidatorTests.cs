using Specforge.Core.Configuration;
using Specforge.Core.Exceptions;
using Specforge.Core.Identifiers;

using Xunit;

namespace Specforge.Tests.Identifiers;

public class IdValidatorTests
{
    private static SpecforgeConfig Config()
    {
        SpecforgePackageConfig package = new("specforge-mvp", "spec/packages/specforge-mvp", ["RDM"]);
        return new SpecforgeConfig(1, "spec/shared", "spec/templates", [package], "/repo/.specforge.json");
    }

    private static KindRegistry Registry() => KindRegistry.FromConfig(Config(), "specforge-mvp");

    private static ValidationContext Context() => ValidationContext.FromConfig(Config());

    [Theory]
    [InlineData("DEC-001")]
    [InlineData("ITEM-013")]
    [InlineData("RDM-014")]
    [InlineData("REV-DEC-001-002")]
    [InlineData("ART-WORK-PLAN")]
    public void Validate_AcceptsKnownForms(string input) => IdValidator.Validate(input, Registry(), Context());

    [Fact]
    public void Validate_QualifiedKnownPackage_Passes() =>
        IdValidator.Validate("specforge-mvp/DEC-001", Registry(), Context());

    [Fact]
    public void Validate_QualifiedUnknownPackage_Throws() =>
        Assert.Throws<SpecforgeInvalidIdentifierException>(
            () => IdValidator.Validate("other-pkg/DEC-001", Registry(), Context()));

    [Fact]
    public void Validate_UnknownKind_Throws()
    {
        SpecforgeInvalidIdentifierException ex = Assert.Throws<SpecforgeInvalidIdentifierException>(
            () => IdValidator.Validate("ZZZ-001", Registry(), Context()));
        Assert.Equal("ZZZ-001", ex.Given);
        Assert.NotEmpty(ex.ExpectedPatterns);
    }

    [Theory]
    [InlineData("dec-001")]
    [InlineData("DEC-1")]
    [InlineData("garbage")]
    public void Validate_Malformed_Throws(string input) =>
        Assert.Throws<SpecforgeInvalidIdentifierException>(() => IdValidator.Validate(input, Registry(), Context()));
}

using Specforge.Core.Configuration;
using Specforge.Core.Exceptions;
using Specforge.Core.Identifiers;

using Xunit;

namespace Specforge.Tests.Identifiers;

public class KindRegistryTests
{
    private static SpecforgeConfig ConfigWithExtraKinds(params string[] extras)
    {
        SpecforgePackageConfig package = new("pkg", "spec/packages/pkg", extras);
        return new SpecforgeConfig(1, "spec/shared", "spec/templates", [package], "/repo/.specforge.json");
    }

    [Theory]
    [InlineData("DEC")]
    [InlineData("ITEM")]
    [InlineData("ART")]
    [InlineData("REV")]
    [InlineData("CMT")]
    public void CoreKinds_AlwaysRecognized(string kind)
    {
        KindRegistry registry = KindRegistry.FromConfig(ConfigWithExtraKinds(), "pkg");
        Assert.True(registry.IsKnownKind(kind));
    }

    [Fact]
    public void ExtraKinds_Recognized()
    {
        KindRegistry registry = KindRegistry.FromConfig(ConfigWithExtraKinds("RDM", "BSP"), "pkg");
        Assert.True(registry.IsKnownKind("RDM"));
        Assert.True(registry.IsKnownKind("BSP"));
        Assert.False(registry.IsKnownKind("ZZZ"));
    }

    [Fact]
    public void IsReservedKind_TrueForCore_FalseForExtras()
    {
        Assert.True(KindRegistry.IsReservedKind("DEC"));
        Assert.False(KindRegistry.IsReservedKind("RDM"));
    }

    [Fact]
    public void ValidateExtraKinds_ThrowsOnReserved()
    {
        SpecforgeReservedKindException ex = Assert.Throws<SpecforgeReservedKindException>(
            () => KindRegistry.ValidateExtraKinds(["RDM", "DEC"]));
        Assert.Equal("DEC", ex.Given);
    }

    [Fact]
    public void ValidateExtraKinds_AcceptsNonReserved() =>
        KindRegistry.ValidateExtraKinds(["RDM", "BSP", "SE", "EH"]);

    [Fact]
    public void FromConfig_UnknownPackage_Throws() =>
        Assert.Throws<SpecforgeUnknownPackageException>(() => KindRegistry.FromConfig(ConfigWithExtraKinds(), "nope"));
}

using Specforge.Core.Configuration;
using Specforge.Core.Exceptions;

using Xunit;

namespace Specforge.Tests.Configuration;

public class SessionStateTests
{
    private static SpecforgeConfig Config(params string[] packageNames)
    {
        List<SpecforgePackageConfig> packages = [.. packageNames.Select(n => new SpecforgePackageConfig(n, $"spec/packages/{n}", SpecforgePackageConfig.NoExtraKinds))];
        return new SpecforgeConfig(1, "spec/shared", "spec/templates", packages, "/repo/.specforge.json");
    }

    [Fact]
    public void SinglePackage_AutoSelects()
    {
        SessionState state = new();
        state.Initialize(Config("only"));

        Assert.Equal("only", state.ActivePackageName);
        Assert.Equal("only", state.RequireActivePackage().Name);
    }

    [Fact]
    public void MultiPackage_NoSelection()
    {
        SessionState state = new();
        state.Initialize(Config("alpha", "beta"));

        Assert.Null(state.ActivePackageName);
    }

    [Fact]
    public void RequireActivePackage_MultiNoSelection_Throws()
    {
        SessionState state = new();
        state.Initialize(Config("alpha", "beta"));

        SpecforgePackageNotSelectedException ex = Assert.Throws<SpecforgePackageNotSelectedException>(
            () => state.RequireActivePackage());

        Assert.Equal(2, ex.AvailablePackages.Count);
    }

    [Fact]
    public void SelectUnknown_Throws()
    {
        SessionState state = new();
        state.Initialize(Config("alpha", "beta"));

        SpecforgeUnknownPackageException ex = Assert.Throws<SpecforgeUnknownPackageException>(
            () => state.Select("missing"));

        Assert.Equal("missing", ex.RequestedName);
        Assert.Equal(2, ex.AvailablePackages.Count);
    }

    [Fact]
    public void Switch_Selection()
    {
        SessionState state = new();
        state.Initialize(Config("alpha", "beta"));

        state.Select("alpha");
        Assert.Equal("alpha", state.ActivePackageName);

        state.Select("beta");
        Assert.Equal("beta", state.ActivePackageName);
    }
}

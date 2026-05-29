using Specforge.Core.Configuration;
using Specforge.Core.Exceptions;
using Specforge.Tests.TestSupport;

using Xunit;

namespace Specforge.Tests.Configuration;

public class ConfigParserTests
{
    private const string FakePath = "/repo/.specforge.json";

    [Fact]
    public void ValidV1_Parses()
    {
        SpecforgeConfig config = ConfigLoader.ParseAndValidate(ConfigJson.Single(), FakePath);

        Assert.Equal(1, config.SchemaVersion);
        Assert.Equal("spec/shared", config.Shared);
        Assert.Equal("spec/templates", config.Templates);
        SpecforgePackageConfig package = Assert.Single(config.Packages);
        Assert.Equal("specforge-mvp", package.Name);
        Assert.Equal("spec/packages/specforge-mvp", package.Path);
        Assert.Empty(package.ExtraKinds);
    }

    [Fact]
    public void MissingSchemaVersion_Throws_InvalidSuggestion()
    {
        const string json = """
            { "shared": "spec/shared", "templates": "spec/templates", "packages": [] }
            """;

        SpecforgeSchemaVersionException ex = Assert.Throws<SpecforgeSchemaVersionException>(
            () => ConfigLoader.ParseAndValidate(json, FakePath));

        Assert.Null(ex.ClaimedVersion);
        Assert.Contains("invalid", ex.Suggestion, StringComparison.Ordinal);
    }

    [Fact]
    public void StringSchemaVersion_Throws_InvalidSuggestion()
    {
        const string json = """
            { "schemaVersion": "1", "shared": "spec/shared", "templates": "spec/templates", "packages": [] }
            """;

        SpecforgeSchemaVersionException ex = Assert.Throws<SpecforgeSchemaVersionException>(
            () => ConfigLoader.ParseAndValidate(json, FakePath));

        Assert.Equal("1", Assert.IsType<string>(ex.ClaimedVersion));
        Assert.Contains("invalid", ex.Suggestion, StringComparison.Ordinal);
    }

    [Fact]
    public void FutureSchemaVersion_Throws_UpgradeSuggestion()
    {
        const string json = """
            { "schemaVersion": 2, "shared": "spec/shared", "templates": "spec/templates", "packages": [] }
            """;

        SpecforgeSchemaVersionException ex = Assert.Throws<SpecforgeSchemaVersionException>(
            () => ConfigLoader.ParseAndValidate(json, FakePath));

        Assert.Equal(2, Assert.IsType<int>(ex.ClaimedVersion));
        Assert.Contains("upgrade", ex.Suggestion, StringComparison.Ordinal);
        Assert.Equal([1, 1], ex.SupportedRange);
    }

    [Fact]
    public void UnknownTopLevelField_Accepted()
    {
        const string json = """
            {
              "schemaVersion": 1,
              "shared": "spec/shared",
              "templates": "spec/templates",
              "tracing": { "enabled": true },
              "packages": [ { "name": "p", "path": "spec/packages/p" } ]
            }
            """;

        SpecforgeConfig config = ConfigLoader.ParseAndValidate(json, FakePath);

        Assert.Single(config.Packages);
    }

    [Fact]
    public void UnknownFieldInsidePackage_Rejected()
    {
        const string json = """
            {
              "schemaVersion": 1,
              "shared": "spec/shared",
              "templates": "spec/templates",
              "packages": [ { "name": "p", "path": "spec/packages/p", "bogus": 1 } ]
            }
            """;

        SpecforgeConfigValidationException ex = Assert.Throws<SpecforgeConfigValidationException>(
            () => ConfigLoader.ParseAndValidate(json, FakePath));

        Assert.Contains(ex.Errors, e => e.Pointer == "/packages/0/bogus");
    }
}

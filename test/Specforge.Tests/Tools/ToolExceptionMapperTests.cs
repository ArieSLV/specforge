using System.Text.Json;

using Microsoft.Extensions.Logging.Abstractions;

using Specforge.Core.Configuration;
using Specforge.Core.Exceptions;
using Specforge.Mcp.Tools;

using Xunit;

namespace Specforge.Tests.Tools;

public class ToolExceptionMapperTests
{
    private static readonly ToolExceptionMapper Mapper = new(NullLogger<ToolExceptionMapper>.Instance);

    private static IReadOnlyList<SpecforgePackageConfig> TwoPackages =>
    [
        new SpecforgePackageConfig("alpha", "spec/packages/alpha", SpecforgePackageConfig.NoExtraKinds),
        new SpecforgePackageConfig("beta", "spec/packages/beta", SpecforgePackageConfig.NoExtraKinds),
    ];

    private static JsonElement Error(McpErrorEnvelope envelope)
    {
        using JsonDocument doc = JsonDocument.Parse(envelope.ToJson());
        return doc.RootElement.GetProperty("error").Clone();
    }

    [Fact]
    public void ConfigNotFound_MapsCodeSuggestionAndData()
    {
        JsonElement error = Error(Mapper.Map(new SpecforgeConfigNotFoundException("/start")));

        Assert.Equal("specforge.config.not_found", error.GetProperty("code").GetString());
        Assert.Contains("init", error.GetProperty("suggestion").GetString()!, StringComparison.Ordinal);
        Assert.Equal("/start", error.GetProperty("data").GetProperty("searchedPath").GetString());
    }

    [Fact]
    public void SchemaVersion_MapsClaimedVersionAndRange()
    {
        JsonElement error = Error(Mapper.Map(new SpecforgeSchemaVersionException("/p", 2, [1, 1], "upgrade specforge")));

        Assert.Equal("specforge.config.schema_version_unsupported", error.GetProperty("code").GetString());
        Assert.Equal(2, error.GetProperty("data").GetProperty("claimedVersion").GetInt32());
        List<int> range = [.. error.GetProperty("data").GetProperty("supportedRange").EnumerateArray().Select(e => e.GetInt32())];
        Assert.Equal([1, 1], range);
    }

    [Fact]
    public void ConfigValidation_MapsErrorPointers()
    {
        SpecforgeConfigValidationException ex = new("/p", [new ConfigValidationError("/packages", "bad")]);

        JsonElement error = Error(Mapper.Map(ex));

        Assert.Equal("specforge.config.validation_failed", error.GetProperty("code").GetString());
        List<JsonElement> errors = [.. error.GetProperty("data").GetProperty("errors").EnumerateArray()];
        JsonElement first = Assert.Single(errors);
        Assert.Equal("/packages", first.GetProperty("pointer").GetString());
    }

    [Fact]
    public void PackageNotSelected_MapsAvailablePackages()
    {
        JsonElement error = Error(Mapper.Map(new SpecforgePackageNotSelectedException(TwoPackages)));

        Assert.Equal("specforge.package.not_selected", error.GetProperty("code").GetString());
        List<JsonElement> available = [.. error.GetProperty("data").GetProperty("availablePackages").EnumerateArray()];
        Assert.Equal(2, available.Count);
    }

    [Fact]
    public void UnknownPackage_MapsRequestedName()
    {
        JsonElement error = Error(Mapper.Map(new SpecforgeUnknownPackageException("x", TwoPackages)));

        Assert.Equal("specforge.package.unknown", error.GetProperty("code").GetString());
        Assert.Equal("x", error.GetProperty("data").GetProperty("requestedName").GetString());
    }

    [Fact]
    public void Unhandled_MapsToInternalErrorWithCorrelationId()
    {
        JsonElement error = Error(Mapper.Map(new InvalidOperationException("boom")));

        Assert.Equal("specforge.tool.internal_error", error.GetProperty("code").GetString());
        Assert.False(string.IsNullOrEmpty(error.GetProperty("data").GetProperty("correlationId").GetString()));
        Assert.False(string.IsNullOrEmpty(error.GetProperty("suggestion").GetString()));
    }
}

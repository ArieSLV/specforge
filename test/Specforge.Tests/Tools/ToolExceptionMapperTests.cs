using System.Text.Json;

using Microsoft.Extensions.Logging.Abstractions;

using Specforge.Core.Configuration;
using Specforge.Core.Exceptions;
using Specforge.Core.Skills;
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

    [Fact]
    public void InvalidIdentifier_MapsGivenAndExpectedPatterns()
    {
        JsonElement error = Error(Mapper.Map(new SpecforgeInvalidIdentifierException("dec-001", ["^[A-Z]{2,6}-[0-9]{3}$"])));

        Assert.Equal("specforge.id.invalid", error.GetProperty("code").GetString());
        Assert.Equal("dec-001", error.GetProperty("data").GetProperty("given").GetString());
        Assert.NotEmpty(error.GetProperty("data").GetProperty("expectedPatterns").EnumerateArray());
    }

    [Fact]
    public void ReservedKind_MapsGivenAndReservedKinds()
    {
        JsonElement error = Error(Mapper.Map(new SpecforgeReservedKindException("DEC", ["DEC", "ITEM", "ART", "REV", "CMT"])));

        Assert.Equal("specforge.id.kind_reserved", error.GetProperty("code").GetString());
        Assert.Equal("DEC", error.GetProperty("data").GetProperty("given").GetString());
        Assert.NotEmpty(error.GetProperty("data").GetProperty("reservedKinds").EnumerateArray());
    }

    [Fact]
    public void KindExhausted_MapsKindAndPackage()
    {
        JsonElement error = Error(Mapper.Map(new SpecforgeKindExhaustedException("XYZ", "pkg")));

        Assert.Equal("specforge.id.kind_exhausted", error.GetProperty("code").GetString());
        Assert.Equal("XYZ", error.GetProperty("data").GetProperty("kind").GetString());
        Assert.Equal("pkg", error.GetProperty("data").GetProperty("package").GetString());
    }

    [Fact]
    public void EmbeddedSkillNotFound_MapsResourceName()
    {
        JsonElement error = Error(Mapper.Map(new SpecforgeEmbeddedSkillNotFoundException("specforge.skills/x/SKILL.md", "broken catalog")));

        Assert.Equal("specforge.skills.catalog_missing", error.GetProperty("code").GetString());
        Assert.Equal("specforge.skills/x/SKILL.md", error.GetProperty("data").GetProperty("resourceName").GetString());
    }

    [Fact]
    public void SkillInstall_MapsAgentPathAndPartialResult()
    {
        SkillInstallResult partial = new(
            new Dictionary<string, SkillInstallAgentResult> { ["claude-code"] = new(1, ["/p/a"], []) },
            "1.0.0");
        SpecforgeSkillInstallException ex = new("codex", "/p/x", typeof(IOException), "denied", partial);

        JsonElement error = Error(Mapper.Map(ex));

        Assert.Equal("specforge.skills.install_failed", error.GetProperty("code").GetString());
        JsonElement data = error.GetProperty("data");
        Assert.Equal("codex", data.GetProperty("agent").GetString());
        Assert.Equal("IOException", data.GetProperty("innerType").GetString());
        Assert.Equal(1, data.GetProperty("partialResult").GetProperty("agents").GetProperty("claude-code").GetProperty("writtenCount").GetInt32());
    }
}

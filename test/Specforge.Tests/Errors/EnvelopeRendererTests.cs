using System.Text.Json;

using Microsoft.Extensions.Logging.Abstractions;

using Specforge.Core.Configuration;
using Specforge.Core.Exceptions;
using Specforge.Mcp.Errors;
using Specforge.Tests.TestSupport;

using Xunit;

namespace Specforge.Tests.Errors;

public class EnvelopeRendererTests
{
    private static readonly EnvelopeRenderer Renderer = new(new ToolExceptionMapper(), NullLogger<EnvelopeRenderer>.Instance);

    private static JsonElement Err(Exception ex)
    {
        using JsonDocument doc = JsonDocument.Parse(Renderer.Render(ex).ToJson());
        return doc.RootElement.GetProperty("error").Clone();
    }

    [Fact]
    public void EveryFixture_RendersItsExpectedCode()
    {
        foreach ((SpecforgeException ex, string expectedCode) in ExceptionFixtures.All)
        {
            Assert.Equal(expectedCode, Err(ex).GetProperty("code").GetString());
        }
    }

    [Fact]
    public void ConfigNotFound_MapsCodeSuggestionAndData()
    {
        JsonElement error = Err(new SpecforgeConfigNotFoundException("/start"));
        Assert.Equal("specforge.config.not_found", error.GetProperty("code").GetString());
        Assert.Contains("init", error.GetProperty("suggestion").GetString()!, StringComparison.Ordinal);
        Assert.Equal("/start", error.GetProperty("data").GetProperty("searchedPath").GetString());
    }

    [Fact]
    public void SchemaVersion_MapsClaimedVersionAndRange()
    {
        JsonElement error = Err(new SpecforgeSchemaVersionException("/p", 2, [1, 1], "upgrade specforge"));
        Assert.Equal("specforge.config.schema_version_unsupported", error.GetProperty("code").GetString());
        Assert.Equal(2, error.GetProperty("data").GetProperty("claimedVersion").GetInt32());
        Assert.Equal([1, 1], error.GetProperty("data").GetProperty("supportedRange").EnumerateArray().Select(e => e.GetInt32()));
    }

    [Fact]
    public void ConfigValidation_MapsErrorPointers()
    {
        JsonElement error = Err(new SpecforgeConfigValidationException("/p", [new ConfigValidationError("/packages", "bad")]));
        Assert.Equal("specforge.config.validation_failed", error.GetProperty("code").GetString());
        JsonElement first = Assert.Single([.. error.GetProperty("data").GetProperty("errors").EnumerateArray()]);
        Assert.Equal("/packages", first.GetProperty("pointer").GetString());
    }

    [Fact]
    public void UnknownPackage_MapsRequestedName()
    {
        JsonElement error = Err(new SpecforgeUnknownPackageException("x",
            [new SpecforgePackageConfig("alpha", "spec/packages/alpha", SpecforgePackageConfig.NoExtraKinds)]));
        Assert.Equal("specforge.package.unknown", error.GetProperty("code").GetString());
        Assert.Equal("x", error.GetProperty("data").GetProperty("requestedName").GetString());
    }

    [Fact]
    public void IdNotFound_MapsId()
    {
        JsonElement error = Err(new SpecforgeIdNotFoundException("REV-DEC-001-002"));
        Assert.Equal("specforge.id.not_found", error.GetProperty("code").GetString());
        Assert.Equal("REV-DEC-001-002", error.GetProperty("data").GetProperty("id").GetString());
        Assert.False(string.IsNullOrEmpty(error.GetProperty("suggestion").GetString()));
    }

    [Fact]
    public void DeleteForbidden_MapsStatusAndAllowedStates()
    {
        JsonElement error = Err(new SpecforgeDeleteForbiddenException("DEC-001", "Approved", ["Not started", "Draft"]));
        Assert.Equal("specforge.lifecycle.delete_forbidden", error.GetProperty("code").GetString());
        Assert.Equal("Approved", error.GetProperty("data").GetProperty("currentStatus").GetString());
        Assert.NotEmpty(error.GetProperty("data").GetProperty("allowedStates").EnumerateArray());
    }

    [Fact]
    public void InvalidArgument_MapsArgumentAndExpected()
    {
        JsonElement error = Err(new SpecforgeInvalidArgumentException("CLAUDE.md", "balanced block", "remove the partial block"));
        Assert.Equal("specforge.tool.invalid_argument", error.GetProperty("code").GetString());
        Assert.Equal("CLAUDE.md", error.GetProperty("data").GetProperty("argument").GetString());
    }

    [Fact]
    public void UnknownException_RendersInternalErrorWithCorrelationId()
    {
        JsonElement error = Err(new InvalidOperationException("boom"));
        Assert.Equal("specforge.tool.internal_error", error.GetProperty("code").GetString());
        Assert.False(string.IsNullOrEmpty(error.GetProperty("data").GetProperty("correlationId").GetString()));
        Assert.False(string.IsNullOrEmpty(error.GetProperty("suggestion").GetString()));
    }

    [Fact]
    public void InvalidArgumentHelper_BuildsTheArgumentEnvelope()
    {
        using JsonDocument doc = JsonDocument.Parse(EnvelopeRenderer.InvalidArgument("aspect", "bogus", new[] { "ids", "all" }, "choose one").ToJson());
        JsonElement error = doc.RootElement.GetProperty("error");
        Assert.Equal("specforge.tool.invalid_argument", error.GetProperty("code").GetString());
        Assert.Equal("aspect", error.GetProperty("data").GetProperty("argument").GetString());
        Assert.Equal("bogus", error.GetProperty("data").GetProperty("given").GetString());
        Assert.Equal("choose one", error.GetProperty("suggestion").GetString());
    }
}

using System.Text.Json;

using Specforge.Core.Configuration;

using Xunit;

namespace Specforge.Tests.Configuration;

public class ConfigValidatorTests
{
    [Fact]
    public void MissingRequiredFields_ProduceJsonPointerErrors()
    {
        using JsonDocument doc = JsonDocument.Parse("""{ "schemaVersion": 1 }""");

        IReadOnlyList<ConfigValidationError> errors = ConfigValidator.Validate(doc.RootElement);

        Assert.Contains(errors, e => e.Pointer == "/shared");
        Assert.Contains(errors, e => e.Pointer == "/templates");
        Assert.Contains(errors, e => e.Pointer == "/packages");
    }

    [Fact]
    public void PackagesNotArray_ProducesPointerError()
    {
        using JsonDocument doc = JsonDocument.Parse("""
            { "schemaVersion": 1, "shared": "s", "templates": "t", "packages": "not-an-array" }
            """);

        IReadOnlyList<ConfigValidationError> errors = ConfigValidator.Validate(doc.RootElement);

        Assert.Contains(errors, e => e.Pointer == "/packages");
    }

    [Fact]
    public void ExtraKindsShapeInvalid_ProducesError()
    {
        using JsonDocument doc = JsonDocument.Parse("""
            {
              "schemaVersion": 1, "shared": "s", "templates": "t",
              "packages": [ { "name": "p", "path": "x", "extraKinds": ["toolong7"] } ]
            }
            """);

        IReadOnlyList<ConfigValidationError> errors = ConfigValidator.Validate(doc.RootElement);

        Assert.Contains(errors, e => e.Pointer == "/packages/0/extraKinds/0");
    }

    [Fact]
    public void ExtraKindsReservedButValidShape_NoValidatorError()
    {
        // "DEC" is shape-valid (^[A-Z]{2,6}$). Reserved-kind enforcement is ITEM-003's job, not the
        // ITEM-002 shape validator's — so this must NOT produce an extraKinds shape error here.
        using JsonDocument doc = JsonDocument.Parse("""
            {
              "schemaVersion": 1, "shared": "s", "templates": "t",
              "packages": [ { "name": "p", "path": "x", "extraKinds": ["DEC"] } ]
            }
            """);

        IReadOnlyList<ConfigValidationError> errors = ConfigValidator.Validate(doc.RootElement);

        Assert.DoesNotContain(errors, e => e.Pointer.Contains("extraKinds", StringComparison.Ordinal));
    }

    [Fact]
    public void ExtraKindsDuplicate_ProducesError()
    {
        using JsonDocument doc = JsonDocument.Parse("""
            {
              "schemaVersion": 1, "shared": "s", "templates": "t",
              "packages": [ { "name": "p", "path": "x", "extraKinds": ["RDM", "RDM"] } ]
            }
            """);

        IReadOnlyList<ConfigValidationError> errors = ConfigValidator.Validate(doc.RootElement);

        Assert.Contains(errors, e => e.Pointer == "/packages/0/extraKinds/1");
    }
}

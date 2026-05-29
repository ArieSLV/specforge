using System.Text.Json;
using System.Text.RegularExpressions;

using Specforge.Core.Identifiers;

namespace Specforge.Core.Configuration;

/// <summary>
/// DEC-002 shape validation with JSON-Pointer (RFC 6901) error paths. Assumes the
/// <c>schemaVersion</c> has already been accepted by <see cref="SchemaVersionGate"/>.
/// Tolerates unknown top-level fields (DEC-006 additive rule) but rejects unknown fields
/// inside <c>packages[]</c> entries (strictly typed). Validates <c>extraKinds</c> shape only
/// (<c>^[A-Z]{2,6}$</c>); reserved-kind semantics are enforced later by the ID validator (ITEM-003).
/// </summary>
public static partial class ConfigValidator
{
    private static readonly HashSet<string> AllowedPackageFields = ["name", "path", "extraKinds"];

    [GeneratedRegex("^[A-Z]{2,6}$")]
    private static partial Regex KindShape();

    /// <summary>Collects every shape error; an empty result means the file is structurally valid.</summary>
    public static IReadOnlyList<ConfigValidationError> Validate(JsonElement root)
    {
        List<ConfigValidationError> errors = [];

        RequireNonEmptyString(root, "shared", "/shared", errors);
        RequireNonEmptyString(root, "templates", "/templates", errors);

        if (!root.TryGetProperty("packages", out JsonElement packages))
        {
            errors.Add(new ConfigValidationError("/packages", "required field 'packages' is missing"));
        }
        else if (packages.ValueKind != JsonValueKind.Array)
        {
            errors.Add(new ConfigValidationError("/packages", "field 'packages' must be an array"));
        }
        else
        {
            int i = 0;
            foreach (JsonElement pkg in packages.EnumerateArray())
            {
                ValidatePackage(pkg, $"/packages/{i}", errors);
                i++;
            }
        }

        return errors;
    }

    /// <summary>
    /// DEC-004 reserved-kind enforcement (retrofit added by ITEM-003): rejects a package whose
    /// <c>extraKinds</c> contains a core kind, throwing <see cref="Exceptions.SpecforgeReservedKindException"/>
    /// at config-load time rather than waiting for a tool to use the kind.
    /// </summary>
    public static void EnforceReservedKinds(IReadOnlyList<SpecforgePackageConfig> packages)
    {
        ArgumentNullException.ThrowIfNull(packages);
        foreach (SpecforgePackageConfig package in packages)
        {
            KindRegistry.ValidateExtraKinds(package.ExtraKinds);
        }
    }

    private static void ValidatePackage(JsonElement pkg, string basePtr, List<ConfigValidationError> errors)
    {
        if (pkg.ValueKind != JsonValueKind.Object)
        {
            errors.Add(new ConfigValidationError(basePtr, "package entry must be a JSON object"));
            return;
        }

        RequireNonEmptyString(pkg, "name", $"{basePtr}/name", errors);
        RequireNonEmptyString(pkg, "path", $"{basePtr}/path", errors);

        foreach (JsonProperty prop in pkg.EnumerateObject())
        {
            if (!AllowedPackageFields.Contains(prop.Name))
            {
                errors.Add(new ConfigValidationError(
                    $"{basePtr}/{Escape(prop.Name)}",
                    $"unknown field '{prop.Name}' in package entry"));
            }
        }

        if (pkg.TryGetProperty("extraKinds", out JsonElement extraKinds))
        {
            ValidateExtraKinds(extraKinds, $"{basePtr}/extraKinds", errors);
        }
    }

    private static void ValidateExtraKinds(JsonElement extraKinds, string basePtr, List<ConfigValidationError> errors)
    {
        if (extraKinds.ValueKind != JsonValueKind.Array)
        {
            errors.Add(new ConfigValidationError(basePtr, "field 'extraKinds' must be an array of strings"));
            return;
        }

        HashSet<string> seen = new(StringComparer.Ordinal);
        int j = 0;
        foreach (JsonElement entry in extraKinds.EnumerateArray())
        {
            string ptr = $"{basePtr}/{j}";
            if (entry.ValueKind != JsonValueKind.String)
            {
                errors.Add(new ConfigValidationError(ptr, "extraKinds entry must be a string"));
            }
            else
            {
                string value = entry.GetString()!;
                if (!KindShape().IsMatch(value))
                {
                    errors.Add(new ConfigValidationError(ptr, $"extraKinds entry '{value}' must match ^[A-Z]{{2,6}}$"));
                }
                else if (!seen.Add(value))
                {
                    errors.Add(new ConfigValidationError(ptr, $"duplicate extraKinds entry '{value}'"));
                }
            }

            j++;
        }
    }

    private static void RequireNonEmptyString(JsonElement obj, string field, string ptr, List<ConfigValidationError> errors)
    {
        if (!obj.TryGetProperty(field, out JsonElement value))
        {
            errors.Add(new ConfigValidationError(ptr, $"required field '{field}' is missing"));
        }
        else if (value.ValueKind != JsonValueKind.String)
        {
            errors.Add(new ConfigValidationError(ptr, $"field '{field}' must be a string"));
        }
        else if (string.IsNullOrWhiteSpace(value.GetString()))
        {
            errors.Add(new ConfigValidationError(ptr, $"field '{field}' must be a non-empty string"));
        }
    }

    private static string Escape(string token) => token.Replace("~", "~0", StringComparison.Ordinal).Replace("/", "~1", StringComparison.Ordinal);
}

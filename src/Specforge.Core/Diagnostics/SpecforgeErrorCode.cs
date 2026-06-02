using System.Reflection;

namespace Specforge.Core.Diagnostics;

/// <summary>
/// The single, canonical store of every <c>specforge.&lt;domain&gt;.&lt;reason&gt;</c> error code
/// (DEC-007 §"Error-Code Catalog"; centralized by ITEM-011). Lives in Core so typed exceptions can set
/// their <c>ErrorCode</c> from a constant and the MCP host maps from the same source — no string-literal
/// scatter. Constants only, so the Core→no-MCP boundary is preserved.
/// </summary>
public static class SpecforgeErrorCode
{
    public const string ConfigNotFound = "specforge.config.not_found";
    public const string ConfigSchemaVersionUnsupported = "specforge.config.schema_version_unsupported";
    public const string ConfigValidationFailed = "specforge.config.validation_failed";
    public const string PackageNotSelected = "specforge.package.not_selected";
    public const string PackageUnknown = "specforge.package.unknown";
    public const string IdInvalid = "specforge.id.invalid";
    public const string IdNotFound = "specforge.id.not_found";
    public const string IdKindReserved = "specforge.id.kind_reserved";
    public const string IdKindExhausted = "specforge.id.kind_exhausted";
    public const string SkillsInstallFailed = "specforge.skills.install_failed";
    public const string SkillsCatalogMissing = "specforge.skills.catalog_missing";
    public const string LifecycleDeleteForbidden = "specforge.lifecycle.delete_forbidden";
    public const string ToolInvalidArgument = "specforge.tool.invalid_argument";
    public const string ToolInternalError = "specforge.tool.internal_error";

    /// <summary>Every declared code, reflected from the <c>public const string</c> fields above.</summary>
    public static IReadOnlySet<string> AllCodes { get; } =
        typeof(SpecforgeErrorCode)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f is { IsLiteral: true, IsInitOnly: false } && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToHashSet(StringComparer.Ordinal);
}

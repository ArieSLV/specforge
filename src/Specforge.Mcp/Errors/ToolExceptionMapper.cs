using Specforge.Core.Configuration;
using Specforge.Core.Diagnostics;
using Specforge.Core.Exceptions;
using Specforge.Mcp.Tools;

namespace Specforge.Mcp.Errors;

/// <summary>
/// The single, consolidated map from a Core typed exception to its <see cref="MappedError"/> (ITEM-011;
/// absorbs the per-item arms that accumulated across ITEM-002..010). Returns <see langword="null"/> for
/// any non-Specforge exception — the <see cref="EnvelopeRenderer"/> renders those as
/// <c>tool.internal_error</c>. The <see cref="HandledTypes"/> set is the contract the orphan-exception
/// invariant test checks against the actual Core exception inventory.
/// </summary>
public sealed class ToolExceptionMapper
{
    /// <summary>Every Core exception type this mapper has an arm for (drift-checked against the assembly).</summary>
    public static IReadOnlySet<Type> HandledTypes { get; } = new HashSet<Type>
    {
        typeof(SpecforgeConfigNotFoundException),
        typeof(SpecforgeSchemaVersionException),
        typeof(SpecforgeConfigValidationException),
        typeof(SpecforgePackageNotSelectedException),
        typeof(SpecforgeUnknownPackageException),
        typeof(SpecforgeInvalidIdentifierException),
        typeof(SpecforgeIdNotFoundException),
        typeof(SpecforgeReservedKindException),
        typeof(SpecforgeKindExhaustedException),
        typeof(SpecforgeSkillInstallException),
        typeof(SpecforgeEmbeddedSkillNotFoundException),
        typeof(SpecforgeDeleteForbiddenException),
        typeof(SpecforgeInvalidArgumentException),
    };

    /// <summary>Maps a known typed exception to its <see cref="MappedError"/>, or null for an unmapped type.</summary>
    public MappedError? Map(Exception exception) => exception switch
    {
        SpecforgeConfigNotFoundException e => new MappedError(
            SpecforgeErrorCode.ConfigNotFound, "run init to generate .specforge.json at the project root",
            _ => new { searchedPath = e.SearchedPath }),

        SpecforgeSchemaVersionException e => new MappedError(
            SpecforgeErrorCode.ConfigSchemaVersionUnsupported, e.Suggestion,
            _ => new { path = e.Path, claimedVersion = e.ClaimedVersion, supportedRange = e.SupportedRange }),

        SpecforgeConfigValidationException e => new MappedError(
            SpecforgeErrorCode.ConfigValidationFailed, "fix the listed fields in .specforge.json",
            _ => new { path = e.Path, errors = e.Errors.Select(x => new { pointer = x.Pointer, message = x.Message }) }),

        SpecforgePackageNotSelectedException e => new MappedError(
            SpecforgeErrorCode.PackageNotSelected, "call use_package with one of the listed names",
            _ => new { availablePackages = e.AvailablePackages.Select(ToPackageData) }),

        SpecforgeUnknownPackageException e => new MappedError(
            SpecforgeErrorCode.PackageUnknown, "call use_package with one of the listed names",
            _ => new { requestedName = e.RequestedName, availablePackages = e.AvailablePackages.Select(ToPackageData) }),

        SpecforgeInvalidIdentifierException e => new MappedError(
            SpecforgeErrorCode.IdInvalid, "correct the identifier to match one of the expected patterns",
            _ => new { given = e.Given, expectedPatterns = e.ExpectedPatterns }),

        SpecforgeIdNotFoundException e => new MappedError(
            SpecforgeErrorCode.IdNotFound, "verify the identifier exists in this package; use the relevant list_* tool to enumerate available IDs",
            _ => new { id = e.Id }),

        SpecforgeReservedKindException e => new MappedError(
            SpecforgeErrorCode.IdKindReserved, "choose a non-reserved kind for extraKinds",
            _ => new { given = e.Given, reservedKinds = e.ReservedKinds }),

        SpecforgeKindExhaustedException e => new MappedError(
            SpecforgeErrorCode.IdKindExhausted, "split the package or otherwise reorganize — 999 IDs of one kind reached",
            _ => new { kind = e.Kind, package = e.Package }),

        SpecforgeSkillInstallException e => new MappedError(
            SpecforgeErrorCode.SkillsInstallFailed, "check filesystem permissions on the target directory",
            _ => new { agent = e.Agent, path = e.Path, innerType = e.InnerExceptionType.Name, innerMessage = e.InnerMessage, partialResult = InstallSkillsTool.ToPayload(e.PartialResult) }),

        SpecforgeEmbeddedSkillNotFoundException e => new MappedError(
            SpecforgeErrorCode.SkillsCatalogMissing, "rebuild specforge — the binary is missing embedded skill resources",
            _ => new { resourceName = e.ResourceName }),

        SpecforgeDeleteForbiddenException e => new MappedError(
            SpecforgeErrorCode.LifecycleDeleteForbidden, "transition the artifact to Withdrawn via set_*_status instead, or supersede with a new artifact",
            _ => new { id = e.Id, currentStatus = e.CurrentStatus, allowedStates = e.AllowedStates }),

        SpecforgeInvalidArgumentException e => new MappedError(
            SpecforgeErrorCode.ToolInvalidArgument, e.Suggestion,
            _ => new { argument = e.Argument, given = e.Given, expected = e.Expected }),

        _ => null,
    };

    private static object ToPackageData(SpecforgePackageConfig package) => new { name = package.Name, path = package.Path };
}

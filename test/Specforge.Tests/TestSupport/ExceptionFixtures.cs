using Specforge.Core.Configuration;
using Specforge.Core.Diagnostics;
using Specforge.Core.Exceptions;
using Specforge.Core.Skills;

namespace Specforge.Tests.TestSupport;

/// <summary>One representative instance per typed Core exception, paired with its expected code (ITEM-011 tests).</summary>
internal static class ExceptionFixtures
{
    private static IReadOnlyList<SpecforgePackageConfig> TwoPackages =>
    [
        new SpecforgePackageConfig("alpha", "spec/packages/alpha", SpecforgePackageConfig.NoExtraKinds),
        new SpecforgePackageConfig("beta", "spec/packages/beta", SpecforgePackageConfig.NoExtraKinds),
    ];

    private static SkillInstallResult Partial =>
        new(new Dictionary<string, SkillInstallAgentResult> { ["claude-code"] = new(1, ["/p/a"], []) }, "1.0.0");

    /// <summary>Each typed exception with the error code it must map to.</summary>
    public static IReadOnlyList<(SpecforgeException Exception, string ExpectedCode)> All { get; } =
    [
        (new SpecforgeConfigNotFoundException("/start"), SpecforgeErrorCode.ConfigNotFound),
        (new SpecforgeSchemaVersionException("/p", 2, [1, 1], "upgrade specforge"), SpecforgeErrorCode.ConfigSchemaVersionUnsupported),
        (new SpecforgeConfigValidationException("/p", [new ConfigValidationError("/packages", "bad")]), SpecforgeErrorCode.ConfigValidationFailed),
        (new SpecforgePackageNotSelectedException(TwoPackages), SpecforgeErrorCode.PackageNotSelected),
        (new SpecforgeUnknownPackageException("x", TwoPackages), SpecforgeErrorCode.PackageUnknown),
        (new SpecforgeInvalidIdentifierException("dec-001", ["^[A-Z]{2,6}-[0-9]{3}$"]), SpecforgeErrorCode.IdInvalid),
        (new SpecforgeIdNotFoundException("REV-DEC-001-002"), SpecforgeErrorCode.IdNotFound),
        (new SpecforgeReservedKindException("DEC", ["DEC", "ITEM", "ART", "REV", "CMT"]), SpecforgeErrorCode.IdKindReserved),
        (new SpecforgeKindExhaustedException("XYZ", "pkg"), SpecforgeErrorCode.IdKindExhausted),
        (new SpecforgeSkillInstallException("codex", "/p/x", typeof(IOException), "denied", Partial), SpecforgeErrorCode.SkillsInstallFailed),
        (new SpecforgeEmbeddedSkillNotFoundException("specforge.skills/x/SKILL.md", "broken catalog"), SpecforgeErrorCode.SkillsCatalogMissing),
        (new SpecforgeDeleteForbiddenException("DEC-001", "Approved", ["Not started", "Placeholder", "Draft", "Draft for user review"]), SpecforgeErrorCode.LifecycleDeleteForbidden),
        (new SpecforgeInvalidArgumentException("CLAUDE.md", "balanced block", "remove the partial block"), SpecforgeErrorCode.ToolInvalidArgument),
    ];
}

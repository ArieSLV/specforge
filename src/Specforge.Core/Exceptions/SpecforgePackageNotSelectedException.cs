using Specforge.Core.Configuration;

namespace Specforge.Core.Exceptions;

/// <summary>
/// A package-requiring operation ran while the config defines more than one package
/// and none is selected (DEC-002 §"Package Selection"). Maps to <c>specforge.package.not_selected</c>.
/// </summary>
public sealed class SpecforgePackageNotSelectedException : SpecforgeException
{
    public SpecforgePackageNotSelectedException(IReadOnlyList<SpecforgePackageConfig> availablePackages)
        : base(Diagnostics.SpecforgeErrorCode.PackageNotSelected,
            "No active package is selected and the configuration defines more than one package.")
    {
        AvailablePackages = availablePackages;
    }

    /// <summary>The packages the caller may choose from via <c>use_package</c>.</summary>
    public IReadOnlyList<SpecforgePackageConfig> AvailablePackages { get; }
}

using Specforge.Core.Configuration;

namespace Specforge.Core.Exceptions;

/// <summary>
/// <c>use_package</c> was called with a name not present in the active config.
/// Maps to <c>specforge.package.unknown</c>.
/// </summary>
public sealed class SpecforgeUnknownPackageException : SpecforgeException
{
    public SpecforgeUnknownPackageException(string requestedName, IReadOnlyList<SpecforgePackageConfig> availablePackages)
        : base("specforge.package.unknown",
            $"No package named '{requestedName}' exists in the active configuration.")
    {
        RequestedName = requestedName;
        AvailablePackages = availablePackages;
    }

    /// <summary>The name the caller asked for.</summary>
    public string RequestedName { get; }

    /// <summary>The packages the caller may choose from instead.</summary>
    public IReadOnlyList<SpecforgePackageConfig> AvailablePackages { get; }
}

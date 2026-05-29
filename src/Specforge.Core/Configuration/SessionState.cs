using Specforge.Core.Exceptions;

namespace Specforge.Core.Configuration;

/// <summary>
/// Process-local session state (DEC-002): the loaded config plus the active package selection.
/// Nothing here is persisted to disk; a new session rebuilds it.
/// </summary>
public sealed class SessionState
{
    private SpecforgeConfig? _config;
    private string? _activePackageName;

    /// <summary>The loaded config, or <see langword="null"/> before the first successful load.</summary>
    public SpecforgeConfig? Config => _config;

    /// <summary>The active package name, or <see langword="null"/> when unselected (multi-package, no choice yet).</summary>
    public string? ActivePackageName => _activePackageName;

    /// <summary>
    /// Caches the loaded config and applies DEC-002's auto-select rule: a single-package config
    /// selects its only package; a multi-package config leaves the selection null.
    /// </summary>
    public void Initialize(SpecforgeConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        _config = config;
        _activePackageName = config.Packages.Count == 1 ? config.Packages[0].Name : null;
    }

    /// <summary>
    /// Selects an existing package by name (DEC-002 switchable selection). Throws
    /// <see cref="SpecforgeUnknownPackageException"/> when the name is not in the config.
    /// </summary>
    public void Select(string name)
    {
        SpecforgeConfig config = _config ?? throw new InvalidOperationException("Configuration is not loaded.");
        bool exists = config.Packages.Any(p => string.Equals(p.Name, name, StringComparison.Ordinal));
        if (!exists)
        {
            throw new SpecforgeUnknownPackageException(name, config.Packages);
        }

        _activePackageName = name;
    }

    /// <summary>
    /// Returns the active package, or throws <see cref="SpecforgePackageNotSelectedException"/>
    /// when none is selected. Used by every tool that needs a package context.
    /// </summary>
    public SpecforgePackageConfig RequireActivePackage()
    {
        SpecforgeConfig config = _config ?? throw new InvalidOperationException("Configuration is not loaded.");
        if (_activePackageName is null)
        {
            throw new SpecforgePackageNotSelectedException(config.Packages);
        }

        return config.Packages.First(p => string.Equals(p.Name, _activePackageName, StringComparison.Ordinal));
    }

    /// <summary>Clears the active selection (config remains loaded).</summary>
    public void Clear() => _activePackageName = null;
}

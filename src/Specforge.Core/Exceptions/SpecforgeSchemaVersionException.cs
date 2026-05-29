namespace Specforge.Core.Exceptions;

/// <summary>
/// The config declares a <c>schemaVersion</c> outside the binary's supported range,
/// or the field is missing / malformed (DEC-006 §"Unsupported-Version Behavior").
/// Maps to <c>specforge.config.schema_version_unsupported</c>.
/// </summary>
public sealed class SpecforgeSchemaVersionException : SpecforgeException
{
    public SpecforgeSchemaVersionException(string path, object? claimedVersion, IReadOnlyList<int> supportedRange, string suggestion)
        : base("specforge.config.schema_version_unsupported",
            $"Configuration at '{path}' declares an unsupported or invalid schemaVersion.")
    {
        Path = path;
        ClaimedVersion = claimedVersion;
        SupportedRange = supportedRange;
        Suggestion = suggestion;
    }

    /// <summary>Absolute path of the offending config file.</summary>
    public string Path { get; }

    /// <summary>The declared version: an <see cref="int"/>, the offending non-int value, or <see langword="null"/> when missing.</summary>
    public object? ClaimedVersion { get; }

    /// <summary>Inclusive <c>[min, max]</c> range this binary supports.</summary>
    public IReadOnlyList<int> SupportedRange { get; }

    /// <summary>Actionable next step (upgrade vs. invalid-field variant).</summary>
    public string Suggestion { get; }
}

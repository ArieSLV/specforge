using Specforge.Core.Configuration;

namespace Specforge.Core.Exceptions;

/// <summary>
/// The config's <c>schemaVersion</c> is supported but the file shape is wrong
/// (missing required field, wrong JSON type, malformed JSON). Distinct from
/// <see cref="SpecforgeSchemaVersionException"/> per DEC-006 §"Error Types".
/// Maps to <c>specforge.config.validation_failed</c>.
/// </summary>
public sealed class SpecforgeConfigValidationException : SpecforgeException
{
    public SpecforgeConfigValidationException(string path, IReadOnlyList<ConfigValidationError> errors)
        : base("specforge.config.validation_failed",
            $"Configuration at '{path}' failed shape validation with {errors.Count} error(s).")
    {
        Path = path;
        Errors = errors;
    }

    /// <summary>Absolute path of the offending config file.</summary>
    public string Path { get; }

    /// <summary>JSON-Pointer-located shape errors (RFC 6901).</summary>
    public IReadOnlyList<ConfigValidationError> Errors { get; }
}

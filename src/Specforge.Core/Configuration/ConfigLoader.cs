using System.Text.Json;

using Specforge.Core.Configuration.Migrations;
using Specforge.Core.Exceptions;

namespace Specforge.Core.Configuration;

/// <summary>
/// Orchestrates the full load pipeline (DEC-002/DEC-006): discovery → file read → JSON parse →
/// schema-version gate → shape validation → in-memory upgrade → projection. Returns a
/// <see cref="SpecforgeConfig"/> or throws one of the typed config exceptions.
/// </summary>
public sealed class ConfigLoader
{
    /// <summary>
    /// Discovers and loads the config by walking up from <paramref name="startDir"/>.
    /// Throws <see cref="SpecforgeConfigNotFoundException"/> when no <c>.specforge.json</c> exists.
    /// </summary>
    public async Task<SpecforgeConfig> LoadAsync(string startDir, CancellationToken ct = default)
    {
        string? path = await ConfigDiscovery.FindConfigAsync(startDir, ct).ConfigureAwait(false);
        if (path is null)
        {
            throw new SpecforgeConfigNotFoundException(Path.GetFullPath(startDir));
        }

        string text = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
        return ParseAndValidate(text, path);
    }

    /// <summary>
    /// Pure (no I/O) variant for a known config path and content — used by tests and by
    /// <see cref="LoadAsync"/>. Applies the same gate → validate → upgrade → project pipeline.
    /// </summary>
    public static SpecforgeConfig ParseAndValidate(string json, string configPath)
    {
        JsonDocument document;
        try
        {
            document = ConfigParser.ParsePermissive(json);
        }
        catch (JsonException ex)
        {
            throw new SpecforgeConfigValidationException(
                configPath,
                [new ConfigValidationError(string.Empty, $"file is not valid JSON: {ex.Message}")]);
        }

        using (document)
        {
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new SpecforgeConfigValidationException(
                    configPath,
                    [new ConfigValidationError(string.Empty, "configuration root must be a JSON object")]);
            }

            SchemaVersionGate.Check(root, configPath);

            IReadOnlyList<ConfigValidationError> errors = ConfigValidator.Validate(root);
            if (errors.Count > 0)
            {
                throw new SpecforgeConfigValidationException(configPath, errors);
            }

            SpecforgeConfig config = ConfigParser.Project(root, configPath);
            ConfigValidator.EnforceReservedKinds(config.Packages); // DEC-004 reserved-kind check (ITEM-003 retrofit)
            return V1Migration.Apply(config);
        }
    }
}

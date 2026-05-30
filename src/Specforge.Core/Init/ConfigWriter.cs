using System.Text.Json;
using System.Text.Json.Serialization;

using Specforge.Core.Configuration;

namespace Specforge.Core.Init;

/// <summary>
/// Serializes a <see cref="SpecforgeConfig"/> to canonical <c>.specforge.json</c> and writes it
/// atomically (temp file + rename). camelCase keys; <c>extraKinds</c> omitted when empty.
/// </summary>
public sealed class ConfigWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>Renders the canonical JSON text (trailing newline included).</summary>
    public string Serialize(SpecforgeConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        ConfigDto dto = new(
            config.SchemaVersion,
            config.Shared,
            config.Templates,
            [.. config.Packages.Select(p => new PackageDto(p.Name, p.Path, p.ExtraKinds.Count == 0 ? null : [.. p.ExtraKinds]))]);
        return JsonSerializer.Serialize(dto, SerializerOptions) + "\n";
    }

    /// <summary>Writes <paramref name="content"/> to <paramref name="targetPath"/> via a temp file + rename.</summary>
    public async Task WriteAtomicAsync(string content, string targetPath, CancellationToken ct)
    {
        string directory = Path.GetDirectoryName(targetPath) ?? ".";
        Directory.CreateDirectory(directory);
        string temp = targetPath + ".tmp";
        await File.WriteAllTextAsync(temp, content, ct).ConfigureAwait(false);
        File.Move(temp, targetPath, overwrite: true);
    }

    private sealed record ConfigDto(int schemaVersion, string shared, string templates, IReadOnlyList<PackageDto> packages);

    private sealed record PackageDto(string name, string path, IReadOnlyList<string>? extraKinds);
}

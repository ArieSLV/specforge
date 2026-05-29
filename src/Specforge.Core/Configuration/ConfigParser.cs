using System.Text.Json;

namespace Specforge.Core.Configuration;

/// <summary>
/// System.Text.Json parsing into a permissive DOM plus projection of a *validated* DOM
/// into the immutable <see cref="SpecforgeConfig"/> record graph.
/// </summary>
public static class ConfigParser
{
    private static readonly JsonDocumentOptions PermissiveOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip,
    };

    /// <summary>Parses raw JSON text into a DOM. Throws <see cref="JsonException"/> on malformed JSON.</summary>
    public static JsonDocument ParsePermissive(string json) => JsonDocument.Parse(json, PermissiveOptions);

    /// <summary>
    /// Projects a DOM that has already passed <see cref="SchemaVersionGate"/> and
    /// <see cref="ConfigValidator"/> into a <see cref="SpecforgeConfig"/>. Paths are kept relative;
    /// only <paramref name="configPath"/> is normalized to absolute.
    /// </summary>
    public static SpecforgeConfig Project(JsonElement root, string configPath)
    {
        int schemaVersion = root.GetProperty("schemaVersion").GetInt32();
        string shared = root.GetProperty("shared").GetString()!;
        string templates = root.GetProperty("templates").GetString()!;

        List<SpecforgePackageConfig> packages = [];
        foreach (JsonElement pkg in root.GetProperty("packages").EnumerateArray())
        {
            string name = pkg.GetProperty("name").GetString()!;
            string path = pkg.GetProperty("path").GetString()!;

            IReadOnlyList<string> extraKinds = SpecforgePackageConfig.NoExtraKinds;
            if (pkg.TryGetProperty("extraKinds", out JsonElement ek) && ek.ValueKind == JsonValueKind.Array)
            {
                List<string> kinds = [];
                foreach (JsonElement entry in ek.EnumerateArray())
                {
                    kinds.Add(entry.GetString()!);
                }

                extraKinds = kinds;
            }

            packages.Add(new SpecforgePackageConfig(name, path, extraKinds));
        }

        return new SpecforgeConfig(schemaVersion, shared, templates, packages, Path.GetFullPath(configPath));
    }
}

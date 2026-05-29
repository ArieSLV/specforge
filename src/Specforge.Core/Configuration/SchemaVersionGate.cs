using System.Text.Json;

using Specforge.Core.Diagnostics;
using Specforge.Core.Exceptions;

namespace Specforge.Core.Configuration;

/// <summary>
/// DEC-006 §"Unsupported-Version Behavior": hard-refuse when <c>schemaVersion</c> is missing,
/// malformed, below 1, or above the binary's supported maximum. Never silently degrades.
/// </summary>
public static class SchemaVersionGate
{
    /// <summary>
    /// Validates the <c>schemaVersion</c> field on <paramref name="root"/>. Throws
    /// <see cref="SpecforgeSchemaVersionException"/> on any violation; returns normally when supported.
    /// </summary>
    public static void Check(JsonElement root, string configPath)
    {
        int min = BinaryInfo.SupportedSchemaVersionMin;
        int max = BinaryInfo.SupportedSchemaVersionMax;
        IReadOnlyList<int> range = [min, max];
        string invalidSuggestion = $"invalid schemaVersion; expected positive integer in [{min}..{max}]";

        if (!root.TryGetProperty("schemaVersion", out JsonElement sv))
        {
            throw new SpecforgeSchemaVersionException(configPath, null, range, invalidSuggestion);
        }

        if (sv.ValueKind != JsonValueKind.Number || !sv.TryGetInt32(out int claimed))
        {
            object? offending = sv.ValueKind switch
            {
                JsonValueKind.String => sv.GetString(),
                JsonValueKind.Number => sv.GetDouble(),
                JsonValueKind.True or JsonValueKind.False => sv.GetBoolean(),
                JsonValueKind.Null => null,
                _ => sv.GetRawText(),
            };
            throw new SpecforgeSchemaVersionException(configPath, offending, range, invalidSuggestion);
        }

        if (claimed < min)
        {
            throw new SpecforgeSchemaVersionException(configPath, claimed, range, invalidSuggestion);
        }

        if (claimed > max)
        {
            throw new SpecforgeSchemaVersionException(configPath, claimed, range,
                $"upgrade specforge to a version that supports schemaVersion={claimed} or lower");
        }
    }
}

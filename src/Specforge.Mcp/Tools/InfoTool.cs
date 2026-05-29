using System.Text.Json;

using Specforge.Core.Configuration;
using Specforge.Core.Diagnostics;
using Specforge.Core.Exceptions;
using Specforge.Mcp.Hosting;

namespace Specforge.Mcp.Tools;

/// <summary>
/// <c>info</c> (DEC-006/DEC-007): report binary version, supported schemaVersion range, active package,
/// and config path. Must always succeed — safe to call before config discovery.
/// </summary>
public sealed class InfoTool(ConfigLoader loader, SessionState session, SpecforgeWorkingDirectory workingDirectory, BinaryInfo binaryInfo)
    : SpecforgeToolBase(loader, session, workingDirectory)
{
    private static readonly JsonElement Schema = JsonSerializer.Deserialize<JsonElement>(
        """
        {
          "type": "object",
          "description": "Report binaryVersion, supportedSchemaVersionRange, activePackage, and configPath. Read-only diagnostic; always succeeds, including before config discovery.",
          "properties": {},
          "additionalProperties": false
        }
        """);

    public override string Name => "info";

    public override JsonElement InputSchema => Schema;

    public override async Task<ToolResult> InvokeAsync(JsonElement args, CancellationToken ct)
    {
        string? configPath = null;
        string? activePackage = null;

        try
        {
            await EnsureLoadedAsync(ct).ConfigureAwait(false);
            configPath = Session.Config!.ConfigPath;
            activePackage = Session.ActivePackageName;
        }
        catch (SpecforgeConfigNotFoundException)
        {
            configPath = null;
        }
#pragma warning disable CA1031 // info is a diagnostic that must never throw, even on a malformed config.
        catch (Exception)
        {
            configPath = await SafeDiscoverAsync(ct).ConfigureAwait(false);
            activePackage = null;
        }
#pragma warning restore CA1031

        return ToolResult.Ok(new
        {
            binaryVersion = binaryInfo.Version,
            supportedSchemaVersionRange = binaryInfo.SupportedSchemaVersionRange,
            activePackage,
            configPath,
        });
    }

    private async Task<string?> SafeDiscoverAsync(CancellationToken ct)
    {
        try
        {
            return await ConfigDiscovery.FindConfigAsync(StartDirectory, ct).ConfigureAwait(false);
        }
#pragma warning disable CA1031 // best-effort discovery for diagnostics; failures degrade to null.
        catch (Exception)
        {
            return null;
        }
#pragma warning restore CA1031
    }
}

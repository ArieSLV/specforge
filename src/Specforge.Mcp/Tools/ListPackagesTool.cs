using System.Text.Json;

using Specforge.Core.Configuration;
using Specforge.Core.Exceptions;
using Specforge.Mcp.Hosting;

namespace Specforge.Mcp.Tools;

/// <summary>
/// <c>list_packages</c> (DEC-007): enumerate the active config's packages. Returns an empty array
/// — never an error — when no config is found.
/// </summary>
public sealed class ListPackagesTool(ConfigLoader loader, SessionState session, SpecforgeWorkingDirectory workingDirectory)
    : SpecforgeToolBase(loader, session, workingDirectory)
{
    private static readonly JsonElement Schema = JsonSerializer.Deserialize<JsonElement>(
        """
        {
          "type": "object",
          "description": "Enumerate the active configuration's packages. Read-only; returns [] when no config is found.",
          "properties": {},
          "additionalProperties": false
        }
        """);

    public override string Name => "list_packages";

    public override JsonElement InputSchema => Schema;

    public override async Task<ToolResult> InvokeAsync(JsonElement args, CancellationToken ct)
    {
        try
        {
            await EnsureLoadedAsync(ct).ConfigureAwait(false);
        }
        catch (SpecforgeConfigNotFoundException)
        {
            return ToolResult.Ok(new { packages = Array.Empty<object>() });
        }

        var packages = Session.Config!.Packages.Select(p => new
        {
            name = p.Name,
            path = p.Path,
            extraKinds = p.ExtraKinds,
        });

        return ToolResult.Ok(new { packages });
    }
}

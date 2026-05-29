using System.Text.Json;

using Specforge.Core.Configuration;
using Specforge.Mcp.Hosting;

namespace Specforge.Mcp.Tools;

/// <summary>
/// Shared base for tools that need a loaded config. Centralizes the once-per-session
/// discovery + load + cache step so every tool body stays focused on its own logic.
/// </summary>
public abstract class SpecforgeToolBase(ConfigLoader loader, SessionState session, SpecforgeWorkingDirectory workingDirectory) : IMcpTool
{
    /// <summary>The process-local session state (loaded config + active selection).</summary>
    protected SessionState Session => session;

    /// <summary>The directory config discovery walks up from.</summary>
    protected string StartDirectory => workingDirectory.Path;

    public abstract string Name { get; }

    public abstract JsonElement InputSchema { get; }

    public abstract Task<ToolResult> InvokeAsync(JsonElement args, CancellationToken ct);

    /// <summary>Loads and caches the config on first use; returns the cached config thereafter.</summary>
    protected async Task<SpecforgeConfig> EnsureLoadedAsync(CancellationToken ct)
    {
        if (session.Config is null)
        {
            SpecforgeConfig config = await loader.LoadAsync(workingDirectory.Path, ct).ConfigureAwait(false);
            session.Initialize(config);
        }

        return session.Config!;
    }
}

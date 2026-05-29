namespace Specforge.Mcp.Hosting;

/// <summary>
/// The host-provided working directory config discovery walks up from (DEC-002). Injected so tools
/// can be exercised in-proc against a fixture directory without mutating process cwd.
/// </summary>
/// <param name="Path">Absolute directory to start <c>.specforge.json</c> discovery from.</param>
public sealed record SpecforgeWorkingDirectory(string Path);

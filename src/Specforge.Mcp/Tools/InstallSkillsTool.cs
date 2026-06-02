using System.Text.Json;

using Specforge.Core.Skills;

namespace Specforge.Mcp.Tools;

/// <summary>
/// <c>install_skills</c> (DEC-005 / DEC-007): install the embedded skill catalog to the user-wide
/// agent directories. Always-overwrite, idempotent. Write failures propagate as
/// <see cref="Specforge.Core.Exceptions.SpecforgeSkillInstallException"/> for the host to map.
/// </summary>
public sealed class InstallSkillsTool(ISkillInstaller installer) : IMcpTool
{
    private static readonly JsonElement Schema = JsonSerializer.Deserialize<JsonElement>(
        """
        {
          "type": "object",
          "description": "Install the embedded skill catalog to the user-wide agent skill directories. Always-overwrite reinstall; idempotent (DEC-005).",
          "properties": {
            "agent": {
              "type": "string",
              "enum": ["claude-code", "codex", "all"],
              "default": "all",
              "description": "Restrict installation to one agent. Default 'all' covers both Claude Code and Codex."
            },
            "dryRun": {
              "type": "boolean",
              "default": false,
              "description": "Report planned writes without touching the filesystem."
            }
          },
          "additionalProperties": false
        }
        """);

    public string Name => "install_skills";

    public JsonElement InputSchema => Schema;

    public async Task<ToolResult> InvokeAsync(JsonElement args, CancellationToken ct)
    {
        string agentValue = "all";
        if (args.ValueKind == JsonValueKind.Object && args.TryGetProperty("agent", out JsonElement agentElement))
        {
            if (agentElement.ValueKind != JsonValueKind.String)
            {
                return InvalidAgent(agentValue);
            }

            agentValue = agentElement.GetString()!;
        }

        if (!TryParseAgent(agentValue, out SkillInstallAgent agent))
        {
            return InvalidAgent(agentValue);
        }

        bool dryRun = args.ValueKind == JsonValueKind.Object
            && args.TryGetProperty("dryRun", out JsonElement dryRunElement)
            && dryRunElement.ValueKind == JsonValueKind.True;

        SkillInstallResult result = await installer.InstallAsync(agent, dryRun, ct).ConfigureAwait(false);
        return ToolResult.Ok(ToPayload(result));
    }

    /// <summary>Projects a <see cref="SkillInstallResult"/> to the camelCase JSON payload (reused by the error mapper).</summary>
    public static object ToPayload(SkillInstallResult result) => new
    {
        agents = result.Agents.ToDictionary(
            kv => kv.Key,
            kv => (object)new
            {
                writtenCount = kv.Value.WrittenCount,
                writtenPaths = kv.Value.WrittenPaths,
                overwrittenPaths = kv.Value.OverwrittenPaths,
            }),
        binaryVersion = result.BinaryVersion,
    };

    private static bool TryParseAgent(string value, out SkillInstallAgent agent)
    {
        switch (value)
        {
            case "claude-code": agent = SkillInstallAgent.ClaudeCode; return true;
            case "codex": agent = SkillInstallAgent.Codex; return true;
            case "all": agent = SkillInstallAgent.All; return true;
            default: agent = SkillInstallAgent.All; return false;
        }
    }

    private static ToolResult InvalidAgent(string given) =>
        ToolResult.Fail(Errors.EnvelopeRenderer.InvalidArgument("agent", given, "claude-code | codex | all", "correct the agent value"));
}

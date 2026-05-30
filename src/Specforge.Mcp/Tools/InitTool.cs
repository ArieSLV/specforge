using System.Text.Json;

using Specforge.Core.Init;
using Specforge.Mcp.Hosting;

namespace Specforge.Mcp.Tools;

/// <summary>
/// <c>init</c> (DEC-002/005/006/007/008): bootstrap or update a specforge workspace. Writes
/// <c>.specforge.json</c>, behavioral files (SPECFORGE.md + tagged blocks), and the Codex
/// <c>agents/openai.yaml</c>; <c>scaffold=true</c> also creates package skeletons and copies shared/templates.
/// </summary>
public sealed class InitTool(IInitService initService, SpecforgeWorkingDirectory workingDirectory) : IMcpTool
{
    private static readonly JsonElement Schema = JsonSerializer.Deserialize<JsonElement>(
        """
        {
          "type": "object",
          "description": "Bootstrap or update a specforge workspace. Writes .specforge.json, SPECFORGE.md, tagged blocks in CLAUDE.md/AGENTS.md, and agents/openai.yaml. scaffold=true also creates package skeletons and copies canonical shared/templates. Re-init fully replaces packages[] and upgrades the schema. (DEC-002/005/006/007/008)",
          "required": ["packages"],
          "properties": {
            "packages": {
              "type": "array",
              "minItems": 1,
              "description": "Packages to record in .specforge.json. Full replacement on re-init.",
              "items": {
                "type": "object",
                "required": ["name", "path"],
                "properties": {
                  "name": { "type": "string", "minLength": 1, "description": "Package name." },
                  "path": { "type": "string", "minLength": 1, "description": "Package directory, relative to the project root." },
                  "extraKinds": { "type": "array", "items": { "type": "string", "pattern": "^[A-Z]{2,6}$" }, "description": "Optional per-package extra ID kinds (DEC-004)." }
                },
                "additionalProperties": false
              }
            },
            "shared": { "type": "string", "default": "spec/shared", "description": "Repo-level shared docs directory." },
            "templates": { "type": "string", "default": "spec/templates", "description": "Repo-level templates directory." },
            "scaffold": { "type": "boolean", "default": false, "description": "Create package skeletons and copy embedded shared/templates." },
            "behavioralFiles": { "type": "string", "enum": ["all", "claude-only", "codex-only", "none"], "default": "all", "description": "Which behavioral files to write (DEC-008)." },
            "path": { "type": "string", "description": "Project root. Defaults to the host working directory." },
            "dryRun": { "type": "boolean", "default": false, "description": "Plan writes without touching the filesystem." }
          },
          "additionalProperties": false
        }
        """);

    public string Name => "init";

    public JsonElement InputSchema => Schema;

    public async Task<ToolResult> InvokeAsync(JsonElement args, CancellationToken ct)
    {
        if (args.ValueKind != JsonValueKind.Object)
        {
            return Invalid("(root)", "a JSON object");
        }

        if (!args.TryGetProperty("packages", out JsonElement packagesElement) || packagesElement.ValueKind != JsonValueKind.Array)
        {
            return Invalid("packages", "a non-empty array of {name, path, extraKinds?}");
        }

        List<InitPackageInput> packages = [];
        int index = 0;
        foreach (JsonElement package in packagesElement.EnumerateArray())
        {
            if (package.ValueKind != JsonValueKind.Object
                || !TryString(package, "name", out string name)
                || !TryString(package, "path", out string path))
            {
                return Invalid($"packages[{index}]", "an object with non-empty string 'name' and 'path'");
            }

            IReadOnlyList<string>? extraKinds = null;
            if (package.TryGetProperty("extraKinds", out JsonElement extraKindsElement) && extraKindsElement.ValueKind == JsonValueKind.Array)
            {
                extraKinds = [.. extraKindsElement.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()!)];
            }

            packages.Add(new InitPackageInput(name, path, extraKinds));
            index++;
        }

        if (packages.Count == 0)
        {
            return Invalid("packages", "a non-empty array of {name, path, extraKinds?}");
        }

        string shared = TryString(args, "shared", out string sharedValue) ? sharedValue : "spec/shared";
        string templates = TryString(args, "templates", out string templatesValue) ? templatesValue : "spec/templates";
        bool scaffold = args.TryGetProperty("scaffold", out JsonElement scaffoldElement) && scaffoldElement.ValueKind == JsonValueKind.True;
        bool dryRun = args.TryGetProperty("dryRun", out JsonElement dryRunElement) && dryRunElement.ValueKind == JsonValueKind.True;

        BehavioralFilesScope scope = BehavioralFilesScope.All;
        if (args.TryGetProperty("behavioralFiles", out JsonElement scopeElement))
        {
            if (scopeElement.ValueKind != JsonValueKind.String || !TryParseScope(scopeElement.GetString()!, out scope))
            {
                return Invalid("behavioralFiles", "all | claude-only | codex-only | none");
            }
        }

        string root = TryString(args, "path", out string pathValue) ? Path.GetFullPath(pathValue) : workingDirectory.Path;

        InitOptions options = new(root, packages, shared, templates, scaffold, scope, dryRun);
        InitResult result = await initService.RunAsync(options, ct).ConfigureAwait(false);
        return ToolResult.Ok(ToPayload(result));
    }

    private static object ToPayload(InitResult result) => new
    {
        dryRun = result.DryRun,
        writtenPaths = result.WrittenPaths,
        overwrittenPaths = result.OverwrittenPaths,
        skippedPaths = result.SkippedPaths,
        config = Section(result.Config),
        behavioral = Section(result.Behavioral),
        scaffold = Section(result.Scaffold),
    };

    private static object Section(InitSectionResult section) => new
    {
        writtenCount = section.WrittenCount,
        writtenPaths = section.WrittenPaths,
        overwrittenPaths = section.OverwrittenPaths,
        skippedPaths = section.SkippedPaths,
    };

    private static bool TryString(JsonElement obj, string name, out string value)
    {
        if (obj.TryGetProperty(name, out JsonElement element)
            && element.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(element.GetString()))
        {
            value = element.GetString()!;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static bool TryParseScope(string value, out BehavioralFilesScope scope)
    {
        switch (value)
        {
            case "all": scope = BehavioralFilesScope.All; return true;
            case "claude-only": scope = BehavioralFilesScope.ClaudeOnly; return true;
            case "codex-only": scope = BehavioralFilesScope.CodexOnly; return true;
            case "none": scope = BehavioralFilesScope.None; return true;
            default: scope = BehavioralFilesScope.All; return false;
        }
    }

    private static ToolResult Invalid(string argument, string expected) => ToolResult.Fail(new McpErrorEnvelope(
        "specforge.tool.invalid_argument",
        $"argument '{argument}' is invalid; expected {expected}.",
        "correct the argument value",
        JsonSerializer.SerializeToElement(new { argument, expected })));
}

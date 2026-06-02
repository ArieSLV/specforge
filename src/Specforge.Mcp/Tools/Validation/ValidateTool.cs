using System.Text.Json;

using Specforge.Core.Configuration;
using Specforge.Core.Validation;
using Specforge.Mcp.Hosting;

namespace Specforge.Mcp.Tools.Validation;

/// <summary><c>validate</c> (DEC-007): read-only spec-graph consistency check; returns findings-as-data (SUCCESS even when errors exist).</summary>
public sealed class ValidateTool(ConfigLoader loader, SessionState session, SpecforgeWorkingDirectory workingDirectory, IValidationService service)
    : SpecforgeToolBase(loader, session, workingDirectory)
{
    private static readonly JsonElement Schema = JsonSerializer.Deserialize<JsonElement>(
        """
        {
          "type": "object",
          "description": "Read-only consistency check over the active package's spec graph. Returns a structured findings list (severity error|warning|info); the envelope is SUCCESS even when errors are present (findings-as-data). Gate downstream work on data.errorCount == 0. No side effects.",
          "required": ["aspect"],
          "properties": {
            "aspect": {
              "type": "string",
              "enum": ["ids", "links", "lifecycle", "impact-coverage", "all"],
              "description": "Which consistency family to check. 'all' runs every aspect in order and never short-circuits."
            }
          },
          "additionalProperties": false
        }
        """);

    public override string Name => "validate";

    public override JsonElement InputSchema => Schema;

    public override async Task<ToolResult> InvokeAsync(JsonElement args, CancellationToken ct)
    {
        if (!TryGetString(args, "aspect", out string aspect) || !ValidationAspect.Accepted.Contains(aspect, StringComparer.Ordinal))
        {
            return ToolResult.Fail(Errors.EnvelopeRenderer.InvalidArgument("aspect", GetOptionalString(args, "aspect"), ValidationAspect.Accepted, "choose one of the listed aspects"));
        }

        await EnsureLoadedAsync(ct).ConfigureAwait(false);
        ValidationResult result = await service.ValidateAsync(aspect, ct).ConfigureAwait(false);

        int total = result.ErrorCount + result.WarningCount + result.InfoCount;
        string message = total == 0
            ? "clean"
            : $"{result.ErrorCount} error(s), {result.WarningCount} warning(s), {result.InfoCount} info";

        return ToolResult.Ok(new
        {
            aspect = result.AspectRequested,
            errorCount = result.ErrorCount,
            warningCount = result.WarningCount,
            infoCount = result.InfoCount,
            message,
            findings = result.Findings.Select(f => new
            {
                aspect = f.Aspect,
                severity = f.Severity,
                target = f.Target,
                message = f.Message,
                suggestion = f.Suggestion,
            }),
        });
    }
}

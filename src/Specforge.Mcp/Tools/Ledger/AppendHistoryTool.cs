using System.Text.Json;

using Specforge.Core.Configuration;
using Specforge.Core.Identifiers;
using Specforge.Core.Ledger;
using Specforge.Mcp.Hosting;

namespace Specforge.Mcp.Tools.Ledger;

/// <summary><c>append_history</c> (DEC-007): append one event row to <c>history.md</c> (LedgerId or literal target).</summary>
public sealed class AppendHistoryTool(ConfigLoader loader, SessionState session, SpecforgeWorkingDirectory workingDirectory, IHistoryLedgerService history)
    : SpecforgeToolBase(loader, session, workingDirectory)
{
    private static readonly JsonElement Schema = JsonSerializer.Deserialize<JsonElement>(
        """
        {
          "type": "object",
          "description": "Append a single event row to the active package's ledger/history.md. The target is either a LedgerId (e.g., ART-DEC-001) or one of the literal project-wide targets (milestone), (multiple). History is append-only; there is no delete_history.",
          "required": ["target", "event", "detail"],
          "properties": {
            "target": { "type": "string", "description": "A LedgerId form (per DEC-004) or a literal target.", "examples": ["ART-DEC-001", "(milestone)", "(multiple)"] },
            "event": { "type": "string", "minLength": 1, "maxLength": 80, "description": "Short single-line event name.", "examples": ["Milestone reached", "Reviewed"] },
            "detail": { "type": "string", "minLength": 1, "description": "Free-form detail of the event." },
            "dryRun": { "type": "boolean", "default": false, "description": "Return the planned write without touching the filesystem." }
          },
          "additionalProperties": false
        }
        """);

    public override string Name => "append_history";

    public override JsonElement InputSchema => Schema;

    public override async Task<ToolResult> InvokeAsync(JsonElement args, CancellationToken ct)
    {
        if (!TryGetString(args, "target", out string target))
        {
            return InvalidArgument("target", "a LedgerId form (e.g., ART-DEC-001) or one of the literal targets (milestone), (multiple)");
        }

        if (!TryGetString(args, "event", out string eventName))
        {
            return InvalidArgument("event", "a non-empty string");
        }

        if (eventName.Length > 80 || eventName.AsSpan().IndexOfAny('\n', '\r') >= 0)
        {
            return InvalidArgument("event", "a single-line string of at most 80 characters");
        }

        if (!TryGetString(args, "detail", out string detail))
        {
            return InvalidArgument("detail", "a non-empty string");
        }

        bool isLiteral = HistoryLiteralTarget.IsLiteralTarget(target);
        if (!isLiteral && !IdParser.TryParse(target, out _))
        {
            return InvalidArgument("target", "use a LedgerId form (e.g., ART-DEC-001) or one of the literal targets (milestone), (multiple)");
        }

        await EnsureLoadedAsync(ct).ConfigureAwait(false);
        bool dryRun = GetBool(args, "dryRun");
        if (!dryRun)
        {
            if (isLiteral)
            {
                await history.AppendLiteralAsync(target, eventName, detail, ct).ConfigureAwait(false);
            }
            else
            {
                await history.AppendAsync(target, eventName, detail, ct).ConfigureAwait(false);
            }
        }

        return ToolResult.Ok(new
        {
            target,
            @event = eventName,
            detail,
            dryRun,
            plannedWrites = new[] { $"{LedgerPaths.LedgerFile(Session, "history.md")} (+{eventName})" },
        });
    }
}

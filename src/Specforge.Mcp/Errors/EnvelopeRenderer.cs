using System.Text.Json;

using Microsoft.Extensions.Logging;

using Specforge.Core.Diagnostics;
using Specforge.Mcp.Tools;

namespace Specforge.Mcp.Errors;

/// <summary>
/// Default <see cref="IEnvelopeRenderer"/> (ITEM-011): the one place envelopes are built. Known typed
/// exceptions render via <see cref="ToolExceptionMapper"/>; any other exception becomes
/// <c>tool.internal_error</c> with a fresh <c>correlationId</c> that is also written to stderr (DEC-003),
/// so a human can cross-reference the AI-visible failure with the server log line.
/// </summary>
public sealed class EnvelopeRenderer(ToolExceptionMapper mapper, ILogger<EnvelopeRenderer> logger) : IEnvelopeRenderer
{
    public McpErrorEnvelope Render(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        MappedError? mapped = mapper.Map(exception);
        if (mapped is not null)
        {
            object? data = mapped.BuildData(exception);
            return new McpErrorEnvelope(mapped.Code, exception.Message, mapped.Suggestion, data is null ? null : JsonSerializer.SerializeToElement(data));
        }

        string correlationId = Guid.NewGuid().ToString("n");
        logger.LogError(exception, "Unhandled exception in tool invocation. correlationId={CorrelationId}", correlationId);
        return new McpErrorEnvelope(
            SpecforgeErrorCode.ToolInternalError,
            "an internal error occurred while handling the tool call",
            "check the specforge stderr log entry tagged with the correlation id",
            JsonSerializer.SerializeToElement(new { correlationId }));
    }

    /// <summary>
    /// Builds the <c>tool.invalid_argument</c> envelope (the only code Core never raises directly).
    /// Static — no correlation/log state is needed for an expected, AI-visible validation failure.
    /// </summary>
    public static McpErrorEnvelope InvalidArgument(string argument, object? given, object expected, string? suggestion = null) =>
        new(SpecforgeErrorCode.ToolInvalidArgument,
            $"argument '{argument}' is invalid; expected {Describe(expected)}.",
            suggestion ?? "correct the argument value",
            JsonSerializer.SerializeToElement(new { argument, given, expected }));

    private static string Describe(object expected) => expected as string ?? JsonSerializer.Serialize(expected);
}

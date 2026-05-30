using System.Text.Json;

using Microsoft.Extensions.Logging;

using Specforge.Core.Configuration;
using Specforge.Core.Exceptions;
using Specforge.Core.Skills;

namespace Specforge.Mcp.Tools;

/// <summary>
/// Maps Core's typed exceptions to MCP error envelopes (DEC-007 catalog). ITEM-002 covers the five
/// codes its scope produces plus the generic <c>specforge.tool.internal_error</c> catch-all; ITEM-011
/// centralizes the full table.
/// </summary>
public sealed class ToolExceptionMapper(ILogger<ToolExceptionMapper> logger)
{
    /// <summary>Converts <paramref name="exception"/> to the envelope to surface to the caller.</summary>
    public McpErrorEnvelope Map(Exception exception)
    {
        switch (exception)
        {
            case SpecforgeConfigNotFoundException e:
                return new McpErrorEnvelope(
                    e.ErrorCode, e.Message,
                    "run init to generate .specforge.json at the project root",
                    Data(new { searchedPath = e.SearchedPath }));

            case SpecforgeSchemaVersionException e:
                return new McpErrorEnvelope(
                    e.ErrorCode, e.Message, e.Suggestion,
                    Data(new { path = e.Path, claimedVersion = e.ClaimedVersion, supportedRange = e.SupportedRange }));

            case SpecforgeConfigValidationException e:
                return new McpErrorEnvelope(
                    e.ErrorCode, e.Message,
                    "fix the listed fields in .specforge.json",
                    Data(new { path = e.Path, errors = e.Errors.Select(x => new { pointer = x.Pointer, message = x.Message }) }));

            case SpecforgePackageNotSelectedException e:
                return new McpErrorEnvelope(
                    e.ErrorCode, e.Message,
                    "call use_package with one of the listed names",
                    Data(new { availablePackages = e.AvailablePackages.Select(ToPackageData) }));

            case SpecforgeUnknownPackageException e:
                return new McpErrorEnvelope(
                    e.ErrorCode, e.Message,
                    "call use_package with one of the listed names",
                    Data(new { requestedName = e.RequestedName, availablePackages = e.AvailablePackages.Select(ToPackageData) }));

            case SpecforgeEmbeddedSkillNotFoundException e:
                return new McpErrorEnvelope(
                    e.ErrorCode, e.Message,
                    "rebuild specforge — the binary is missing embedded skill resources",
                    Data(new { resourceName = e.ResourceName }));

            case SpecforgeSkillInstallException e:
                return new McpErrorEnvelope(
                    e.ErrorCode, e.Message,
                    "check filesystem permissions on the target directory",
                    Data(new
                    {
                        agent = e.Agent,
                        path = e.Path,
                        innerType = e.InnerExceptionType.Name,
                        innerMessage = e.InnerMessage,
                        partialResult = InstallSkillsTool.ToPayload(e.PartialResult),
                    }));

            case SpecforgeInvalidArgumentException e:
                return new McpErrorEnvelope(
                    e.ErrorCode, e.Message, e.Suggestion,
                    Data(new { argument = e.Argument, given = e.Given, expected = e.Expected }));

            case SpecforgeInvalidIdentifierException e:
                return new McpErrorEnvelope(
                    e.ErrorCode, e.Message,
                    "correct the identifier to match one of the expected patterns",
                    Data(new { given = e.Given, expectedPatterns = e.ExpectedPatterns }));

            case SpecforgeReservedKindException e:
                return new McpErrorEnvelope(
                    e.ErrorCode, e.Message,
                    "choose a non-reserved kind for extraKinds",
                    Data(new { given = e.Given, reservedKinds = e.ReservedKinds }));

            case SpecforgeKindExhaustedException e:
                return new McpErrorEnvelope(
                    e.ErrorCode, e.Message,
                    "split the package or otherwise reorganize — 999 IDs of one kind reached",
                    Data(new { kind = e.Kind, package = e.Package }));

            default:
                string correlationId = Guid.NewGuid().ToString("n");
                logger.LogError(exception, "Unhandled exception in tool invocation. correlationId={CorrelationId}", correlationId);
                return new McpErrorEnvelope(
                    "specforge.tool.internal_error",
                    "an internal error occurred while handling the tool call",
                    "check the specforge stderr log entry tagged with the correlation id",
                    Data(new { correlationId }));
        }
    }

    private static object ToPackageData(SpecforgePackageConfig package) => new { name = package.Name, path = package.Path };

    private static JsonElement Data(object value) => JsonSerializer.SerializeToElement(value);
}

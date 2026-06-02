namespace Specforge.Mcp.Errors;

/// <summary>
/// The mapping of a known typed exception to its envelope code, suggestion, and structured data
/// (ITEM-011). <see cref="BuildData"/> receives the originating exception and returns its <c>data</c> object.
/// </summary>
public sealed record MappedError(string Code, string? Suggestion, Func<Exception, object?> BuildData);

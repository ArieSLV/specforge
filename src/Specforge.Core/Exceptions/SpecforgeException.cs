namespace Specforge.Core.Exceptions;

/// <summary>
/// Base type for every documented specforge failure mode (DEC-003 §"Error Handling").
/// Carries a stable, dotted <see cref="ErrorCode"/> that the MCP layer dispatches on
/// (DEC-007 §"Error Envelope") — independent of the C# exception type name.
/// </summary>
public abstract class SpecforgeException : Exception
{
    protected SpecforgeException(string errorCode, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    /// <summary>Stable dotted code, e.g. <c>specforge.config.not_found</c>.</summary>
    public string ErrorCode { get; }
}

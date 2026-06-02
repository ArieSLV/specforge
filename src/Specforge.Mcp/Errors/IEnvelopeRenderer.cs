using Specforge.Mcp.Tools;

namespace Specforge.Mcp.Errors;

/// <summary>The single point that turns an exception into the failure envelope (ITEM-011).</summary>
public interface IEnvelopeRenderer
{
    /// <summary>Renders <paramref name="exception"/> to its envelope; unmapped types become <c>tool.internal_error</c>.</summary>
    McpErrorEnvelope Render(Exception exception);
}

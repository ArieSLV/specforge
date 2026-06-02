using Specforge.Core.Diagnostics;
using Specforge.Mcp.Errors;
using Specforge.Tests.TestSupport;

using Xunit;

namespace Specforge.Tests.Errors;

/// <summary>
/// Every catalog code must have a production path (ITEM-011): either a typed-exception mapper arm or
/// the two directly-emitted codes (<c>tool.invalid_argument</c> / <c>tool.internal_error</c>).
/// </summary>
public class OrphanCodeTests
{
    private static IReadOnlySet<string> ProducibleCodes()
    {
        ToolExceptionMapper mapper = new();
        HashSet<string> produced = [.. ExceptionFixtures.All.Select(f => mapper.Map(f.Exception)!.Code)];
        produced.Add(SpecforgeErrorCode.ToolInvalidArgument); // EnvelopeRenderer.InvalidArgument
        produced.Add(SpecforgeErrorCode.ToolInternalError);   // EnvelopeRenderer catch-all
        return produced;
    }

    [Fact]
    public void EveryCode_HasAProductionPath()
    {
        IReadOnlySet<string> producible = ProducibleCodes();
        foreach (string code in SpecforgeErrorCode.AllCodes)
        {
            Assert.True(producible.Contains(code), $"orphan code with no production path: {code}");
        }
    }

    [Fact]
    public void NoProducedCode_IsAbsentFromTheCatalog()
    {
        foreach (string code in ProducibleCodes())
        {
            Assert.Contains(code, SpecforgeErrorCode.AllCodes);
        }
    }
}

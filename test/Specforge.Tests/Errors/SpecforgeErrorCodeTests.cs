using System.Text.RegularExpressions;

using Specforge.Core.Diagnostics;
using Specforge.Core.Exceptions;

using Xunit;

namespace Specforge.Tests.Errors;

public class SpecforgeErrorCodeTests
{
    [Fact]
    public void AllCodes_ContainsExactlyFourteen()
    {
        Assert.Equal(14, SpecforgeErrorCode.AllCodes.Count);
    }

    [Fact]
    public void EveryCode_MatchesTheNamespacePattern()
    {
        Regex pattern = new("^specforge\\.[a-z_]+\\.[a-z_]+$");
        foreach (string code in SpecforgeErrorCode.AllCodes)
        {
            Assert.False(string.IsNullOrWhiteSpace(code));
            Assert.Matches(pattern, code);
        }
    }

    [Fact]
    public void IdNotFound_IsPresentAndTyped()
    {
        Assert.Contains(SpecforgeErrorCode.IdNotFound, SpecforgeErrorCode.AllCodes);

        SpecforgeIdNotFoundException ex = new("REV-DEC-001-002");
        Assert.Equal(SpecforgeErrorCode.IdNotFound, ex.ErrorCode);
        Assert.Equal("REV-DEC-001-002", ex.Id);
    }
}

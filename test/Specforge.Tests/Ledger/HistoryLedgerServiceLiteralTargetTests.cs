using Specforge.Core.Exceptions;
using Specforge.Core.Ledger;
using Specforge.Tests.TestSupport;

using Xunit;

namespace Specforge.Tests.Ledger;

public class HistoryLedgerServiceLiteralTargetTests
{
    [Theory]
    [InlineData("(milestone)")]
    [InlineData("(multiple)")]
    public async Task AppendLiteral_AcceptedLiterals_WritesRow(string literal)
    {
        using PackageWorkspace ws = new();
        HistoryLedgerService history = new(ws.Session);

        await history.AppendLiteralAsync(literal, "Stage milestone", "all items approved", CancellationToken.None);

        string text = await ws.ReadLedgerAsync("history.md");
        Assert.Contains(literal, text, StringComparison.Ordinal);
        Assert.Contains("Stage milestone", text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("(security)")]
    [InlineData("milestone")]
    [InlineData("ART-DEC-001")]
    [InlineData("")]
    public async Task AppendLiteral_NonLiteralString_Throws(string notLiteral)
    {
        using PackageWorkspace ws = new();
        HistoryLedgerService history = new(ws.Session);

        await Assert.ThrowsAsync<SpecforgeInvalidIdentifierException>(
            () => history.AppendLiteralAsync(notLiteral, "Event", "detail", CancellationToken.None));
    }

    [Fact]
    public async Task AppendAsync_LedgerIdTarget_StillWritesAnyString()
    {
        using PackageWorkspace ws = new();
        HistoryLedgerService history = new(ws.Session);

        await history.AppendAsync("ART-DEC-001", "Created", "create_decision: X", CancellationToken.None);

        Assert.Contains("ART-DEC-001", await ws.ReadLedgerAsync("history.md"), StringComparison.Ordinal);
    }
}

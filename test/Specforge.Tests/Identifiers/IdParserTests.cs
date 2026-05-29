using Specforge.Core.Identifiers;

using Xunit;

namespace Specforge.Tests.Identifiers;

public class IdParserTests
{
    [Theory]
    [InlineData("DEC-001", "DEC", 1)]
    [InlineData("ITEM-013", "ITEM", 13)]
    [InlineData("ABCDEF-999", "ABCDEF", 999)]
    [InlineData("CMT-001", "CMT", 1)]
    public void Parses_Atomic(string input, string kind, int number)
    {
        Assert.True(IdParser.TryParse(input, out ParsedIdentifier? id));
        AtomicIdentifier atomic = Assert.IsType<AtomicIdentifier>(id);
        Assert.Equal(kind, atomic.Kind);
        Assert.Equal(number, atomic.Number);
    }

    [Fact]
    public void Parses_CompositeReview()
    {
        Assert.True(IdParser.TryParse("REV-DEC-001-002", out ParsedIdentifier? id));
        CompositeReviewIdentifier review = Assert.IsType<CompositeReviewIdentifier>(id);
        Assert.Equal("DEC", review.Target.Kind);
        Assert.Equal(1, review.Target.Number);
        Assert.Equal(2, review.Sequence);
    }

    [Theory]
    [InlineData("ART-WORK-PLAN", "WORK-PLAN")]
    [InlineData("ART-GLOSSARY", "GLOSSARY")]
    [InlineData("ART-DEC-001", "DEC-001")]
    public void Parses_DescriptiveArt(string input, string slug)
    {
        Assert.True(IdParser.TryParse(input, out ParsedIdentifier? id));
        DescriptiveArtIdentifier art = Assert.IsType<DescriptiveArtIdentifier>(id);
        Assert.Equal(slug, art.Slug);
    }

    [Fact]
    public void Parses_Qualified()
    {
        Assert.True(IdParser.TryParse("specforge-mvp/DEC-001", out ParsedIdentifier? id));
        QualifiedIdentifier qualified = Assert.IsType<QualifiedIdentifier>(id);
        Assert.Equal("specforge-mvp", qualified.Package);
        AtomicIdentifier inner = Assert.IsType<AtomicIdentifier>(qualified.Inner);
        Assert.Equal("DEC", inner.Kind);
        Assert.Equal(1, inner.Number);
    }

    [Theory]
    [InlineData("DEC-1")]
    [InlineData("DEC-01")]
    [InlineData("dec-001")]
    [InlineData("DEC-0001")]
    [InlineData("DEC-001-")]
    [InlineData("--DEC-001")]
    [InlineData("D-001")]
    [InlineData("ABCDEFG-001")]
    [InlineData("")]
    [InlineData("pkg/sub/DEC-001")]
    [InlineData("/DEC-001")]
    public void Rejects_Invalid(string input)
    {
        Assert.False(IdParser.TryParse(input, out ParsedIdentifier? id));
        Assert.Null(id);
    }

    [Theory]
    [InlineData("ART-DEC-001")]
    [InlineData("ART-DEC-008")]
    [InlineData("ART-ITEM-001")]
    [InlineData("ART-ITEM-013")]
    [InlineData("ART-WORK-PLAN")]
    [InlineData("ART-GLOSSARY")]
    [InlineData("ART-LEDGER-COMMITS")]
    [InlineData("DEC-001")]
    [InlineData("ITEM-002")]
    [InlineData("CMT-001")]
    [InlineData("REV-DEC-001-001")]
    public void Dogfood_RepoIdentifiers_Parse(string input)
    {
        // Every identifier kind present in this repo's ledger files must parse (DEC-004 dogfood).
        Assert.True(IdParser.TryParse(input, out _));
    }
}

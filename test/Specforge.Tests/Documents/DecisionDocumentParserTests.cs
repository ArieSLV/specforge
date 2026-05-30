using Specforge.Core.Documents;

using Xunit;

namespace Specforge.Tests.Documents;

public class DecisionDocumentParserTests
{
    private const string Sample =
        "# DEC-001-DISTRIBUTION-AND-TRANSPORT - Distribution and Transport\n\n" +
        "Status: Approved\nDate: 2026-05-25\nOwner: AI assistant (drafted)\nReview owner: User\nSupersedes: none\n\n" +
        "## Context\n\nThe pressure that drove this.\n\n" +
        "## Decision\n\nWe chose X.\n\n```text\n## Not a heading inside a fence\n```\n\n" +
        "## Consequences\n\nFollow-on effects.\n";

    [Fact]
    public void Parse_ExtractsMetadata()
    {
        DecisionDocument doc = DecisionDocumentParser.Parse(Sample, "/x/DEC-001-DISTRIBUTION-AND-TRANSPORT.md");

        Assert.Equal("DEC-001", doc.Id);
        Assert.Equal("Distribution and Transport", doc.Title);
        Assert.Equal("Approved", doc.Status);
        Assert.Equal("2026-05-25", doc.Date);
        Assert.Equal("User", doc.ReviewOwner);
        Assert.Equal("none", doc.Supersedes);
    }

    [Fact]
    public void Parse_ExtractsSections()
    {
        DecisionDocument doc = DecisionDocumentParser.Parse(Sample, "/x/DEC-001.md");

        Assert.Contains("Context", doc.Sections.Keys);
        Assert.Contains("Decision", doc.Sections.Keys);
        Assert.Contains("Consequences", doc.Sections.Keys);
        Assert.Contains("We chose X.", doc.Sections["Decision"], StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_IgnoresHeadingsInsideFencedCode()
    {
        DecisionDocument doc = DecisionDocumentParser.Parse(Sample, "/x/DEC-001.md");
        Assert.DoesNotContain("Not a heading inside a fence", doc.Sections.Keys);
    }

    [Fact]
    public void Parse_MissingHeading_Throws() =>
        Assert.Throws<FormatException>(() => DecisionDocumentParser.Parse("no heading here\n", "/x/bad.md"));
}

using Specforge.Core.Exceptions;
using Specforge.Core.Skills;

using Xunit;

namespace Specforge.Tests.Skills;

public class SkillFrontmatterParserTests
{
    [Fact]
    public void Valid_ParsesNameDescriptionAndExtras()
    {
        const string md = "---\nname: draft-decision\ndescription: Draft a decision record.\nkeywords: [spec, decision]\n---\n\nBody.";

        SkillFrontmatter frontmatter = SkillFrontmatterParser.Parse(md);

        Assert.Equal("draft-decision", frontmatter.Name);
        Assert.Equal("Draft a decision record.", frontmatter.Description);
        Assert.True(frontmatter.Extras.ContainsKey("keywords"));
        Assert.False(frontmatter.Extras.ContainsKey("name"));
    }

    [Fact]
    public void MissingOpeningDelimiter_Throws() =>
        Assert.Throws<SpecforgeEmbeddedSkillNotFoundException>(
            () => SkillFrontmatterParser.Parse("name: x\ndescription: y\n"));

    [Fact]
    public void MissingClosingDelimiter_Throws() =>
        Assert.Throws<SpecforgeEmbeddedSkillNotFoundException>(
            () => SkillFrontmatterParser.Parse("---\nname: x\ndescription: y\n"));

    [Fact]
    public void MalformedYaml_Throws() =>
        Assert.Throws<SpecforgeEmbeddedSkillNotFoundException>(
            () => SkillFrontmatterParser.Parse("---\nname: [unterminated\n---\n"));

    [Fact]
    public void MissingName_Throws() =>
        Assert.Throws<SpecforgeEmbeddedSkillNotFoundException>(
            () => SkillFrontmatterParser.Parse("---\ndescription: y\n---\n"));

    [Fact]
    public void MissingDescription_Throws() =>
        Assert.Throws<SpecforgeEmbeddedSkillNotFoundException>(
            () => SkillFrontmatterParser.Parse("---\nname: x\n---\n"));

    [Fact]
    public void DescriptionExactly200_Passes()
    {
        string description = new('x', 200);
        SkillFrontmatter frontmatter = SkillFrontmatterParser.Parse($"---\nname: s\ndescription: {description}\n---\n");
        Assert.Equal(200, frontmatter.Description.Length);
    }

    [Fact]
    public void Description201_Throws()
    {
        string description = new('x', 201);
        Assert.Throws<SpecforgeEmbeddedSkillNotFoundException>(
            () => SkillFrontmatterParser.Parse($"---\nname: s\ndescription: {description}\n---\n"));
    }
}

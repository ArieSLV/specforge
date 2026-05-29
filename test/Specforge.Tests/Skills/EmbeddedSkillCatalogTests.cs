using System.Reflection;

using Specforge.Core.Exceptions;
using Specforge.Core.Skills;

using Xunit;

namespace Specforge.Tests.Skills;

public class EmbeddedSkillCatalogTests
{
    private static Assembly TestAssembly => typeof(EmbeddedSkillCatalogTests).Assembly;

    [Fact]
    public void ValidPrefix_ReturnsSkill()
    {
        EmbeddedSkillCatalog catalog = new(TestAssembly, "test.skills.valid/");

        EmbeddedSkill skill = Assert.Single(catalog.GetSkills());

        Assert.Equal("valid-skill", skill.Name);
        Assert.Equal("A valid test skill used by the embedded-catalog tests.", skill.Description);
        Assert.True(skill.ExtraFrontmatter.ContainsKey("keywords"));
    }

    [Fact]
    public void EmptyPrefix_ReturnsEmpty()
    {
        EmbeddedSkillCatalog catalog = new(TestAssembly, "test.skills.empty/");
        Assert.Empty(catalog.GetSkills());
    }

    [Fact]
    public void BackslashLogicalName_IsNormalized()
    {
        // Guards the %(RecursiveDir) backslash bug: a logical name with a '\' separator must still
        // resolve to a skill (the production glob produces such names on Windows).
        EmbeddedSkillCatalog catalog = new(TestAssembly, "test.skills.winpath/");
        EmbeddedSkill skill = Assert.Single(catalog.GetSkills());
        Assert.Equal("winpath", skill.Name);
    }

    [Fact]
    public void BrokenSkill_Throws()
    {
        EmbeddedSkillCatalog catalog = new(TestAssembly, "test.skills.broken/");
        Assert.Throws<SpecforgeEmbeddedSkillNotFoundException>(() => catalog.GetSkills());
    }

    [Fact]
    public void NameMismatch_Throws()
    {
        EmbeddedSkillCatalog catalog = new(TestAssembly, "test.skills.mismatch/");
        SpecforgeEmbeddedSkillNotFoundException ex = Assert.Throws<SpecforgeEmbeddedSkillNotFoundException>(() => catalog.GetSkills());
        Assert.Contains("name-mismatch", ex.ResourceName, StringComparison.Ordinal);
    }
}

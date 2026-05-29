using System.Reflection;

using Specforge.Core.Exceptions;
using Specforge.Core.Skills;

using Xunit;

namespace Specforge.Tests.Skills;

public class SkillCatalogValidatorTests
{
    private static Assembly TestAssembly => typeof(SkillCatalogValidatorTests).Assembly;

    private static SkillCatalogValidator ValidatorFor(string prefix) =>
        new(new EmbeddedSkillCatalog(TestAssembly, prefix));

    [Fact]
    public void ValidCatalog_ReturnsCount() =>
        Assert.Equal(1, ValidatorFor("test.skills.valid/").Validate());

    [Fact]
    public void EmptyCatalog_ReturnsZero() =>
        Assert.Equal(0, ValidatorFor("test.skills.empty/").Validate());

    [Fact]
    public void BrokenCatalog_Throws()
    {
        SpecforgeEmbeddedSkillNotFoundException ex = Assert.Throws<SpecforgeEmbeddedSkillNotFoundException>(
            () => ValidatorFor("test.skills.broken/").Validate());
        Assert.False(string.IsNullOrEmpty(ex.ResourceName));
    }
}

using Specforge.Core.Exceptions;
using Specforge.Core.Identifiers;

using Xunit;

namespace Specforge.Tests.Identifiers;

public class TitleSlugTests
{
    [Theory]
    [InlineData("AB")]
    [InlineData("DISTRIBUTION-AND-TRANSPORT")]
    [InlineData("DEC-001")]
    [InlineData("A1-B2")]
    public void Validate_AcceptsValidSlugs(string slug) => TitleSlug.Validate(slug);

    [Theory]
    [InlineData("lower")]
    [InlineData("A--B")]
    [InlineData("-AB")]
    [InlineData("AB-")]
    [InlineData("A")]
    [InlineData("A B")]
    public void Validate_RejectsInvalidSlugs(string slug) =>
        Assert.Throws<SpecforgeInvalidIdentifierException>(() => TitleSlug.Validate(slug));

    [Fact]
    public void Validate_RejectsOver60Chars()
    {
        string slug = new('A', 61);
        Assert.Throws<SpecforgeInvalidIdentifierException>(() => TitleSlug.Validate(slug));
    }

    [Theory]
    [InlineData("Distribution and Transport", "DISTRIBUTION-AND-TRANSPORT")]
    [InlineData("Hello, World!", "HELLO-WORLD")]
    [InlineData("  spaced  out  ", "SPACED-OUT")]
    public void FromTitle_Normalizes(string title, string expected) =>
        Assert.Equal(expected, TitleSlug.FromTitle(title));

    [Fact]
    public void FromTitle_TruncatesToMaxLength()
    {
        string title = new('a', 80);
        string slug = TitleSlug.FromTitle(title);
        Assert.True(slug.Length <= TitleSlug.MaxLength);
    }

    [Theory]
    [InlineData("!!!")]
    [InlineData("   ")]
    public void FromTitle_RejectsEmptyResult(string title) =>
        Assert.Throws<SpecforgeInvalidIdentifierException>(() => TitleSlug.FromTitle(title));
}

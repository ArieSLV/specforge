using Specforge.Core.Exceptions;
using Specforge.Core.Init;

using Xunit;

namespace Specforge.Tests.Init;

public class TaggedBlockMergerTests
{
    private static readonly TaggedBlockMerger Merger = new();
    private const string Body = "BLOCK BODY";

    [Fact]
    public void Merge_NoFile_CreatesBlockOnly()
    {
        string result = Merger.Merge(null, Body, "CLAUDE.md");
        Assert.StartsWith(TaggedBlockMerger.StartDelimiter, result, StringComparison.Ordinal);
        Assert.Contains(Body, result, StringComparison.Ordinal);
        Assert.Contains(TaggedBlockMerger.EndDelimiter, result, StringComparison.Ordinal);
    }

    [Fact]
    public void Merge_NoBlock_PrependsAndPreservesUserContent()
    {
        string result = Merger.Merge("# User guidance\nhello\n", Body, "CLAUDE.md");
        Assert.StartsWith(TaggedBlockMerger.StartDelimiter, result, StringComparison.Ordinal);
        Assert.Contains("# User guidance", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Merge_ExistingBlock_ReplacesAndPreservesSurrounding()
    {
        string existing = $"# Top\n{TaggedBlockMerger.StartDelimiter}\nOLD\n{TaggedBlockMerger.EndDelimiter}\n# Bottom\n";
        string result = Merger.Merge(existing, Body, "CLAUDE.md");

        Assert.Contains("# Top", result, StringComparison.Ordinal);
        Assert.Contains("# Bottom", result, StringComparison.Ordinal);
        Assert.Contains(Body, result, StringComparison.Ordinal);
        Assert.DoesNotContain("OLD", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Merge_IsIdempotent()
    {
        string first = Merger.Merge("# Top\n", Body, "CLAUDE.md");
        string second = Merger.Merge(first, Body, "CLAUDE.md");
        Assert.Equal(first, second);
    }

    [Fact]
    public void Merge_ImbalancedBlock_Throws()
    {
        string existing = $"{TaggedBlockMerger.StartDelimiter}\nno end delimiter here\n";
        SpecforgeInvalidArgumentException ex = Assert.Throws<SpecforgeInvalidArgumentException>(
            () => Merger.Merge(existing, Body, "CLAUDE.md"));
        Assert.Equal("CLAUDE.md", ex.Argument);
    }
}

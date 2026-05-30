using Specforge.Core.Configuration;
using Specforge.Core.Init;

using Xunit;

namespace Specforge.Tests.Init;

public class EmbeddedTemplateCatalogTests
{
    // Uses the production Core assembly resources (spec/shared/** and spec/templates/** embedded by ITEM-006).
    private static readonly EmbeddedTemplateCatalog Catalog = new(typeof(SpecforgeConfig).Assembly);

    [Fact]
    public void GetShared_EnumeratesSharedDocs()
    {
        IReadOnlyList<EmbeddedTemplate> shared = Catalog.GetShared();
        Assert.NotEmpty(shared);
        Assert.Contains(shared, t => t.RelativePath.Contains("glossary.md", StringComparison.Ordinal));
    }

    [Fact]
    public void GetTemplates_EnumeratesTemplates()
    {
        IReadOnlyList<EmbeddedTemplate> templates = Catalog.GetTemplates();
        Assert.NotEmpty(templates);
        Assert.Contains(templates, t => t.RelativePath.Contains("item_spec.md", StringComparison.Ordinal));
    }

    [Fact]
    public void GetShared_StreamsAreReadable()
    {
        EmbeddedTemplate template = Catalog.GetShared()[0];
        using Stream stream = template.OpenStream();
        using StreamReader reader = new(stream);
        Assert.NotEmpty(reader.ReadToEnd());
    }
}

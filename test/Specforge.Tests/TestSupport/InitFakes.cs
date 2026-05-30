using System.Text;

using Specforge.Core.Init;

namespace Specforge.Tests.TestSupport;

/// <summary>In-memory template catalog double for scaffold / init tests.</summary>
public sealed class FakeTemplateCatalog(IReadOnlyList<EmbeddedTemplate> shared, IReadOnlyList<EmbeddedTemplate> templates) : IEmbeddedTemplateCatalog
{
    public static FakeTemplateCatalog Empty { get; } = new([], []);

    public IReadOnlyList<EmbeddedTemplate> GetShared() => shared;

    public IReadOnlyList<EmbeddedTemplate> GetTemplates() => templates;

    public static EmbeddedTemplate Template(string prefix, string relativePath, string content) =>
        new($"{prefix}{relativePath}", relativePath, () => new MemoryStream(Encoding.UTF8.GetBytes(content)));
}

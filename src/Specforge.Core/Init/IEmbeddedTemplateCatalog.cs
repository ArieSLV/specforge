namespace Specforge.Core.Init;

/// <summary>
/// Enumerates the embedded canonical shared-docs (<c>specforge.shared/**</c>) and templates
/// (<c>specforge.templates/**</c>) that scaffold mode copies into a fresh project (DEC-008 / ITEM-006).
/// </summary>
public interface IEmbeddedTemplateCatalog
{
    /// <summary>Embedded <c>spec/shared/**</c> resources.</summary>
    IReadOnlyList<EmbeddedTemplate> GetShared();

    /// <summary>Embedded <c>spec/templates/**</c> resources.</summary>
    IReadOnlyList<EmbeddedTemplate> GetTemplates();
}

using System.Reflection;

namespace Specforge.Core.Init;

/// <summary>Default <see cref="IEmbeddedTemplateCatalog"/> backed by an assembly's manifest resources.</summary>
public sealed class EmbeddedTemplateCatalog(
    Assembly assembly,
    string sharedPrefix = "specforge.shared/",
    string templatesPrefix = "specforge.templates/") : IEmbeddedTemplateCatalog
{
    public IReadOnlyList<EmbeddedTemplate> GetShared() => Enumerate(sharedPrefix);

    public IReadOnlyList<EmbeddedTemplate> GetTemplates() => Enumerate(templatesPrefix);

    private IReadOnlyList<EmbeddedTemplate> Enumerate(string prefix)
    {
        List<EmbeddedTemplate> templates = [];
        foreach (string resource in assembly.GetManifestResourceNames())
        {
            // %(RecursiveDir) emits '\' on Windows — normalize for the relative path (see EmbeddedSkillCatalog).
            string normalized = resource.Replace('\\', '/');
            if (!normalized.StartsWith(prefix, StringComparison.Ordinal))
            {
                continue;
            }

            string relative = normalized[prefix.Length..];
            if (relative.Length == 0 || string.Equals(Path.GetFileName(relative), ".gitkeep", StringComparison.Ordinal))
            {
                continue;
            }

            string captured = resource;
            templates.Add(new EmbeddedTemplate(
                resource,
                relative,
                () => assembly.GetManifestResourceStream(captured) ?? throw new InvalidOperationException($"Embedded resource '{captured}' could not be opened.")));
        }

        return [.. templates.OrderBy(t => t.RelativePath, StringComparer.Ordinal)];
    }
}

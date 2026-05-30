namespace Specforge.Core.Init;

/// <summary>An embedded canonical shared-doc or template file (DEC-008 / ITEM-006 scaffold source).</summary>
/// <param name="LogicalPath">Full embedded-resource logical name.</param>
/// <param name="RelativePath">Path under the resource prefix, '/'-separated (e.g. <c>glossary.md</c>).</param>
/// <param name="OpenStream">Opens a fresh read stream over the resource.</param>
public sealed record EmbeddedTemplate(string LogicalPath, string RelativePath, Func<Stream> OpenStream);

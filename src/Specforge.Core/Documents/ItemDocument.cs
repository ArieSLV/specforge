namespace Specforge.Core.Documents;

/// <summary>Parsed representation of an item spec file (ITEM-008 §7): header + section map + required-section check.</summary>
public sealed record ItemDocument(
    string Id,
    string Title,
    string Status,
    string ReviewOwner,
    IReadOnlyList<string> DependsOn,
    IReadOnlyList<string> UpdatesLedgerRows,
    IReadOnlyDictionary<string, string> Sections,
    IReadOnlyList<string> MissingRequiredSections,
    string RawMarkdown,
    string AbsolutePath);

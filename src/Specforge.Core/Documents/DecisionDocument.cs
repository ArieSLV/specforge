namespace Specforge.Core.Documents;

/// <summary>Parsed representation of a decision record file (ITEM-007 §7): metadata + section map + raw source.</summary>
public sealed record DecisionDocument(
    string Id,
    string Title,
    string Status,
    string Date,
    string Owner,
    string ReviewOwner,
    string? Supersedes,
    string? Covers,
    string? Amends,
    IReadOnlyDictionary<string, string> Sections,
    string RawMarkdown,
    string AbsolutePath);

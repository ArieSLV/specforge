namespace Specforge.Core.Documents;

/// <summary>
/// Produces the initial decision record file body (ITEM-007). A minimal, contract-conformant skeleton
/// (heading + metadata + the core sections); the AI fills section bodies via subsequent edits.
/// </summary>
public sealed class DecisionFileWriter
{
    /// <summary>Renders a new decision file's markdown.</summary>
    public string Render(string id, string slug, string plainTitle, string status, string date, string owner, string reviewOwner) =>
        string.Join('\n',
        [
            $"# {id}-{slug} - {plainTitle}",
            string.Empty,
            $"Status: {status}",
            $"Date: {date}",
            $"Owner: {owner}",
            $"Review owner: {reviewOwner}",
            string.Empty,
            "## Context",
            string.Empty,
            "(to be written)",
            string.Empty,
            "## Decision",
            string.Empty,
            "(to be written)",
            string.Empty,
            "## Consequences",
            string.Empty,
            "(to be written)",
            string.Empty,
        ]);
}

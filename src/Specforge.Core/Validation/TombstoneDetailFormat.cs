using System.Text.RegularExpressions;

namespace Specforge.Core.Validation;

/// <summary>
/// The locked public-contract format for <c>Deleted</c> history-event detail strings (ITEM-010):
/// <c>Tombstone &lt;deleted-id&gt;: &lt;human-readable rest&gt;</c>. The four delete tools
/// (delete_decision / delete_item / delete_review / delete_commit) call <see cref="Format"/> so the
/// ids-aspect gap verifier has a deterministic parser target.
/// </summary>
public static partial class TombstoneDetailFormat
{
    [GeneratedRegex(@"^Tombstone\s+(?<id>[A-Za-z][A-Za-z0-9-]*[A-Za-z0-9]):")]
    private static partial Regex TombstonePrefix();

    /// <summary>Renders the canonical detail string for a <c>Deleted</c> history event.</summary>
    public static string Format(string deletedId, string humanRest) => $"Tombstone {deletedId}: {humanRest}";

    /// <summary>
    /// Extracts the deleted id from a history detail string of the form <c>Tombstone &lt;id&gt;: ...</c>;
    /// returns <see langword="false"/> (and null) for any non-matching string.
    /// </summary>
    public static bool TryExtractDeletedId(string? historyDetail, out string? deletedId)
    {
        deletedId = null;
        if (string.IsNullOrEmpty(historyDetail))
        {
            return false;
        }

        Match match = TombstonePrefix().Match(historyDetail);
        if (!match.Success)
        {
            return false;
        }

        deletedId = match.Groups["id"].Value;
        return true;
    }
}

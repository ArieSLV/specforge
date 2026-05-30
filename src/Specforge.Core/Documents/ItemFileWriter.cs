using System.Text;

namespace Specforge.Core.Documents;

/// <summary>
/// Produces the initial item spec file body (ITEM-008). Emits the full section template
/// (<see cref="RequiredItemSections.FullTemplate"/>) so a newly created item is contract-complete;
/// the AI fills section bodies via subsequent edits.
/// </summary>
public sealed class ItemFileWriter
{
    public string Render(string id, string slug, string title, string status, IReadOnlyList<string>? dependsOn)
    {
        StringBuilder builder = new();
        builder.Append($"# {id}-{slug} - {title}\n\n");
        builder.Append($"Status: {status}\n");
        builder.Append("Review owner: User\n");
        builder.Append($"Depends on: {(dependsOn is { Count: > 0 } ? string.Join(", ", dependsOn) : "-")}\n");
        builder.Append($"Updates ledger rows: new ART-{id}\n\n");

        foreach (string section in RequiredItemSections.FullTemplate)
        {
            builder.Append($"## {section}\n\n(to be written)\n\n");
        }

        return builder.ToString();
    }
}

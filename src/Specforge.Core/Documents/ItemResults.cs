namespace Specforge.Core.Documents;

/// <summary>One row of <c>list_items</c> output.</summary>
public sealed record ItemSummary(string Id, string Title, string Status, string Path);

/// <summary>Outcome of <c>create_item</c>.</summary>
public sealed record CreateItemResult(string Id, string Slug, string ItemPath, string ArtifactId, bool DryRun, IReadOnlyList<string> PlannedWrites);

/// <summary>Outcome of <c>set_item_status</c>.</summary>
public sealed record SetItemStatusResult(string Id, string PreviousStatus, string NewStatus, string? ReviewId, bool DryRun);

/// <summary>Outcome of <c>delete_item</c>.</summary>
public sealed record DeleteItemResult(string Id, string ItemPath, string ArtifactId, int RemovedReviewRows, bool DryRun);

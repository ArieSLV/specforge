namespace Specforge.Core.Documents;

/// <summary>One row of <c>list_decisions</c> output.</summary>
public sealed record DecisionSummary(string Id, string Title, string Status, string Path);

/// <summary>Outcome of <c>create_decision</c>.</summary>
public sealed record CreateDecisionResult(string Id, string Slug, string DecisionPath, string ArtifactId, bool DryRun, IReadOnlyList<string> PlannedWrites);

/// <summary>Outcome of <c>set_decision_status</c>.</summary>
public sealed record SetDecisionStatusResult(string Id, string PreviousStatus, string NewStatus, string? ReviewId, bool DryRun);

/// <summary>Outcome of <c>delete_decision</c>.</summary>
public sealed record DeleteDecisionResult(string Id, string DecisionPath, string ArtifactId, int RemovedReviewRows, bool DryRun);

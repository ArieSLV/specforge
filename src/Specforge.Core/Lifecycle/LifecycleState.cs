namespace Specforge.Core.Lifecycle;

/// <summary>
/// The lifecycle states recognized for decision/item documents (ITEM-007 §6 — the binding list, which
/// the spec graph and DEC-007 delete semantics use). Note: the Stage 0 <c>document_lifecycle.md</c>
/// shared doc predates this finalized vocabulary and lists different labels; this enumeration follows
/// the item-spec / DEC-007 usage that the tooling actually enforces.
/// </summary>
public static class LifecycleState
{
    public const string NotStarted = "Not started";
    public const string Placeholder = "Placeholder";
    public const string Draft = "Draft";
    public const string DraftForUserReview = "Draft for user review";
    public const string Approved = "Approved";
    public const string Refined = "Refined";
    public const string Superseded = "Superseded";
    public const string Withdrawn = "Withdrawn";

    /// <summary>All recognized states.</summary>
    public static IReadOnlyList<string> All { get; } =
        [NotStarted, Placeholder, Draft, DraftForUserReview, Approved, Refined, Superseded, Withdrawn];

    /// <summary>States from which a delete is permitted (DEC-007 Delete Semantics).</summary>
    public static IReadOnlyList<string> PreApproved { get; } =
        [NotStarted, Placeholder, Draft, DraftForUserReview];
}

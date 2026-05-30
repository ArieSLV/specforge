namespace Specforge.Core.Lifecycle;

/// <summary>
/// The directed graph of valid status transitions for spec documents (ITEM-007 §6). Adding a state is
/// a single edit here plus a shared-doc update.
/// </summary>
public sealed class LifecycleStateMachine
{
    private static readonly Dictionary<string, string[]> Transitions = new(StringComparer.Ordinal)
    {
        [LifecycleState.NotStarted] = [LifecycleState.Placeholder, LifecycleState.Draft, LifecycleState.Withdrawn],
        [LifecycleState.Placeholder] = [LifecycleState.Draft, LifecycleState.Withdrawn],
        [LifecycleState.Draft] = [LifecycleState.DraftForUserReview, LifecycleState.Withdrawn],
        [LifecycleState.DraftForUserReview] = [LifecycleState.Approved, LifecycleState.Draft, LifecycleState.Withdrawn],
        [LifecycleState.Approved] = [LifecycleState.Refined, LifecycleState.Superseded, LifecycleState.Withdrawn],
        [LifecycleState.Refined] = [LifecycleState.Superseded, LifecycleState.Withdrawn],
        [LifecycleState.Superseded] = [],
        [LifecycleState.Withdrawn] = [],
    };

    /// <summary>True if <paramref name="toStatus"/> is a permitted next state from <paramref name="fromStatus"/>.</summary>
    public bool IsValidTransition(string fromStatus, string toStatus) =>
        Transitions.TryGetValue(fromStatus, out string[]? next) && next.Contains(toStatus, StringComparer.Ordinal);

    /// <summary>The permitted next states from <paramref name="fromStatus"/> (empty for terminal/unknown states).</summary>
    public IReadOnlyList<string> AllowedTransitions(string fromStatus) =>
        Transitions.TryGetValue(fromStatus, out string[]? next) ? next : [];

    /// <summary>True if <paramref name="status"/> is a recognized lifecycle state.</summary>
    public bool IsKnownState(string status) => Transitions.ContainsKey(status);
}

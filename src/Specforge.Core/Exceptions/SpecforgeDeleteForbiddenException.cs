namespace Specforge.Core.Exceptions;

/// <summary>
/// A delete was attempted on a status-bearing artifact whose status is past the pre-Approved set
/// (DEC-007 Delete Semantics). Maps to <c>specforge.lifecycle.delete_forbidden</c>.
/// </summary>
public sealed class SpecforgeDeleteForbiddenException : SpecforgeException
{
    public SpecforgeDeleteForbiddenException(string id, string currentStatus, IReadOnlyList<string> allowedStates)
        : base("specforge.lifecycle.delete_forbidden", $"Cannot delete '{id}' in status '{currentStatus}'; delete is allowed only from a pre-Approved state.")
    {
        Id = id;
        CurrentStatus = currentStatus;
        AllowedStates = allowedStates;
    }

    /// <summary>The artifact identifier.</summary>
    public string Id { get; }

    /// <summary>The artifact's current status.</summary>
    public string CurrentStatus { get; }

    /// <summary>The states from which a delete would be permitted.</summary>
    public IReadOnlyList<string> AllowedStates { get; }
}

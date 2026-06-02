using Specforge.Core.Diagnostics;

namespace Specforge.Core.Exceptions;

/// <summary>
/// A referenced identifier (a REV/CMT row to delete, or an append target) does not exist in the active
/// package (ITEM-011, closing the ITEM-009 audit gap). Maps to <c>specforge.id.not_found</c>.
/// </summary>
public sealed class SpecforgeIdNotFoundException(string id, string? message = null)
    : SpecforgeException(SpecforgeErrorCode.IdNotFound, message ?? $"No ledger row found for id '{id}'.")
{
    /// <summary>The identifier that could not be found.</summary>
    public string Id { get; } = id;
}

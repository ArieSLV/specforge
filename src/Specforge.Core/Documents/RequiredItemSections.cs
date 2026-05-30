namespace Specforge.Core.Documents;

/// <summary>
/// The required-for-Approved item-spec sections (ITEM-008 §Terminology, sourced from
/// <c>spec/shared/spec_item_contract.md</c>). Single in-code source of truth; a contract-drift test
/// asserts each name appears in the shared contract.
/// </summary>
public static class RequiredItemSections
{
    public static IReadOnlyList<string> Names { get; } =
    [
        "Handoff Summary",
        "Problem Slice",
        "Approved Decisions",
        "Current Code State",
        "Target Behavior",
        "Invariants",
        "Code Scope",
        "Test Scope",
        "Test Plan",
        "Impact Assessment",
        "Validation",
        "Done Criteria",
    ];

    /// <summary>The full item-spec section template emitted for a newly created item (superset of required).</summary>
    public static IReadOnlyList<string> FullTemplate { get; } =
    [
        "Handoff Summary",
        "Problem Slice",
        "Terminology Used",
        "Approved Decisions",
        "Current Code State",
        "Target Behavior",
        "Invariants",
        "Code Scope",
        "Test Scope",
        "Test Plan",
        "Test Evidence",
        "Impact Assessment",
        "Validation",
        "Open Questions",
        "Done Criteria",
        "Links",
    ];
}

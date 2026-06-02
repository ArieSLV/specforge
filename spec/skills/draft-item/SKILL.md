---
name: draft-item
description: Use when the user requests a new item spec for an implementation slice. Drafts an ITEM-NNN record using create_item, populates required sections, and transitions to Draft for user review.
---

Use this skill when the user wants to specify a new implementation slice — typically after at least one foundation decision (`DEC-NNN`) is approved. It produces a single new item file in the active package's `items/` directory, the corresponding `ART-ITEM-NNN` ledger row, and a `Created` history event. Items are the unit of Stage 1 specification and the granular unit of Stage 2 implementation.

## When to use

- A foundation decision is approved and the user wants to specify the next implementation slice.
- The user explicitly asks for an item spec.
- An existing item spec has grown too large and should be split.

## Steps

1. If unsure which package is active, call `info`, then call `use_package` if needed.
2. Survey related approved decisions with `list_decisions` (filter by `status=Approved`) to enumerate the new item's dependencies.
3. Survey existing items with `list_items` to avoid duplication.
4. Frame the implementation slice: what is being built, what depends on it, and what it does not cover.
5. Call `create_item` with the plain title, `status=Draft`, and a `dependsOn` list naming the approved decisions the item realizes.
6. Edit the new file's body sections per the item template and the required-section list in `spec/shared/spec_item_contract.md`: Handoff Summary, Problem Slice, Approved Decisions, Current Code State, Target Behavior, Invariants, Code Scope, Test Scope, Test Plan, Impact Assessment, Validation, Done Criteria, plus the optional Terminology Used / Open Questions / Links.
7. Invoke the `impact-assessment` skill to populate the Impact Assessment table.
8. Invoke the `validate-spec-graph` skill with `aspect=impact-coverage` to verify every required section is populated before transitioning.
9. Call `set_item_status` to move the record to `Draft for user review` when ready.

## Tools used

- `info`
- `use_package`
- `list_decisions`
- `list_items`
- `create_item`
- `set_item_status`
- `validate`

## Shared docs referenced

- `spec/shared/spec_item_contract.md`
- `spec/shared/document_lifecycle.md`
- `spec/shared/impact_assessment_checklist.md`
- `spec/templates/item_spec.md`

## Cross-skill references

- `impact-assessment`
- `validate-spec-graph`
- `review-decision`
- `review-item`

## Anti-patterns

- Do not draft an item before its prerequisite decisions are approved; an item that realizes an undecided fork will churn.
- Do not omit required sections to "fill in later" — an Approved item missing a required section is a hard `validate` error.

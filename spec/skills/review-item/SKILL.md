---
name: review-item
description: Use when an item is Draft for user review or the user requests a review pass. Reads the body, checks required sections and lifecycle, then transitions to Approved or Refined.
---

Use this skill when the user wants you to review an item spec — either freshly drafted (`Draft for user review`) or one returning for a second pass. The skill reads the full item body, verifies the required sections are populated and self-consistent, applies the impact-assessment lens, and either records approval via `set_item_status` (which appends the review row when a reviewer and notes are supplied) or surfaces actionable feedback so the user can refine. It mirrors `review-decision` structurally.

## When to use

- An item is in `Draft for user review` status and the user requests review.
- The user wants to formally approve an item.
- The user wants to request changes on an in-flight item.

## Steps

1. Call `get_item` to read the full body and metadata. The payload includes `missingRequiredSections` — surface that list first when it is non-empty.
2. Verify the required sections are present and non-empty per `spec/shared/spec_item_contract.md`: Handoff Summary, Problem Slice, Terminology Used, Approved Decisions, Current Code State, Target Behavior, Invariants, Code Scope, Test Scope, Test Plan, Test Evidence, Impact Assessment, Validation, Open Questions, Done Criteria, Links.
3. Run `validate` with `aspect=impact-coverage` to catch missing-required-section findings; consult the `validate-spec-graph` skill for fix recipes if the findings are non-trivial.
4. Cross-check the Impact Assessment table by invoking the `impact-assessment` skill if any aspect is missing or implausible.
5. Inspect the `Depends on` line; verify each referenced id exists or is intentionally forward-looking.
6. If approving, call `set_item_status` with `status=Approved` plus `reviewer` and `notes` — this appends the `REV-ITEM-NNN-NNN` row automatically.
7. If requesting changes, list the issues and suggest `set_item_status` with `status=Refined`.
8. If withdrawing, suggest `set_item_status` with `status=Withdrawn` after confirmation.

## Tools used

- `get_item`
- `set_item_status`
- `validate`

## Shared docs referenced

- `spec/shared/document_lifecycle.md`
- `spec/shared/spec_item_contract.md`
- `spec/shared/impact_assessment_checklist.md`

## Cross-skill references

- `impact-assessment`
- `validate-spec-graph`

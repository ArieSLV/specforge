---
name: review-decision
description: Use when a decision is Draft for user review or the user requests a review pass. Reads the body, checks required sections and lifecycle, then transitions to Approved or Refined.
---

Use this skill when the user wants you to review a decision record — either freshly drafted (`Draft for user review`) or one returning for a second pass. The skill reads the full decision body, verifies the required sections are populated and self-consistent, applies the impact-assessment lens, and either records approval via `set_decision_status` (which appends the review row) or surfaces actionable feedback so the user can refine.

## When to use

- A decision is in `Draft for user review` status and the user requests review.
- The user wants to formally approve a decision.
- The user wants to request changes on an in-flight decision.

## Steps

1. Call `get_decision` to read the full body and metadata.
2. Verify the required sections are present and non-empty: Context, Decision, Alternatives Considered, Consequences, Impact Assessment, Source Links, Related Decisions, Open Questions.
3. Run `validate` with `aspect=lifecycle` to catch status mismatches or missing review evidence for this decision.
4. Cross-check the Impact Assessment table by invoking the `impact-assessment` skill if any aspect is missing or implausible.
5. If approving, call `set_decision_status` with `status=Approved` plus `reviewer` and `notes` — this appends the `REV-DEC-NNN-NNN` row automatically.
6. If requesting changes, list the issues and suggest `set_decision_status` with `status=Refined` to record a refinement pass.
7. If withdrawing, suggest `set_decision_status` with `status=Withdrawn` after confirmation.

## Tools used

- `get_decision`
- `set_decision_status`
- `validate`

## Shared docs referenced

- `spec/shared/document_lifecycle.md`
- `spec/shared/impact_assessment_checklist.md`

## Cross-skill references

- `impact-assessment`
- `validate-spec-graph`

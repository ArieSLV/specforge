---
name: impact-assessment
description: Use when populating the Impact Assessment section of a decision or item. Walks the canonical aspect checklist and records No impact / Indirect / Direct per aspect with concrete justification.
---

Use this skill when authoring or revising the Impact Assessment section of any decision or item record. The skill walks the canonical aspect list from `spec/shared/impact_assessment_checklist.md`, evaluates each aspect against the current artifact's scope, and records the impact category (No impact / Indirect / Direct) with a single-sentence justification. It is normally invoked from inside `draft-decision` or `draft-item`, not standalone.

## When to use

- Drafting a new decision record (called from `draft-decision`).
- Drafting a new item spec (called from `draft-item`).
- Revising an existing Impact Assessment section in response to a scope change.

## Steps

1. Read the canonical aspect list from `spec/shared/impact_assessment_checklist.md`.
2. For each aspect, classify the artifact's impact:
   - No impact: the artifact does not affect this aspect — record the row explicitly rather than omitting it.
   - Indirect: the artifact influences this aspect only indirectly (for example, a new error code affects test-coverage scope but not behavior).
   - Direct: the artifact changes this aspect's behavior or contract.
3. Write a single-sentence justification per row that refers concretely to the artifact's content. Vague justifications such as "see Decision" are not acceptable — the table must stand alone.
4. If a new aspect emerges that is not in the checklist, propose it to the user; do not silently extend the table.
5. Return to the calling skill.

## Tools used

- (none — this is a pure authoring skill)

## Shared docs referenced

- `spec/shared/impact_assessment_checklist.md`

## Cross-skill references

- `draft-decision`
- `draft-item`
- `review-decision`
- `review-item`

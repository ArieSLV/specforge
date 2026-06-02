---
name: draft-decision
description: Use when the user wants to record a new architectural decision or resolve a design fork. Drafts a DEC-NNN via create_decision, populates required sections, and transitions to Draft for review.
---

Use this skill when the user wants to record a new architectural decision — typically when a design fork emerges in conversation (which library, which pattern, which lifecycle), or when the user explicitly says "let's write a DEC". It produces a single new decision file in the active package's `decisions/` directory, the corresponding `ART-DEC-NNN` ledger row, and a `Created` history event — all in one tool call via `create_decision`.

## When to use

- The user is resolving an architectural fork that affects multiple future items.
- The user explicitly asks for a decision record.
- The current task cannot proceed until a design choice is locked.

## Steps

1. If unsure which package is active, call `info` to check, then call `use_package` if a different package is needed.
2. Survey related decisions with `list_decisions` (filter by `status=Approved`) and read context with `get_decision`.
3. Frame the design fork: what is being decided, what alternatives exist, and why one is preferred.
4. If the user has not chosen between the alternatives, use the host's structured-question facility to elicit the choice first. Do not draft against an unresolved fork.
5. Invoke the `impact-assessment` skill to populate the Impact Assessment table.
6. Call `create_decision` with the plain title and `status=Draft` to allocate the `DEC-NNN` id and scaffold the file.
7. Edit the new file's body sections (Context, Decision, Alternatives Considered, Consequences, Impact Assessment, Source Links, Related Decisions, Open Questions) per the decision template.
8. Call `set_decision_status` to move the record to `Draft for user review` once the draft is ready.
9. Invoke the `review-decision` skill so the user can review.

## Tools used

- `info`
- `use_package`
- `list_decisions`
- `get_decision`
- `create_decision`
- `set_decision_status`

## Shared docs referenced

- `spec/shared/document_lifecycle.md`
- `spec/shared/impact_assessment_checklist.md`
- `spec/templates/decision_record.md`

## Cross-skill references

- `impact-assessment`
- `review-decision`

## Anti-patterns

- Do not draft a decision against an unresolved fork — elicit the choice first.
- Do not hand-edit the ledger; `create_decision` and `set_decision_status` keep the ART row and history events in sync.

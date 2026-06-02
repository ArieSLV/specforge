---
name: validate-spec-graph
description: Use before approving any artifact or before publishing a release. Runs validate(aspect=all), interprets findings by severity, and proposes fixes referencing the appropriate tools.
---

Use this skill to confirm the spec graph is internally consistent — every artifact has its required sections, every cross-reference resolves, every lifecycle status agrees between file and ledger, and every identifier is allocated cleanly. Invoke it before approving any item, before publishing a binary release, and after any large mass-rename or refactor.

## When to use

- Before approving any decision or item.
- Before publishing a release.
- After a mass rename, schema upgrade, or other cross-cutting edit.
- When investigating an apparent inconsistency you surfaced.

## Steps

1. Call `validate` with `aspect=all` and inspect `data.findings`.
2. Triage each finding by severity:
   - `error`: must be resolved before continuing; use the finding's suggestion field for the fix path.
   - `warning`: review and decide. Common warnings are a dangling `Depends on` (acceptable transiently if the target is planned next) and Approved-without-review (record the review via `append_review`, or re-run `set_decision_status` / `set_item_status` with a reviewer and notes).
   - `info`: informational, e.g. a Draft item with missing required sections (acceptable while in flight).
3. Apply the fix that matches each error pattern:
   - An `ids` gap with no tombstone: delete the gap-introducing artifact properly via `delete_decision` / `delete_item` / `delete_review` / `delete_commit` (which append tombstone evidence), or append the explanatory event via `append_history` if the gap is intentional.
   - A `links` dangling reference: create the missing artifact via `create_decision` / `create_item`, fix the reference, or remove it.
   - A `lifecycle` file-vs-ledger status mismatch: call `set_decision_status` or `set_item_status` with the intended status to sync.
   - An `impact-coverage` missing section: edit the artifact to add the section before reattempting approval.
4. Re-run `validate` with `aspect=all`. Loop until the error count is zero.
5. Surface the remaining warnings and info findings to the user with the option to fix or defer.

## Tools used

- `validate`
- `set_decision_status`
- `set_item_status`
- `create_decision`
- `create_item`
- `delete_decision`
- `delete_item`
- `delete_review`
- `delete_commit`
- `append_history`
- `append_review`

## Shared docs referenced

- `spec/shared/document_lifecycle.md`
- `spec/shared/spec_item_contract.md`

## Cross-skill references

- `review-decision`
- `review-item`
- `draft-decision`
- `draft-item`

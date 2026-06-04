# Document Lifecycle

Status: Draft

Normative lifecycle rules for all specforge specification artifacts across all packages.

## Status Values

| Status | Meaning |
|---|---|
| `Not started` | Planned but not drafted. |
| `Placeholder` | A reserved ledger slot before any real content exists (e.g., a ledger file promoted to `Draft` on its first real append). |
| `Draft` | Written or being written; not yet handed to a reviewer. |
| `Draft for user review` | The author believes the document is ready; it is handed to a human reviewer for approval. |
| `Approved` | Accepted as input for downstream specification or implementation. |
| `Refined` | Replaced as standalone authority by a newer record, while one or more underlying principles remain valid evidence. |
| `Superseded` | Replaced by a newer artifact. |
| `Withdrawn` | Retired without a replacement (the lifecycle's terminal "discarded" state). The ID is tombstoned and never reused (DEC-004). |

These eight states are the binding vocabulary the tooling enforces (`Specforge.Core` `LifecycleState`): `set_decision_status` / `set_item_status` reject any other value with `specforge.tool.invalid_argument`, and `validate(aspect=lifecycle)` flags any unrecognized status.

> **"Done" is not a lifecycle status.** An item spec defines a *Done Criteria* section; once those criteria are met the item is *implemented*, but its lifecycle status remains `Approved` — the implementation is recorded as a `CMT` commit-ledger row plus an `Implemented` history event, not a status change. Earlier revisions of this document listed `Ready for review`, `Needs changes`, `Needs user decision`, `Needs architect confirmation`, `Obsolete`, and `Done`; those labels predated the finalized vocabulary above and have been reconciled out.

## Transitions

`set_decision_status` and `set_item_status` enforce this directed graph (`Specforge.Core` `LifecycleStateMachine`). A transition not listed here is rejected.

| From | Permitted next states |
|---|---|
| `Not started` | `Placeholder`, `Draft`, `Withdrawn` |
| `Placeholder` | `Draft`, `Withdrawn` |
| `Draft` | `Draft for user review`, `Withdrawn` |
| `Draft for user review` | `Approved`, `Draft`, `Withdrawn` |
| `Approved` | `Refined`, `Superseded`, `Withdrawn` |
| `Refined` | `Superseded`, `Withdrawn` |
| `Superseded` | _(terminal)_ |
| `Withdrawn` | _(terminal)_ |

- There is **no** transition from `Approved` back to `Draft`. An approved artifact that needs material change is `Refined` (a newer record takes over as authority while this one remains valid evidence), `Superseded` (fully replaced), or `Withdrawn` (retired). A small, additive correction to an approved artifact is instead recorded **in place** as an amendment — a `## Amendments` section in the document plus an `Amended` history event — with the status left at `Approved`.
- **Delete is restricted to the pre-approval states.** `delete_decision` / `delete_item` (DEC-007 Delete Semantics) are permitted only from `Not started`, `Placeholder`, `Draft`, or `Draft for user review`. From `Approved` onward, retire via a `Withdrawn` / `Superseded` transition; a delete is refused with `specforge.lifecycle.delete_forbidden`.

## Stable Reference Rule

Review findings, decision records, item specs, ledgers, and handoff notes MUST use stable references rather than relying on line numbers.

Preferred order when pointing to a specific statement:

1. Stable ID: decision ID, item ID, artifact ID, ledger row ID, register row ID
2. File path
3. Section heading
4. Short evidence quote or exact field name
5. Line number as optional current-snapshot hint only

Line numbers MUST NOT be the only locator for a review finding or decision input.

Example preferred format:

```text
File: spec/packages/specforge-mvp/decisions/DEC-001-DISTRIBUTION-AND-TRANSPORT.md
Locator: section "Registration with MCP Host", paragraph beginning "specforge is registered once, user-wide..."
Evidence: "registered once, user-wide"
Line: optional current-snapshot hint only
```

## Approval Rule

A document cannot be `Approved` while it contains:

- Unresolved forks
- Assumptions not marked with a decision status
- Missing impact assessment for a meaningful decision
- Missing handoff summary on an item spec
- Missing decision / item / test / validation coverage links for a design invariant

## Review Routing

Every document sent for review SHOULD make the review scope explicit:

- Scope being reviewed
- Changes since previous review
- Explicit questions
- Known non-goals
- Required reviewer role
- Target outcome (approval / answer / directional feedback)

Architect-level review (when applicable) MUST target a specific decision record, shared contract, or register row — never the full package.

## Normative Language

| Term | Meaning |
|---|---|
| `MUST` | Required for the design or implementation to be considered correct. |
| `MUST NOT` | Forbidden behavior or implementation direction. |
| `SHOULD` | Strongly preferred unless a recorded decision explains otherwise. |
| `MAY` | Allowed but not required. |

Speculation belongs in a decision record under non-approved status, not in `MUST`/`SHOULD` text.

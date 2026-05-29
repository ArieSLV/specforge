# Document Lifecycle

Status: Draft

Normative lifecycle rules for all specforge specification artifacts across all packages.

## Status Values

| Status | Meaning |
|---|---|
| `Not started` | Planned but not drafted. |
| `Draft` | Written or being written; not yet ready for review. |
| `Ready for review` | Author believes the document is ready for targeted review. |
| `Needs changes` | Review found required changes. |
| `Needs user decision` | Blocked on a concrete question for the user. |
| `Needs architect confirmation` | Blocked on architect-level review. |
| `Approved` | Accepted as input for downstream specification or implementation. |
| `Refined` | Replaced as standalone authority by a newer record, while one or more underlying principles remain valid evidence. |
| `Superseded` | Replaced by a newer artifact. |
| `Obsolete` | Intentionally discarded. |
| `Done` | Completed implementation item; requires linked decision, item, test, and validation coverage. |

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
Locator: section "Decision", paragraph beginning "The server is registered..."
Evidence: "user-wide .mcp.json registration"
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

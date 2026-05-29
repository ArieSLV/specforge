# specforge-mvp Work Ledger

Status: Draft
Created: 2026-05-25

This is the dashboard view. Detailed data lives under `ledger/`.

If status differs between this file and `ledger/*.md`, the detailed ledger row is authoritative.

## Current Focus

| Area | Status | Next action |
|---|---|---|
| Specification scaffold | Draft | Established 2026-05-25; serves as package entry point |
| Stage 0 decisions | **Complete** | All 8 of 8 approved 2026-05-28 (DEC-001..DEC-008). Foundation locked: distribution, configuration, runtime, ID scheme, skill packaging, schema versioning, MVP tool set, instruction layer. |
| Stage 1 items | **Complete** | All 13 of 13 approved 2026-05-28 (ITEM-001..ITEM-013). Specs locked: scaffold + config + IDs + skill machinery + bootstrap-and-authoring tools (5 categories of MCP tools) + validation + error envelope + 7 SKILL.md content (catalog amended from 6 to 7 post-audit) + docs. 21 MCP tools across 6 categories. 14 error codes. 12 typed Core exceptions. |
| Stage 2 implementation | **Unblocked** | Begin with ITEM-001-SOLUTION-BOOTSTRAP (solution scaffold; foundation for every other item). Per `work_plan.md` Stage 2 rule: code merges only after each item's spec is Approved — all 13 are now Approved, so any item is implementable. |
| Stage 3 first external adoption | Not started | Awaits Stage 2 completion (or at least a runnable MVP binary). |

## Active Blockers

_None._ Stage 1 complete; Stage 2 unblocked. Next decision is whether to begin Stage 2 implementation immediately, or to do an end-to-end audit/review pass before writing code.

## Ledger Files

| File | Status |
|---|---|
| `ledger/README.md` | Draft |
| `ledger/artifacts.md` | Draft |
| `ledger/items.md` | Draft |
| `ledger/commits.md` | Placeholder |
| `ledger/reviews.md` | Draft |
| `ledger/history.md` | Draft |

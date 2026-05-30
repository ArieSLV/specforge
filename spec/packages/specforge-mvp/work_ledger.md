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
| Stage 2 implementation | **In progress** | Implemented: ITEM-001 (`13a306d`), ITEM-002 (`284e3be`), ITEM-003 (`0605d29`), ITEM-004 (`a86526f`), ITEM-005 (`302ba40`), ITEM-006 init tool (`ef32f58`). **6 of 13 items done.** 178 tests pass; 12 error codes (+ invalid_argument carrier); **5 MCP tools**. Next: ITEM-007 (decision tools — first spec-graph item: 5 decision tools, ledger row primitives, LifecycleStateMachine, Markdig, DecisionDocument parser, SpecforgeDeleteForbiddenException = 13th code; tool count 5 → 10). |
| Stage 3 first external adoption | Not started | Awaits Stage 2 completion (or at least a runnable MVP binary). |

## Active Blockers

_None._ ITEM-001 and ITEM-002 implemented and committed. Build env notes (persist across all Stage 2 builds): (1) inherited `MSBuildSDKsPath` (RavenDB toolchain, pins SDK 8.0.403) must be cleared per build invocation (`$env:MSBuildSDKsPath = $null; dotnet build ...`) so `global.json`'s .NET 10 pin takes effect — otherwise NETSDK1045; (2) `dotnet` commands must run with cwd inside `D:\Work\specforge` (the RavenDB sibling repo's `global.json` pins an uninstalled 10.0.300). Next item: ITEM-007.

## Ledger Files

| File | Status |
|---|---|
| `ledger/README.md` | Draft |
| `ledger/artifacts.md` | Draft |
| `ledger/items.md` | Draft |
| `ledger/commits.md` | Draft |
| `ledger/reviews.md` | Draft |
| `ledger/history.md` | Draft |

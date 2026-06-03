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
| Stage 2 implementation | **In progress** | Implemented: ITEM-001..011 + ITEM-012 seven SKILL.md bodies + content drift-guard tests (`84a5ded`). **12 of 13 items done.** 348 tests pass; 14 error codes; **21 MCP tools**; **7-skill catalog populated** (was empty). Dogfood `validate(aspect=all)` clean. Next: ITEM-013 (docs / getting-started — README revision + `docs/getting-started.md` + DocsContentReferenceTests; the FINAL item). |
| Stage 3 first external adoption | Not started | Awaits Stage 2 completion (or at least a runnable MVP binary). |

## Active Blockers

_None._ ITEM-001 and ITEM-002 implemented and committed. Build env notes (persist across all Stage 2 builds): (1) inherited `MSBuildSDKsPath` (RavenDB toolchain, pins SDK 8.0.403) must be cleared per build invocation (`$env:MSBuildSDKsPath = $null; dotnet build ...`) so `global.json`'s .NET 10 pin takes effect — otherwise NETSDK1045; (2) `dotnet` commands must run with cwd inside `D:\Work\specforge` (the RavenDB sibling repo's `global.json` pins an uninstalled 10.0.300). MCP harness note: drive write tools (create/set/delete) one request→response at a time; pipelining frames without awaiting responses can race the server (DEC-003 single-session, no cross-request concurrency). Next item: ITEM-013 (final).

## Ledger Files

| File | Status |
|---|---|
| `ledger/README.md` | Draft |
| `ledger/artifacts.md` | Draft |
| `ledger/items.md` | Draft |
| `ledger/commits.md` | Draft |
| `ledger/reviews.md` | Draft |
| `ledger/history.md` | Draft |

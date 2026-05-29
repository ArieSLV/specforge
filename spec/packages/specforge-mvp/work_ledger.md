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
| Stage 2 implementation | **In progress** | ITEM-001 (solution bootstrap) **implemented** 2026-05-29 — CMT-001 (`13a306d`), all 6 Done Criteria met. `git init` done (branch `master`). Next: ITEM-002 (config model + first 3 tools + first MCP host wiring). |
| Stage 3 first external adoption | Not started | Awaits Stage 2 completion (or at least a runnable MVP binary). |

## Active Blockers

_None._ ITEM-001 implemented and committed. Build env note: inherited `MSBuildSDKsPath` (RavenDB toolchain, pins SDK 8.0.403) must be cleared per build invocation (`$env:MSBuildSDKsPath = $null; dotnet build ...`) so `global.json`'s .NET 10 pin takes effect — otherwise build fails with NETSDK1045. Next item: ITEM-002.

## Ledger Files

| File | Status |
|---|---|
| `ledger/README.md` | Draft |
| `ledger/artifacts.md` | Draft |
| `ledger/items.md` | Draft |
| `ledger/commits.md` | Draft |
| `ledger/reviews.md` | Draft |
| `ledger/history.md` | Draft |

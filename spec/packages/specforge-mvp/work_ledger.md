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
| Stage 2 implementation | **Complete** | **All 13 of 13 items implemented (ITEM-001..ITEM-013).** Final item ITEM-013 (docs: `README.md` revision + `docs/getting-started.md` + `DocsContentReferenceTests`) landed at CMT-013 (`55e2cd3`). 356 tests pass; 14 error codes; 13 typed Core exceptions; **21 MCP tools** across 5 families (setup/decisions/items/ledger/validate); **7-skill catalog** authored + drift-guarded. Dogfood `validate(aspect=all)` clean (errorCount=0). Runnable self-contained Windows x64 MCP binary. |
| Stage 3 first external adoption | Not started | **Unblocked** — Stage 2 complete; a runnable MVP binary exists. Adopt specforge to manage at least one external package (e.g., the next RavenDB ticket) to validate portability assumptions; findings drive a `specforge-v2` package, not silent MVP edits. |

## Active Blockers

_None._ ITEM-001 and ITEM-002 implemented and committed. Build env notes (persist across all Stage 2 builds): (1) inherited `MSBuildSDKsPath` (RavenDB toolchain, pins SDK 8.0.403) must be cleared per build invocation (`$env:MSBuildSDKsPath = $null; dotnet build ...`) so `global.json`'s .NET 10 pin takes effect — otherwise NETSDK1045; (2) `dotnet` commands must run with cwd inside `D:\Work\specforge` (the RavenDB sibling repo's `global.json` pins an uninstalled 10.0.300). MCP harness note: drive write tools (create/set/delete) one request→response at a time; pipelining frames without awaiting responses can race the server (DEC-003 single-session, no cross-request concurrency). **Stage 2 complete — all 13 items implemented and committed; Stage 3 (first external adoption) is next.** Post-Stage-2 hygiene (2026-06-04): MIT `LICENSE` + GitHub Actions CI (`.github/workflows/ci.yml`) added; real-host validation passed on Claude Code 2.1.150 (`claude mcp add` → ✓ Connected, `tools/list` → 21 tools, `install_skills agent=all` wrote 14 SKILL.md). **Follow-ups resolved 2026-06-04:** (1) GitHub remote configured and pushed — public repo at `github.com/ArieSLV/specforge`, CI green; (2) DEC-001 amended in place (additive; still `Approved`) to record `claude mcp add` / `~/.claude.json` as the canonical registration path, with a user-scope `.mcp.json` fallback — done as an in-place amendment because the enforced state machine has no `Approved → Draft` transition; (3) `document_lifecycle.md` reconciled with the enforced `LifecycleState` (8 states + a Transitions section; stale Stage-0 labels removed); (4) ITEM-011 prose corrected (typed-exception count 12 → 13 — `SpecforgeInvalidArgumentException` / ITEM-006 had been omitted from its inventory and dependency graph).

## Ledger Files

| File | Status |
|---|---|
| `ledger/README.md` | Draft |
| `ledger/artifacts.md` | Draft |
| `ledger/items.md` | Draft |
| `ledger/commits.md` | Draft |
| `ledger/reviews.md` | Draft |
| `ledger/history.md` | Draft |

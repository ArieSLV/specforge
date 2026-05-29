# ITEM-009-LEDGER-APPEND-TOOLS - Standalone Ledger Append + Delete Tools

Status: Approved
Review owner: User
Depends on: `ITEM-003-ID-VALIDATOR`, `ITEM-007-DECISION-TOOLS`, `DEC-004-ID-SCHEME-CUSTOMIZATION`, `DEC-007-MVP-TOOL-SET`
Updates ledger rows: new `ART-ITEM-009`; new `CMT-NNN` rows for implementation commits; promotes `ART-LEDGER-COMMITS` from Placeholder to Draft on first runtime append

## Handoff Summary

Ship the five standalone ledger tools: three append operations (`append_history`, `append_review`, `append_commit`) and two delete operations (`delete_review`, `delete_commit`). All five are thin MCP wrappers around the ledger services introduced by ITEM-007, plus one new service — `ICommitLedgerService` — because ITEM-007 only introduced typed `CommitRow` but no service to write to `<package>/ledger/commits.md`. ITEM-009 has no new NuGet packages, no new error codes, and no new typed exceptions. The interesting design surfaces are: (1) `append_history` accepts a non-LedgerId literal target `(milestone)` or `(multiple)` to support project-wide events the dogfood already uses; (2) `delete_review`/`delete_commit` have **no lifecycle gate** (reviews and commits don't have lifecycle states) — the gate is just `confirm: true` per DEC-007 Delete Semantics; (3) per-target REV seq allocation and global CMT seq allocation are tombstone-tolerant by the simple "max+1 over existing rows" rule (ITEM-010's `TombstoneAwareIdAllocator` decorator verifies gap evidence later). Tool count: 15 → 20.

- Tool count: 15 → 20.
- 1 new ledger service (`ICommitLedgerService`); 0 new NuGet packages; 0 new error codes; 0 new typed exceptions.
- First runtime writer to `<package>/ledger/commits.md` — promotes `ART-LEDGER-COMMITS` to Draft on first append.
- `append_history` accepts `(milestone)` and `(multiple)` literal targets in addition to LedgerIds.

## Problem Slice

This item resolves "how does the AI explicitly post events into the ledger that aren't already produced as side-effects by `create_*`/`set_*_status`/`delete_*`?" — and the symmetric "how does the AI undo a typo'd review or commit row?"

Explicit non-goals (owned by other items):

- `validate` tool with tombstone-aware `validate(aspect=ids)` — `ITEM-010-VALIDATE-TOOL`. ITEM-009 produces `Deleted` history events with tombstone evidence; ITEM-010 consumes them.
- Centralized envelope-mapping audit — `ITEM-011-ERROR-ENVELOPE`. ITEM-009 reuses existing mappings; no new code added to the mapper.
- A `delete_history` tool — explicitly NOT in DEC-007. History is append-only as an audit-log invariant; typo recovery requires direct file edit (out-of-band of specforge).
- A `link_commit_to_item(commitId, itemId)` helper — out of MVP. `append_commit` already accepts a target LedgerId.
- A `list_history(filter)` / `list_reviews(target)` / `list_commits(target)` read tool — out of MVP. Read-side is covered by `get_decision`/`get_item` returning related rows in their payloads (ITEM-007/008) plus direct file reads.

## Terminology Used

- **Standalone append**: an MCP tool whose only effect is appending one ledger row + (for reviews and commits) one `Created` history event. Distinct from the multi-effect appends that happen as side-effects of `create_decision` / `set_decision_status(Approved)` / etc.
- **Literal target**: a non-LedgerId placeholder string allowed in the History target column. MVP accepts exactly two: `(milestone)` for project-wide events, `(multiple)` for events affecting multiple artifacts. Already used in the dogfood `history.md`.
- **Tombstone-tolerant allocation**: the seq allocator picks `max(existing) + 1` over existing rows. Deleted rows leave a gap but the next number is never reused. ITEM-007's `IReviewLedgerService.AppendAsync` already behaves this way; ITEM-009's new `ICommitLedgerService.AppendAsync` follows the same rule.

## Approved Decisions

- `DEC-004-ID-SCHEME-CUSTOMIZATION` — sections "Identifier Form" (composite REV; sequential CMT; git-ref column), "Counter Advancement" (tombstones never reused), "Identifier Stability".
- `DEC-007-MVP-TOOL-SET` — sections "Ledger (5)", "Delete Semantics", "Error-Code Catalog". Critical: the table explicitly lists `append_history`, `append_review`, `append_commit`, `delete_review`, `delete_commit` as the five ledger tools.
- `spec/shared/document_lifecycle.md` — confirms reviews and commits do NOT have lifecycle states (they're event records, not artifacts with status). This is the basis for "no lifecycle gate" on delete.

## Current Code State

After `ITEM-007-DECISION-TOOLS` + `ITEM-008-ITEM-TOOLS` land:

- `Specforge.Core` carries `Ledger/` with `LedgerRow`, `LedgerTable`, `LedgerTableParser`, `LedgerTableWriter`, three services (`IArtifactLedgerService`, `IHistoryLedgerService`, `IReviewLedgerService`), and four typed row records (`ArtifactRow`, `HistoryRow`, `ReviewRow`, `CommitRow`). `CommitRow` exists but no service writes to `<package>/ledger/commits.md`.
- `IReviewLedgerService` already exposes `AppendAsync(target, reviewer, outcome, notes)` (per-target seq allocation) and `RemoveAsync(LedgerId id)` and `FindByTargetAsync(LedgerId targetId)`. ITEM-009 reuses all three.
- `IHistoryLedgerService` already exposes `AppendAsync(targetId, eventName, detail)` and is append-only by design. ITEM-009 adds one overload (or one validation step) to accept literal targets `(milestone)` and `(multiple)`.
- `Specforge.Mcp` registers fifteen tools through ITEM-008. No ledger-side tools yet.
- `ToolExceptionMapper` already maps every code ITEM-009 needs (`specforge.id.*`, `specforge.tool.invalid_argument`, `specforge.package.*`, `specforge.tool.internal_error`).
- `<package>/ledger/commits.md` exists as a Placeholder file (per the ledger schema from `ledger/README.md`); ITEM-009 first promotes it to Draft via first append.

## Target Behavior

After this item is Done:

1. **`ICommitLedgerService`** (new):
   - `Task<LedgerId> AppendAsync(LedgerId targetId, string gitRef, string detail)` — allocates next global `CMT-NNN` over existing rows; writes a row to `<package>/ledger/commits.md` with columns `(LedgerId, Target, GitRef, Date, Detail)`.
   - `Task RemoveAsync(LedgerId id)` — removes the row.
   - `Task<CommitRow?> FindByLedgerIdAsync(LedgerId id)` — used by `delete_commit` to verify existence.
2. **`IHistoryLedgerService` literal-target acceptance** (extension to ITEM-007's contract):
   - The existing `AppendAsync(LedgerId targetId, ...)` overload stays for typed-LedgerId callers.
   - A new overload `AppendAsync(string targetSentinel, string eventName, string detail)` accepts the two literal forms `(milestone)` and `(multiple)`; throws `SpecforgeInvalidIdentifierException` for any other free-form string (the literal set is closed).
   - Internally both overloads end up writing to the same `<package>/ledger/history.md` table; the target cell is the literal text.
3. **`append_history(target: string, event: string, detail: string)`** — standalone write tool:
   - `target` is required. Accepts either a valid bare/qualified LedgerId (per DEC-004) or one of the two literal forms `(milestone)` / `(multiple)`. Other strings → `specforge.tool.invalid_argument` with `data.argument="target"`, `suggestion: "use a LedgerId form (e.g., ART-DEC-001) or one of the literal targets (milestone), (multiple)"`.
   - `event` is required, non-empty, ≤ 80 chars, single line.
   - `detail` is required, non-empty.
   - Multi-effect: 1 row append to history.md. (No history side-effect on append_history itself — appending history IS the operation; there's nothing to log about logging.)
   - Accepts `dryRun`.
4. **`append_review(target: string, reviewer: string, outcome: string, notes?: string)`** — standalone write tool:
   - `target` is required. Must be a bare/qualified LedgerId pointing to an existing `ART-DEC-NNN` or `ART-ITEM-NNN` row (validated by reading the items/artifacts ledger; missing → `specforge.id.not_found`). Other ART kinds may target reviews in future packages via `extraKinds`, but MVP restricts to DEC/ITEM targets to keep the contract simple.
   - `reviewer` is required, non-empty (typically `User` in the dogfood).
   - `outcome` is required, non-empty. Common values: `Approved`, `Requested changes`, `Refined`, `Withdrawn`. No enum gate at MVP — keeps it open for future outcome vocabularies.
   - `notes` is optional, free-form.
   - Multi-effect: 1 REV row append (per-target seq allocated by `IReviewLedgerService`) + 1 history event `Reviewed` with `detail=outcome` and `target=ART-DEC-NNN`/`ART-ITEM-NNN`.
   - Accepts `dryRun`.
5. **`append_commit(target: string, gitRef: string, detail: string)`** — standalone write tool:
   - `target` is required. Must be a bare/qualified LedgerId pointing to an existing artifact row (most commonly an `ART-ITEM-NNN` whose Stage 2 implementation is being recorded; can also be `ART-DEC-NNN` for documentation commits).
   - `gitRef` is required, non-empty, no whitespace, ≤ 64 chars. No SHA-shape regex (allows short SHAs, tags, branch names per organization convention). The validator rejects only obviously malformed inputs (empty, whitespace-only, too-long).
   - `detail` is required, non-empty, single-line summary of the commit's effect on `target`.
   - Multi-effect: 1 CMT row append (global seq allocated by `ICommitLedgerService`) + 1 history event `Implemented` with `detail=gitRef` and `target=ART-NNN`. On the very first append in a fresh repo, the implementation also promotes the `ART-LEDGER-COMMITS` row's Status from `Placeholder` to `Draft` in `<package>/ledger/artifacts.md` (a third side-effect, the once-per-package promotion event also logged via history).
   - Accepts `dryRun`.
6. **`delete_review(id: string, confirm: boolean, dryRun?: boolean)`** — standalone delete tool per DEC-007 Delete Semantics:
   - Require `confirm: true`. Without it, return `specforge.tool.invalid_argument` with `data.argument="confirm"`, `data.expected="true"`, `suggestion: "pass confirm=true to perform the delete; pass dryRun=true to preview"`.
   - `id` must be a valid composite REV form (`REV-<target>-<seq>` per DEC-004).
   - Verify row exists. Missing → `specforge.id.not_found` with `data.id`.
   - **No lifecycle gate.** Reviews have no lifecycle state per `spec/shared/document_lifecycle.md`. `SpecforgeDeleteForbiddenException` is NOT thrown by this tool.
   - Multi-effect: 1 REV row removal + 1 `Deleted` history event with `target=<target-of-deleted-review>` and `detail=REV-<target>-<seq> tombstoned by delete_review`.
   - Number is **tombstoned** — next `append_review` for the same target advances past the deleted seq.
7. **`delete_commit(id: string, confirm: boolean, dryRun?: boolean)`** — standalone delete tool per DEC-007 Delete Semantics:
   - Same shape as `delete_review` but for CMT.
   - `id` must be a valid sequential `CMT-NNN` form.
   - Verify row exists. Missing → `specforge.id.not_found`.
   - **No lifecycle gate.**
   - Multi-effect: 1 CMT row removal + 1 `Deleted` history event with `target=<target-of-deleted-commit>` and `detail=CMT-NNN (was <gitRef>) tombstoned by delete_commit`.
   - Number is **tombstoned** — next `append_commit` advances past the deleted seq.

## Invariants

- **`Specforge.Core` references no `ModelContextProtocol.*` assembly.** Architecture test from ITEM-001 still passes.
- **No new NuGet packages.** No new error codes. No new typed exceptions. No new embedded-resource globs.
- **History is append-only.** `IHistoryLedgerService` exposes no `RemoveAsync`. No `delete_history` tool exists. Typo recovery requires direct file edit (out-of-band of specforge).
- **Reviews and commits have no lifecycle.** Their delete tools never throw `SpecforgeDeleteForbiddenException`. The pre-Approved gate from DEC-007 Delete Semantics applies only to decisions and items.
- **REV seq is per-target; CMT seq is global per package.** Both allocators use the simple "max(existing) + 1" rule and are tombstone-tolerant by virtue of never reusing numbers.
- **`append_history` literal-target set is closed**: exactly `(milestone)` and `(multiple)` accepted. Extending this set is a future DEC change, not a per-package config.
- **`append_commit` target verification scope**: verifies the target row exists in the artifacts/items ledger. Does NOT verify the git ref exists in the repo (specforge is not a git client; the AI/user is responsible for accuracy).
- **`append_review` outcome is free-form.** No enum gate at MVP. ITEM-010's `validate(aspect=lifecycle)` may later flag unusual outcomes against expected vocabularies, but ITEM-009 does not.
- **First `append_commit` promotes `ART-LEDGER-COMMITS`** from Placeholder to Draft in `<package>/ledger/artifacts.md`. This is a one-time per-package event; idempotent (the second `append_commit` is a no-op for the promotion).

## Code Scope

**In scope (created or modified by this item):**

`Specforge.Core` (new):

- `Ledger/ICommitLedgerService.cs` — interface with `AppendAsync`, `RemoveAsync`, `FindByLedgerIdAsync`.
- `Ledger/CommitLedgerService.cs` — implementation over `LedgerTableParser`/`LedgerTableWriter`; same atomic temp-rename pattern as ITEM-007's services.
- `Ledger/HistoryLiteralTarget.cs` — `static readonly string[] AcceptedLiterals = ["(milestone)", "(multiple)"];` and helper `bool IsLiteralTarget(string s)`.

`Specforge.Core` (modifications):

- `Ledger/IHistoryLedgerService.cs` — add overload `Task AppendAsync(string literalTarget, string eventName, string detail)`. Throws `SpecforgeInvalidIdentifierException` for any string not in `HistoryLiteralTarget.AcceptedLiterals`. (If ITEM-007 already accepted any string target with no validation, ITEM-009 tightens validation to the closed literal set.)
- `Ledger/HistoryLedgerService.cs` — implement the new overload; reuse the same writer.

`Specforge.Mcp` (new):

- `Tools/Ledger/AppendHistoryTool.cs`
- `Tools/Ledger/AppendReviewTool.cs`
- `Tools/Ledger/AppendCommitTool.cs`
- `Tools/Ledger/DeleteReviewTool.cs`
- `Tools/Ledger/DeleteCommitTool.cs`

`Specforge.Mcp` (modifications):

- `Hosting/SpecforgeCoreServices.cs` — register `ICommitLedgerService` as singleton.
- `Program.cs` — register the five new ledger tools.

`Specforge.Tests` (new):

- `Ledger/CommitLedgerServiceTests.cs` — global CMT-NNN allocation; promotes Placeholder → Draft; round-trip byte-equivalence on commits.md.
- `Ledger/HistoryLedgerServiceLiteralTargetTests.cs` — `(milestone)` and `(multiple)` accepted; any other free-form string rejected with `SpecforgeInvalidIdentifierException`.
- `Tools/Ledger/AppendHistoryToolTests.cs` — LedgerId target + literal-target paths; invalid target rejected.
- `Tools/Ledger/AppendReviewToolTests.cs` — multi-effect verified (REV row + history event); per-target seq advance; non-existent target rejected; outcome free-form accepted.
- `Tools/Ledger/AppendCommitToolTests.cs` — multi-effect verified (CMT row + history event + first-call Placeholder→Draft promotion); global seq advance; idempotent promotion on subsequent calls; non-whitespace gitRef rule.
- `Tools/Ledger/DeleteReviewToolTests.cs` — confirm gate; non-existent rejected; row removal + history event; no lifecycle exception thrown; seq advances past tombstone.
- `Tools/Ledger/DeleteCommitToolTests.cs` — same shape as DeleteReviewToolTests but for CMT.

**Out of scope (deferred to later items):**

- `validate(aspect=ids)` consulting `Deleted` history events for tombstone verification — `ITEM-010-VALIDATE-TOOL`.
- A `delete_history` tool — not in DEC-007; history is append-only.
- `list_history` / `list_reviews` / `list_commits` read tools — not in DEC-007; reads via `get_decision`/`get_item` payloads + direct file reads.
- Extending the literal-target set beyond `(milestone)` / `(multiple)` — future DEC.
- Linking a commit to multiple targets — single target per CMT row in MVP.
- Outcome vocabulary enforcement on `append_review` — possible future validate-aspect.

## Test Scope

Seven new test classes. ~50-60 new tests. xUnit; temp-dir + fixture-ledger pattern from ITEM-007/008 reused. The first-call promotion test on `commits.md` is the only structurally unusual scenario.

## Test Plan

1. Implement `HistoryLiteralTarget` constants and helper; add tests in `HistoryLedgerServiceLiteralTargetTests`.
2. Extend `IHistoryLedgerService` with the literal-target overload; verify rejection of free-form strings.
3. Implement `ICommitLedgerService` + `CommitLedgerService` with the Placeholder→Draft promotion side-effect; add `CommitLedgerServiceTests`.
4. Implement the five ledger tools, wiring them through `Specforge.Mcp.Program.cs`.
5. Run `dotnet test`; verify all new tests + all prior-item tests + architecture test still pass.
6. Manual smoke: against the dogfood repo, `append_history target="(milestone)" event="Smoke test" detail="..."` succeeds; `append_review target="ART-DEC-001" reviewer="User" outcome="Refined" notes="..."` adds a new REV row and a history event; `append_commit target="ART-ITEM-001" gitRef="abc1234" detail="..."` adds a new CMT row and (if first ever) promotes `ART-LEDGER-COMMITS` to Draft; `delete_review` and `delete_commit` reverse the additions with proper tombstone history events.

## Test Evidence

- Console output of `dotnet build` (zero warnings).
- Console output of `dotnet test` (all pass).
- Architecture test still green.
- A transcript at `test/Specforge.Tests/Evidence/itm009-append-cycle.txt` showing `append_history` (LedgerId + literal target), `append_review`, `append_commit` (with Placeholder→Draft promotion), and the resulting `commits.md` initial-state content.
- A transcript at `test/Specforge.Tests/Evidence/itm009-delete-cycle.txt` showing `delete_review` and `delete_commit` round-trips with tombstone history events and post-delete seq advancement.

## Impact Assessment

| Aspect | Impact | Notes |
|---|---|---|
| MCP tool surface | Direct | 5 new tools; tool count 15 → 20. |
| Spec graph operations | Direct | Standalone ledger writes/deletes land. |
| Build pipeline | No impact | No new dependencies. |
| Core library boundary | No impact | All new code Core-side; architecture test still passes. |
| Error handling | No impact | No new codes; reuses existing argument/id-not-found mappings. |
| Configuration discovery | Indirect | All tools require an active package. |
| ID scheme | Direct | First runtime consumers of `IIdAllocator` for the CMT global seq and `IReviewLedgerService` per-target seq outside of ITEM-007's cascade path. |
| Schema versioning | No impact | DEC-006 mechanics unchanged. |
| Skill packaging | No impact | DEC-005 mechanics unchanged. |
| Lifecycle policy | Indirect | Reviews and commits explicitly have no lifecycle states; ITEM-009 codifies this by NOT calling `LifecycleStateMachine` in its delete tools. |
| Ledger structure | Direct | First runtime writer to `commits.md`; promotion of `ART-LEDGER-COMMITS` Placeholder→Draft. |
| Documentation | Indirect | Top-level README (ITEM-013) must cover the 5 tools' workflow. |
| External adoption | Indirect | An adopter who implements an item runs `append_commit` at the end; this is the only ledger tool every adopter is expected to call after every Stage 2 commit. |
| Test coverage scope | Direct | ~50-60 new tests + 2 evidence transcripts. |
| Performance | No measurable | Ledger files are short; appending one row is trivial. |
| Concurrency | No measurable | Single-session process; one tool call at a time. |
| Maintenance burden | Indirect | The closed literal-target set introduces a vocabulary that must be kept in sync with dogfood usage; if a third literal becomes useful, it's a DEC + spec change, not a config knob. |

## Validation

- **Build**: `dotnet build Specforge.sln -c Release` succeeds with zero warnings.
- **Test**: `dotnet test` succeeds; all new tests pass; all prior tests + architecture test still pass.
- **Boundary**: ITEM-001's architecture test still passes (no new MCP/Hosting refs in Core).
- **Append history literal**: `append_history target="(milestone)" event="X" detail="Y"` writes the row; `target="(unknown)"` returns `specforge.tool.invalid_argument` with `data.argument="target"`.
- **Append review multi-effect**: `append_review` against an `ART-ITEM-001` target writes the REV row AND a history `Reviewed` event in one call.
- **Append commit promotion**: against a fresh package whose `ART-LEDGER-COMMITS` is Placeholder, the first `append_commit` promotes it to Draft and writes the CMT row and a history event; the second `append_commit` writes only the CMT row + history event (no promotion).
- **Delete review no-lifecycle**: `delete_review` on a REV row associated with an Approved decision succeeds (no lifecycle exception thrown); the REV row is removed and a `Deleted` history event is appended.
- **Tombstone advancement**: after `delete_review REV-DEC-001-002`, the next `append_review target=ART-DEC-001 ...` produces `REV-DEC-001-003` (skipping the tombstoned 002, never reusing).
- **Delete `confirm` requirement**: both delete tools without `confirm: true` return `specforge.tool.invalid_argument` with `data.argument="confirm"`.

## Open Questions

- Whether `append_commit` should also accept an optional `date` argument (default = system clock UTC) for backfilling historic commits during a migration. Leaning yes — small additive arg; finalize during implementation.
- Whether `append_history` should accept `(security)` or `(performance)` as additional literal targets for cross-cutting events. Leaning no for MVP — keep the literal set minimal; extend via future DEC when a third pattern emerges from real usage.
- Whether `delete_review`/`delete_commit` should refuse if the target row's parent (e.g., the decision the review targets) is already Approved. Leaning no — the parent's status doesn't bear on whether a typo'd review row can be removed; DEC-007 placed the lifecycle gate on decisions/items only.
- Whether `append_review`'s outcome should be auto-mapped to a history event name (`Approved` → `Approved`, `Requested changes` → `Reviewed`, etc.) or always render as `Reviewed`. Leaning always-`Reviewed` for MVP (simpler; outcome detail goes in `detail` field); finalize during implementation.

## Done Criteria

The item is **Done** (post-Approved) when:

1. All files listed in "Code Scope (In scope)" exist at the specified paths.
2. No additions to `Directory.Packages.props`.
3. `dotnet build Specforge.sln -c Release` reports zero warnings.
4. `dotnet test` runs every new test plus all prior-item tests; all pass.
5. `Specforge.Core.dll` carries no `ModelContextProtocol.*` reference (architecture test).
6. `Specforge.Mcp` registers twenty tools (`list_packages`, `use_package`, `info`, `install_skills`, `init`, `list_decisions`, `get_decision`, `create_decision`, `set_decision_status`, `delete_decision`, `list_items`, `get_item`, `create_item`, `set_item_status`, `delete_item`, `append_history`, `append_review`, `append_commit`, `delete_review`, `delete_commit`).
7. Both smoke transcripts exist under `test/Specforge.Tests/Evidence/`.
8. A `CMT-NNN` row is appended to `ledger/commits.md` recording the implementation commit's short SHA — and this very append exercises the Placeholder→Draft promotion path end-to-end in production.
9. A history event is appended to `ledger/history.md` marking the transition.

## Links

- `../decisions/DEC-004-ID-SCHEME-CUSTOMIZATION.md` (approved) — sections "Identifier Form" (composite REV, sequential CMT, git-ref column), "Counter Advancement".
- `../decisions/DEC-007-MVP-TOOL-SET.md` (approved) — sections "Ledger (5)", "Delete Semantics", "Error-Code Catalog".
- `./ITEM-003-ID-VALIDATOR.md` (approved) — `IdValidator`, `IdParser` consumed for target validation; `IIdAllocator` consumed indirectly by `IReviewLedgerService` and `ICommitLedgerService`.
- `./ITEM-007-DECISION-TOOLS.md` (approved) — ledger row primitives, `IHistoryLedgerService`, `IReviewLedgerService`, `CommitRow` record, `ToolExceptionMapper` rows all reused as-is; `IHistoryLedgerService` extended with literal-target overload.
- `../../../shared/document_lifecycle.md` — confirms reviews and commits have no lifecycle states; basis for "no lifecycle gate on delete_review/delete_commit".
- `../../../shared/impact_assessment_checklist.md` — aspects checklist.
- `../ledger/README.md` — ledger schema; `commits.md` columns; `history.md` columns.
- `../ledger/history.md` — dogfood reference for `(milestone)` and `(multiple)` literal targets in real usage.

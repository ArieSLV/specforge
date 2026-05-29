# ITEM-010-VALIDATE-TOOL - Validate Tool, Four Aspect Validators, Tombstone-Aware ID Allocator

Status: Approved
Review owner: User
Depends on: `ITEM-003-ID-VALIDATOR`, `ITEM-007-DECISION-TOOLS`, `ITEM-008-ITEM-TOOLS`, `ITEM-009-LEDGER-APPEND-TOOLS`, `DEC-004-ID-SCHEME-CUSTOMIZATION`, `DEC-007-MVP-TOOL-SET`
Updates ledger rows: new `ART-ITEM-010`; new `CMT-NNN` rows for implementation commits

## Handoff Summary

Ship the final MCP tool of the MVP catalog: `validate(aspect: ids|links|lifecycle|impact-coverage|all)`. The tool is read-only — it produces a structured findings list and never mutates the spec graph. ITEM-010 introduces four aspect validators (one per non-`all` aspect), a `TombstoneAwareIdAllocator` decorator over ITEM-003's base allocator (deferred from ITEM-003/007/009 through three prior items), and an orchestrator service that runs aspects sequentially without short-circuiting. The structured `data.findings` payload returns `[{aspect, severity, target?, message, suggestion?}]` with severity ∈ `{error, warning, info}`. Critically, ITEM-010 formalizes the **tombstone-detail format contract** (`detail` of `Deleted` history events MUST start with `Tombstone <deleted-id>:`) — a forward-compatible refinement of ITEM-007/008/009's wording, locked here so the gap-verification logic has a deterministic parser target. Tool count: 20 → 21 — DEC-007's catalog complete.

- Tool count: 20 → 21. The MVP MCP surface is now feature-complete after this item.
- 0 new NuGet packages, 0 new error codes, 0 new typed exceptions, 0 new embedded-resource globs.
- 1 new public-contract refinement: tombstone-detail format (additive; not breaking).
- Findings-as-data, not findings-as-exceptions: even when validate detects errors, the MCP envelope is SUCCESS with `data.findings` populated. The AI consumes the list.
- Read-only: no `dryRun` (no mutations to preview).

## Problem Slice

This item resolves "how does the AI / a human reviewer confirm the spec graph is internally consistent, and how does specforge surface inconsistencies that the multi-effect tools couldn't prevent on their own?"

Explicit non-goals (owned by other items or out of MVP):

- Auto-repair / `validate --fix` mode — out of MVP. Mutations go through dedicated tools by design (DEC-007 separation-of-concerns). A future `repair_*` tool family could be added per finding category.
- Centralized envelope-mapping audit — `ITEM-011-ERROR-ENVELOPE`. ITEM-010 reuses existing mappings; no new codes added.
- Cross-package validation — `validate` operates on the active package only in MVP. Multi-package consistency checks defer to a future DEC.
- Outcome-vocabulary enforcement on `append_review` outcomes — possible future aspect, but ITEM-009 deliberately left outcome free-form; ITEM-010 does NOT lint outcomes.
- Skill content validation — `ITEM-004-EMBEDDED-SKILL-CATALOG` already does startup-time validation; ITEM-010 does not duplicate.
- Markdown-structure validation of decision/item files beyond required-section presence — out of MVP. Parsers throw on truly ill-formed files; ITEM-010 catches the throw and produces a finding rather than crashing.

## Terminology Used

- **Aspect**: a named family of consistency rules (`ids`, `links`, `lifecycle`, `impact-coverage`). The `all` value runs every aspect.
- **Finding**: a single consistency observation. Shape: `{aspect, severity, target?, message, suggestion?}`. `target` is optional (some findings are package-wide).
- **Severity**: `error` = a hard-rule violation that should block downstream work; `warning` = a soft-rule issue worth flagging; `info` = informational / forward-compatibility note.
- **Tombstone-detail format**: the locked format for `Deleted` history-event detail strings: `Tombstone <deleted-id>: <human-readable rest>`. Public contract for ITEM-007/008/009 implementations.
- **Tombstone-aware allocator**: ITEM-010's decorator over `IIdAllocator` (ITEM-003) that, in addition to `NextAsync`, exposes `VerifyGapsAsync(kind, observedIds)` returning the list of unexplained gaps. Used by the ids aspect validator.

## Approved Decisions

- `DEC-004-ID-SCHEME-CUSTOMIZATION` — sections "Identifier Form", "Counter Advancement" (tombstones never reused), "Identifier Stability" (numbers never re-used across kinds), "Cross-Package Qualifier" — drives both the ids and links aspect validators.
- `DEC-007-MVP-TOOL-SET` — sections "Validation (1)", "Error-Code Catalog" (validate uses no new codes; reuses `package.*` / `tool.invalid_argument`), "Tombstone-Aware `validate(aspect=ids)`" reference.
- `spec/shared/document_lifecycle.md` — authoritative source of valid states and transitions, encoded by `LifecycleStateMachine` from ITEM-007. Lifecycle aspect validator consults the state set.
- `spec/shared/spec_item_contract.md` — required-section list, encoded by `RequiredItemSections` from ITEM-008. Impact-coverage aspect validator consumes the list.

## Current Code State

After `ITEM-007` + `ITEM-008` + `ITEM-009` land:

- `Specforge.Core` has every parser/service needed: `IdParser`/`IdValidator`/`KindRegistry`/`IIdAllocator` (ITEM-003); `LedgerRow`/`LedgerTable`/`LedgerTableParser` + `IArtifactLedgerService`/`IHistoryLedgerService`/`IReviewLedgerService`/`ICommitLedgerService` (ITEM-007 + ITEM-009); `LifecycleStateMachine` (ITEM-007); `DecisionDocument`/`DecisionDocumentParser` (ITEM-007); `ItemDocument`/`ItemDocumentParser`/`RequiredItemSections` (ITEM-008).
- `Specforge.Mcp` registers twenty tools through ITEM-009. No validate tool yet.
- `IIdAllocator` (ITEM-003) is non-tombstone-aware: it scans existing IDs and returns `max+1`. No `VerifyGapsAsync` method exists.
- History `Deleted` events exist in the abstract (ITEM-007/008/009 specs all describe them) but no parser extracts the deleted ID from `detail`. ITEM-010 introduces the parser AND locks the format.
- `ToolExceptionMapper` already maps every code ITEM-010 needs (`specforge.package.*`, `specforge.tool.invalid_argument` for invalid `aspect` value).

## Target Behavior

After this item is Done:

1. **`TombstoneAwareIdAllocator`** (Core, decorator over `IIdAllocator`):
   - `Task<int> NextAsync(string kind)` — delegates to inner allocator (no change in behavior).
   - `Task<IReadOnlyList<int>> VerifyGapsAsync(string kind, IReadOnlyList<int> observedSeqs)` — given the seqs observed in the package (e.g., `[1, 2, 4, 5]`), returns the unexplained-gap list (`[3]`) by consulting `<package>/ledger/history.md` for `Deleted` events whose tombstone-detail prefix matches each gap.
   - Static helper `Maybe<string> TryExtractDeletedId(string historyDetail)` parses the `^Tombstone (<id>):` prefix; returns the captured ID or none.
2. **`ValidationFinding`** record:
   ```csharp
   record ValidationFinding(
       string Aspect,        // "ids" | "links" | "lifecycle" | "impact-coverage"
       string Severity,      // "error" | "warning" | "info"
       string? Target,       // bare or qualified LedgerId, or null for package-wide
       string Message,
       string? Suggestion);
   ```
3. **`ValidationResult`** record:
   ```csharp
   record ValidationResult(
       string AspectRequested,                       // echoes the input
       IReadOnlyList<ValidationFinding> Findings,    // ordered: aspect (ids, links, lifecycle, impact-coverage), severity (error, warning, info), target (lexicographic)
       int ErrorCount,
       int WarningCount,
       int InfoCount);
   ```
4. **`IIdsAspectValidator`** + implementation:
   - For each artifact-bearing source (decisions/, items/, ledger rows), extracts every identifier (LedgerIds, file-name IDs, embedded IDs in body refs).
   - Per-kind, computes the observed seq set.
   - Calls `TombstoneAwareIdAllocator.VerifyGapsAsync(kind, observedSeqs)` and emits `severity=error` finding for each unexplained gap.
   - Emits `severity=error` finding for any identifier that fails `IdValidator.Validate`.
   - Emits `severity=error` for any duplicate identifier across files.
   - Emits `severity=error` for any kind used outside the registered set (core 5 + `extraKinds`).
   - Emits `severity=error` for any reserved-kind violation in `extraKinds`.
5. **`ILinksAspectValidator`** + implementation:
   - For each decision file: parse via `DecisionDocumentParser`; check `Supersedes`/`Covers`/`Amends` references. Dangling reference (target doesn't exist and has no tombstone) → `severity=error`.
   - For each item file: parse via `ItemDocumentParser`; check `Depends on` references. Dangling reference → `severity=warning` (per ITEM-008 invariant: items can predate targets, so a dangling Depends on at any moment may be transient).
   - For each REV row: verify the `Target` cell references an existing artifact row. Dangling → `severity=error` (REV requires a real target).
   - For each CMT row: verify the `Target` cell references an existing artifact row. Dangling → `severity=error`.
   - For each `ART-*` row's `Depends on` cell: verify each referenced LedgerId exists. Dangling → `severity=warning`.
6. **`ILifecycleAspectValidator`** + implementation:
   - For each artifact file in `decisions/` and `items/`: read `Status:` header line; compare to the corresponding `ART-*` row's Status cell. Mismatch → `severity=error`.
   - For each Approved decision file: check that at least one `REV-DEC-NNN-*` row exists. Missing → `severity=warning` (Approved without review evidence is suspicious but not strictly invalid — manual approvals are still legitimate).
   - For each Approved item file: same check for `REV-ITEM-NNN-*`. Missing → `severity=warning`.
   - For each artifact: verify the file's `Status:` value is in the canonical set from `LifecycleStateMachine.RecognizedStates`. Unknown state → `severity=error`.
7. **`IImpactCoverageAspectValidator`** + implementation:
   - For each item file: parse via `ItemDocumentParser`; consult `MissingRequiredSections`.
     - If item is Approved AND missing sections is non-empty → `severity=error` (Approved gate violation: every Approved item must have every required section).
     - If item is Draft / Draft-for-user-review AND missing sections is non-empty → `severity=info` (informational; item is still in flight).
   - Empty (whitespace-only) required section is treated equivalently to missing.
   - Decision files are skipped (decisions don't have a required-section contract).
8. **`IValidationService`** orchestrator:
   - `Task<ValidationResult> ValidateAsync(string aspect)`.
   - For `aspect="all"`: runs every aspect validator in declared order (`ids`, `links`, `lifecycle`, `impact-coverage`); aggregates findings.
   - For a specific aspect: runs only that one.
   - Aggregates `ErrorCount` / `WarningCount` / `InfoCount` from the findings list.
   - Never short-circuits on errors.
   - Each aspect validator is resolved via DI as `IIdsAspectValidator` / `ILinksAspectValidator` / etc., making each independently testable and mockable.
9. **`validate(aspect: string)`** MCP tool:
   - JSON Schema: `aspect: enum["ids", "links", "lifecycle", "impact-coverage", "all"]`, required, no default. Invalid value → `specforge.tool.invalid_argument` with `data.argument="aspect"`, `data.expected=["ids", "links", "lifecycle", "impact-coverage", "all"]`.
   - Requires an active package (per ITEM-002 contract); selection-error envelope from ITEM-002 carries through unchanged.
   - On success: returns the `ValidationResult` JSON-serialized in `data` plus a human-readable `message` summarizing `"N errors, M warnings, K info"` (or `"clean"` if all zero).
   - No `dryRun` — the tool is read-only.
   - No side effects on the spec graph.

## Invariants

- **`Specforge.Core` references no `ModelContextProtocol.*` assembly.** Architecture test from ITEM-001 still passes.
- **Read-only tool.** `ValidateTool.ExecuteAsync` reads files but never writes. A dedicated test asserts file mtimes are unchanged after `validate(aspect=all)`.
- **Findings-as-data.** Even when the findings list contains errors, the MCP envelope is SUCCESS. The tool only throws via the envelope for malformed input (`tool.invalid_argument`) or operational failures (`tool.internal_error`, `package.*`).
- **`aspect=all` runs every aspect, no short-circuit.** Even if `ids` produces 1000 errors, `links`/`lifecycle`/`impact-coverage` still run. Rationale: the AI gets full context in one call; running 4 tools is a higher round-trip cost than one tool returning a long list.
- **Tombstone-detail format contract**: `Deleted` history events MUST have `detail` starting with `^Tombstone (<deleted-id>):`. ITEM-007's `delete_decision`, ITEM-008's `delete_item`, ITEM-009's `delete_review` / `delete_commit` implementations all conform. This is a forward-compatible refinement of those items' specs (which described detail wording illustratively, not contractually). ITEM-010 makes the format authoritative and provides the parser.
- **Cross-package out of scope.** `validate` only walks artifacts under the active package's `decisions/`, `items/`, `ledger/`. Cross-package links flagged as `severity=info` if encountered (not error — they may resolve under a different active-package selection).
- **Empty findings list is the all-clean signal.** Tooling and CI gates should check `data.errorCount == 0` rather than testing for a specific finding count.
- **`set_*_status(Approved)` does NOT call validate**. Per ITEM-007/008 design, the setters are mechanical. The AI is expected to call `validate(aspect=all)` (or the relevant aspect) explicitly before approving. This is encoded in the skill content owned by ITEM-012.

## Code Scope

**In scope (created or modified by this item):**

`Specforge.Core` (new):

- `Validation/ValidationFinding.cs` — record.
- `Validation/ValidationResult.cs` — record.
- `Validation/IIdsAspectValidator.cs` + `IdsAspectValidator.cs`.
- `Validation/ILinksAspectValidator.cs` + `LinksAspectValidator.cs`.
- `Validation/ILifecycleAspectValidator.cs` + `LifecycleAspectValidator.cs`.
- `Validation/IImpactCoverageAspectValidator.cs` + `ImpactCoverageAspectValidator.cs`.
- `Validation/IValidationService.cs` + `ValidationService.cs` — orchestrator.
- `Validation/TombstoneAwareIdAllocator.cs` — decorator over `IIdAllocator`; `VerifyGapsAsync` + `TryExtractDeletedId`.
- `Validation/TombstoneDetailFormat.cs` — static class with the canonical prefix regex and `Format(LedgerId deletedId, string humanRest) -> string` helper for the four delete tools to call (refactor opportunity in ITEM-007/008/009 implementations during Stage 2 — they call this helper instead of formatting the detail string themselves).

`Specforge.Mcp` (new):

- `Tools/Validation/ValidateTool.cs`.

`Specforge.Mcp` (modifications):

- `Hosting/SpecforgeCoreServices.cs` — register the four aspect validators, `IValidationService`, and `TombstoneAwareIdAllocator` (decorator pattern; replaces direct `IIdAllocator` resolution where validation needs the decorated version).
- `Program.cs` — register the validate tool.

`Specforge.Tests` (new):

- `Validation/TombstoneAwareIdAllocatorTests.cs` — explained gap, unexplained gap, `TryExtractDeletedId` parser.
- `Validation/IdsAspectValidatorTests.cs` — duplicate IDs, invalid format, reserved kind, unregistered kind, unexplained gap, explained gap (clean).
- `Validation/LinksAspectValidatorTests.cs` — dangling `Supersedes`, dangling `Depends on` (warning vs error severity split), dangling REV target, dangling CMT target.
- `Validation/LifecycleAspectValidatorTests.cs` — status mismatch (file vs ART row), Approved-without-REV (warning), unknown lifecycle state.
- `Validation/ImpactCoverageAspectValidatorTests.cs` — missing required section in Approved item (error), missing required section in Draft item (info), empty section equivalent to missing, decisions skipped.
- `Validation/ValidationServiceTests.cs` — `aspect=all` runs every aspect; ordering of findings; aggregate counts.
- `Tools/Validation/ValidateToolTests.cs` — JSON Schema enum gate; invalid aspect rejected with proper envelope; clean repo returns `errorCount=0`; sad-path repo produces expected finding list.
- `Validation/DogfoodValidationTests.cs` — runs `validate(aspect=all)` against this very repo (`spec/packages/specforge-mvp/`) and asserts `errorCount=0`. Self-consistency proof.

**Out of scope (deferred to later items or out of MVP):**

- Auto-repair / `validate --fix` mode — out of MVP.
- Cross-package validation — out of MVP.
- Outcome-vocabulary enforcement on append_review — possible future aspect.
- Markdown well-formedness lint beyond required-section presence — out of MVP.
- A `lint_skill_content` aspect — skill content is validated at server startup by ITEM-004's `SkillCatalogValidator`; ITEM-010 does not duplicate.

## Test Scope

Eight new test classes. ~80-100 new tests. xUnit; temp-dir + fixture-spec-graph pattern. The dogfood test (`DogfoodValidationTests`) is the marquee proof — running validate against the spec graph used to specify validate itself must come back clean. xUnit; temp-dir + fixture-config pattern from ITEM-005/006/007/008/009 reused for sad-path tests.

## Test Plan

1. Implement `TombstoneDetailFormat` constants and the `^Tombstone (<id>):` regex; write the parser unit tests.
2. Implement `TombstoneAwareIdAllocator` decorator with `VerifyGapsAsync`; test happy path (gap explained) and sad path (gap unexplained).
3. Implement `IdsAspectValidator`. Test each finding category in isolation.
4. Implement `LinksAspectValidator`. Test the severity-split between dangling `Supersedes` (error) and dangling `Depends on` (warning).
5. Implement `LifecycleAspectValidator`. Test status-mismatch, Approved-without-REV, unknown-state.
6. Implement `ImpactCoverageAspectValidator`. Test Approved-error vs Draft-info severity split.
7. Implement `ValidationService` orchestrator. Test `aspect=all` ordering and `aspect=single` filtering.
8. Implement `ValidateTool` with JSON Schema enum. Test the MCP-side argument-validation envelope.
9. Implement `DogfoodValidationTests`: against the live spec graph, assert `errorCount=0`. If the spec graph isn't clean, the test fails and forces a fix.
10. Run `dotnet test`; verify all new tests + all prior-item tests + architecture test still pass.
11. Manual smoke: against the dogfood repo, `validate(aspect=all)` returns `"clean"` (errorCount=warningCount=infoCount=0 — or a controlled set of warnings/infos that don't include errors).

## Test Evidence

- Console output of `dotnet build` (zero warnings).
- Console output of `dotnet test` (all pass).
- Architecture test still green.
- A transcript at `test/Specforge.Tests/Evidence/itm010-dogfood-validate-all.txt` showing the final `validate(aspect=all)` output against the spec graph used to specify ITEM-010 itself. Errors must be zero.
- A transcript at `test/Specforge.Tests/Evidence/itm010-sad-path.txt` showing validate output against a fixture spec graph deliberately seeded with one finding per finding-category, demonstrating the parser produces all expected findings.

## Impact Assessment

| Aspect | Impact | Notes |
|---|---|---|
| MCP tool surface | Direct | 1 new tool; tool count 20 → 21; DEC-007 catalog now complete. |
| Spec graph operations | Direct | First read-only consistency-checking tool. |
| Build pipeline | No impact | No new dependencies. |
| Core library boundary | No impact | All new code Core-side; architecture test still passes. |
| Error handling | No impact | No new error codes. Findings-as-data, not findings-as-exceptions. |
| Configuration discovery | Indirect | Requires active package; selection-error envelope from ITEM-002 carries through. |
| ID scheme | Direct | First runtime consumer of `TombstoneAwareIdAllocator`; locks tombstone-detail format contract. |
| Schema versioning | No impact | DEC-006 mechanics unchanged. |
| Skill packaging | No impact | DEC-005 mechanics unchanged. |
| Lifecycle policy | Direct | Lifecycle aspect validator codifies file-vs-ledger consistency rule first encoded in `LifecycleStateMachine` from ITEM-007. |
| Ledger structure | Direct | First consumer of `Deleted` history events as machine-readable data; the tombstone-detail format contract makes this possible. |
| Documentation | Indirect | Top-level README (ITEM-013) must cover the validate workflow. ITEM-012 `validate-spec-graph` skill content owns the AI-facing recipe. |
| External adoption | Direct | Adopters run `validate(aspect=all)` before `set_*_status(Approved)`; this is the only tool with a "called by everyone always" pattern. |
| Test coverage scope | Direct | ~80-100 new tests + 1 dogfood test + 2 evidence transcripts. |
| Performance | No measurable | Spec graphs are small; reading every file once + cross-checking is cheap. |
| Concurrency | No measurable | Single-session process. |
| Maintenance burden | Direct | Locks the tombstone-detail format as a public contract; every future delete tool must conform. The `TombstoneDetailFormat.Format(...)` helper centralizes the formatting so adding a new delete tool is one helper-call change. |

## Validation

- **Build**: `dotnet build Specforge.sln -c Release` succeeds with zero warnings.
- **Test**: `dotnet test` succeeds; all new tests pass; all prior tests + architecture test still pass.
- **Boundary**: ITEM-001's architecture test still passes (no new MCP/Hosting refs in Core).
- **Read-only invariant**: a test runs `validate(aspect=all)` against a fixture spec graph, captures file mtimes before and after, asserts no file changed.
- **Aspect=all aggregation**: a fixture spec graph deliberately seeded with one finding per aspect produces 4 findings (one per aspect, severity error) in declared order.
- **No short-circuit**: a fixture seeded with 1000 ids-aspect errors plus 1 links-aspect error still surfaces the links finding.
- **Tombstone-detail parsing**: a fixture history.md with detail `Tombstone DEC-005: file removed` results in DEC-005 being recognized as a tombstone; detail `Foo bar baz` does not match.
- **Dogfood clean**: `validate(aspect=all)` against the live `spec/packages/specforge-mvp/` returns `errorCount=0`. This is enforced as a unit test that runs every CI.
- **Tool catalog complete**: `Specforge.Mcp` registers 21 tools after this item lands. (Catalog enumeration test.)

## Open Questions

- Whether `validate(aspect=lifecycle)` should warn or remain silent when a `Placeholder` artifact has had no transitions for a long time. Leaning silent — there's no formal aging concept in MVP; could become a future `validate(aspect=hygiene)` aspect.
- Whether `validate(aspect=links)` should resolve cross-package references when both packages happen to be in the same `.specforge.json`. Leaning no for MVP — active-package scope only; raise the multi-package case as a future DEC.
- Whether the `ValidationResult` should be cached for the session to avoid repeated I/O on `aspect=all`. Leaning no — file reads are cheap and caching introduces invalidation hazards. Reconsider after first external adoption.
- Whether the tombstone-detail format should also include a timestamp suffix for forensic ordering (e.g., `Tombstone DEC-005 at 2026-05-28: ...`). Leaning no — the History row already has a Date column; redundant.
- Whether to surface a `data.summary` text rendering (multi-line bullet list of findings) alongside the structured `findings` array for direct AI consumption. Leaning yes — minor convenience; finalize during implementation. The summary uses tombstone-detail-style structured prefixes so the AI can both read it humanly and parse it deterministically.

## Done Criteria

The item is **Done** (post-Approved) when:

1. All files listed in "Code Scope (In scope)" exist at the specified paths.
2. No additions to `Directory.Packages.props`.
3. `dotnet build Specforge.sln -c Release` reports zero warnings.
4. `dotnet test` runs every new test plus all prior-item tests; all pass.
5. `Specforge.Core.dll` carries no `ModelContextProtocol.*` reference (architecture test).
6. `Specforge.Mcp` registers twenty-one tools (`list_packages`, `use_package`, `info`, `install_skills`, `init`, `list_decisions`, `get_decision`, `create_decision`, `set_decision_status`, `delete_decision`, `list_items`, `get_item`, `create_item`, `set_item_status`, `delete_item`, `append_history`, `append_review`, `append_commit`, `delete_review`, `delete_commit`, `validate`).
7. Both smoke transcripts exist under `test/Specforge.Tests/Evidence/`.
8. The dogfood validation test (`DogfoodValidationTests.ValidateAll_LiveSpecGraph_HasNoErrors`) passes against the live spec graph.
9. A `CMT-NNN` row is appended to `ledger/commits.md` recording the implementation commit's short SHA.
10. A history event is appended to `ledger/history.md` marking the transition.

## Links

- `../decisions/DEC-004-ID-SCHEME-CUSTOMIZATION.md` (approved) — sections "Identifier Form", "Counter Advancement", "Identifier Stability", "Cross-Package Qualifier".
- `../decisions/DEC-007-MVP-TOOL-SET.md` (approved) — sections "Validation (1)", "Delete Semantics" (tombstone evidence rule), "Error-Code Catalog".
- `./ITEM-003-ID-VALIDATOR.md` (approved) — `IdValidator`, `IdParser`, `KindRegistry`, `IIdAllocator` all consumed; `IIdAllocator` is the decoration target.
- `./ITEM-007-DECISION-TOOLS.md` (approved) — `DecisionDocumentParser`, `LifecycleStateMachine`, the three ledger services, `LedgerTableParser`, all consumed; the `delete_decision` `Deleted` history event format is contractually refined here.
- `./ITEM-008-ITEM-TOOLS.md` (approved) — `ItemDocumentParser`, `RequiredItemSections`, the items-table routing in `IArtifactLedgerService`, all consumed; the `delete_item` `Deleted` history event format is contractually refined here.
- `./ITEM-009-LEDGER-APPEND-TOOLS.md` (approved) — `ICommitLedgerService`, the `HistoryLiteralTarget` literal set, and the `delete_review` / `delete_commit` `Deleted` history event formats, all consumed; the latter two are contractually refined here.
- `../../../shared/document_lifecycle.md` — authoritative state set.
- `../../../shared/spec_item_contract.md` — required-section list.
- `../../../shared/impact_assessment_checklist.md` — aspects checklist.

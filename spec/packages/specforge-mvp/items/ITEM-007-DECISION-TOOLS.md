# ITEM-007-DECISION-TOOLS - Decision Tools, Ledger Row Primitives, and Lifecycle State Machine

Status: Approved
Review owner: User
Depends on: `ITEM-002-CONFIG-MODEL`, `ITEM-003-ID-VALIDATOR`, `ITEM-006-INIT-TOOL`, `DEC-004-ID-SCHEME-CUSTOMIZATION`, `DEC-007-MVP-TOOL-SET`
Updates ledger rows: new `ART-ITEM-007`; new `CMT-NNN` rows for implementation commits

## Handoff Summary

Ship the first spec-graph-aware tools: five MCP tools that read and mutate decision records. To do that, ITEM-007 introduces three reusable infrastructure layers that ITEM-008 (item tools) and ITEM-009 (ledger appends) will also depend on: (1) ledger row primitives (`LedgerRow`, `LedgerTable`, `LedgerTableParser`, `LedgerTableWriter`) backed by Markdig — the first item to introduce Markdig — for reading and rewriting markdown-table-format ledger files; (2) three high-level ledger services (`IArtifactLedgerService`, `IHistoryLedgerService`, `IReviewLedgerService`) that wrap the primitives with row-by-LedgerId semantics; (3) a `LifecycleStateMachine` Core service enforcing the transitions documented in `spec/shared/document_lifecycle.md`. The five tools — `list_decisions`, `get_decision`, `create_decision`, `set_decision_status`, `delete_decision` — wire these layers together. `delete_decision` enforces the pre-Approved state restriction from DEC-007 and cascades to associated REV rows; the new `SpecforgeDeleteForbiddenException` carries the lifecycle code (13th error mapping).

- Tool count: 5 → 10.
- First Markdig NuGet introduction.
- First `DecisionDocument` model + parser; structured `get_decision` payload.
- Multi-effect operations are atomic-ish: `create_decision` produces a file + `ART-DEC-NNN` row + history event in one call; `set_decision_status(Approved, reviewer, notes)` updates status + ART row + appends history event + appends `REV-DEC-NNN-NNN`; `delete_decision` removes file + ART row + cascades to REV rows + appends `Deleted` history event with tombstone evidence.

## Problem Slice

This item resolves "how does the AI manipulate decision records through specforge instead of through raw file edits?"

Explicit non-goals (owned by other items):

- Item-side tools (`list_items`, `get_item`, `create_item`, `set_item_status`, `delete_item`) — `ITEM-008-ITEM-TOOLS`. Same structural shape; ITEM-008 reuses every primitive introduced here.
- Standalone ledger appends (`append_history`, `append_review`, `append_commit`, `delete_review`, `delete_commit`) — `ITEM-009-LEDGER-APPEND-TOOLS`. ITEM-009 wraps the ledger services as standalone MCP tools.
- `validate` tool with tombstone-aware `validate(aspect=ids)` — `ITEM-010-VALIDATE-TOOL`. ITEM-010 wraps `IIdAllocator` (ITEM-003) in a tombstone-aware decorator.
- Centralized envelope-mapping audit — `ITEM-011-ERROR-ENVELOPE`. ITEM-007 adds its one new code to the mapper; ITEM-011 audits the full set later.
- The seven SKILL.md bodies that name these tools by verb — `ITEM-012-SKILL-CONTENT`. (Catalog amended from six to seven on 2026-05-28.)

## Terminology Used

- **Ledger row primitives**: `LedgerRow` (cell array), `LedgerTable` (header + rows), `LedgerTableParser`, `LedgerTableWriter`. Low-level. Markdig-backed.
- **Ledger services**: `IArtifactLedgerService`, `IHistoryLedgerService`, `IReviewLedgerService`. High-level row-by-LedgerId operations.
- **`DecisionDocument`**: parsed representation of a decision file (header + section map + raw markdown).
- **Lifecycle state machine**: encodes the directed graph of valid status transitions from `spec/shared/document_lifecycle.md`. Lives in `Specforge.Core`.
- **Multi-effect operation**: a single tool call that atomically maintains cross-file invariants (per DEC-007 Round 1 Q2).
- **REV cascade**: deleting a decision removes all `REV-DEC-NNN-*` rows that target it (DEC-007 Delete Semantics).

## Approved Decisions

- `DEC-004-ID-SCHEME-CUSTOMIZATION` — sections "Identifier Form", "Counter Advancement", "Identifier Stability", "File-Naming Convention" — drives slug generation in `create_decision` and the immutability rule enforced in `set_decision_status`.
- `DEC-007-MVP-TOOL-SET` — sections "Decisions (4)" / Decisions (5) post-amendment, "Delete Semantics", "Error-Code Catalog" (rows `lifecycle.delete_forbidden`, plus reused `id.*`, `package.*`, `tool.invalid_argument`).
- `DEC-003-RUNTIME-AND-ARCHITECTURE` — sections "Baseline Libraries" (`Markdig` introduction here), "Error Handling", "Dependency Direction".
- `DEC-008-INSTRUCTION-LAYER-DESIGN` — section "MVP Skill Catalog" — the `draft-decision` and `review-decision` skills (authored in ITEM-012) consume the five tools introduced here; tool names must match the skill references exactly.
- `spec/shared/document_lifecycle.md` — authoritative source for the lifecycle state machine encoded here.

## Current Code State

After `ITEM-006-INIT-TOOL` lands:

- `Specforge.Core` carries `Configuration/`, `Identifiers/`, `Skills/`, `Init/`, `Diagnostics/`, plus all typed exceptions through the 12th code (`SpecforgeSkillInstallException`).
- `Specforge.Mcp` registers five tools (`list_packages`, `use_package`, `info`, `install_skills`, `init`).
- `Specforge.Core.csproj` carries three embedded-resource globs (skills, shared, templates). No Markdig package reference yet.
- `ILedgerReader` from ITEM-003 reads identifiers from ledger files but does not surface full row content. ITEM-007 extends this with proper table parsing.
- No `DecisionDocument` model. No section parser. No lifecycle state machine.
- Decision files in this repo (`decisions/DEC-001-*`...`DEC-008-*`) and the dogfood ledger files serve as fixture data for the parser tests.

## Target Behavior

After this item is Done:

1. **`LedgerTableParser.Parse(string markdown)`** returns a `LedgerTable` with column headers and `LedgerRow` entries. Uses Markdig's pipe-table extension. Rows preserve their source-line range so writers can do byte-stable rewrites.
2. **`LedgerTableWriter.Write(LedgerTable table) -> string`** produces a markdown table that round-trips byte-equivalent through `Parse`. Idempotent: `Write(Parse(input))` equals `input` modulo whitespace normalization documented in the implementation notes.
3. **`IArtifactLedgerService`**:
   - `Task<ArtifactRow?> FindByLedgerIdAsync(LedgerId id)`
   - `Task AppendAsync(ArtifactRow row)`
   - `Task UpdateAsync(LedgerId id, Action<ArtifactRow> mutate)`
   - `Task RemoveAsync(LedgerId id)`
   - Operates on `<package>/ledger/artifacts.md`.
4. **`IHistoryLedgerService`**:
   - `Task AppendAsync(LedgerId targetId, string eventName, string detail)`
   - Dates from system clock (UTC, ISO `YYYY-MM-DD`).
   - History rows have no LedgerId column per DEC-004 (`(date, target, event)` composite key) — appends only, no updates or removals from this item.
5. **`IReviewLedgerService`**:
   - `Task<CompositeLedgerId> AppendAsync(LedgerId targetId, string reviewer, string outcome, string? notes)` — allocates the next per-target seq (`REV-<target>-NNN`).
   - `Task RemoveAsync(CompositeLedgerId id)` — used by `delete_review` (ITEM-009) and by `delete_decision` cascade.
   - `Task<IReadOnlyList<ReviewRow>> FindByTargetAsync(LedgerId targetId)` — used by cascade in `delete_decision`.
6. **`LifecycleStateMachine`**:
   - `bool IsValidTransition(string fromStatus, string toStatus)` — true for transitions listed in `spec/shared/document_lifecycle.md`.
   - `IReadOnlyList<string> AllowedTransitions(string fromStatus)` — returns the set of valid next statuses.
   - Lifecycle states recognized (per shared doc): `Not started`, `Placeholder`, `Draft`, `Draft for user review`, `Approved`, `Refined`, `Superseded`, `Withdrawn`.
7. **`DecisionDocument`** record:
   ```csharp
   record DecisionDocument(
       string Id,                                    // DEC-001
       string Title,                                 // "Distribution and Transport"
       string Status,
       string Date,
       string Owner,
       string ReviewOwner,
       string? Supersedes,
       string? Covers,
       string? Amends,
       IReadOnlyDictionary<string, string> Sections, // section name → markdown body
       string RawMarkdown,
       string AbsolutePath);
   ```
8. **`DecisionDocumentParser.Parse(string markdown, string absolutePath)`** walks the Markdig AST; identifies top-level `# DEC-NNN-TITLE - <Plain Title>` heading; extracts the front-matter-style metadata lines; identifies every `## <Section>` heading; collects the markdown body between section headings into the `Sections` map. Unknown top-level sections preserved (forward-compatible).
9. **`list_decisions(status?: string)`** — read-only tool returning `[{id, title, status, path}]` for the active package. Optional `status` filter exact-match.
10. **`get_decision(id: string)`** — read-only tool returning the full `DecisionDocument` JSON-serialized (raw markdown + parsed sections + metadata). Accepts bare or qualified identifier per DEC-004.
11. **`create_decision(title: string, status?: string)`** — multi-effect write:
    - Allocate next `DEC-NNN` via `IIdAllocator` (ITEM-003).
    - Generate title slug via `TitleSlug.FromTitle` (ITEM-003). On collision (rare; same title slug exists in another `DEC-NNN-*.md`), throw `specforge.tool.invalid_argument` with `suggestion: "amend the title to disambiguate"`.
    - Write `<package>/decisions/DEC-NNN-<SLUG>.md` from the template at `<resolved templates path>/decision_record.md` with placeholders substituted (`<NNN>`, `<TITLE>`, `<Plain Title>`, plus `Status:`, `Date:`, `Owner:`, `Review owner:` lines populated where DEC-007 supplies them).
    - Append `ART-DEC-NNN` row to artifacts ledger via `IArtifactLedgerService`.
    - Append `Created` history event.
    - Default `status` is `Draft`. `Draft for user review` permitted at creation. Approved-and-later rejected.
    - Accepts `dryRun`.
12. **`set_decision_status(id, status, reviewer?, notes?)`** — multi-effect write:
    - Read existing decision file; validate the transition via `LifecycleStateMachine`. Reject with `specforge.tool.invalid_argument` and `data.allowedTransitions` on invalid.
    - Update the `Status:` line in the decision file.
    - Update the corresponding `ART-DEC-NNN` row's Status cell.
    - Append history event `Approved` / `Superseded` / `Withdrawn` / etc. with `detail` derived from `reviewer + notes` when supplied.
    - When `status=Approved` and both `reviewer` and `notes` are supplied, append `REV-DEC-NNN-<next-seq>` to reviews ledger.
    - Accepts `dryRun`.
13. **`delete_decision(id, confirm, dryRun?)`** — multi-effect write per DEC-007 Delete Semantics:
    - Require `confirm: true`. Without it, return `specforge.tool.invalid_argument` with `data.argument="confirm"`, `data.expected="true"`, `suggestion: "pass confirm=true to perform the delete; pass dryRun=true to preview"`.
    - Read current status. If status ∉ `{Not started, Placeholder, Draft, Draft for user review}`, throw `SpecforgeDeleteForbiddenException` with `currentStatus`, `allowedStates`, code `specforge.lifecycle.delete_forbidden`.
    - Delete the decision file from disk.
    - Remove the `ART-DEC-NNN` row from artifacts ledger.
    - Find and remove every `REV-DEC-NNN-*` row from reviews ledger (cascade).
    - Append a `Deleted` history event with `detail` recording the tombstone evidence (so `validate(aspect=ids)` in ITEM-010 can distinguish legitimate gaps).
    - Number is **tombstoned** — next `create_decision` advances past the deleted number. Tombstone discovery in ITEM-003's `IIdAllocator` continues to be non-tombstone-aware (it scans existing IDs); ITEM-010's `TombstoneAwareIdAllocator` decorator consults history events to verify gaps are explained.

## Invariants

- **`Specforge.Core` references no `ModelContextProtocol.*` assembly.** Architecture test from ITEM-001 still passes.
- **Markdig is the only new dependency** beyond what ITEM-002/004/006 introduced. Pinned in `Directory.Packages.props`.
- **Ledger writes are atomic-ish via temp-rename** (same pattern as `ConfigWriter` in ITEM-006).
- **Round-tripping a ledger table through Parse → Write produces a byte-equivalent result** (no whitespace drift across re-writes — verified by a dedicated test).
- **Tombstone evidence is the `Deleted` history event** keyed by target LedgerId. No separate manifest file.
- **The `LifecycleStateMachine` enumerates allowed transitions, not states.** Adding a new lifecycle state in the future is a shared-doc change plus a state-machine table edit.
- **Identifier acceptance** in every tool that takes an `id` argument: bare (`DEC-001`) when inside the active package; qualified (`<package>/DEC-001`) for cross-package per DEC-004.
- **`delete_decision` cascade is one-level only**: REV rows directly targeting the deleted decision. Indirect references (e.g., another decision's "Supersedes" link) are NOT auto-edited; they will be surfaced by ITEM-010's `validate(aspect=links)` as dangling references.

## Code Scope

**In scope (created or modified by this item):**

`Specforge.Core` (new):

- `Ledger/LedgerRow.cs` — record with `IReadOnlyList<string> Cells`, original source-line indices.
- `Ledger/LedgerTable.cs` — record with `IReadOnlyList<string> Headers`, `IReadOnlyList<LedgerRow> Rows`, file path.
- `Ledger/LedgerTableParser.cs` — Markdig pipe-table parser → `LedgerTable`.
- `Ledger/LedgerTableWriter.cs` — `LedgerTable` → markdown string; pipe-table layout, column-width-normalized.
- `Ledger/ArtifactRow.cs`, `HistoryRow.cs`, `ReviewRow.cs`, `CommitRow.cs` — typed records mapping the columns of each ledger file.
- `Ledger/IArtifactLedgerService.cs` + `ArtifactLedgerService.cs`.
- `Ledger/IHistoryLedgerService.cs` + `HistoryLedgerService.cs`.
- `Ledger/IReviewLedgerService.cs` + `ReviewLedgerService.cs`.
- `Lifecycle/LifecycleState.cs` — string-typed wrapper (or enum + parser).
- `Lifecycle/LifecycleStateMachine.cs` — hard-coded transition graph keyed off the shared doc.
- `Documents/DecisionDocument.cs` — record.
- `Documents/DecisionDocumentParser.cs` — Markdig-backed section parser.
- `Documents/DecisionFileWriter.cs` — produces the initial template-substituted decision file.
- `Exceptions/SpecforgeDeleteForbiddenException.cs` — `ErrorCode = "specforge.lifecycle.delete_forbidden"`, carries `Id`, `CurrentStatus`, `AllowedStates`.

`Specforge.Core.csproj` (modifications):

- Add `<PackageReference Include="Markdig" />`.

`Directory.Packages.props` (modifications):

- Add `<PackageVersion Include="Markdig" Version="..." />` — pinned at impl time.

`Specforge.Mcp` (new):

- `Tools/Decisions/ListDecisionsTool.cs`
- `Tools/Decisions/GetDecisionTool.cs`
- `Tools/Decisions/CreateDecisionTool.cs`
- `Tools/Decisions/SetDecisionStatusTool.cs`
- `Tools/Decisions/DeleteDecisionTool.cs`

`Specforge.Mcp` (modifications):

- `Tools/ToolExceptionMapper.cs` — add `case` arm for `SpecforgeDeleteForbiddenException` → `specforge.lifecycle.delete_forbidden` with `data: {id, currentStatus, allowedStates}` and the documented suggestion.
- `Hosting/SpecforgeCoreServices.cs` — register the three ledger services + lifecycle state machine + decision parser/writer as singletons.
- `Program.cs` — register the five new decision tools.

`Specforge.Tests` (new):

- `Ledger/LedgerTableParserTests.cs` — happy path; ill-formed table; round-trip byte-equivalence.
- `Ledger/LedgerTableWriterTests.cs` — column-width normalization; reproducible output ordering.
- `Ledger/ArtifactLedgerServiceTests.cs` — Find/Append/Update/Remove against fixture artifacts.md.
- `Ledger/HistoryLedgerServiceTests.cs` — Append-only; date format.
- `Ledger/ReviewLedgerServiceTests.cs` — per-target seq allocation; FindByTarget; Remove.
- `Lifecycle/LifecycleStateMachineTests.cs` — every allowed transition from the shared doc; every forbidden transition.
- `Documents/DecisionDocumentParserTests.cs` — parses every DEC-001..008 fixture (dogfood); unknown sections preserved; ill-formed input throws.
- `Documents/DecisionFileWriterTests.cs` — produces the expected template instantiation.
- `Tools/Decisions/ListDecisionsToolTests.cs`
- `Tools/Decisions/GetDecisionToolTests.cs`
- `Tools/Decisions/CreateDecisionToolTests.cs` — multi-effect verified across file + ART row + history event.
- `Tools/Decisions/SetDecisionStatusToolTests.cs` — REV row appended on Approved + reviewer + notes.
- `Tools/Decisions/DeleteDecisionToolTests.cs` — cascade verified across file removal + ART removal + REV removal + history event; pre-Approved gate; `confirm` requirement.
- `Tools/ToolExceptionMapperTests.cs` — extend with `delete_forbidden` envelope.

**Out of scope (deferred to later items):**

- Item-side tools — `ITEM-008-ITEM-TOOLS`.
- Standalone ledger appends/deletes — `ITEM-009-LEDGER-APPEND-TOOLS`.
- Tombstone-aware ID allocation — `ITEM-010-VALIDATE-TOOL` (via `TombstoneAwareIdAllocator` decorator).
- Cross-package decision references (the parser accepts qualified IDs but resolution against multiple package configs is out of MVP).
- A `link_decisions(superseder, superseded)` helper — out of MVP (DEC-007 Open Question).

## Test Scope

Twelve new test classes plus an extension of `ToolExceptionMapperTests`. ~80-100 new tests. xUnit; temp-dir + fixture-config pattern from ITEM-005/006 reused.

## Test Plan

1. Add Markdig to `Directory.Packages.props` and `Specforge.Core.csproj`; verify build.
2. Implement ledger row primitives (`LedgerRow`, `LedgerTable`, `Parser`, `Writer`); confirm byte-equivalence round trip on every existing ledger file in this repo.
3. Implement the three ledger services using temp-dir fixtures.
4. Implement `LifecycleStateMachine` by walking `spec/shared/document_lifecycle.md` manually and encoding the transitions.
5. Implement `DecisionDocument` + parser; dogfood test against all eight existing decision files.
6. Implement `DecisionFileWriter` against the template at `spec/templates/decision_record.md`.
7. Implement the five decision tools, wiring them through `Specforge.Mcp.Program.cs`.
8. Extend `ToolExceptionMapper` with the new code.
9. Run `dotnet test`; verify all new tests + all prior-item tests + architecture test still pass.
10. Manual smoke: against the dogfood repo, `list_decisions` returns 8 entries; `get_decision DEC-001` returns the parsed structure; a sandboxed temp-repo run of `create_decision "Test Topic"` produces the expected three-file effect; `delete_decision` on the created Draft works.

## Test Evidence

- Console output of `dotnet build` (zero warnings).
- Console output of `dotnet test` (all pass).
- Architecture test still green.
- A transcript at `test/Specforge.Tests/Evidence/itm007-dogfood-parse.txt` confirming all 8 existing DEC files parse successfully and round-trip ledger tables produce byte-equivalent output.
- A transcript at `test/Specforge.Tests/Evidence/itm007-create-delete.txt` showing a `create_decision` → `set_decision_status(Draft for user review)` → `delete_decision` round-trip in a temp dir with the expected file/ledger/history side effects.

## Impact Assessment

| Aspect | Impact | Notes |
|---|---|---|
| MCP tool surface | Direct | 5 new tools; tool count 5 → 10. |
| Spec graph operations | Direct | First spec-graph-aware multi-effect operations land. |
| Build pipeline | Direct | First Markdig NuGet reference. |
| Core library boundary | No impact | All new code Core-side; architecture test still passes. |
| Error handling | Direct | 13th error code; multi-effect operations produce structured envelopes. |
| Configuration discovery | Indirect | All tools require an active package; selection-error envelope from ITEM-002 carries through. |
| ID scheme | Direct | First consumer of `IIdAllocator`, `TitleSlug.FromTitle`, `IdValidator` in tool args. |
| Schema versioning | No impact | DEC-006 mechanics unchanged. |
| Skill packaging | No impact | DEC-005 mechanics unchanged. |
| Lifecycle policy | Direct | First in-code encoding of `spec/shared/document_lifecycle.md`. |
| Ledger structure | Direct | First in-code parsing of ledger files; row primitives reused by ITEM-008/009. |
| Documentation | Indirect | Top-level README (ITEM-013) must cover the 5 tools' workflow. |
| External adoption | Indirect | An adopter who runs `init scaffold=true` and then `create_decision "X"` exercises this item end-to-end. |
| Test coverage scope | Direct | ~80-100 new tests + 2 evidence transcripts. |
| Performance | No measurable | Ledger files are short; parsing one is trivial. |
| Concurrency | No measurable | Single-session process; one tool call at a time. |
| Maintenance burden | Indirect | Ledger row primitives become the standard substrate for every future ledger-touching feature. |

## Validation

- **Build**: `dotnet build Specforge.sln -c Release` succeeds with zero warnings.
- **Test**: `dotnet test` succeeds; all new tests pass; all prior tests + architecture test still pass.
- **Boundary**: ITEM-001's architecture test still passes after Markdig introduction (Markdig lives only in Core; no MCP infrastructure pulled in).
- **Round-trip**: parsing every existing ledger file in this repo (`artifacts.md`, `items.md`, `reviews.md`, `history.md`, `commits.md`) and writing back produces byte-equivalent output.
- **Dogfood**: parsing every existing decision file (DEC-001..008) succeeds; the parsed `Sections` dictionary contains the expected section names.
- **Multi-effect atomicity**: `create_decision dryRun=true` enumerates the three planned writes; `create_decision dryRun=false` performs all three; an injected mid-call failure on the history append leaves the file + ART row intact but reports the partial state.
- **Lifecycle gate**: `set_decision_status` on a fixture decision with an invalid transition returns `tool.invalid_argument` with `data.allowedTransitions` listing the legitimate options.
- **Delete gate**: `delete_decision` on an Approved fixture returns `specforge.lifecycle.delete_forbidden`; on a Draft fixture with `confirm: true` removes the file + ART row + cascaded REV rows + appends `Deleted` history event.
- **Delete `confirm` requirement**: `delete_decision` without `confirm: true` returns `tool.invalid_argument` with `data.argument="confirm"`.

## Open Questions

- Markdig pinned version — chosen at impl time, recorded in `Directory.Packages.props`.
- Whether `DecisionDocument.Sections` should preserve trailing whitespace per section — leaning yes (byte-stability across re-parses); finalize during implementation.
- Whether the lifecycle state machine should be configurable per package (some adopters may want extra states) — defer; current shared doc defines a fixed set, and a future DEC can introduce per-package customization if requested.
- Whether `create_decision` should accept an optional `body` arg pre-populating the Decision section — leaning no (the template is intentionally minimal so the AI authors body sections via separate Edit calls); finalize during implementation.
- Whether `delete_decision`'s `Deleted` history event should include a hash of the deleted file's content (for forensics) — leaning no for MVP (git history is the forensic source).

## Done Criteria

The item is **Done** (post-Approved) when:

1. All files listed in "Code Scope (In scope)" exist at the specified paths.
2. `Specforge.Core.csproj` references Markdig; `Directory.Packages.props` pins the version.
3. `dotnet build Specforge.sln -c Release` reports zero warnings.
4. `dotnet test` runs every new test plus all prior-item tests; all pass.
5. `Specforge.Core.dll` carries no `ModelContextProtocol.*` reference (architecture test).
6. `Specforge.Mcp` registers ten tools (`list_packages`, `use_package`, `info`, `install_skills`, `init`, `list_decisions`, `get_decision`, `create_decision`, `set_decision_status`, `delete_decision`).
7. Both smoke transcripts exist under `test/Specforge.Tests/Evidence/`.
8. A `CMT-NNN` row is appended to `ledger/commits.md` recording the implementation commit's short SHA.
9. A history event is appended to `ledger/history.md` marking the transition.

## Links

- `../decisions/DEC-004-ID-SCHEME-CUSTOMIZATION.md` (approved) — sections "Identifier Form", "Counter Advancement", "Identifier Stability", "File-Naming Convention".
- `../decisions/DEC-007-MVP-TOOL-SET.md` (approved) — sections "Decisions (5)", "Delete Semantics", "Error-Code Catalog".
- `../decisions/DEC-003-RUNTIME-AND-ARCHITECTURE.md` (approved) — section "Baseline Libraries" (Markdig).
- `../decisions/DEC-008-INSTRUCTION-LAYER-DESIGN.md` (approved) — section "MVP Skill Catalog" — `draft-decision`, `review-decision` skills consume these tools.
- `../../../shared/document_lifecycle.md` — authoritative lifecycle states encoded in `LifecycleStateMachine`.
- `./ITEM-002-CONFIG-MODEL.md` (approved) — `IMcpTool` pattern + envelope rendering reused; active-package state.
- `./ITEM-003-ID-VALIDATOR.md` (approved) — `IIdAllocator`, `TitleSlug`, `IdValidator` consumed by `create_decision` and `delete_decision`.
- `./ITEM-006-INIT-TOOL.md` (approved) — `ConfigWriter`'s atomic temp-rename pattern reused for ledger writes.
- `../../../templates/decision_record.md` — template instantiated by `DecisionFileWriter`.
- `../../../templates/item_spec.md` — item template.
- `../../../shared/spec_item_contract.md` — required-section contract.
- `../../../shared/impact_assessment_checklist.md` — aspects checklist.

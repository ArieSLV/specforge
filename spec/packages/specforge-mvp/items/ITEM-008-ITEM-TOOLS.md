# ITEM-008-ITEM-TOOLS - Item Tools (List, Get, Create, Set-Status, Delete)

Status: Approved
Review owner: User
Depends on: `ITEM-003-ID-VALIDATOR`, `ITEM-006-INIT-TOOL`, `ITEM-007-DECISION-TOOLS`, `DEC-004-ID-SCHEME-CUSTOMIZATION`, `DEC-007-MVP-TOOL-SET`, `spec/shared/spec_item_contract.md`
Updates ledger rows: new `ART-ITEM-008`; new `CMT-NNN` rows for implementation commits

## Handoff Summary

Ship the item-side mirror of ITEM-007's decision tools: five MCP tools (`list_items`, `get_item`, `create_item`, `set_item_status`, `delete_item`) that read and mutate item spec records. ITEM-008 is intentionally a thin item — it consumes every infrastructure piece introduced by ITEM-007 (ledger row primitives, three ledger services, `LifecycleStateMachine`, `SpecforgeDeleteForbiddenException` with its envelope mapping). The only genuinely new code is the item-document layer: `ItemDocument` record, `ItemDocumentParser` (Markdig-backed, structurally analogous to `DecisionDocumentParser` but required-section-aware per `spec/shared/spec_item_contract.md`), and `ItemFileWriter` instantiating `spec/templates/item_spec.md`. Five new Mcp tool classes plus one DI registration line per tool. Tool count: 10 → 15. No new NuGet packages, no new error codes, no new embedded-resource globs, no new typed exceptions.

- Tool count: 10 → 15.
- No new dependencies.
- First `ItemDocument` model + parser; structured `get_item` payload.
- Multi-effect operations are atomic-ish (same shape as ITEM-007): `create_item` produces file + `ART-ITEM-NNN` row + history event; `set_item_status(Approved, reviewer, notes)` updates status + ART row + appends history event + appends `REV-ITEM-NNN-NNN`; `delete_item` removes file + ART row + cascades to REV rows + appends `Deleted` history event.

## Problem Slice

This item resolves "how does the AI manipulate item specs through specforge instead of through raw file edits?" — the symmetric companion to ITEM-007 for the items half of the spec graph.

Explicit non-goals (owned by other items):

- Decision-side tools — `ITEM-007-DECISION-TOOLS` (already approved). ITEM-008 reuses every primitive from ITEM-007.
- Standalone ledger appends (`append_history`, `append_review`, `append_commit`, `delete_review`, `delete_commit`) — `ITEM-009-LEDGER-APPEND-TOOLS`.
- `validate` tool with tombstone-aware `validate(aspect=ids)` and required-section gate `validate(aspect=impact-coverage)` — `ITEM-010-VALIDATE-TOOL`. `set_item_status(id, Approved)` here does **not** retroactively enforce required-section completeness; ITEM-010's validator owns that gate.
- Centralized envelope-mapping audit — `ITEM-011-ERROR-ENVELOPE`. ITEM-008 adds no new code; ITEM-011 audits the full set.
- Item-skill content (`draft-item`, `review-item`) — `ITEM-012-SKILL-CONTENT`. (Both skills land in ITEM-012; the catalog was amended from six to seven on 2026-05-28 to add `review-item` after the Stage 1 spec audit surfaced the missing item-side review skill.)

## Terminology Used

- **Item document**: a parsed item spec file. Required sections per `spec/shared/spec_item_contract.md`: `Handoff Summary`, `Problem Slice`, `Approved Decisions`, `Current Code State`, `Target Behavior`, `Invariants`, `Code Scope`, `Test Scope`, `Test Plan`, `Impact Assessment`, `Validation`, `Done Criteria`. Optional sections preserved verbatim.
- **Item header**: the top metadata block of an item file — `Status:`, `Review owner:`, `Depends on:`, `Updates ledger rows:` lines preceding the first `##` heading. The parser pulls these into typed properties.
- **Required-section contract**: the list of sections an item must contain to be Approved. `ItemDocumentParser` records which required sections are present and which are missing, but does NOT reject the file; downstream validators (ITEM-010) consume the missing-section list.

## Approved Decisions

- `DEC-004-ID-SCHEME-CUSTOMIZATION` — sections "Identifier Form", "Counter Advancement", "Identifier Stability", "File-Naming Convention" — drives ID allocation in `create_item` and the immutability rule enforced in `set_item_status`.
- `DEC-007-MVP-TOOL-SET` — sections "Items (5)", "Delete Semantics", "Error-Code Catalog" (rows reused: `lifecycle.delete_forbidden`, `id.*`, `package.*`, `tool.invalid_argument`).
- `spec/shared/spec_item_contract.md` — authoritative source of the required-section list encoded in `ItemDocumentParser`.
- `spec/shared/document_lifecycle.md` — same lifecycle states as decisions; `LifecycleStateMachine` from ITEM-007 reused unchanged.
- `spec/templates/item_spec.md` — template instantiated by `ItemFileWriter`.

## Current Code State

After `ITEM-007-DECISION-TOOLS` lands:

- `Specforge.Core` carries `Ledger/` (row primitives + three services), `Lifecycle/LifecycleStateMachine`, `Documents/DecisionDocument` + parser + writer, and `Exceptions/SpecforgeDeleteForbiddenException` (13th code).
- `Specforge.Mcp` registers ten tools (`list_packages`, `use_package`, `info`, `install_skills`, `init`, `list_decisions`, `get_decision`, `create_decision`, `set_decision_status`, `delete_decision`).
- `Markdig` is referenced by `Specforge.Core.csproj`. No additional NuGet packages needed.
- `ToolExceptionMapper` maps `SpecforgeDeleteForbiddenException` → `specforge.lifecycle.delete_forbidden` with the documented `data` payload.
- `ArtifactLedgerService.AppendAsync(ArtifactRow)` accepts any `ART-*` row; `ITEM-NNN` rows write to `<package>/ledger/items.md` (a distinct table) — see Invariants for the per-kind routing rule.
- `IIdAllocator.NextAsync("ITEM")` from ITEM-003 already supports the `ITEM` kind.
- No `ItemDocument` model. No item-specific parser. No item-side tool classes.
- Item files in this repo (`items/ITEM-001-*`...`ITEM-007-*` post-approval, ITEM-008 itself once Approved) serve as fixture data for the parser tests.

## Target Behavior

After this item is Done:

1. **`ItemDocument`** record:
   ```csharp
   record ItemDocument(
       string Id,                                    // ITEM-001
       string Title,                                 // "Solution Bootstrap"
       string Status,
       string ReviewOwner,
       IReadOnlyList<string> DependsOn,              // parsed from "Depends on:" line
       IReadOnlyList<string> UpdatesLedgerRows,      // parsed from "Updates ledger rows:" line
       IReadOnlyDictionary<string, string> Sections, // section name → markdown body
       IReadOnlyList<string> MissingRequiredSections,
       string RawMarkdown,
       string AbsolutePath);
   ```
2. **`ItemDocumentParser.Parse(string markdown, string absolutePath)`** walks the Markdig AST; identifies top-level `# ITEM-NNN-TITLE - <Plain Title>` heading; extracts the header lines (`Status:`, `Review owner:`, `Depends on:`, `Updates ledger rows:`); identifies every `## <Section>` heading; collects markdown body between section headings into `Sections`; computes `MissingRequiredSections` against the canonical list from `spec/shared/spec_item_contract.md`.
3. **`ItemFileWriter.Write(string id, string title, string status, IReadOnlyList<string>? dependsOn, string templateBody) -> string`** produces the initial template-substituted item file from `spec/templates/item_spec.md`. Substitutes `<NNN>`, `<TITLE>`, `<Plain Title>` placeholders and populates `Status:`/`Review owner:`/`Depends on:` lines.
4. **`list_items(status?: string)`** — read-only tool returning `[{id, title, status, path}]` for the active package. Optional `status` filter exact-match.
5. **`get_item(id: string)`** — read-only tool returning the full `ItemDocument` JSON-serialized (raw markdown + parsed sections + metadata + `missingRequiredSections`). Accepts bare or qualified identifier per DEC-004.
6. **`create_item(title: string, status?: string, dependsOn?: string[])`** — multi-effect write:
   - Allocate next `ITEM-NNN` via `IIdAllocator` (ITEM-003).
   - Generate title slug via `TitleSlug.FromTitle` (ITEM-003). On slug collision, throw `specforge.tool.invalid_argument` with `suggestion: "amend the title to disambiguate"`.
   - Validate each `dependsOn` entry parses as a valid identifier via `IdValidator` (ITEM-003). Existence is NOT verified here (kept consistent with the `create_decision` shape — links can predate the targets per the spec graph's intentional looseness).
   - Write `<package>/items/ITEM-NNN-<SLUG>.md` from the template at `<resolved templates path>/item_spec.md` with placeholders substituted plus header lines populated.
   - Append `ART-ITEM-NNN` row to items ledger (`<package>/ledger/items.md`) via `IArtifactLedgerService` (the artifact ledger service is target-table-aware via the `ART-<KIND>-` prefix — see Invariants).
   - Append `Created` history event with `target=ART-ITEM-NNN`.
   - Default `status` is `Draft`. `Placeholder` permitted at creation. `Draft for user review` permitted. Approved-and-later rejected.
   - Accepts `dryRun`.
7. **`set_item_status(id, status, reviewer?, notes?)`** — multi-effect write:
   - Read existing item file; validate the transition via `LifecycleStateMachine` (reused from ITEM-007). Reject with `specforge.tool.invalid_argument` and `data.allowedTransitions` on invalid.
   - Update the `Status:` line in the item file.
   - Update the corresponding `ART-ITEM-NNN` row's Status cell.
   - Append history event (`Approved`/`Superseded`/`Withdrawn`/...) with `detail` derived from `reviewer + notes` when supplied.
   - When `status=Approved` and both `reviewer` and `notes` are supplied, append `REV-ITEM-NNN-<next-seq>` to reviews ledger.
   - Does NOT enforce required-section completeness — that gate is ITEM-010's `validate(aspect=impact-coverage)`. Rationale: keep `set_item_status` mechanical; require an explicit pre-Approved `validate` call as the AI's responsibility per DEC-007 separation-of-concerns.
   - Accepts `dryRun`.
8. **`delete_item(id, confirm, dryRun?)`** — multi-effect write per DEC-007 Delete Semantics:
   - Require `confirm: true`. Without it, return `specforge.tool.invalid_argument` with `data.argument="confirm"`, `data.expected="true"`, `suggestion: "pass confirm=true to perform the delete; pass dryRun=true to preview"`.
   - Read current status. If status ∉ `{Not started, Placeholder, Draft, Draft for user review}`, throw `SpecforgeDeleteForbiddenException` (reused from ITEM-007) with `currentStatus`, `allowedStates`, code `specforge.lifecycle.delete_forbidden`.
   - Delete the item file from disk.
   - Remove the `ART-ITEM-NNN` row from items ledger.
   - Find and remove every `REV-ITEM-NNN-*` row from reviews ledger (cascade).
   - Append a `Deleted` history event with `detail` recording the tombstone evidence (so `validate(aspect=ids)` in ITEM-010 can distinguish legitimate gaps).
   - Number is **tombstoned** — next `create_item` advances past the deleted number. Tombstone discovery is the responsibility of ITEM-010's `TombstoneAwareIdAllocator` decorator (which consults history events). ITEM-003's base allocator continues to scan existing IDs.

## Invariants

- **`Specforge.Core` references no `ModelContextProtocol.*` assembly.** Architecture test from ITEM-001 still passes.
- **No new NuGet packages.** Markdig (introduced by ITEM-007) is sufficient.
- **No new error codes.** All paths reuse codes mapped in `ToolExceptionMapper` after ITEM-007.
- **`IArtifactLedgerService` is target-table-aware**: rows with kind `DEC` go to `<package>/ledger/artifacts.md` (the cross-kind artifact table); rows with kind `ITEM` go to `<package>/ledger/items.md` (the items-only mirror table per current ledger layout). Concretely: the service inspects the `LedgerId`'s kind prefix (`ART-DEC-*` vs `ART-ITEM-*`) and routes to the configured file path. If this routing is not already implemented by ITEM-007, ITEM-008 finishes the work (lightweight — adds a kind-keyed path lookup; no new public API). **This is the one production-code surface ITEM-008 may modify in Core beyond pure additions.**
- **`LifecycleStateMachine` is reused unchanged** — both decisions and items share the lifecycle state set.
- **Required-section list lives in one place**: `spec/shared/spec_item_contract.md` is the authoritative source; `ItemDocumentParser` hard-codes the list at compile time (no runtime parsing of the shared doc). A contract-drift test (see Test Scope) ensures the hard-coded list matches the shared doc.
- **Identifier acceptance** in every tool that takes an `id` argument: bare (`ITEM-001`) when inside the active package; qualified (`<package>/ITEM-001`) for cross-package per DEC-004.
- **`delete_item` cascade is one-level only**: REV rows directly targeting the deleted item. Indirect references (e.g., another item's `Depends on` link) are NOT auto-edited; they will be surfaced by ITEM-010's `validate(aspect=links)` as dangling references.
- **`create_item` does not validate `dependsOn` target existence**: spec-graph references can predate their targets. Existence is enforced by ITEM-010's `validate(aspect=links)`, not at create time.

## Code Scope

**In scope (created or modified by this item):**

`Specforge.Core` (new):

- `Documents/ItemDocument.cs` — record matching the shape in "Target Behavior" §1.
- `Documents/ItemDocumentParser.cs` — Markdig-backed; required-section-aware.
- `Documents/ItemFileWriter.cs` — template-substituted from `spec/templates/item_spec.md`.
- `Documents/RequiredItemSections.cs` — `static readonly IReadOnlyList<string>` holding the canonical required-section names from `spec_item_contract.md` (single source of truth in code).

`Specforge.Core` (modifications):

- `Ledger/ArtifactLedgerService.cs` — if ITEM-007 implemented this as artifacts-only, extend it with target-table routing keyed on the `ART-<KIND>-*` prefix so `ART-ITEM-*` rows land in `<package>/ledger/items.md` (the existing items-table layout) and `ART-DEC-*` rows land in `<package>/ledger/artifacts.md` (the existing cross-kind table). Configuration of the per-kind file path lives in `Configuration/PackageContext` from ITEM-002. If ITEM-007 already implemented this routing, ITEM-008 does nothing here.

`Specforge.Mcp` (new):

- `Tools/Items/ListItemsTool.cs`
- `Tools/Items/GetItemTool.cs`
- `Tools/Items/CreateItemTool.cs`
- `Tools/Items/SetItemStatusTool.cs`
- `Tools/Items/DeleteItemTool.cs`

`Specforge.Mcp` (modifications):

- `Hosting/SpecforgeCoreServices.cs` — register `ItemDocumentParser` and `ItemFileWriter` as singletons.
- `Program.cs` — register the five new item tools.

`Specforge.Tests` (new):

- `Documents/ItemDocumentParserTests.cs` — parses every existing `ITEM-001..008` fixture (dogfood); detects missing required sections; unknown sections preserved; ill-formed input throws.
- `Documents/ItemFileWriterTests.cs` — produces the expected template instantiation; populates `Depends on:` correctly when given multi-element array.
- `Documents/RequiredItemSectionsContractTests.cs` — reads `spec/shared/spec_item_contract.md` at test time and asserts the in-code list matches; protects against drift.
- `Tools/Items/ListItemsToolTests.cs`
- `Tools/Items/GetItemToolTests.cs`
- `Tools/Items/CreateItemToolTests.cs` — multi-effect verified across file + ART row + history event; `dependsOn` array round-trips.
- `Tools/Items/SetItemStatusToolTests.cs` — REV row appended on Approved + reviewer + notes; required-section gate is NOT applied here (assert that a setter on an incomplete item still succeeds — explicit non-enforcement test).
- `Tools/Items/DeleteItemToolTests.cs` — cascade verified across file removal + ART removal + REV removal + history event; pre-Approved gate; `confirm` requirement.
- `Ledger/ArtifactLedgerServiceItemRoutingTests.cs` — if the routing change in Code Scope is needed: `ART-DEC-*` row writes to `artifacts.md`; `ART-ITEM-*` row writes to `items.md`.

**Out of scope (deferred to later items):**

- Standalone ledger appends/deletes — `ITEM-009-LEDGER-APPEND-TOOLS`.
- Required-section completeness gate on Approved transitions — `ITEM-010-VALIDATE-TOOL` (`aspect=impact-coverage`).
- Tombstone-aware ID allocation — `ITEM-010-VALIDATE-TOOL` (via `TombstoneAwareIdAllocator` decorator).
- Cross-package item references at runtime — the parser accepts qualified IDs but multi-package resolution is out of MVP.
- A `link_items(parent, child)` helper or `Depends on:` mutation tool — out of MVP.

## Test Scope

Eight or nine new test classes (depending on whether the artifact-routing change is needed). ~60-80 new tests. xUnit; temp-dir + fixture-config pattern from ITEM-005/006/007 reused. The contract-drift test (`RequiredItemSectionsContractTests`) is the one unusual case — it reads a shared spec file at test time rather than relying on an in-test fixture.

## Test Plan

1. Implement `RequiredItemSections` static list; write the contract-drift test first (red/green: the test should pass on day one).
2. Implement `ItemDocument` record + `ItemDocumentParser`; dogfood against all existing `ITEM-001..007` files (after ITEM-007 is Approved + its file's header reflects approved status).
3. Implement `ItemFileWriter` against `spec/templates/item_spec.md`.
4. If the artifact-routing change is needed (i.e., ITEM-007 did not already implement it): extend `ArtifactLedgerService` and add `ArtifactLedgerServiceItemRoutingTests`.
5. Implement the five item tools, wiring them through `Specforge.Mcp.Program.cs`.
6. Run `dotnet test`; verify all new tests + all prior-item tests + architecture test still pass.
7. Manual smoke: against the dogfood repo, `list_items` returns ≥8 entries; `get_item ITEM-001` returns the parsed structure with empty `missingRequiredSections`; a sandboxed temp-repo run of `create_item "Test Topic" dependsOn=["DEC-001"]` produces the expected three-file effect; `delete_item` on the created Draft works.

## Test Evidence

- Console output of `dotnet build` (zero warnings).
- Console output of `dotnet test` (all pass).
- Architecture test still green.
- A transcript at `test/Specforge.Tests/Evidence/itm008-dogfood-parse.txt` confirming every existing item file parses successfully with empty `MissingRequiredSections` for Approved items.
- A transcript at `test/Specforge.Tests/Evidence/itm008-create-delete.txt` showing a `create_item` → `set_item_status(Draft for user review)` → `delete_item` round-trip in a temp dir with the expected file/ledger/history side effects.

## Impact Assessment

| Aspect | Impact | Notes |
|---|---|---|
| MCP tool surface | Direct | 5 new tools; tool count 10 → 15. |
| Spec graph operations | Direct | Item-side multi-effect operations land. |
| Build pipeline | No impact | No new dependencies. |
| Core library boundary | No impact | All new code Core-side; architecture test still passes. |
| Error handling | No impact | No new codes; reuses `SpecforgeDeleteForbiddenException` and existing argument-error codes. |
| Configuration discovery | Indirect | All tools require an active package; selection-error envelope from ITEM-002 carries through. |
| ID scheme | Direct | First consumer of `IIdAllocator("ITEM")` outside ITEM-003's tests. |
| Schema versioning | No impact | DEC-006 mechanics unchanged. |
| Skill packaging | No impact | DEC-005 mechanics unchanged. |
| Lifecycle policy | Direct | First in-code consumer of `LifecycleStateMachine` for items (decisions consumed it first in ITEM-007). |
| Ledger structure | Direct | First in-code consumer of items-table routing in `IArtifactLedgerService`. |
| Documentation | Indirect | Top-level README (ITEM-013) must cover the 5 tools' workflow. |
| External adoption | Indirect | An adopter who runs `init scaffold=true`, then `create_decision`, then `create_item dependsOn=[that-decision]` exercises this item end-to-end. |
| Test coverage scope | Direct | ~60-80 new tests + 1 contract-drift test + 2 evidence transcripts. |
| Performance | No measurable | Item files are short; parsing one is trivial. |
| Concurrency | No measurable | Single-session process; one tool call at a time. |
| Maintenance burden | Indirect | The required-section contract-drift test couples the test suite to a shared doc; any future amendment to the contract requires updating both the doc and the in-code list. Intentional. |

## Validation

- **Build**: `dotnet build Specforge.sln -c Release` succeeds with zero warnings.
- **Test**: `dotnet test` succeeds; all new tests pass; all prior tests + architecture test still pass.
- **Boundary**: ITEM-001's architecture test still passes (no new MCP/Hosting refs in Core).
- **Contract drift**: `RequiredItemSectionsContractTests` reads `spec/shared/spec_item_contract.md` and asserts the in-code required-section list is exactly the same set.
- **Dogfood parse**: parsing every existing item file (`ITEM-001..007` and ITEM-008 itself once Approved) succeeds; the parsed `Sections` dictionary contains the expected section names; `MissingRequiredSections` is empty for all Approved items.
- **Multi-effect atomicity**: `create_item dryRun=true` enumerates the three planned writes; `create_item dryRun=false` performs all three.
- **Required-section non-enforcement at setter**: `set_item_status` on a fixture item missing several required sections still succeeds when invoked with a valid transition (asserts the deliberate split-of-concern with ITEM-010).
- **Lifecycle gate**: `set_item_status` on a fixture item with an invalid transition returns `tool.invalid_argument` with `data.allowedTransitions`.
- **Delete gate**: `delete_item` on an Approved fixture returns `specforge.lifecycle.delete_forbidden`; on a Draft fixture with `confirm: true` removes the file + ART row + cascaded REV rows + appends `Deleted` history event.
- **Delete `confirm` requirement**: `delete_item` without `confirm: true` returns `tool.invalid_argument` with `data.argument="confirm"`.
- **Items-table routing**: `create_item` writes its `ART-ITEM-NNN` row to `<package>/ledger/items.md`, not `artifacts.md`.

## Open Questions

- Whether `create_item` should also validate that each `dependsOn` entry's kind is in the package's `KindRegistry` (currently rejects only on shape, not kind-vs-package-extraKinds). Leaning yes — small additional check, prevents typos at create time. Finalize during implementation.
- Whether `get_item`'s `MissingRequiredSections` payload should also flag empty (whitespace-only) required sections — leaning yes; finalize during implementation.
- Whether `set_item_status(Approved)` should warn (not reject) when `MissingRequiredSections` is non-empty, surfacing the list in the success envelope's `data.warnings` — leaning no for MVP (keeps the setter strictly mechanical; surfacing warnings is ITEM-010's job).
- Whether `create_item`'s template path should accept a per-package override — out of MVP; current behavior uses the embedded canonical template at the resolved templates path from ITEM-006.

## Done Criteria

The item is **Done** (post-Approved) when:

1. All files listed in "Code Scope (In scope)" exist at the specified paths.
2. No additions to `Directory.Packages.props`.
3. `dotnet build Specforge.sln -c Release` reports zero warnings.
4. `dotnet test` runs every new test plus all prior-item tests; all pass.
5. `Specforge.Core.dll` carries no `ModelContextProtocol.*` reference (architecture test).
6. `Specforge.Mcp` registers fifteen tools (`list_packages`, `use_package`, `info`, `install_skills`, `init`, `list_decisions`, `get_decision`, `create_decision`, `set_decision_status`, `delete_decision`, `list_items`, `get_item`, `create_item`, `set_item_status`, `delete_item`).
7. Both smoke transcripts exist under `test/Specforge.Tests/Evidence/`.
8. A `CMT-NNN` row is appended to `ledger/commits.md` recording the implementation commit's short SHA.
9. A history event is appended to `ledger/history.md` marking the transition.

## Links

- `../decisions/DEC-004-ID-SCHEME-CUSTOMIZATION.md` (approved) — sections "Identifier Form", "Counter Advancement", "Identifier Stability", "File-Naming Convention".
- `../decisions/DEC-007-MVP-TOOL-SET.md` (approved) — sections "Items (5)", "Delete Semantics", "Error-Code Catalog".
- `./ITEM-003-ID-VALIDATOR.md` (approved) — `IIdAllocator`, `TitleSlug`, `IdValidator` consumed by `create_item` and `delete_item`.
- `./ITEM-006-INIT-TOOL.md` (approved) — `ConfigWriter`'s atomic temp-rename pattern reused for ledger writes (via ITEM-007's `LedgerTableWriter`).
- `./ITEM-007-DECISION-TOOLS.md` (approved) — ledger row primitives (`LedgerRow`, `LedgerTable`, `LedgerTableParser`, `LedgerTableWriter`), three ledger services, `LifecycleStateMachine`, `SpecforgeDeleteForbiddenException`, and `ToolExceptionMapper` rows reused as-is.
- `../../../shared/spec_item_contract.md` — required-section list encoded in `RequiredItemSections`.
- `../../../shared/document_lifecycle.md` — authoritative lifecycle states (already encoded in `LifecycleStateMachine` from ITEM-007).
- `../../../templates/item_spec.md` — template instantiated by `ItemFileWriter`.
- `../../../shared/impact_assessment_checklist.md` — aspects checklist.

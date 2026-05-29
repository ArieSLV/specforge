# ITEM-003-ID-VALIDATOR - ID Validator, Kind Registry, and Allocator

Status: Approved
Review owner: User (approved 2026-05-28)
Depends on: `ITEM-001-SOLUTION-BOOTSTRAP`, `ITEM-002-CONFIG-MODEL`, `DEC-004-ID-SCHEME-CUSTOMIZATION`, `DEC-007-MVP-TOOL-SET`
Updates ledger rows: new `ART-ITEM-003`; new `CMT-NNN` rows for implementation commits

## Handoff Summary

Consolidate `DEC-004-ID-SCHEME-CUSTOMIZATION` into a set of reusable `Specforge.Core` services that every subsequent identifier-handling item depends on. Five components: `IdParser` (string → parsed Identifier or null), `IdValidator` (throws typed exceptions on shape violations), `TitleSlug` (validates and normalizes filename title slugs), `KindRegistry` (knows the five core kinds plus per-package `extraKinds`, enforces reserved-kind exclusion), `IIdAllocator` (computes next-free-number per kind per package, throws on kind exhaustion). Adds three new typed Core exceptions and their `ToolExceptionMapper` rows in `Specforge.Mcp`. Retroactively tightens ITEM-002's `ConfigValidator` to invoke the reserved-kind check on `extraKinds` at parse time.

- Core-only item. No new MCP tool ships.
- Consumed by ITEM-006 (`init` writes `extraKinds`), ITEM-007 (`create_decision` allocates the next DEC-NNN), ITEM-008 (`create_item` allocates the next ITEM-NNN), ITEM-009 (`append_review`/`append_commit` allocate composite/`CMT` numbers), ITEM-010 (`validate(aspect=ids)` audits identifier health).
- Defers tombstone-aware gap detection to ITEM-010 (this item produces the allocator interface and a non-tombstone-aware default; ITEM-010 wraps it with tombstone logic).

## Problem Slice

This item resolves "how does specforge parse, validate, and allocate identifiers consistently across every tool that touches a spec entity?"

Explicit non-goals (owned by other items):

- Tombstone-aware sequential-gap detection — `ITEM-010-VALIDATE-TOOL`. ITEM-003 ships a basic allocator that scans current artifacts; ITEM-010 layers the `Deleted`-history-event consultation on top.
- Cross-link integrity (`validate(aspect=links)`) — `ITEM-010`.
- Any MCP tool surfacing these capabilities — none in this item. The exceptions flow through any tool that later calls the validator.
- The `delete_*` lifecycle state check (`specforge.lifecycle.delete_forbidden`) — `ITEM-007`/`ITEM-008`/`ITEM-009`. ITEM-003 produces only the three identifier-related codes (`id.invalid`, `id.kind_reserved`, `id.kind_exhausted`).

## Terminology Used

- **Atomic identifier**: `<KIND>-<NUMBER>` where `KIND` is 2-6 uppercase letters and `NUMBER` is exactly 3 digits (DEC-004).
- **Composite identifier**: `REV-<KIND>-<NUMBER>-<SEQ>` where the inner `<KIND>-<NUMBER>` is a target identifier and `<SEQ>` is a 3-digit per-target counter (DEC-004).
- **Descriptive ART identifier**: `ART-<DESCRIPTIVE-SLUG>` (`ART-WORK-PLAN`, `ART-GLOSSARY`). DEC-004 admits two flavors of `ART-` identifiers; this is the non-mirroring one.
- **Qualified reference**: `<package>/<id>` form for cross-package references (DEC-004 "Cross-Package Referencing").
- **Title slug**: the uppercased-hyphenated piece of a filename, distinct from the identifier itself. Rules in DEC-004 "File-Naming Convention".
- **Kind registry**: per-active-package knowledge of which kinds are recognized — the five core kinds plus the package's optional `extraKinds`.
- **Reserved kind**: any of `{DEC, ITEM, ART, REV, CMT}`. `extraKinds` must not contain any reserved kind.

Glossary cross-references: `../../../shared/glossary.md`.

## Approved Decisions

- `DEC-004-ID-SCHEME-CUSTOMIZATION` — sections "Identifier Form", "Core Kinds", "Artifact ID Form", "Per-Package Customization (Additive)", "File-Naming Convention", "Identifier Stability", "Counter Advancement", "Cross-Package Referencing", "Reserved Kinds and Validation".
- `DEC-007-MVP-TOOL-SET` — section "Error-Code Catalog": rows `specforge.id.invalid`, `specforge.id.kind_reserved`, `specforge.id.kind_exhausted` (each carries the `data` shape ITEM-003 must populate when throwing).
- `DEC-007-MVP-TOOL-SET` — section "Surface Principles": identifier inputs accept both bare and qualified forms; subsequent items rely on `IdParser` to demux.
- `DEC-003-RUNTIME-AND-ARCHITECTURE` — sections "Dependency Direction", "Error Handling": typed Core exceptions, Mcp envelope mapping pattern this item extends.

## Current Code State

After `ITEM-002-CONFIG-MODEL` lands:

- `Specforge.Core` carries `Configuration/*`, `Diagnostics/BinaryInfo`, and five typed exceptions extending `SpecforgeException`.
- `ConfigValidator` checks `extraKinds` shape (`^[A-Z]{2,6}$`) but does NOT yet check reserved-kind collision. The check is deferred to this item.
- `Specforge.Mcp` has `ToolExceptionMapper` covering 5 codes plus the generic `specforge.tool.internal_error`. ITEM-003 extends it with 3 new code mappings.
- No identifier-parsing code exists. Tools that take identifiers as arguments have not been written yet (they arrive in ITEM-006 onward).

## Target Behavior

After this item is Done:

1. **`IdParser.TryParse(string input, out ParsedIdentifier? id)`** returns `true` if `input` matches one of the four recognized forms:
   - Atomic: `^[A-Z]{2,6}-[0-9]{3}$` (e.g. `DEC-001`).
   - Composite REV: `^REV-[A-Z]{2,6}-[0-9]{3}-[0-9]{3}$` (e.g. `REV-DEC-001-002`).
   - Descriptive ART: `^ART-[A-Z][A-Z0-9-]*[A-Z0-9]$` (e.g. `ART-WORK-PLAN`).
   - Qualified: `<package>/<bare-form-above>` (e.g. `specforge-mvp/DEC-001`).
2. **`IdValidator.Validate(string input, KindRegistry registry, ValidationContext context)`** throws `SpecforgeInvalidIdentifierException` carrying `given` and `expectedPatterns: [...]` when `IdParser` rejects, or when the parsed kind is not in the registry, or (for qualified) when the package is unknown.
3. **`TitleSlug.Validate(string slug)`** throws `SpecforgeInvalidIdentifierException` when slug violates DEC-004's rules: `^[A-Z][A-Z0-9-]*[A-Z0-9]$`, max 60 chars, no consecutive hyphens.
4. **`TitleSlug.FromTitle(string title)`** normalizes a plain title (`"Distribution and Transport"`) into a slug (`"DISTRIBUTION-AND-TRANSPORT"`) per DEC-004's slug rules; non-slug-safe characters are stripped or replaced; result is checked against the validator before being returned.
5. **`KindRegistry`**:
   - `static IReadOnlyList<string> CoreKinds => ["DEC", "ITEM", "ART", "REV", "CMT"]`.
   - `KindRegistry.FromConfig(SpecforgeConfig config, string activePackageName)` builds the live registry for a session.
   - `IsKnownKind(string kind)` returns true if `kind` is core or in the active package's `extraKinds`.
   - `IsReservedKind(string kind)` returns true if `kind ∈ CoreKinds`.
6. **`KindRegistry.ValidateExtraKinds(IReadOnlyList<string> extras)`** throws `SpecforgeReservedKindException` carrying `given` and `reservedKinds` if any extra equals a core kind. Called from `ConfigValidator` at parse time (retroactive change to ITEM-002).
7. **`IIdAllocator.NextNumberAsync(string kind, string packageName, CancellationToken ct)`** scans the package's `ledger/artifacts.md` and (for `CMT`) `ledger/commits.md` and (for `REV`) `ledger/reviews.md` to find the highest in-use number for the given kind, returns `highest + 1`. Throws `SpecforgeKindExhaustedException` carrying `kind` and `package` when the result would exceed `999`.
8. **`IIdAllocator` is interface-first**. The default implementation is non-tombstone-aware; ITEM-010 ships `TombstoneAwareIdAllocator` decorating it. Callers in ITEM-007/008/009 depend on `IIdAllocator`, not the concrete type — so swapping the implementation later is a DI-registration change.
9. **`ConfigValidator` from ITEM-002 gains the reserved-kind check**: an `extraKinds` array containing `"DEC"` now produces `SpecforgeReservedKindException` at config-load time rather than waiting for a tool to use the kind.
10. **`ToolExceptionMapper` gains three new rows** mapping `SpecforgeInvalidIdentifierException` → `specforge.id.invalid`, `SpecforgeReservedKindException` → `specforge.id.kind_reserved`, `SpecforgeKindExhaustedException` → `specforge.id.kind_exhausted`. Each populates the `data` shape exactly as DEC-007's catalog specifies.

## Invariants

- **`Specforge.Core` continues to reference no `ModelContextProtocol.*` assembly**. ITEM-001's architecture test still passes.
- **All three new exceptions live in `Specforge.Core.Exceptions`** and extend the `SpecforgeException` base from ITEM-002.
- **`IdParser` is pure**: no I/O, no DI dependencies, no async. It either matches a regex or returns `null`.
- **`IdValidator` does not perform I/O either** — it consumes a pre-built `KindRegistry` (which is itself in-memory).
- **`IIdAllocator` is the only ID-related service that touches the filesystem**, and only for reads of ledger markdown files.
- **`KindRegistry` is rebuilt per session** from the active config; it is not cached across `use_package` calls (a new selection swaps in a new registry).
- **Three-digit width is enforced everywhere**. `IdParser` rejects `DEC-1`, `DEC-01`, `DEC-0001`; the allocator returns three-digit-formatted strings only.
- **Sequence-gap detection is NOT in this item**. The allocator returns `max(found) + 1`; legitimate vs illegitimate gaps are ITEM-010's concern.

## Code Scope

**In scope (created or modified by this item):**

`Specforge.Core` (new files):

- `Identifiers/ParsedIdentifier.cs` — discriminated union (sum type via abstract record + concrete variants):
  - `AtomicIdentifier(string Kind, int Number)`
  - `CompositeReviewIdentifier(AtomicIdentifier Target, int Sequence)`
  - `DescriptiveArtIdentifier(string Slug)`
  - `QualifiedIdentifier(string Package, ParsedIdentifier Inner)`
- `Identifiers/IdParser.cs` — static `TryParse` + `Parse` methods using compiled regexes from `IdRegexes`.
- `Identifiers/IdRegexes.cs` — `static readonly Regex` instances for each form, declared with the `[GeneratedRegex]` source generator attribute (.NET 7+ feature) for AOT-readiness.
- `Identifiers/IdValidator.cs` — service that orchestrates parsing + kind-registry membership + qualified-package resolution.
- `Identifiers/TitleSlug.cs` — `Validate(string)` and `FromTitle(string)` static methods.
- `Identifiers/KindRegistry.cs` — sealed class with the listed methods; built via `FromConfig` factory.
- `Identifiers/IIdAllocator.cs` — interface.
- `Identifiers/IdAllocator.cs` — default implementation; scans ledger files via an injected `ILedgerReader` (which itself is defined here as a tiny interface returning the identifiers from a ledger file — concrete implementation reads the markdown table rows; this thin layer lets ITEM-007/008/009 swap in faster readers if needed).
- `Identifiers/LedgerReader.cs` — concrete `ILedgerReader` reading `ledger/*.md` files into identifier lists.
- `Exceptions/SpecforgeInvalidIdentifierException.cs` — `ErrorCode = "specforge.id.invalid"`, carries `Given`, `ExpectedPatterns`.
- `Exceptions/SpecforgeReservedKindException.cs` — `"specforge.id.kind_reserved"`, carries `Given`, `ReservedKinds`.
- `Exceptions/SpecforgeKindExhaustedException.cs` — `"specforge.id.kind_exhausted"`, carries `Kind`, `Package`.

`Specforge.Core` (modifications):

- `Configuration/ConfigValidator.cs` — gains a call to `KindRegistry.ValidateExtraKinds` per package entry. Now throws `SpecforgeReservedKindException` at parse time instead of accepting `extraKinds: ["DEC"]`.

`Specforge.Mcp` (modifications):

- `Tools/ToolExceptionMapper.cs` — three new `case` arms mapping the new exceptions to envelope codes with `data` per DEC-007.
- `Hosting/SpecforgeCoreServices.cs` — register `IIdAllocator → IdAllocator`, `ILedgerReader → LedgerReader`, plus a transient `KindRegistry` factory keyed off the live session.

`Specforge.Tests` (new files):

- `Identifiers/IdParserTests.cs` — exhaustive happy/sad-path on all four forms.
- `Identifiers/IdValidatorTests.cs` — including qualified-reference resolution against a fixture config.
- `Identifiers/TitleSlugTests.cs` — validate and normalize; 60-char boundary; no-consecutive-hyphens enforcement.
- `Identifiers/KindRegistryTests.cs` — core kinds always recognized; package extras recognized; reserved-kind collision in `ValidateExtraKinds` throws.
- `Identifiers/IdAllocatorTests.cs` — uses fixture ledger files; verifies highest+1 across artifacts/reviews/commits; kind-exhausted at 999.
- `Configuration/ConfigValidatorTests.cs` — extended with a case where `extraKinds: ["DEC"]` throws `SpecforgeReservedKindException` at parse time.
- `Tools/ToolExceptionMapperTests.cs` — extended with the three new exception → envelope mappings.

**Out of scope (deferred to later items):**

- Tombstone-aware gap detection — `ITEM-010-VALIDATE-TOOL`.
- Cross-package reference resolution against multiple configs (this item resolves against the *active* package's view; multi-package qualified-ref tooling expansions live wherever cross-package operations land — none in MVP).
- Any MCP tool surfacing these capabilities — the three exceptions surface only when downstream tools (ITEM-006+) invoke the validator.
- Number-allocation with tombstone awareness — wrapped by ITEM-010's `TombstoneAwareIdAllocator` decorator.

## Test Scope

Six new test classes plus one extension of `ConfigValidatorTests` and one extension of `ToolExceptionMapperTests`. Combined, ~40-50 new tests. All xUnit, hand-rolled fakes for `ILedgerReader` and `KindRegistry` where helpful.

## Test Plan

1. Implement the regexes in `IdRegexes` using `[GeneratedRegex]`; verify each pattern matches the canonical positive and negative samples from DEC-004 "Reserved Kinds and Validation".
2. Build `IdParser` on top; full enumeration of inputs (valid atomic, valid composite, valid descriptive ART, valid qualified, plus invalid permutations: wrong case, wrong width, missing dash, trailing dash, etc.).
3. Add `TitleSlug`; verify the 60-char boundary, the `[A-Z0-9-]` set, and the `FromTitle` normalization (input: `"Distribution and Transport"` → output: `"DISTRIBUTION-AND-TRANSPORT"`).
4. Build `KindRegistry` and the reserved-kind enforcement; extend `ConfigValidator` and its tests; confirm a `.specforge.json` fixture with `extraKinds: ["DEC"]` fails to load.
5. Build `IIdAllocator` with the simple implementation; write fixture ledger files (e.g. `artifacts.md` with `ART-DEC-001`, `ART-DEC-002`, `ART-DEC-003`); verify the allocator returns `004` for the next DEC.
6. Verify kind-exhaustion: a fixture with `ART-XYZ-999` causes `IIdAllocator.NextNumberAsync("XYZ", ...)` to throw `SpecforgeKindExhaustedException`.
7. Extend `ToolExceptionMapperTests` to confirm each of the three new codes produces the exact envelope shape (`code`, `message`, `suggestion`, `data` fields).
8. Run `dotnet test test/Specforge.Tests -c Release`; all new tests pass; ITEM-001 architecture test still passes; ITEM-002 tests still pass.

## Test Evidence

- Console output of `dotnet build` (zero warnings).
- Console output of `dotnet test` (all tests pass, including ITEM-001's architecture test).
- A short transcript of `IdParser.TryParse` against each documented form in DEC-004's `Reserved Kinds and Validation` examples, captured to `test/Specforge.Tests/Evidence/itm003-parser.txt`.

## Impact Assessment

| Aspect | Impact | Notes |
|---|---|---|
| ID scheme | Direct | This item implements DEC-004 end-to-end (except tombstones). |
| Configuration discovery | Indirect | `ConfigValidator` from ITEM-002 gains the reserved-kind check. |
| MCP tool surface | No impact | No new tool ships here. |
| MCP error envelope | Direct | Three new mapping rows in `ToolExceptionMapper`. |
| Core library boundary | No impact | All new code in Core; architecture test still passes. |
| Schema versioning | No impact | DEC-006 mechanics unchanged. |
| Skill packaging | No impact | DEC-005 mechanics unchanged. |
| Spec graph operations | Indirect | Every subsequent item that touches identifiers depends on the services from this item. |
| Test coverage scope | Direct | ~40-50 new unit tests. |
| Performance | No measurable | Allocator does at most a small file scan per call. |
| Concurrency | No measurable | Single-process; allocator scans are deterministic. |
| External adoption | Indirect | Once landed, adopters validating their existing layouts get clear errors about invalid identifiers. |
| Documentation | No impact | No user-facing tool surface change. |
| Maintenance burden | Indirect | Future kind-related rules live in this item's services (single source of truth for ID logic). |

## Validation

- **Build**: `dotnet build Specforge.sln -c Release` succeeds with zero warnings.
- **Test**: `dotnet test test/Specforge.Tests -c Release` runs every new test plus all prior tests; all pass.
- **Boundary**: ITEM-001's architecture test still passes (`Specforge.Core` carries no MCP reference despite the new identifier infrastructure).
- **Retro-fit**: an in-test fixture `.specforge.json` with `extraKinds: ["DEC"]` fails `ConfigValidator` with the expected `SpecforgeReservedKindException`.
- **Dogfood**: `IdParser.TryParse` succeeds for every identifier in this repo's existing ledger files (`ART-DEC-001` through `ART-DEC-008`, `ART-ITEM-001`, `ART-ITEM-002`, etc.) — a smoke test confirming the parser handles real-world inputs.
- **Sample envelope**: a deliberate `IdValidator.Validate("dec-001", ...)` call produces the exact JSON envelope `{"code": "specforge.id.invalid", "message": "...", "suggestion": "...", "data": {"given": "dec-001", "expectedPatterns": [...]}}` — verified in `ToolExceptionMapperTests`.

## Open Questions

- Whether `KindRegistry` should be a singleton with mutable state (rebuilt on `use_package`) or a transient built per-request — leaning singleton with explicit `Rebuild()` on selection change to avoid repeated rebuilds. Implementation detail; not blocking.
- Whether `IIdAllocator` should support a batch API (`NextNumbersAsync(kind, count)`) for bulk authoring — defer; no current caller needs it.
- Whether `TitleSlug.FromTitle` should reject input that produces an empty slug (e.g., title containing only punctuation) — leaning yes, throw `SpecforgeInvalidIdentifierException` with `given = original title`. Finalize during implementation.
- Whether `IdParser` should accept lowercase input and normalize, or strictly reject — DEC-004's regex is uppercase-only; rejecting is consistent. Confirmed: reject (no normalization on parse). `TitleSlug.FromTitle` is the canonical entry point for normalizing user-supplied text.

## Done Criteria

The item is **Done** (post-Approved) when:

1. All files listed in "Code Scope (In scope)" exist at the specified paths in the repository.
2. `dotnet build Specforge.sln -c Release` reports zero warnings.
3. `dotnet test test/Specforge.Tests -c Release` runs every new test (~40-50) plus ITEM-001 architecture test plus all ITEM-002 tests; all pass.
4. `Specforge.Core` carries no `ModelContextProtocol.*` reference (architecture test).
5. The retro-fit on `ConfigValidator` is active: a config with `extraKinds: ["DEC"]` is rejected at load time.
6. The dogfood test passes: every identifier in this repo's `ledger/*.md` files parses successfully.
7. A `CMT-NNN` row is appended to `ledger/commits.md` recording the implementation commit's short SHA.
8. A history event is appended to `ledger/history.md` marking the transition.

## Links

- `../decisions/DEC-004-ID-SCHEME-CUSTOMIZATION.md` (approved) — sections "Identifier Form", "Core Kinds", "Artifact ID Form", "Per-Package Customization", "File-Naming Convention", "Identifier Stability", "Counter Advancement", "Cross-Package Referencing", "Reserved Kinds and Validation".
- `../decisions/DEC-007-MVP-TOOL-SET.md` (approved) — section "Error-Code Catalog" (rows `specforge.id.*`), "Surface Principles" (identifier inputs).
- `../decisions/DEC-003-RUNTIME-AND-ARCHITECTURE.md` (approved) — sections "Dependency Direction", "Error Handling".
- `./ITEM-001-SOLUTION-BOOTSTRAP.md` (approved) — scaffold this item builds on.
- `./ITEM-002-CONFIG-MODEL.md` (approved) — config services and `SpecforgeException` base this item extends; `ConfigValidator` modified here.
- `../../../templates/item_spec.md` — item template.
- `../../../shared/spec_item_contract.md` — required-section contract.
- `../../../shared/document_lifecycle.md` — status states.
- `../../../shared/impact_assessment_checklist.md` — aspects checklist.

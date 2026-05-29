# ITEM-011-ERROR-ENVELOPE - Error Envelope Centralization, Audit, and DEC-007 Amendment

Status: Approved
Review owner: User
Depends on: `ITEM-002-CONFIG-MODEL`, `ITEM-003-ID-VALIDATOR`, `ITEM-004-EMBEDDED-SKILL-CATALOG`, `ITEM-005-SKILL-INSTALLER`, `ITEM-007-DECISION-TOOLS`, `ITEM-009-LEDGER-APPEND-TOOLS`, `ITEM-010-VALIDATE-TOOL`, `DEC-007-MVP-TOOL-SET`
Updates ledger rows: new `ART-ITEM-011`; amends `ART-DEC-007` (adds `specforge.id.not_found` to the catalog); new `CMT-NNN` rows for implementation commits

## Handoff Summary

Ship the error-envelope consolidation pass that the prior nine items deferred. ITEM-011 is an audit-and-centralization item — no new MCP tools, no new NuGet packages. It pulls the `ToolExceptionMapper` arms that accumulated incrementally across ITEM-002 → ITEM-010 into a single canonical surface, introduces a `SpecforgeErrorCode` static class as the one-and-only string constant store, defines an `IEnvelopeRenderer` for converting Core exceptions into the JSON envelope shape, and adds three invariant tests that prevent the catalog from drifting: (1) every typed `Specforge*Exception` in Core has a mapper arm; (2) every code in `SpecforgeErrorCode` appears in DEC-007's catalog table; (3) every code in DEC-007's catalog has at least one production path that produces it. The audit also discovers and closes a real gap: ITEM-009's `delete_review` / `delete_commit` emit `specforge.id.not_found` for missing rows, but DEC-007's catalog table never listed that code. ITEM-011 amends DEC-007 with a 14th entry plus introduces a `SpecforgeIdNotFoundException` (12th typed exception) so the code has a typed source. Finally, ITEM-011 locks the `tool.internal_error` fallback path: any unhandled exception bubbles to a single catch site that emits the envelope with a generated `correlationId` GUID and writes the full exception detail (type, message, stack trace) to stderr tagged with the same GUID per DEC-003.

- 0 new tools (tool count stays at 21).
- 0 new NuGet packages.
- 1 new typed exception: `SpecforgeIdNotFoundException` (12th).
- 1 amendment to DEC-007: adds `specforge.id.not_found` to the catalog (13 → 14 codes).
- 3 new invariant tests prevent future drift.

## Problem Slice

This item resolves "is the error surface actually as documented?" — the answer should be yes, and ITEM-011 produces machine-checked evidence of it.

Explicit non-goals (out of MVP or owned by other items):

- New tools — none. ITEM-010 closed the catalog.
- Behavioral change of any existing tool — none. ITEM-011 is non-functional from the AI's perspective; the envelopes that emerge from each tool are byte-identical to what ITEM-002..010 specified, except for the now-properly-typed `specforge.id.not_found` (which previously was emitted by tool-side inline checks and is now emitted via `SpecforgeIdNotFoundException`).
- Auto-repair / lint of existing exception emission sites in prior items' implementation — ITEM-011 documents the canonical pattern; Stage 2 implementations of ITEM-002..010 follow it. Where a prior item's implementation diverges (e.g., emits a code inline rather than via a typed exception), ITEM-011's invariant tests flag it as a finding for the Stage 2 implementation to resolve.
- Two-phase `previewToken` pattern as a delete-confirmation alternative — DEC-007 OQ; explicitly deferred.

## Terminology Used

- **Error envelope**: the structured JSON shape `{ error: { code, message, suggestion?, data?, correlationId? } }` every MCP tool produces on failure. Locked by DEC-007 §"Error Envelope"; refined here with `correlationId` semantics for `tool.internal_error`.
- **Canonical code constant store**: a single C# file holding every `specforge.*.*` code as a `public const string`. Eliminates string-literal scatter.
- **Mapper arm**: a single branch in the centralized exception-to-code map (typically a `switch` on the exception type) that decides which code an exception maps to and which structured `data` payload accompanies it.
- **Orphan exception**: a `Specforge*Exception` type in Core that has no mapper arm. Bug — would render as `tool.internal_error` instead of its proper code.
- **Orphan code**: a code constant in `SpecforgeErrorCode` that has no production path emitting it. Bug — dead code or documentation drift.
- **`correlationId`**: a fresh GUID generated at the `tool.internal_error` catch site; included in the envelope's `data` AND tagged on the stderr log entry that records the full exception type+message+stack trace. Lets a human cross-reference an AI-visible failure with the corresponding server log line.

## Approved Decisions

- `DEC-007-MVP-TOOL-SET` — sections "Error Envelope", "Error-Code Catalog" (13 codes), §"`specforge.tool.internal_error` deliberately does not leak exception type or stack — that information is logged to stderr by Specforge.Mcp (per DEC-003) under the same correlationId for human diagnosis."
- `DEC-003-RUNTIME-AND-ARCHITECTURE` — sections "Error Handling" (typed Core exceptions, MCP envelope mapping deferred to DEC-007), "Logging" (stderr-only logging convention).
- All Stage 0 decisions establishing exception types: DEC-002 (config/package), DEC-004 (id), DEC-005 (skills), DEC-006 (schema versioning), DEC-007 (lifecycle delete).

## Current Code State

After `ITEM-010` lands:

- `Specforge.Core/Exceptions/` carries 11 typed exception classes:
  1. `SpecforgeConfigNotFoundException`
  2. `SpecforgeConfigValidationException`
  3. `SpecforgeSchemaVersionException`
  4. `SpecforgePackageNotSelectedException`
  5. `SpecforgeUnknownPackageException`
  6. `SpecforgeInvalidIdentifierException`
  7. `SpecforgeReservedKindException`
  8. `SpecforgeKindExhaustedException`
  9. `SpecforgeEmbeddedSkillNotFoundException`
  10. `SpecforgeSkillInstallException`
  11. `SpecforgeDeleteForbiddenException`
- `Specforge.Mcp/Tools/ToolExceptionMapper.cs` (or equivalent — file name set by ITEM-002 and extended by every subsequent tool item) carries 11 mapper arms plus 2 generic-emission paths (`tool.invalid_argument`, `tool.internal_error`).
- Code-literal strings (`"specforge.config.not_found"` etc.) appear in multiple files: the mapper, individual tools (for `tool.invalid_argument` emissions), `SpecforgeDeleteForbiddenException.ErrorCode`, etc. ITEM-011 collapses them all to constant references.
- ITEM-009 `delete_review` / `delete_commit` emit `specforge.id.not_found` via tool-side inline checks (no typed exception currently produces this code). DEC-007's catalog table at line 70-84 does NOT list this code — it is a real gap.
- No invariant tests prevent drift between the mapper, the code constants, and DEC-007's catalog.

## Target Behavior

After this item is Done:

1. **`SpecforgeErrorCode`** (static class, `Specforge.Mcp/Errors/SpecforgeErrorCode.cs`):
   ```csharp
   public static class SpecforgeErrorCode
   {
       public const string ConfigNotFound = "specforge.config.not_found";
       public const string ConfigSchemaVersionUnsupported = "specforge.config.schema_version_unsupported";
       public const string ConfigValidationFailed = "specforge.config.validation_failed";
       public const string PackageNotSelected = "specforge.package.not_selected";
       public const string PackageUnknown = "specforge.package.unknown";
       public const string IdInvalid = "specforge.id.invalid";
       public const string IdNotFound = "specforge.id.not_found";          // NEW — closes ITEM-009 gap
       public const string IdKindReserved = "specforge.id.kind_reserved";
       public const string IdKindExhausted = "specforge.id.kind_exhausted";
       public const string SkillsInstallFailed = "specforge.skills.install_failed";
       public const string SkillsCatalogMissing = "specforge.skills.catalog_missing";
       public const string LifecycleDeleteForbidden = "specforge.lifecycle.delete_forbidden";
       public const string ToolInvalidArgument = "specforge.tool.invalid_argument";
       public const string ToolInternalError = "specforge.tool.internal_error";

       public static IReadOnlySet<string> AllCodes { get; } = /* reflection-built */;
   }
   ```
2. **`SpecforgeIdNotFoundException`** (new in `Specforge.Core/Exceptions/`): the 12th typed exception. Fields: `string Id`. Constructor: `(string id, string? message = null)`. `ErrorCode` property returns `"specforge.id.not_found"`. ITEM-009's `delete_review` / `delete_commit` implementations are updated in Stage 2 to throw this exception instead of emitting the code inline (the catalog audit test guarantees this).
3. **`IEnvelopeRenderer`** + **`EnvelopeRenderer`** (new in `Specforge.Mcp/Errors/`):
   - `EnvelopeJson Render(Exception ex)` — accepts any exception; returns the rendered envelope.
   - For known typed exceptions: dispatches via `ToolExceptionMapper`; returns `{ code, message, suggestion?, data? }` per the per-code contract in DEC-007 §"Error-Code Catalog".
   - For unknown exceptions: returns `{ code = "specforge.tool.internal_error", message = "Unexpected error.", suggestion = "check the specforge stderr log entry tagged with the correlation id", data = { correlationId = <fresh GUID> } }` AND writes a stderr log line `[ERROR] correlationId=<GUID> exceptionType=<typename> message=<message>\n<stacktrace>` per the DEC-003 stderr-only convention.
4. **`ToolExceptionMapper`** (consolidated; file path: `Specforge.Mcp/Errors/ToolExceptionMapper.cs`):
   - A single `switch` on the exception's `GetType()` returning a `MappedError` record `{ Code, Suggestion?, BuildData(ex) => object? }`.
   - 12 typed-exception arms covering every type in `Specforge.Core/Exceptions/`.
   - Throws `InvalidOperationException` (becomes `tool.internal_error` at the catch site) for unknown types — this is the safety net the `IEnvelopeRenderer` catches; production code should never hit it because the orphan-exception invariant test guarantees coverage.
5. **`SpecforgeDeleteForbiddenException.ErrorCode`** stays as a class-level constant pointing to `SpecforgeErrorCode.LifecycleDeleteForbidden`. Same for any other typed exception that exposes an `ErrorCode` property — single source of truth.
6. **Tool-side `tool.invalid_argument` emission** (the only code Core never raises) goes through a single helper `EnvelopeRenderer.InvalidArgument(string argument, object? given, string expected, string? suggestion = null)`. Every tool's argument-validation site calls this helper instead of constructing the envelope manually.
7. **DEC-007 amendment**: ITEM-011 updates the DEC-007 catalog table (lines 70-84 in the current file) to add a new row:

   | Code | Triggering Core exception | `data` shape | Default suggestion |
   |---|---|---|---|
   | `specforge.id.not_found` | `SpecforgeIdNotFoundException` (defined in ITEM-011) | `{ id: string }` | `"verify the identifier exists in this package; use the relevant list_* tool to enumerate available IDs"` |

   The amendment also updates DEC-007 §"Consequences" with: "Catalog amended from 13 to 14 codes by ITEM-011 to close the audit gap on `delete_review` / `delete_commit` missing-row emission." The amendment is structurally additive — no existing code's contract changes.

## Invariants

- **`Specforge.Core` references no `ModelContextProtocol.*` assembly.** Architecture test from ITEM-001 still passes.
- **No string-literal error codes outside `SpecforgeErrorCode.cs`** in `Specforge.Mcp`. A code-search test (a unit test that does grep-equivalent over the Mcp assembly source) asserts the only file with a `"specforge."` string literal matching the code regex is `SpecforgeErrorCode.cs`. (Exception sources in Core may use the literal once each as the `ErrorCode` property value — those are also unified via constants from a shared `Specforge.Core/Diagnostics/SpecforgeErrorCode.cs` mirror file, or via `Specforge.Mcp`'s constant referenced from a Core marker interface; design pinned during implementation.)
- **One file per concept**: `SpecforgeErrorCode.cs` (constants), `ToolExceptionMapper.cs` (typed-exception → code map), `EnvelopeRenderer.cs` (final JSON shape). Tools never construct envelopes directly; they throw or call `EnvelopeRenderer.InvalidArgument(...)`.
- **`tool.internal_error` is the ONLY catch-all**. Any exception that reaches the outermost tool-handler boundary without being one of the typed exceptions becomes `tool.internal_error`. No other code is ever produced by a `catch (Exception)`.
- **`correlationId` is only present in `tool.internal_error` envelopes**. Other codes do not carry it. (Future codes may opt in if they need cross-log correlation, but the MVP rule is one-code-one-use.)
- **stderr logging happens only for `tool.internal_error`**. Other codes are AI-visible failures expected by design; logging them duplicates the envelope. (Server-startup errors and config-load diagnostics use stderr separately per DEC-003 and are out of scope.)
- **Invariant tests are runtime tests, not source-introspection tests**: they reflect over the actual built assembly and ask "does every type in `Specforge.Core.Exceptions/` have a mapper arm?" This catches additions to Core that don't update the mapper.

## Code Scope

**In scope (created or modified by this item):**

`Specforge.Core` (new):

- `Exceptions/SpecforgeIdNotFoundException.cs` — 12th typed exception. `ErrorCode` property exposes the canonical string.

`Specforge.Mcp` (new):

- `Errors/SpecforgeErrorCode.cs` — the canonical constant store. 14 codes after the amendment.
- `Errors/MappedError.cs` — `record MappedError(string Code, string? Suggestion, Func<Exception, object?> BuildData)`.
- `Errors/ToolExceptionMapper.cs` — single consolidated `switch`; one arm per typed exception. Existing scattered switch-statements from ITEM-002..010 are absorbed.
- `Errors/IEnvelopeRenderer.cs` + `EnvelopeRenderer.cs` — single point of envelope construction; owns `correlationId` generation and stderr emission.
- `Errors/EnvelopeJson.cs` — typed record matching the JSON shape; serialized by `System.Text.Json` with camelCase contract.

`Specforge.Mcp` (modifications):

- Every tool's `catch` site: replaces direct envelope construction with `EnvelopeRenderer.Render(ex)`.
- Every tool's argument-validation site: replaces direct envelope construction with `EnvelopeRenderer.InvalidArgument(...)`.
- `Hosting/SpecforgeCoreServices.cs` — registers `IEnvelopeRenderer` as singleton.
- `Tools/Ledger/DeleteReviewTool.cs` and `Tools/Ledger/DeleteCommitTool.cs` — replace inline `id.not_found` emission with `throw new SpecforgeIdNotFoundException(id)` so the typed exception path is used.

`spec/packages/specforge-mvp/decisions/DEC-007-MVP-TOOL-SET.md` (modifications):

- Add `specforge.id.not_found` row to the Error-Code Catalog table.
- Update Consequences bullet about tool-count fixity (clarify: catalog amended additively from 13 → 14 codes; no behavioral break).
- Append a history event documenting the amendment.

`Specforge.Tests` (new):

- `Errors/SpecforgeErrorCodeTests.cs` — every constant matches the `specforge\.[a-z_]+\.[a-z_]+` regex; no constant is empty; `AllCodes` set has exactly the documented count (14).
- `Errors/ToolExceptionMapperTests.cs` (extension of existing tests from prior items) — `EveryTypedExceptionHasMapperArm` reflects over `Specforge.Core.Exceptions/`; asserts each type produces a mapper hit. `MapperArmsProduceExpectedCodes` asserts each known exception maps to the documented code with the documented `data` shape.
- `Errors/EnvelopeRendererTests.cs` — every typed exception renders the documented JSON shape; unknown exception renders `tool.internal_error` with a non-empty `correlationId`; the same `correlationId` appears on stderr.
- `Errors/CatalogDriftTests.cs` — reads DEC-007's catalog table at test time (parses the markdown via `LedgerTableParser` from ITEM-007); asserts every code in `SpecforgeErrorCode.AllCodes` is in the table and every code in the table is in `AllCodes`. Bi-directional check.
- `Errors/OrphanCodeTests.cs` — reflection-based: for each code in `SpecforgeErrorCode.AllCodes`, asserts a production path can produce it (either via a typed exception arm in `ToolExceptionMapper` or via direct emission for `tool.invalid_argument` / `tool.internal_error`). Walks the assembly looking for references.
- `Errors/NoStringLiteralCodesTests.cs` — assembly-source-grep equivalent: asserts no source file in `Specforge.Mcp` (except `SpecforgeErrorCode.cs`) contains a `"specforge.<dot>.<dot>"` string literal that doesn't reference `SpecforgeErrorCode.*`.

**Out of scope (deferred or out of MVP):**

- Two-phase `previewToken` confirm flow — DEC-007 OQ, deferred.
- A `data.diagnostics` field on every envelope carrying per-tool timings — out of MVP; performance instrumentation is not a DEC-003 deliverable.
- Localization of `message` / `suggestion` strings — out of MVP; the surface is English-only.
- A unified error-rendering hook for stderr-side diagnostics (server startup, config load) — DEC-003 covers stderr-only convention; ITEM-011 only touches the tool-call path.

## Test Scope

Six new test classes. ~40-60 new tests. Mostly assertions on the catalog invariants and a few render-shape tests for each code. xUnit; the catalog-drift test consumes DEC-007's actual markdown using ITEM-007's `LedgerTableParser` — a fitting case of the spec graph validating itself.

## Test Plan

1. Define `SpecforgeErrorCode` with the 14 codes (including the new `IdNotFound`); add `SpecforgeErrorCodeTests`.
2. Define `SpecforgeIdNotFoundException` in Core; add a minimal test asserting `ErrorCode == SpecforgeErrorCode.IdNotFound`.
3. Consolidate `ToolExceptionMapper` into one file; absorb every prior item's mapper arm. Add the new `SpecforgeIdNotFoundException` arm.
4. Define `IEnvelopeRenderer` + `EnvelopeRenderer`; route `Render(ex)` through the mapper; route unknown types through the `tool.internal_error` path with `correlationId` generation and stderr emission.
5. Refactor every tool's `catch` site to use `EnvelopeRenderer.Render(ex)`. Refactor every tool's argument-validation site to use `EnvelopeRenderer.InvalidArgument(...)`.
6. Refactor `DeleteReviewTool` and `DeleteCommitTool` to throw `SpecforgeIdNotFoundException` instead of inline emission.
7. Amend DEC-007's catalog table; append a history event documenting the amendment.
8. Add `EveryTypedExceptionHasMapperArm`, `MapperArmsProduceExpectedCodes`, `CatalogDriftTests`, `OrphanCodeTests`, `NoStringLiteralCodesTests`.
9. Run `dotnet test`; verify every prior-item test still passes (byte-identical envelope shapes for the unchanged codes; the only behavioral change is the source of `id.not_found`).
10. Manual smoke: trigger each of the 14 codes via fixture tests in `Tools/.../*Tests` and inspect the rendered envelope JSON. Two evidence transcripts.

## Test Evidence

- Console output of `dotnet build` (zero warnings).
- Console output of `dotnet test` (all pass).
- Architecture test still green.
- A transcript at `test/Specforge.Tests/Evidence/itm011-catalog-render.txt` showing one rendered envelope per code from `SpecforgeErrorCode.AllCodes` — proves all 14 codes are reachable.
- A transcript at `test/Specforge.Tests/Evidence/itm011-correlation-id.txt` showing a `tool.internal_error` envelope's `correlationId` matching the GUID appearing in stderr.

## Impact Assessment

| Aspect | Impact | Notes |
|---|---|---|
| MCP tool surface | No impact | No new tools. |
| Spec graph operations | No impact | No behavioral change. |
| Build pipeline | No impact | No new dependencies. |
| Core library boundary | No impact | One new typed exception added to Core; architecture test still passes. |
| Error handling | Direct | This is the consolidation pass. One file per concept, invariant-tested, no string-literal scatter. Catalog amended additively. |
| Configuration discovery | No impact | DEC-002 mechanics unchanged. |
| ID scheme | Indirect | `SpecforgeIdNotFoundException` is the typed source for the previously-untyped `id.not_found` code. |
| Schema versioning | No impact | DEC-006 mechanics unchanged. |
| Skill packaging | No impact | DEC-005 mechanics unchanged. |
| Lifecycle policy | No impact | DEC-007 §"Delete Semantics" unchanged. |
| Ledger structure | No impact | `Deleted` history-event format unchanged. |
| Documentation | Direct | DEC-007's catalog table amended from 13 to 14 codes. |
| External adoption | Indirect | Adopters see a more stable envelope contract; codes never silently change. The 14-code catalog is the public surface specforge promises to honor across binary versions until a future superseding DEC. |
| Test coverage scope | Direct | ~40-60 new tests, including 5 catalog/orphan invariants that protect against future drift. |
| Performance | No measurable | Envelope construction is one allocation per failure; envelope rendering is JSON serialization of a small object. |
| Concurrency | No measurable | Single-session process. |
| Maintenance burden | Direct (reduces) | After ITEM-011, adding a new code requires one DEC amendment + one constant + one mapper arm + one production path; the invariant tests fail until all three are present. Drift becomes impossible without explicit work. |

## Validation

- **Build**: `dotnet build Specforge.sln -c Release` succeeds with zero warnings.
- **Test**: `dotnet test` succeeds; all new tests pass; every prior-item test still passes (byte-identical envelopes for unchanged codes).
- **Boundary**: ITEM-001's architecture test still passes.
- **No-string-literal-codes**: assembly-source-grep test finds zero `"specforge.<dot>.<dot>"` literals outside `SpecforgeErrorCode.cs` in `Specforge.Mcp`.
- **Catalog drift bi-directional**: parses DEC-007's catalog table at test time; assertion (a) every code in the table is in `SpecforgeErrorCode.AllCodes`; (b) every code in `AllCodes` is in the table.
- **Orphan exception**: reflection test enumerates `Specforge.Core.Exceptions/*Exception` types; each one has a mapper arm.
- **Orphan code**: reflection test enumerates `SpecforgeErrorCode.AllCodes`; each has at least one production path identifiable in the assembly.
- **DEC-007 amendment recorded**: the amendment is reflected in DEC-007 file content; `ART-DEC-007` row carries a history event `Amended | DEC-007 catalog amended by ITEM-011 to add specforge.id.not_found (13 → 14 codes)`.
- **`correlationId` cross-reference**: triggering a known-internal-error path produces an envelope whose `data.correlationId` matches the GUID in the stderr log entry's `correlationId=` field.

## Open Questions

- Whether the canonical constant store should live in `Specforge.Core` (so Core exceptions can reference it directly via `ErrorCode = SpecforgeErrorCode.LifecycleDeleteForbidden`) or in `Specforge.Mcp` with a small mirror in Core. Leaning: place it in Core, since the codes are part of the public contract even outside the MCP host; the architecture test invariant (Core has no MCP refs) is preserved because constants don't pull in `ModelContextProtocol`. Finalize during implementation.
- Whether to add a per-envelope `timestamp` field (UTC ISO 8601) for AI-side correlation across multi-tool-call sessions — leaning no for MVP (the host's transcript already timestamps requests); reconsider after first external adoption.
- Whether `tool.internal_error` should redact specific frames (e.g., paths to embedded resources or other binary-internal details) from the stderr log — leaning no for MVP (stderr is server-local; redaction is host-of-last-resort's job).
- Whether to include `tool.invalid_argument` `data.given` always vs. only when small/safe to echo — leaning always for the MVP shape DEC-007 already committed to; reconsider if a payload-bloat scenario emerges.
- Whether to introduce `specforge.lifecycle.transition_invalid` as a distinct code for invalid lifecycle transitions (currently emitted as `tool.invalid_argument` per ITEM-007/008 design). Leaning no — transition is an argument-shape failure; the existing code is appropriate. Reconsider if usage surfaces a need to dispatch.

## Done Criteria

The item is **Done** (post-Approved) when:

1. All files listed in "Code Scope (In scope)" exist at the specified paths.
2. No additions to `Directory.Packages.props`.
3. `dotnet build Specforge.sln -c Release` reports zero warnings.
4. `dotnet test` runs every new test plus all prior-item tests; all pass.
5. `Specforge.Core.dll` carries no `ModelContextProtocol.*` reference (architecture test).
6. `Specforge.Mcp` registers twenty-one tools (catalog unchanged from ITEM-010).
7. DEC-007's Error-Code Catalog table contains 14 rows; `specforge.id.not_found` is the 14th.
8. `SpecforgeErrorCode.AllCodes.Count == 14`.
9. `Specforge.Core/Exceptions/` contains 12 typed exception classes; the new one is `SpecforgeIdNotFoundException`.
10. All 5 invariant tests pass.
11. Both smoke transcripts exist under `test/Specforge.Tests/Evidence/`.
12. A `CMT-NNN` row is appended to `ledger/commits.md`.
13. A history event is appended to `ledger/history.md` marking the transition AND a separate entry on `ART-DEC-007` recording the catalog amendment.

## Links

- `../decisions/DEC-007-MVP-TOOL-SET.md` (approved; **amended additively by this item**) — sections "Error Envelope", "Error-Code Catalog", "Consequences".
- `../decisions/DEC-003-RUNTIME-AND-ARCHITECTURE.md` (approved) — sections "Error Handling", "Logging".
- `./ITEM-002-CONFIG-MODEL.md` (approved) — first 5 typed exceptions; first mapper arms.
- `./ITEM-003-ID-VALIDATOR.md` (approved) — 3 more typed exceptions; 3 more mapper arms.
- `./ITEM-004-EMBEDDED-SKILL-CATALOG.md` (approved) — 1 more typed exception; 1 more mapper arm.
- `./ITEM-005-SKILL-INSTALLER.md` (approved) — 1 more typed exception; 1 more mapper arm.
- `./ITEM-007-DECISION-TOOLS.md` (approved) — `SpecforgeDeleteForbiddenException` (11th typed exception); 13th mapper arm; `LedgerTableParser` reused by `CatalogDriftTests`.
- `./ITEM-009-LEDGER-APPEND-TOOLS.md` (approved) — first emission of the previously-undocumented `id.not_found` code; refactored here to use the new typed exception.
- `./ITEM-010-VALIDATE-TOOL.md` (approved) — `TombstoneDetailFormat` pattern reused as a model for the centralized formatting approach.
- `../../../shared/impact_assessment_checklist.md` — aspects checklist.

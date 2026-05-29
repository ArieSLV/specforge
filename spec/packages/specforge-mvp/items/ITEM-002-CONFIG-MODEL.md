# ITEM-002-CONFIG-MODEL - Config Model and First MCP Tools

Status: Approved
Review owner: User (approved 2026-05-28)
Depends on: `ITEM-001-SOLUTION-BOOTSTRAP`, `DEC-002-CONFIGURATION-AND-DISCOVERY`, `DEC-004-ID-SCHEME-CUSTOMIZATION`, `DEC-006-SCHEMA-VERSIONING`, `DEC-007-MVP-TOOL-SET`
Updates ledger rows: new `ART-ITEM-002`; new `CMT-NNN` rows for implementation commits

## Handoff Summary

Vertical slice 1 of specforge functionality: bring up the configuration model and the first three MCP tools, plus the MCP host wiring that every subsequent tool-bearing item will reuse. Implements `.specforge.json` discovery (cwd walk-up per DEC-002), JSON parsing, shape validation, the schemaVersion check + in-memory upgrade chain per DEC-006, and package enumeration with optional `extraKinds` per DEC-004. Defines five typed Core exceptions. Wires `Specforge.Mcp` to the official MCP C# SDK with a stdio loop and a reusable tool-registration pattern (`IMcpTool` interface) that ITEM-005/006/007/008/009/010 will plug into without re-architecting. Ships three tools: `list_packages`, `use_package`, `info`.

- Bottom-up scope: Core types → typed exceptions → Mcp host wiring → 3 tools → error envelope rendering for 5 of the 13 codes (rest in ITEM-011).
- After this item, the AI can ask `specforge` what config is loaded and which packages exist — basic situational awareness.
- No spec-graph operations (decisions, items, ledger) yet — those come in ITEM-007+.

## Problem Slice

This item resolves "how does specforge find its config, parse it, validate it, version-check it, and surface the resulting state to the AI?"

Explicit non-goals (owned by other items):

- ID validation (regex, slug rules, cross-package qualification) → `ITEM-003-ID-VALIDATOR`. ITEM-002 treats package `name` as an opaque string for selection purposes; only ITEM-003 enforces `^[A-Z]{2,6}-[0-9]{3}$`-shape identifiers.
- Embedded skill catalog and `install_skills` → `ITEM-004`, `ITEM-005`.
- `init` tool (which *writes* `.specforge.json`) → `ITEM-006`. ITEM-002 only *reads* the file.
- Spec-graph tools (decisions, items, ledger) → `ITEM-007`, `ITEM-008`, `ITEM-009`.
- `validate` tool → `ITEM-010`.
- Full Core-exception → MCP-code mapping for all 13 codes → `ITEM-011`. ITEM-002 maps only the 5 codes its scope produces.

## Terminology Used

- **`.specforge.json`**: JSON config file at the project root, per DEC-002.
- **`schemaVersion`**: integer field; current `supportedMax` = 1 per DEC-006.
- **In-memory upgrade chain**: read v_M, populate defaults for v_{M+1}..v_N, runtime sees v_N (DEC-006).
- **Active package**: the package currently in scope for a session, per DEC-002. Held in process memory only.
- **`extraKinds`**: optional per-package additional identifier kinds, per DEC-004.
- **Typed exception** (Core) vs **MCP envelope** (Mcp): per DEC-003 boundary and DEC-007 envelope shape.
- **`IMcpTool`**: interface introduced in this item to make tool registration uniform across `Specforge.Mcp`; later items add new tool classes implementing it.

Glossary cross-references: see `../../../shared/glossary.md`.

## Approved Decisions

- `DEC-002-CONFIGURATION-AND-DISCOVERY` — sections "Configuration File", "Configuration Schema (version 1)", "Discovery Flow", "Package Selection", "Shared and Templates Resolution".
- `DEC-004-ID-SCHEME-CUSTOMIZATION` — section "Per-Package Customization (Additive)": `extraKinds` field shape and validation (no reserved-kind collision, regex `^[A-Z]{2,6}$`, no within-array duplicates).
- `DEC-006-SCHEMA-VERSIONING` — sections "Versioning Style", "Schema v1 Inventory", "Unsupported-Version Behavior", "Migration: In-Memory Upgrade + Opportunistic Write".
- `DEC-007-MVP-TOOL-SET` — sections "Surface Principles", "Error Envelope", "Error-Code Catalog" (rows 1-3, 5, 6), "MVP Tool Catalog" / "Configuration (3)".
- `DEC-003-RUNTIME-AND-ARCHITECTURE` — sections "Dependency Direction" (Core has no MCP refs), "Logging and Diagnostics" (stderr-only), "Error Handling" (typed exceptions in Core, envelope mapping in Mcp).

## Current Code State

After `ITEM-001-SOLUTION-BOOTSTRAP` lands: three empty projects exist (`src/Specforge.Core`, `src/Specforge.Mcp`, `test/Specforge.Tests`), centralized build configuration in place, architecture test passing. `Specforge.Core` contains a placeholder file with no types. `Specforge.Mcp` contains a minimum `Program.cs` that returns 0 immediately. No NuGet packages referenced beyond what xUnit needs in the test project.

Dogfood note: `D:\Work\specforge\.specforge.json` does **not** yet exist on disk. This item produces the first runnable specforge that can load that file, so authoring it (with `schemaVersion: 1`, the existing `spec/packages/specforge-mvp` package, etc.) is part of the Done Criteria below.

## Target Behavior

After this item is Done:

1. **Config discovery walks up from cwd.** `ConfigDiscovery.FindConfigAsync(startDir)` returns the absolute path to the first `.specforge.json` found on or above `startDir`, or `null` if none exists up to the filesystem root. Symlinks are followed; loops are detected (max depth 64).
2. **Loading a missing config does not throw at discovery time.** Throwing happens only when a tool that needs a package is invoked without one — the `info` tool, by contract from DEC-007, must always succeed and return `configPath: null` when no config is found.
3. **Loading a malformed config throws `SpecforgeConfigValidationException`** with the JSON-Pointer-style `errors[]` list per the envelope shape from DEC-007's error catalog row `specforge.config.validation_failed`.
4. **Loading a config with `schemaVersion: 2` against this binary's `supportedMax=1` throws `SpecforgeSchemaVersionException`** with `claimedVersion=2`, `supportedRange=[1,1]`, and the suggestion from DEC-007.
5. **Loading a config with `schemaVersion: 0`, `schemaVersion: "1"` (string), or missing `schemaVersion` throws `SpecforgeSchemaVersionException`** with the `invalid` suggestion variant.
6. **Loading a valid v1 config populates `SpecforgeConfig` immutably** with `shared`, `templates`, and `packages[]` (each carrying `name`, `path`, optional `extraKinds[]`). Paths are stored relative; resolution to absolute happens lazily.
7. **Single-package configs auto-select** the only package as the session's active package at load time (DEC-002 Package Selection rule).
8. **Multi-package configs leave active package null.** Tools that need a package and find no selection throw `SpecforgePackageNotSelectedException` carrying the `availablePackages: [{name, path}]` list (envelope code `specforge.package.not_selected`).
9. **`use_package(name)` selects an existing package or throws `SpecforgeUnknownPackageException`** carrying `requestedName` and `availablePackages` (code `specforge.package.unknown`). Switching to a different package mid-session is allowed.
10. **`list_packages` returns the package list** from the live config. If no config is loaded, returns an empty array (it does not throw).
11. **`info` returns** `{ binaryVersion, supportedSchemaVersionRange: [1, 1], activePackage: name|null, configPath: absolutePath|null }`. Always succeeds, even before discovery.
12. **MCP host runs on stdio.** Launching `specforge.exe` connects via standard MCP `initialize` handshake and exposes exactly three tools at this point: `list_packages`, `use_package`, `info`. Subsequent items add more tools to the same host.

## Invariants

- **`Specforge.Core` references no `ModelContextProtocol.*` assembly** (architecture test from ITEM-001 must continue to pass after ITEM-002 adds the MCP NuGet reference to `Specforge.Mcp` only).
- **Five typed exceptions live in `Specforge.Core.Exceptions`** with no `Microsoft.Extensions.Hosting` or MCP-related using directives.
- **Path resolution is relative to the config file directory, not cwd** (DEC-002 rule).
- **`info` never throws.** A panic in `info` is a defect because the AI uses it for diagnostics.
- **Active-package state is process-local.** No file is written to track it; restart loses selection (DEC-002 "Selection is by name… session-scoped").
- **Config parsing tolerates unknown top-level fields** when `schemaVersion` is recognized (additive-change rule from DEC-006). It does not tolerate unknown fields inside `packages[]` entries — those are strictly typed.
- **Stderr-only logging** per DEC-003: `Microsoft.Extensions.Logging.Console` is configured with `LogToStandardErrorThreshold = LogLevel.Trace`. Nothing this item writes touches stdout outside MCP-protocol payloads.

## Code Scope

**In scope (created by this item):**

`Specforge.Core` (additions to ITEM-001 scaffold):

- `Configuration/SpecforgeConfig.cs` — immutable record: `int SchemaVersion`, `string Shared`, `string Templates`, `IReadOnlyList<SpecforgePackageConfig> Packages`, `string ConfigPath` (absolute).
- `Configuration/SpecforgePackageConfig.cs` — record: `string Name`, `string Path`, `IReadOnlyList<string> ExtraKinds` (empty if absent).
- `Configuration/ConfigDiscovery.cs` — `static Task<string?> FindConfigAsync(string startDir, CancellationToken ct)` walks up using `Directory.GetParent`.
- `Configuration/ConfigLoader.cs` — orchestrates discovery → file read → parse → version check → shape validation → upgrade chain. Returns `SpecforgeConfig` or throws one of the five typed exceptions.
- `Configuration/ConfigParser.cs` — System.Text.Json parsing into a permissive DOM, then projection into typed records.
- `Configuration/ConfigValidator.cs` — shape validation with JSON-Pointer error paths; produces an `IReadOnlyList<ConfigValidationError>` consumed by `SpecforgeConfigValidationException`.
- `Configuration/SchemaVersionGate.cs` — implements DEC-006 Unsupported-Version Behavior: hard refuse on `M > N`, malformed, or missing version.
- `Configuration/Migrations/V1Migration.cs` — identity migration for v1 (placeholder; first real migration arrives when v2 lands).
- `Configuration/SessionState.cs` — holds the active package's name; `Select(name)` validates against the loaded config; `Clear()` resets.
- `Configuration/ConfigValidationError.cs` — record `{string Pointer, string Message}` (JSON Pointer per RFC 6901).
- `Diagnostics/BinaryInfo.cs` — exposes `Version` (from `AssemblyInformationalVersionAttribute`), `SupportedSchemaVersionRange` (constant `[1, 1]` for MVP).
- `Exceptions/SpecforgeException.cs` — abstract base (carries `string ErrorCode`).
- `Exceptions/SpecforgeConfigNotFoundException.cs` — extends `SpecforgeException`, `ErrorCode = "specforge.config.not_found"`, carries `SearchedPath`.
- `Exceptions/SpecforgeConfigValidationException.cs` — `"specforge.config.validation_failed"`, carries `Path`, `Errors[]`.
- `Exceptions/SpecforgeSchemaVersionException.cs` — `"specforge.config.schema_version_unsupported"`, carries `Path`, `ClaimedVersion (object)`, `SupportedRange (int[2])`.
- `Exceptions/SpecforgePackageNotSelectedException.cs` — `"specforge.package.not_selected"`, carries `AvailablePackages`.
- `Exceptions/SpecforgeUnknownPackageException.cs` — `"specforge.package.unknown"`, carries `RequestedName`, `AvailablePackages`.

`Specforge.Mcp` (additions to ITEM-001 scaffold):

- NuGet references (versions pinned in `Directory.Packages.props`):
  - `ModelContextProtocol` (official C# SDK; first introduction)
  - `Microsoft.Extensions.Logging.Console` (first introduction)
  - `Microsoft.Extensions.DependencyInjection` (transitive via MCP SDK, but listed explicitly for clarity)
- `Program.cs` — full rewrite: configure stderr logging, build DI container, register `SpecforgeCoreServices` and the three `IMcpTool` implementations, start the MCP stdio host.
- `Hosting/SpecforgeCoreServices.cs` — extension method on `IServiceCollection` registering Core services (`ConfigDiscovery`, `ConfigLoader`, `SessionState`, `BinaryInfo`) as singletons. Reused by every later item.
- `Hosting/StderrLoggingConfiguration.cs` — sets `LogToStandardErrorThreshold = LogLevel.Trace`; reads `SPECFORGE_LOG_LEVEL` env var; honors `SPECFORGE_LOG_FILE` if set (DEC-003).
- `Tools/IMcpTool.cs` — interface: `string Name`, `JsonElement InputSchema`, `Task<ToolResult> InvokeAsync(JsonElement args, CancellationToken ct)`. Future items add new implementations.
- `Tools/ToolResult.cs` — discriminated union of `Success(JsonElement payload)` and `Failure(McpErrorEnvelope envelope)`.
- `Tools/McpErrorEnvelope.cs` — record `{string Code, string Message, string? Suggestion, JsonElement? Data}` plus `ToJson()` rendering.
- `Tools/ToolExceptionMapper.cs` — switch over Core's typed exceptions; maps 5 codes (the rest filled in by ITEM-011). Generic catch maps to `specforge.tool.internal_error` with a `correlationId` (a `Guid`) and logs the exception detail to stderr.
- `Tools/ListPackagesTool.cs` — reads config via `ConfigLoader.Load()` and returns `packages[]`.
- `Tools/UsePackageTool.cs` — calls `SessionState.Select(name)`; returns `{ previous: name|null, current: name }`.
- `Tools/InfoTool.cs` — synthesizes the `info` payload; never throws (it catches `SpecforgeConfigNotFoundException` and returns `configPath: null`).

`Specforge.Tests` (additions to ITEM-001 scaffold):

- `Configuration/ConfigDiscoveryTests.cs` — walk-up finds config from deep cwd; returns null when absent; max-depth-64 cutoff.
- `Configuration/ConfigParserTests.cs` — valid v1 parses; missing `schemaVersion` throws `SpecforgeSchemaVersionException` with the "invalid" suggestion; `schemaVersion: "1"` (string) throws; `schemaVersion: 2` throws with the "upgrade" suggestion; unknown top-level field accepted; unknown field inside a package rejected.
- `Configuration/ConfigValidatorTests.cs` — missing required fields produce JSON-Pointer errors; `extraKinds` containing `"DEC"` throws (reserved-kind check — but actual reserved-kind enforcement lives in ITEM-003; ITEM-002 only validates `extraKinds` shape `^[A-Z]{2,6}$`).
- `Configuration/SessionStateTests.cs` — single-package auto-select; multi-package no-selection; select unknown name throws; switch selection.
- `Tools/InfoToolTests.cs` — pre-config: returns `configPath: null`; post-config single-package: returns the active package name; post-config multi-package no-selection: returns `activePackage: null`.
- `Tools/ListPackagesToolTests.cs` — returns the package list; empty when no config loaded.
- `Tools/UsePackageToolTests.cs` — sets state; throws unknown-package on bogus name; mapped envelope shape.
- `Tools/ToolExceptionMapperTests.cs` — every documented exception produces the right code + suggestion + `data` shape.
- Architecture test from ITEM-001 continues to pass with the new MCP NuGet reference confined to `Specforge.Mcp`.

**Out of scope (deferred to later items):**

- ID regex enforcement on `packages[].name` and `extraKinds[]` semantic check (reserved-kind validation) — `ITEM-003-ID-VALIDATOR`. ITEM-002 only checks shape (`^[A-Z]{2,6}$`).
- Embedded skill resources and `install_skills` — `ITEM-004`, `ITEM-005`.
- `init` (writing `.specforge.json`) — `ITEM-006`.
- Spec-graph tools (decisions, items, ledger) — `ITEM-007`, `ITEM-008`, `ITEM-009`.
- `validate` tool — `ITEM-010`.
- The remaining 8 error codes (`specforge.id.*`, `specforge.skills.*`, `specforge.lifecycle.delete_forbidden`, `specforge.tool.invalid_argument`) — `ITEM-011-ERROR-ENVELOPE`. ITEM-002 maps only the 5 codes it produces and adds the generic `specforge.tool.internal_error` catch-all.

## Test Scope

The eight test classes named under "Code Scope (Tests)". Each covers one component with happy-path and failure-path assertions. xUnit only. No FluentAssertions, no Moq; hand-rolled fakes where needed (e.g. an in-memory file system for `ConfigDiscoveryTests`).

## Test Plan

1. Implement the Core types, exceptions, and services bottom-up: records → discovery → parser → validator → version gate → session state.
2. Implement the Mcp host wiring and the three tools.
3. Author the test fixtures: a directory with `.specforge.json` for the happy path, several malformed-config fixtures, and a multi-package fixture.
4. Run `dotnet test test/Specforge.Tests -c Release`. All tests pass; architecture test from ITEM-001 still passes.
5. Manual smoke test: launch the published `specforge.exe` from `D:\Work\specforge\` (where `.specforge.json` is dogfooded — see Done Criteria); send a synthetic `initialize` + `info` MCP request pair via a test harness or `mcp-test` CLI; verify the response carries the dogfooded package's name in `activePackage`.
6. Test the deliberate-failure cases (broken `.specforge.json`, future `schemaVersion: 2`, multi-package without `use_package`) via in-proc tool invocation; verify the exact envelope strings.

## Test Evidence

- Console output of `dotnet build` (zero warnings).
- Console output of `dotnet test` (all tests pass).
- A short transcript of an `initialize`/`info` MCP exchange against the launched binary, captured to a file under `test/Specforge.Tests/Evidence/itm002-smoke.txt` and referenced from the implementing commit.
- Architecture test continues to pass — verified by inclusion in the same `dotnet test` run.

## Impact Assessment

| Aspect | Impact | Notes |
|---|---|---|
| Configuration discovery | Direct | This item implements DEC-002 end-to-end. |
| Schema versioning | Direct | This item implements DEC-006's check + upgrade chain. |
| MCP tool surface | Direct | First three tools land here (`list_packages`, `use_package`, `info`). |
| MCP host wiring | Direct | First `ModelContextProtocol` NuGet reference; first stdio loop. |
| Error handling | Direct | Envelope rendering + 5 codes ship; `IMcpTool`/`ToolExceptionMapper` pattern set. |
| Core library boundary | Direct | Architecture test must continue to pass with the new MCP reference confined to Mcp. |
| ID scheme | Indirect | `extraKinds` shape validation here; semantic kind-checks deferred to ITEM-003. |
| Skill packaging | No impact | Untouched (ITEM-004/005). |
| Build pipeline | Indirect | New NuGet references in `Directory.Packages.props` (ModelContextProtocol, MS.Extensions.Logging.Console, System.Text.Json if not transitive). |
| Logging | Direct | First stderr-logging configuration lands; env vars `SPECFORGE_LOG_LEVEL` and `SPECFORGE_LOG_FILE` honored. |
| Documentation | Indirect | Top-level README's "first run" walkthrough (ITEM-013) will eventually show `info`. |
| Test coverage scope | Direct | ~25-30 new unit tests; ITEM-001's architecture test continues. |
| Performance | No measurable | One small file read per session at most. |
| Concurrency | No measurable | Single-session process; no cross-tool contention. |
| External adoption | Indirect | Once this item lands, `info` against an adopted project reports its package list — the first concrete signal that specforge is wired in. |
| Maintenance burden | Indirect | The `IMcpTool` pattern is the recurring shape for every subsequent tool-bearing item; this is the moment to get the shape right. |

## Validation

- **Build**: `dotnet build Specforge.sln -c Release` succeeds with zero warnings.
- **Test**: `dotnet test test/Specforge.Tests -c Release` succeeds; new tests all pass; ITEM-001 architecture tests still pass.
- **Boundary**: architecture test confirms `Specforge.Core.dll` references no `ModelContextProtocol.*` assembly.
- **Smoke**: launch `D:\Work\specforge\bin\specforge.exe` against the dogfooded `.specforge.json` in the repo root; `info` returns the active package name.
- **Negative**: a fixture config with `schemaVersion: 2` produces the exact `specforge.config.schema_version_unsupported` envelope including the suggestion text from DEC-007.
- **Negative**: a fixture config with `packages: "not-an-array"` produces `specforge.config.validation_failed` with a `data.errors[0].pointer` of `/packages`.

## Open Questions

- Exact pinned versions of `ModelContextProtocol` and `Microsoft.Extensions.Logging.Console` — chosen at implementation time and recorded in `Directory.Packages.props`. Not blocking; revisit if either has known issues at the chosen .NET 10 LTS baseline.
- Whether the dogfooded `.specforge.json` at repo root should include `extraKinds: []` explicitly or omit the field — leaning omit (since DEC-002 schema makes it optional and DEC-006's additive rule treats absence as the default). Finalize during implementation.
- Whether `info` should also report the binary's commit SHA when available — defer; `AssemblyInformationalVersionAttribute` typically carries it on CI builds and is exposed via `BinaryInfo.Version`. Not blocking.

## Done Criteria

The item is **Done** (post-Approved) when:

1. All files listed in "Code Scope (In scope)" exist at the specified paths.
2. `dotnet build Specforge.sln -c Release` reports zero warnings.
3. `dotnet test test/Specforge.Tests -c Release` runs every new test and ITEM-001's architecture tests; all pass.
4. The canonical publish command from ITEM-001 still produces `D:\Work\specforge\bin\specforge.exe`, now genuinely speaking MCP.
5. A dogfooded `D:\Work\specforge\.specforge.json` exists in the repo root with `schemaVersion: 1`, `shared: spec/shared`, `templates: spec/templates`, `packages: [{name: "specforge-mvp", path: "spec/packages/specforge-mvp"}]`. Specforge can read its own spec layout.
6. A smoke-test transcript at `test/Specforge.Tests/Evidence/itm002-smoke.txt` shows the published binary handling an `initialize` + `info` MCP exchange and returning the dogfooded package's name.
7. A `CMT-NNN` row is appended to `ledger/commits.md` recording the implementation commit's short SHA.
8. A history event is appended to `ledger/history.md` marking the transition.

## Links

- `../decisions/DEC-002-CONFIGURATION-AND-DISCOVERY.md` (approved) — sections "Configuration File", "Configuration Schema (version 1)", "Discovery Flow", "Package Selection", "Shared and Templates Resolution".
- `../decisions/DEC-003-RUNTIME-AND-ARCHITECTURE.md` (approved) — sections "Dependency Direction", "Baseline Libraries", "Logging and Diagnostics", "Error Handling".
- `../decisions/DEC-004-ID-SCHEME-CUSTOMIZATION.md` (approved) — section "Per-Package Customization (Additive)" — `extraKinds` rules.
- `../decisions/DEC-006-SCHEMA-VERSIONING.md` (approved) — sections "Versioning Style", "Schema v1 Inventory", "Unsupported-Version Behavior", "Migration".
- `../decisions/DEC-007-MVP-TOOL-SET.md` (approved) — sections "Surface Principles", "Error Envelope", "Error-Code Catalog", "Configuration (3)" tools.
- `./ITEM-001-SOLUTION-BOOTSTRAP.md` (approved) — scaffold this item builds on.
- `../../../templates/item_spec.md` — item template.
- `../../../shared/spec_item_contract.md` — required-section contract.
- `../../../shared/document_lifecycle.md` — status states.
- `../../../shared/impact_assessment_checklist.md` — aspects checklist.

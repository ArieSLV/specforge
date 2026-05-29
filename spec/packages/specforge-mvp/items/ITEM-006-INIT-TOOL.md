# ITEM-006-INIT-TOOL - Init Tool, Embedded Templates, and Project-Level Files

Status: Approved
Review owner: User (approved 2026-05-28)
Depends on: `ITEM-002-CONFIG-MODEL`, `ITEM-003-ID-VALIDATOR`, `ITEM-004-EMBEDDED-SKILL-CATALOG`, `ITEM-005-SKILL-INSTALLER`, `DEC-002-CONFIGURATION-AND-DISCOVERY`, `DEC-005-SKILL-INSTALLATION-MODEL`, `DEC-006-SCHEMA-VERSIONING`, `DEC-007-MVP-TOOL-SET`, `DEC-008-INSTRUCTION-LAYER-DESIGN`
Updates ledger rows: new `ART-ITEM-006`; new `CMT-NNN` rows for implementation commits

## Handoff Summary

Ship the `init` MCP tool — the largest single tool item in Stage 1 because it consolidates five DECs' worth of file-writing responsibilities into one bootstrap operation. `init` always writes (a) `.specforge.json` per the args, (b) `SPECFORGE.md` at project root, (c) tagged-block content into `CLAUDE.md` and `AGENTS.md` (creating them if absent), and (d) the project-level Codex `agents/openai.yaml` declaring `specforge` as an MCP dependency. With `scaffold: true`, it additionally creates the package directory skeleton(s) and copies canonical shared/templates content from new embedded resources (`specforge.shared/**` and `specforge.templates/**` resource prefixes). When the config already exists, `init` reads it, runs the DEC-006 in-memory upgrade chain to the current `schemaVersion`, **fully replaces** the `packages[]` array with the args, and writes the upgraded form. Behavioral-file writes follow the DEC-005 always-overwrite rule for `SPECFORGE.md` and the DEC-008 tagged-block-only rule for `CLAUDE.md`/`AGENTS.md`. The `behavioralFiles: all|claude-only|codex-only|none` arg from DEC-008 lets callers opt out of behavioral writes while still updating the config.

- 5th MCP tool. Tool count: 4 → 5.
- Two new embedded-resource globs in `Specforge.Core.csproj` mirror the DEC-005 skills mechanism for canonical shared docs and templates.
- No new error codes — reuses existing config error codes plus `specforge.tool.invalid_argument` for malformed tagged blocks.
- Re-init behavior: full `packages[]` replacement + schema upgrade (resolved via /ask 2026-05-28). Incremental adds out of scope (future `add_package` tool).

## Problem Slice

This item resolves "how does a fresh or existing project become a specforge-managed workspace in one tool call?"

Explicit non-goals (owned by other items):

- A future `add_package(name, path)` MCP tool for incremental package additions — out of MVP.
- A future `remove_package(name)` tool — out of MVP (also requires careful spec-graph cleanup semantics).
- Authoring the canonical content of `spec/shared/*` and `spec/templates/*` — that content was authored manually in Stage 0 and lives in this repo already. ITEM-006 only embeds and copies it.
- The seven SKILL.md bodies — `ITEM-012-SKILL-CONTENT`. (Catalog amended from six to seven on 2026-05-28.)
- `validate` tool — `ITEM-010-VALIDATE-TOOL`.
- Spec-graph operations (decisions, items, ledger appends) — `ITEM-007`/`ITEM-008`/`ITEM-009`.
- User-facing documentation (top-level README rewrite) — `ITEM-013-DOCS-GETTING-STARTED`.

## Terminology Used

- **Adoption mode**: `scaffold: false` (default). `init` writes the config + behavioral files only; package directories and shared/templates are assumed to exist (the user owns the layout).
- **Scaffold mode**: `scaffold: true`. `init` additionally creates the package directory skeleton(s) and copies canonical shared/templates content from embedded resources. Used for fresh-project adoption.
- **Tagged block**: the `<!-- specforge:start --> ... <!-- specforge:end -->` region in `CLAUDE.md`/`AGENTS.md` that `init` owns per DEC-008.
- **Behavioral files**: `SPECFORGE.md` + tagged blocks in `CLAUDE.md`/`AGENTS.md`. Per DEC-008, opt-out via `behavioralFiles: none`.
- **`agents/openai.yaml`**: Codex's project-level MCP dependency declaration. Written under `<project>/agents/openai.yaml` (a directory, not the file name `agents.yaml`).
- **Embedded template catalog**: a new Core service paralleling `IEmbeddedSkillCatalog` from ITEM-004 but for `spec/shared/**` and `spec/templates/**` resources.

## Approved Decisions

- `DEC-002-CONFIGURATION-AND-DISCOVERY` — sections "Configuration File", "Configuration Schema (version 1)", "Discovery Flow", "Consequences" ("`init` must be able to both generate a conforming layout and write a config pointing at an existing layout").
- `DEC-005-SKILL-INSTALLATION-MODEL` — section "Scope Boundary with `init` (DEC-007)" — confirms `agents/openai.yaml` is `init`'s responsibility, not `install_skills`'s.
- `DEC-006-SCHEMA-VERSIONING` — sections "Unsupported-Version Behavior", "Migration: In-Memory Upgrade + Opportunistic Write" — `init` is the canonical "opportunistic write" moment for upgrading a config from v_M to v_N.
- `DEC-007-MVP-TOOL-SET` — section "Setup" (the `init` row), "Surface Principles" (rich JSON Schema + `dryRun`), "Error-Code Catalog" (codes reused: `tool.invalid_argument`, `config.validation_failed`, `config.schema_version_unsupported`).
- `DEC-008-INSTRUCTION-LAYER-DESIGN` — sections "Project-Level Files" (`SPECFORGE.md` + tagged-block convention), "`init` Amendment (DEC-007 extension)" (the `behavioralFiles` arg and the three new file outputs), "Re-`init` Behavior".
- Conversation 2026-05-28 round 6 — `init` re-init fork: full replacement of `packages[]` + schema upgrade (recommended option).

## Current Code State

After `ITEM-005-SKILL-INSTALLER` lands:

- `Specforge.Core` carries `Configuration/`, `Identifiers/`, `Skills/`, `Diagnostics/` plus all typed exceptions through the 12th code.
- `Specforge.Mcp` registers four tools (`list_packages`, `use_package`, `info`, `install_skills`).
- `Specforge.Core.csproj` has one `<EmbeddedResource>` block (for `spec/skills/**`); ITEM-006 adds two more (for `spec/shared/**` and `spec/templates/**`).
- `spec/shared/` and `spec/templates/` already exist in the repo (authored manually in Stage 0); ITEM-006 wraps them into the build pipeline as embedded resources.
- No `init` tool. No `ConfigWriter`. No `SPECFORGE.md`/tagged-block infrastructure.
- The dogfooded `D:\Work\specforge\.specforge.json` from ITEM-002 exists. Running this item's `init` against the live repo must produce a byte-equivalent file (idempotency).

## Target Behavior

After this item is Done:

1. **`InitOptions` value object** carries the tool inputs: `string? Path`, `IReadOnlyList<InitPackageInput> Packages`, `string Shared = "spec/shared"`, `string Templates = "spec/templates"`, `bool Scaffold = false`, `BehavioralFilesScope BehavioralFiles = BehavioralFilesScope.All`, `bool DryRun = false`. `InitPackageInput` is `{Name, Path, ExtraKinds?}`.
2. **`InitResult` value object** carries the per-file outcomes: `{WrittenPaths: string[], OverwrittenPaths: string[], SkippedPaths: string[]}` plus a sub-section for each major area (config / behavioral / scaffold).
3. **Path resolution**: `Path` defaults to the working directory the MCP host was launched with (already available to Core via the host bootstrap; falls back to `Environment.CurrentDirectory`). All paths inside the options are stored relative to that root and resolved at write time.
4. **Config write semantics**:
   - If `<root>/.specforge.json` does not exist: write a fresh v_N config with the args.
   - If it does exist: read it; apply the in-memory upgrade chain from `ConfigLoader` (ITEM-002) to bring it to v_N; **replace `packages[]` with the args** (full replacement); preserve `shared`/`templates` from args (defaults applied if absent); write back.
   - The write is atomic-ish: write to `<path>.tmp`, then `File.Move(temp, target, overwrite: true)` to minimize partial-state risk.
5. **`SPECFORGE.md` write**: always overwrite per DEC-008. Content generated from the live config — sections `Spec layout`, `Identifier scheme`, `Tools available`, `Skills available`, `Lifecycle`, `Generated by` (with `init` date, binary version, schemaVersion).
6. **Tagged-block writes**:
   - `<root>/CLAUDE.md`: locate `<!-- specforge:start -->` and `<!-- specforge:end -->`. If both present, replace only the content between them. If neither present, prepend the full block (with one blank line of separation) to the file. If file does not exist, create it with only the tagged block.
   - `<root>/AGENTS.md`: same logic, same block content.
   - Imbalanced delimiters (start without end, or vice versa, or end before start): throw `SpecforgeException` with code `specforge.tool.invalid_argument`, `data.argument` naming the file path, `suggestion` directing to "remove the partial block or run init with behavioralFiles=none".
   - Block content per DEC-008's `Tagged Block in CLAUDE.md and AGENTS.md` section.
7. **Codex `agents/openai.yaml` write**:
   - Path: `<root>/agents/openai.yaml` (creates the `agents/` directory if absent).
   - Content: minimal YAML declaring `mcp_servers.specforge.command = "specforge"` (or whichever invocation form Codex expects per its current spec); exact shape pinned at implementation time and documented in the implementing commit.
   - Always overwrite — `init` owns this file end-to-end.
8. **Scaffold mode behavior** (`scaffold: true`):
   - For each package in args: create `<root>/<package.path>/` plus subdirs `decisions/`, `items/`, `ledger/`.
   - Write package-level skeleton files: `README.md`, `work_plan.md`, `work_ledger.md`, `decisions/README.md`, `items/README.md`, `ledger/README.md`, `ledger/artifacts.md`, `ledger/items.md`, `ledger/commits.md`, `ledger/reviews.md`, `ledger/history.md` — populated with minimal Status/header content per the canonical examples in `spec/packages/specforge-mvp/` (also embedded for self-reference).
   - Copy embedded `specforge.shared/**` resources to `<root>/<shared>/` (default `<root>/spec/shared/`).
   - Copy embedded `specforge.templates/**` resources to `<root>/<templates>/` (default `<root>/spec/templates/`).
   - Pre-existing files at any of these targets are skipped (recorded in `result.SkippedPaths`) — scaffold is non-destructive on existing content. The user re-running `init scaffold=true` does not lose hand-edited templates.
9. **`behavioralFiles` arg semantics**:
   - `all` (default): write `SPECFORGE.md` + both tagged blocks.
   - `claude-only`: write `SPECFORGE.md` + `CLAUDE.md` block; skip `AGENTS.md`.
   - `codex-only`: write `SPECFORGE.md` + `AGENTS.md` block; skip `CLAUDE.md`.
   - `none`: skip all three behavioral files.
   - The `agents/openai.yaml` write is always performed regardless of `behavioralFiles` (DEC-005 boundary — it's Codex MCP-wiring, not behavioral guidance).
10. **`dryRun: true`**: return the full `InitResult` describing every planned write (including planned tagged-block content as a string), without any filesystem changes. The user can review the plan before committing.
11. **Idempotency dogfood**: running `init` against this repo's existing `.specforge.json` with `packages: [{name: "specforge-mvp", path: "spec/packages/specforge-mvp"}]`, `shared: "spec/shared"`, `templates: "spec/templates"` produces zero changes (the file is rewritten byte-equivalent, the tagged blocks match the existing block content, `SPECFORGE.md` regenerates byte-equivalent content). Used as a smoke test.

## Invariants

- **`Specforge.Core` references no `ModelContextProtocol.*` assembly.** Architecture test from ITEM-001 still passes after adding the new embedded resources.
- **Tagged-block delimiters are exact strings** (`<!-- specforge:start -->` / `<!-- specforge:end -->`), defined as `const string` in one place so a future change is a single edit.
- **User content outside the tagged block is byte-for-byte preserved** through a re-`init`.
- **`SPECFORGE.md` is specforge-owned** — always overwritten, never merged.
- **`agents/openai.yaml` is specforge-owned** — always overwritten.
- **Scaffold mode never destroys existing files** — skips and records.
- **Config write is atomic via temp-rename.**
- **The two new embedded-resource globs do not double-embed `spec/skills/**`**: `Include` patterns are scoped tightly to `spec/shared/**` and `spec/templates/**` only.

## Code Scope

**In scope (created or modified by this item):**

`Specforge.Core` (new):

- `Init/InitOptions.cs` — record/value object as described above.
- `Init/InitPackageInput.cs` — record `{string Name, string Path, IReadOnlyList<string>? ExtraKinds}`.
- `Init/InitResult.cs` — top-level result with per-section breakdown.
- `Init/InitSectionResult.cs` — `{int WrittenCount, IReadOnlyList<string> WrittenPaths, IReadOnlyList<string> OverwrittenPaths, IReadOnlyList<string> SkippedPaths}`.
- `Init/BehavioralFilesScope.cs` — enum.
- `Init/ConfigWriter.cs` — serializes `SpecforgeConfig` to JSON; atomic temp-rename write.
- `Init/SpecforgeMdGenerator.cs` — renders `SPECFORGE.md` from a live config + `BinaryInfo`.
- `Init/TaggedBlockMerger.cs` — find/replace logic; throws on imbalanced delimiters.
- `Init/CodexOpenaiYamlWriter.cs` — writes the minimal Codex MCP-dep YAML.
- `Init/EmbeddedTemplate.cs` — record `{string LogicalPath, Func<Stream> OpenStream}`.
- `Init/IEmbeddedTemplateCatalog.cs` — interface.
- `Init/EmbeddedTemplateCatalog.cs` — default implementation reading the two new resource prefixes.
- `Init/ScaffoldEngine.cs` — orchestrates package-dir skeleton + template/shared copy; non-destructive merge.
- `Init/IInitService.cs` + `Init/InitService.cs` — high-level orchestration tying everything together.

`Specforge.Core.csproj` (modifications):

- Add a new `<ItemGroup>`:
  ```xml
  <ItemGroup>
    <EmbeddedResource Include="..\..\spec\shared\**\*.*">
      <LogicalName>specforge.shared/%(RecursiveDir)%(Filename)%(Extension)</LogicalName>
    </EmbeddedResource>
    <EmbeddedResource Include="..\..\spec\templates\**\*.*">
      <LogicalName>specforge.templates/%(RecursiveDir)%(Filename)%(Extension)</LogicalName>
    </EmbeddedResource>
  </ItemGroup>
  ```

`Specforge.Mcp` (new):

- `Tools/InitTool.cs` — implements `IMcpTool` with the full JSON Schema for `init` args (packages array with name/path/extraKinds, shared, templates, scaffold, behavioralFiles enum, dryRun, optional path).

`Specforge.Mcp` (modifications):

- `Tools/ToolExceptionMapper.cs` — no new code mappings; ensure `specforge.tool.invalid_argument` covers the new tagged-block-imbalance case via the `argument`/`expected`/`suggestion` fields.
- `Hosting/SpecforgeCoreServices.cs` — register `IEmbeddedTemplateCatalog`, `IInitService`, plus the writer/generator/merger services.
- `Program.cs` — register `InitTool` alongside the four existing tools.

`Specforge.Tests` (new):

- `Init/ConfigWriterTests.cs` — write fresh config; upgrade an existing v1 config (no-op for current `supportedMax=1`); full replacement of `packages[]`; atomic temp-rename.
- `Init/SpecforgeMdGeneratorTests.cs` — produces expected sections from a fixture config + fixture `BinaryInfo`.
- `Init/TaggedBlockMergerTests.cs` — happy path (block present); prepend (no block); create-file (no file); imbalanced delimiters throw with the expected envelope shape via `ToolExceptionMapper`.
- `Init/CodexOpenaiYamlWriterTests.cs` — produces the expected YAML.
- `Init/EmbeddedTemplateCatalogTests.cs` — enumerates `specforge.shared/**` and `specforge.templates/**` from the production resources; against the test assembly with fixture resources.
- `Init/ScaffoldEngineTests.cs` — fresh dir gets full skeleton; existing dir non-destructive merge; shared/templates copy.
- `Init/InitServiceTests.cs` — orchestration: each `behavioralFiles` value produces the expected file set; `scaffold=true` adds the scaffold set; `dryRun=true` writes nothing.
- `Tools/InitToolTests.cs` — in-proc invocation; envelope shape on success; envelope shape on `invalid_argument`.

**Out of scope (deferred to later items):**

- Future `add_package` / `remove_package` MCP tools.
- Live editing of `SPECFORGE.md` user-visible content shape (currently hardcoded in the generator; future DEC may parameterize).
- macOS / Linux path resolution for project roots.

## Test Scope

Eight new test classes, one extension. ~50-60 new tests. xUnit; temp-dir pattern from ITEM-005 reused; test assembly embeds fixture templates under a `test.shared/`/`test.templates/` prefix to keep production and test catalogs separate.

## Test Plan

1. Implement the new `<EmbeddedResource>` blocks; verify `dotnet build` succeeds and Specforge.Core.dll carries the new resource names.
2. Implement `IEmbeddedTemplateCatalog` + tests.
3. Implement `ConfigWriter`, `SpecforgeMdGenerator`, `TaggedBlockMerger`, `CodexOpenaiYamlWriter`; each with its own test class.
4. Implement `ScaffoldEngine` + tests using temp dirs.
5. Implement `IInitService` (orchestration) + tests; cover every `behavioralFiles` value; cover `dryRun`.
6. Implement `InitTool` + `InitToolTests`.
7. Manual smoke: run the dogfood idempotency check — `init` against this repo's existing `.specforge.json` produces zero changes.
8. Manual smoke: run `init scaffold=true` against an empty temp dir; verify the resulting tree matches the canonical shape under `spec/packages/specforge-mvp/`.
9. Run `dotnet test`; verify all new tests pass + all prior-item tests still pass + architecture test still passes.

## Test Evidence

- Console output of `dotnet build` (zero warnings).
- Console output of `dotnet test` (all pass).
- A transcript at `test/Specforge.Tests/Evidence/itm006-dogfood-idempotency.txt` showing `init` against this repo producing zero filesystem changes.
- A transcript at `test/Specforge.Tests/Evidence/itm006-scaffold-fresh.txt` showing `init scaffold=true` against an empty temp dir producing the canonical tree.
- A transcript at `test/Specforge.Tests/Evidence/itm006-tagged-block-imbalance.txt` showing the error envelope for a malformed CLAUDE.md.

## Impact Assessment

| Aspect | Impact | Notes |
|---|---|---|
| MCP tool surface | Direct | 5th tool ships; `init` is the only setup-category tool that writes to a target project's filesystem outside the user's home dir. |
| Project-level integration | Direct | Realizes DEC-008's `SPECFORGE.md` + tagged-block convention. |
| Configuration discovery | Direct | First tool that writes (vs only reads) `.specforge.json`. |
| Schema versioning | Direct | First realization of DEC-006's opportunistic-write rule. |
| Skill packaging | No impact | `install_skills` is a separate ritual (DEC-005 boundary). |
| Build pipeline | Direct | Two new embedded-resource globs (shared + templates). |
| Core library boundary | No impact | All new infrastructure Core-side; architecture test still passes. |
| Error handling | Indirect | New `argument` value (tagged-block-file) for the existing `tool.invalid_argument` code. No new codes. |
| Documentation | Indirect | Top-level README (ITEM-013) must describe `init` as the first-touch ritual. |
| External adoption | Direct | `init scaffold=true` is the adoption path for fresh projects; adoption mode (`scaffold=false`) is the path for existing layouts. |
| Test coverage scope | Direct | ~50-60 new tests + a 3-evidence-transcript smoke suite. |
| Performance | No measurable | At most a handful of small file writes per call. |
| Concurrency | No measurable | Single-session process; one `init` call expected per user action. |
| User customization | Indirect | Tagged-block convention preserves user content outside the block. `behavioralFiles=none` is the opt-out. |
| Maintenance burden | Indirect | Adding a behavioral-file output is a one-place edit in `InitService` plus a new enum value. |

## Validation

- **Build**: `dotnet build Specforge.sln -c Release` succeeds with zero warnings.
- **Test**: `dotnet test` succeeds; all new tests pass; all prior-item tests + architecture test still pass.
- **Boundary**: ITEM-001's architecture test still passes after the two new embedded-resource globs.
- **Dogfood idempotency**: running `init` against this repo's live config produces zero filesystem diffs (captured in the evidence transcript).
- **Scaffold against empty**: produces a tree byte-equivalent to the structure at `spec/packages/specforge-mvp/` minus the actual DEC/ITEM/ledger row content.
- **Tagged-block survival**: hand-author a CLAUDE.md containing the tagged block surrounded by user content; re-run `init`; user content remains byte-for-byte.
- **Behavioral opt-out**: `init behavioralFiles=none` writes `.specforge.json` and `agents/openai.yaml` only; no `SPECFORGE.md`, no `CLAUDE.md`/`AGENTS.md` changes.
- **Re-init replacement**: existing config with `packages: [A, B]`; run `init packages: [C]`; config now contains only `C`.

## Open Questions

- Exact YAML shape for `agents/openai.yaml` — pinned at implementation time against the current Codex documentation. Not blocking the spec.
- Whether `SPECFORGE.md` should also list `extraKinds` per package — yes; finalize in `SpecforgeMdGenerator` implementation.
- Whether re-running `init` against an existing config should preserve `extraKinds` on each package when the args omit them — leaning no (full replacement is full replacement; callers must pass the desired final state). Finalize during implementation.
- Whether the tagged-block content should embed a generation timestamp — leaning no (the timestamp lives in `SPECFORGE.md`'s footer; the tagged block stays minimal and stable to avoid noisy diffs on every re-`init`).

## Done Criteria

The item is **Done** (post-Approved) when:

1. All files listed in "Code Scope (In scope)" exist at the specified paths.
2. `Specforge.Core.csproj` carries the two new `<EmbeddedResource>` globs.
3. `dotnet build Specforge.sln -c Release` reports zero warnings.
4. `dotnet test test/Specforge.Tests -c Release` runs all ~50-60 new tests plus every prior-item test; all pass.
5. `Specforge.Core.dll` carries no `ModelContextProtocol.*` reference (architecture test).
6. `Specforge.Mcp` registers five tools (`list_packages`, `use_package`, `info`, `install_skills`, `init`).
7. The three smoke transcripts (dogfood-idempotency, scaffold-fresh, tagged-block-imbalance) exist under `test/Specforge.Tests/Evidence/`.
8. A `CMT-NNN` row is appended to `ledger/commits.md` recording the implementation commit's short SHA.
9. A history event is appended to `ledger/history.md` marking the transition.

## Links

- `../decisions/DEC-002-CONFIGURATION-AND-DISCOVERY.md` (approved) — sections "Configuration File", "Configuration Schema (version 1)", "Consequences".
- `../decisions/DEC-005-SKILL-INSTALLATION-MODEL.md` (approved) — section "Scope Boundary with `init` (DEC-007)" — `agents/openai.yaml` ownership.
- `../decisions/DEC-006-SCHEMA-VERSIONING.md` (approved) — sections "Unsupported-Version Behavior", "Migration: In-Memory Upgrade + Opportunistic Write".
- `../decisions/DEC-007-MVP-TOOL-SET.md` (approved) — section "Setup" (`init` row), "Surface Principles", "Error-Code Catalog".
- `../decisions/DEC-008-INSTRUCTION-LAYER-DESIGN.md` (approved) — sections "Project-Level Files", "`init` Amendment", "Re-`init` Behavior".
- `./ITEM-002-CONFIG-MODEL.md` (approved) — `ConfigLoader` reused for the read+upgrade leg; `IMcpTool` pattern extended.
- `./ITEM-003-ID-VALIDATOR.md` (approved) — `extraKinds` validation invoked when args carry them.
- `./ITEM-004-EMBEDDED-SKILL-CATALOG.md` (approved) — embedded-resource pattern reused for shared+templates.
- `./ITEM-005-SKILL-INSTALLER.md` (approved) — DI registration patterns + result-shape conventions reused.
- `../../../templates/item_spec.md` — item template.
- `../../../shared/spec_item_contract.md` — required-section contract.
- `../../../shared/document_lifecycle.md` — status states.
- `../../../shared/impact_assessment_checklist.md` — aspects checklist.

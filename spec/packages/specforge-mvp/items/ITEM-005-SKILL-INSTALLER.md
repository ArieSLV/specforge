# ITEM-005-SKILL-INSTALLER - Skill Installer and `install_skills` Tool

Status: Approved
Review owner: User (approved 2026-05-28)
Depends on: `ITEM-002-CONFIG-MODEL`, `ITEM-004-EMBEDDED-SKILL-CATALOG`, `DEC-005-SKILL-INSTALLATION-MODEL`, `DEC-007-MVP-TOOL-SET`
Updates ledger rows: new `ART-ITEM-005`; new `CMT-NNN` rows for implementation commits

## Handoff Summary

Take the embedded skill catalog from `ITEM-004-EMBEDDED-SKILL-CATALOG` and the MCP host wiring from `ITEM-002-CONFIG-MODEL` and ship the first end-to-end skill flow: `ISkillInstaller` in `Specforge.Core` orchestrates per-agent installation by reading the embedded catalog and writing to user-wide skill directories via an `ISkillInstallTargetResolver` (default resolves `%USERPROFILE%\.claude\skills\` and `%USERPROFILE%\.agents\skills\`; DI-swappable so tests never touch real home dirs). `install_skills` MCP tool in `Specforge.Mcp` exposes the operation per DEC-007's contract (args `agent: claude-code|codex|all`, `dryRun: bool`; rich JSON Schema; idempotent always-overwrite reinstall). Adds the twelfth code (`specforge.skills.install_failed`) to `ToolExceptionMapper`. The partial-failure semantics from DEC-005 — one agent fails, the other's writes are not rolled back, the result reports both states — are realized via a structured failure envelope that captures successful-and-failed agents in a single `data` payload.

- First MCP tool that performs filesystem writes outside the spec graph.
- After this item, a fresh machine runs `install_skills` once and gets the embedded skills (currently zero — ITEM-012 populates them) materialized in both `~/.claude/skills/` and `~/.agents/skills/`.
- The empty-catalog smoke from ITEM-004 carries forward: `install_skills` against a zero-skill catalog returns success with zero counts, no files written.

## Problem Slice

This item resolves "how do embedded skills reach the user's agent skill directories, and what does the MCP tool surface for that operation look like?"

Explicit non-goals (owned by other items):

- Authoring the seven production SKILL.md bodies — `ITEM-012-SKILL-CONTENT`. ITEM-005 ships with zero or test-fixture skills; production content comes later. (Catalog amended from six to seven on 2026-05-28.)
- Project-level Codex `agents/openai.yaml` wiring — `ITEM-006-INIT-TOOL` per DEC-005's scope boundary.
- `SPECFORGE.md` and the `CLAUDE.md`/`AGENTS.md` tagged-block writes — `ITEM-006` per DEC-008.
- Uninstall — deferred (DEC-005 Open Question).
- Manifest tracking installed skill versions — deferred (DEC-005 Open Question).
- macOS / Linux path resolution — deferred until cross-platform support is in scope (DEC-001 fixed Win-x64 only for MVP).

## Terminology Used

- **Agent**: target environment receiving skills. MVP supports two: Claude Code (`claude-code`) and Codex CLI (`codex`).
- **Target path**: the user-wide directory each agent reads skills from. Resolved via `Environment.SpecialFolder.UserProfile` per DEC-005.
- **Dry run**: `dryRun: true` argument that reports planned writes without touching the filesystem. Same convention as ITEM-006 onward.
- **Always-overwrite reinstall**: per DEC-005, every `install_skills` call rewrites the full catalog; no diff, no merge, no prompt.
- **Partial failure**: one agent's writes succeed, another's fail. Per DEC-005 the failure does not roll back the successful agent's writes; the tool's failure envelope reports both states.
- **`SkillInstallAgent` enum**: in-code representation of the agent choice (`ClaudeCode`, `Codex`, plus the `All` aggregate).

Glossary cross-references: `../../../shared/glossary.md`.

## Approved Decisions

- `DEC-005-SKILL-INSTALLATION-MODEL` — sections "Install Tool Contract", "Target Install Paths", "Re-install Behavior", "Ownership of the Skill Namespace", "Scope Boundary with `init` (DEC-007)", "Error Types".
- `DEC-007-MVP-TOOL-SET` — section "Error-Code Catalog" row `specforge.skills.install_failed`; "Surface Principles" (rich JSON Schema, `dryRun`); "MVP Tool Catalog" entry for `install_skills` in Setup.
- `DEC-003-RUNTIME-AND-ARCHITECTURE` — sections "Dependency Direction", "Error Handling", "Logging and Diagnostics".

## Current Code State

After `ITEM-004-EMBEDDED-SKILL-CATALOG` lands:

- `Specforge.Core` carries `Skills/` (catalog, frontmatter parser, validator, exception) plus the `Configuration/` and `Identifiers/` infrastructure from earlier items.
- `Specforge.Core.csproj` embeds `spec/skills/**` resources (currently filtered to zero by `.gitkeep`-only content).
- `Specforge.Mcp.Program.cs` calls `SkillCatalogValidator.Validate()` at startup; the validator is a no-op against the empty catalog.
- `ToolExceptionMapper` covers eleven codes; ITEM-005 adds the twelfth.
- `install_skills` MCP tool does not yet exist.
- `Specforge.Mcp` registers three tools (`list_packages`, `use_package`, `info`); ITEM-005 brings it to four.

## Target Behavior

After this item is Done:

1. **`ISkillInstaller.InstallAsync(SkillInstallAgent agent, bool dryRun, CancellationToken ct)`** orchestrates the full flow. Returns `SkillInstallResult` on full success. Throws `SpecforgeSkillInstallException` carrying partial-state data on any agent failure.
2. **`ISkillInstallTargetResolver.Resolve(SkillInstallAgent agent)`** returns the absolute directory for a single agent. Default implementation:
   - `ClaudeCode` → `Path.Combine(Environment.GetFolderPath(SpecialFolder.UserProfile), ".claude", "skills")`
   - `Codex` → `Path.Combine(Environment.GetFolderPath(SpecialFolder.UserProfile), ".agents", "skills")`
   - `All` is never passed to `Resolve` (the installer iterates the two concrete agents).
3. **Per-agent installation procedure**:
   1. Resolve target root for the agent.
   2. For each `EmbeddedSkill` in `IEmbeddedSkillCatalog.GetSkills()`:
      1. Compute the skill's target directory: `<agentRoot>\<skill.Name>\`.
      2. For each `EmbeddedSkillFile` in the skill:
         1. Compute target file path: `<skillTargetDir>\<relativeFilePath>` where `relativeFilePath` is the logical path minus the `specforge.skills/<skill-name>/` prefix.
         2. If `dryRun`, record the planned path and continue without I/O.
         3. Otherwise, ensure parent directories exist; check if the file already exists (record as overwrite); open `EmbeddedSkillFile.OpenStream()` and write byte-for-byte.
         4. On `IOException`/`UnauthorizedAccessException`/etc., abort the current agent and throw `SpecforgeSkillInstallException` carrying both the failure detail and the accumulated successful-agent results.
4. **`SkillInstallResult`** record:
   ```csharp
   record SkillInstallResult(
       IReadOnlyDictionary<string, SkillInstallAgentResult> Agents,
       string BinaryVersion);
   record SkillInstallAgentResult(
       int WrittenCount,
       IReadOnlyList<string> WrittenPaths,
       IReadOnlyList<string> OverwrittenPaths);
   ```
   The `Agents` dictionary key is the kebab-case agent name (`"claude-code"`, `"codex"`); values describe completed work.
5. **`SpecforgeSkillInstallException`** carries:
   ```csharp
   {
       string Agent,                    // failing agent name
       string Path,                     // failing target path
       Type InnerExceptionType,         // typeof(IOException), typeof(UnauthorizedAccessException), etc.
       string InnerMessage,             // the inner exception's Message
       SkillInstallResult PartialResult // successful agents' results + the failing agent's pre-failure progress
   }
   ```
   The `ToolExceptionMapper` projects this into the envelope `data` per DEC-007 plus a sibling `partialResult` field carrying the result-shape JSON. `suggestion`: `"check filesystem permissions on the target directory"`.
6. **`install_skills` MCP tool** signature (rich JSON Schema):
   ```jsonc
   {
       "name": "install_skills",
       "description": "Install the embedded skill catalog to the user-wide agent skill directories. Always-overwrite reinstall; idempotent. Per DEC-005.",
       "inputSchema": {
           "type": "object",
           "properties": {
               "agent": {
                   "type": "string",
                   "enum": ["claude-code", "codex", "all"],
                   "default": "all",
                   "description": "Restrict installation to one agent. Default 'all' covers both Claude Code and Codex."
               },
               "dryRun": {
                   "type": "boolean",
                   "default": false,
                   "description": "Report planned writes without touching the filesystem."
               }
           },
           "additionalProperties": false
       }
   }
   ```
7. **Result payload shape** (success envelope):
   ```jsonc
   {
       "agents": {
           "claude-code": {
               "writtenCount": 12,
               "writtenPaths": [".../.claude/skills/draft-decision/SKILL.md", ...],
               "overwrittenPaths": [".../.claude/skills/draft-decision/SKILL.md"]
           },
           "codex": { /* same shape */ }
       },
       "binaryVersion": "0.1.0+abc123"
   }
   ```
8. **Empty catalog**: `install_skills` against an empty `IEmbeddedSkillCatalog` returns success with `writtenCount: 0`, empty arrays, populated `binaryVersion`. No filesystem traversal beyond resolving target roots.
9. **Single-agent invocation**: `install_skills agent=claude-code` populates only the `claude-code` entry in the result; the `codex` entry is absent (not zero — absent — to make the intent unambiguous).
10. **`dryRun`**: result identical in shape to a real call, but `overwrittenPaths` is derived from existence checks (still reads the filesystem to determine which paths would overwrite) while `writtenPaths` lists what *would* be written. No directory creation, no file writes.

## Invariants

- **`Specforge.Core` references no `ModelContextProtocol.*` assembly.** Architecture test from ITEM-001 still passes.
- **Tests never touch real `%USERPROFILE%\.claude\skills\` or `%USERPROFILE%\.agents\skills\`.** Every test uses a temp-dir-backed `ISkillInstallTargetResolver`.
- **All writes are byte-for-byte from the embedded resource stream.** No content transformation, no line-ending normalization, no whitespace trimming.
- **Per-skill atomicity is NOT a guarantee.** Per DEC-005, a write failure mid-skill leaves the already-written files in place; no rollback. ITEM-005 documents this; clients are expected to retry idempotently.
- **`install_skills` is idempotent** when no underlying filesystem error occurs. Calling twice in a row produces the same result (second call's `overwrittenPaths` equals the first call's `writtenPaths`).
- **Path separator is platform-native** (`Path.Combine`). Logical paths use `/`; target paths use `\` on Windows.
- **The tool description and JSON Schema match DEC-005 and DEC-007 verbatim.** Argument enum values, defaults, descriptions all align.

## Code Scope

**In scope (created or modified by this item):**

`Specforge.Core` (new files):

- `Skills/SkillInstallAgent.cs` — enum: `ClaudeCode`, `Codex`, `All`.
- `Skills/SkillInstallResult.cs` — top-level result record.
- `Skills/SkillInstallAgentResult.cs` — per-agent result record.
- `Skills/ISkillInstaller.cs` — interface with `Task<SkillInstallResult> InstallAsync(SkillInstallAgent agent, bool dryRun, CancellationToken ct)`.
- `Skills/SkillInstaller.cs` — default implementation orchestrating catalog × agents × files; depends on `IEmbeddedSkillCatalog`, `ISkillInstallTargetResolver`, `BinaryInfo`, `ILogger<SkillInstaller>`.
- `Skills/ISkillInstallTargetResolver.cs` — interface with `string Resolve(SkillInstallAgent agent)`.
- `Skills/SkillInstallTargetResolver.cs` — default implementation reading `Environment.SpecialFolder.UserProfile`.
- `Exceptions/SpecforgeSkillInstallException.cs` — `ErrorCode = "specforge.skills.install_failed"`, carries `Agent`, `Path`, `InnerExceptionType`, `InnerMessage`, `PartialResult`.

`Specforge.Mcp` (new files):

- `Tools/InstallSkillsTool.cs` — implements `IMcpTool`; rich JSON Schema declared as a `JsonNode`/`JsonElement` constant; `InvokeAsync` deserializes args, calls `ISkillInstaller`, projects result to `ToolResult.Success`; on exception, catches and lets the mapper produce the failure envelope.

`Specforge.Mcp` (modifications):

- `Tools/ToolExceptionMapper.cs` — add a `case` arm for `SpecforgeSkillInstallException`. The mapper produces the full envelope including `partialResult` derived from the exception's `PartialResult` property.
- `Hosting/SpecforgeCoreServices.cs` — register `ISkillInstaller → SkillInstaller`, `ISkillInstallTargetResolver → SkillInstallTargetResolver` as singletons.
- `Program.cs` — register `InstallSkillsTool` alongside `ListPackagesTool`, `UsePackageTool`, `InfoTool`.

`Specforge.Tests` (new files):

- `Skills/SkillInstallerTests.cs` — happy-path against test-fixture catalog and temp dirs; per-agent isolation; single-agent invocation; empty catalog; overwrite detection.
- `Skills/SkillInstallTargetResolverTests.cs` — verifies the two known agent paths on Windows; rejects `All`.
- `Skills/PartialFailureTests.cs` — uses a fake target resolver where one agent path is unwritable; verifies the exception carries both successful-agent's results and the failing-agent's partial progress.
- `Tools/InstallSkillsToolTests.cs` — in-proc invocation; verifies success envelope shape; verifies `dryRun` envelope; verifies failure envelope carries the partial-result payload.
- `Tools/ToolExceptionMapperTests.cs` — extend with `SpecforgeSkillInstallException` → envelope mapping including the `partialResult` field.

**Out of scope (deferred to later items):**

- Six production SKILL.md files — `ITEM-012-SKILL-CONTENT`. ITEM-005 ships with the empty catalog from ITEM-004; tool round-trips return zero-count results.
- `init` tool and project-level files (`SPECFORGE.md`, `agents/openai.yaml`, tagged blocks in `CLAUDE.md`/`AGENTS.md`) — `ITEM-006-INIT-TOOL`.
- Uninstall / manifest tracking — deferred.
- macOS / Linux path variants — deferred.
- Two-phase `previewToken` confirmation for installs — deferred (DEC-005's overwrite-always model is the MVP stance).

## Test Scope

Five new test classes plus an extension of `ToolExceptionMapperTests`. ~30-40 new tests. xUnit only. All tests use temp directories via `Path.GetTempPath()` plus `Guid.NewGuid()`; each test cleans up in a `Dispose` method (or via xUnit's `IAsyncLifetime` for async cleanup).

## Test Plan

1. Implement `SkillInstallAgent`, `SkillInstallResult`, `SkillInstallAgentResult` records.
2. Implement `ISkillInstallTargetResolver` and the default impl; write `SkillInstallTargetResolverTests`.
3. Implement `SpecforgeSkillInstallException` with the partial-result payload.
4. Implement `ISkillInstaller` + default impl; write `SkillInstallerTests` against a fixture catalog (reuse the test fixtures from ITEM-004 — `valid-skill` and the `test.skills/` prefix).
5. Implement the partial-failure path: a custom test-only `ISkillInstallTargetResolver` returns a path whose parent is read-only (use `Directory.CreateDirectory` + `DirectoryInfo.Attributes |= FileAttributes.ReadOnly` on Windows). Verify `SpecforgeSkillInstallException` carries the right shape.
6. Implement `InstallSkillsTool`; wire it into `Program.cs`; write `InstallSkillsToolTests` using the in-proc tool invocation pattern from ITEM-002.
7. Extend `ToolExceptionMapper` and its tests with the new exception → envelope mapping including `partialResult`.
8. Smoke: launch the published `specforge.exe`, send an `install_skills(agent: "all", dryRun: true)` request via the test harness from ITEM-002, verify the response envelope reports zero counts (no embedded skills yet).
9. Run `dotnet test test/Specforge.Tests -c Release`; all new tests pass; all prior-item tests still pass.

## Test Evidence

- Console output of `dotnet build` (zero warnings).
- Console output of `dotnet test` (all tests pass).
- Architecture test continues to pass.
- A transcript at `test/Specforge.Tests/Evidence/itm005-smoke.txt` capturing an `install_skills` dryRun MCP exchange against the published binary with an empty catalog; the response shows zero writes and the populated `binaryVersion`.
- A transcript at `test/Specforge.Tests/Evidence/itm005-partial-failure.txt` capturing the failure envelope produced by a deliberately-broken test fixture (where one agent's target dir is unwritable).

## Impact Assessment

| Aspect | Impact | Notes |
|---|---|---|
| Skill packaging | Direct | Completes the install half of DEC-005 (embedding side done in ITEM-004). |
| MCP tool surface | Direct | First tool ships in this item from the Setup category. |
| Error handling | Direct | New typed Core exception + envelope mapping (twelfth code). Partial-state payload is the first non-trivial structured `data` shape. |
| Build pipeline | No impact | No new NuGet refs beyond what ITEM-002/004 introduced. |
| Core library boundary | No impact | All new code lives in Core (services) or Mcp (tool); architecture test still passes. |
| Configuration discovery | No impact | DEC-002 mechanics unchanged. |
| ID scheme | No impact | Identifiers don't appear in install paths or arguments. |
| Schema versioning | No impact | DEC-006 mechanics unchanged. |
| Test coverage scope | Direct | ~30-40 new unit tests; temp-dir fixture pattern established for filesystem-touching items. |
| Performance | No measurable | One small file write per skill per agent; counts measured in single digits. |
| Concurrency | No measurable | Per-session process; single concurrent `install_skills` call expected. |
| External adoption | No impact | This item ships skill content to the user's home directory; it does not modify any target project. |
| Documentation | Indirect | Top-level README (ITEM-013) must surface `install_skills` as a first-touch ritual. |
| Maintenance burden | Indirect | Adding a new agent in a future DEC means adding an enum value + a `Resolve` case + extending the tool's `agent` enum schema. |
| User customization | Indirect | DEC-005's documented fork-by-renaming escape hatch still applies; tested by the overwrite-detection logic. |

## Validation

- **Build**: `dotnet build Specforge.sln -c Release` succeeds with zero warnings.
- **Test**: `dotnet test test/Specforge.Tests -c Release` succeeds; new tests pass; all prior-item tests still pass.
- **Boundary**: ITEM-001's architecture test still passes (`Specforge.Core` has no MCP reference).
- **Empty catalog**: `install_skills(all, dryRun: true)` against the current `spec/skills/.gitkeep`-only state returns success with zero counts.
- **Empty catalog real run**: `install_skills(all, dryRun: false)` writes nothing (no skills to write), returns success with zero counts, does not create empty directories.
- **Per-agent isolation**: `install_skills(claude-code)` populates only the `claude-code` entry; `codex` entry absent from the result.
- **Overwrite detection**: pre-populate a test target with a fake `SKILL.md`; run install; verify the path appears in `overwrittenPaths`.
- **Partial failure**: with one agent's target unwritable, the failure envelope carries `partialResult` describing the successful agent's writes and the failed agent's pre-error progress.
- **`dryRun` purity**: a `dryRun: true` call against a non-empty fixture catalog produces an envelope but creates zero files on disk (verified by directory enumeration before and after).

## Open Questions

- Whether the `binaryVersion` field should pin to assembly version + commit SHA + build timestamp — ITEM-005 uses `BinaryInfo.Version` (single string from `AssemblyInformationalVersionAttribute`). Finalize during implementation if more granular reporting is needed.
- Whether `install_skills` should expose a `targets` (plural) argument to override the resolver per call — defer; `agent` is enough for MVP.
- Whether to add a debug log line per file written at `Information` level — leaning yes for traceability; gated by `SPECFORGE_LOG_LEVEL`. Finalize during implementation.
- Whether the failure envelope should also surface the count of *skipped* skills in the failing agent (those after the abort point) — leaning yes; the `PartialResult` for that agent stops counting at the failure but a separate `aborted: true` flag clarifies intent. Finalize during implementation.

## Done Criteria

The item is **Done** (post-Approved) when:

1. All files listed in "Code Scope (In scope)" exist at the specified paths.
2. `dotnet build Specforge.sln -c Release` reports zero warnings.
3. `dotnet test test/Specforge.Tests -c Release` runs every new test (~30-40) plus all prior-item tests; all pass.
4. `Specforge.Core.dll` carries no `ModelContextProtocol.*` reference (architecture test).
5. `Specforge.Mcp` registers four tools (`list_packages`, `use_package`, `info`, `install_skills`).
6. The two smoke transcripts (empty-catalog and partial-failure) exist under `test/Specforge.Tests/Evidence/`.
7. A `CMT-NNN` row is appended to `ledger/commits.md` recording the implementation commit's short SHA.
8. A history event is appended to `ledger/history.md` marking the transition.

## Links

- `../decisions/DEC-005-SKILL-INSTALLATION-MODEL.md` (approved) — sections "Install Tool Contract", "Target Install Paths", "Re-install Behavior", "Ownership of the Skill Namespace", "Scope Boundary with `init` (DEC-007)", "Error Types".
- `../decisions/DEC-007-MVP-TOOL-SET.md` (approved) — sections "Surface Principles", "Error-Code Catalog" (row `specforge.skills.install_failed`), "MVP Tool Catalog" / "Setup".
- `../decisions/DEC-003-RUNTIME-AND-ARCHITECTURE.md` (approved) — sections "Dependency Direction", "Error Handling", "Logging and Diagnostics".
- `./ITEM-002-CONFIG-MODEL.md` (approved) — MCP host bootstrap point this item extends; `IMcpTool` pattern reused; `ToolExceptionMapper` extended.
- `./ITEM-004-EMBEDDED-SKILL-CATALOG.md` (approved) — catalog this item consumes; test-fixture pattern reused.
- `../../../templates/item_spec.md` — item template.
- `../../../shared/spec_item_contract.md` — required-section contract.
- `../../../shared/document_lifecycle.md` — status states.
- `../../../shared/impact_assessment_checklist.md` — aspects checklist.

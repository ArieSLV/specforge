# ITEM-004-EMBEDDED-SKILL-CATALOG - Embedded Skill Catalog and Frontmatter Validator

Status: Approved
Review owner: User (approved 2026-05-28)
Depends on: `ITEM-002-CONFIG-MODEL`, `DEC-003-RUNTIME-AND-ARCHITECTURE`, `DEC-005-SKILL-INSTALLATION-MODEL`, `DEC-007-MVP-TOOL-SET`
Updates ledger rows: new `ART-ITEM-004`; new `CMT-NNN` rows for implementation commits

## Handoff Summary

Realize `DEC-005-SKILL-INSTALLATION-MODEL`'s build-time embedding mechanism. Adds the `<EmbeddedResource Include="..\..\spec\skills\**\*.*" />` glob to `Specforge.Core.csproj`, introduces the first `YamlDotNet` NuGet reference, and ships an `IEmbeddedSkillCatalog` service that enumerates the embedded resources at runtime, parses each `SKILL.md`'s YAML frontmatter, and validates per DEC-005's canonical format. A one-shot `SkillCatalogValidator` runs at MCP host bootstrap so a broken skill fails the server start with a clear stderr log — the build-time guarantee surfaced early per DEC-005. The actual seven SKILL.md bodies arrive in ITEM-012 (catalog amended from six to seven on 2026-05-28); ITEM-004 ships with the empty `spec/skills/` directory (anchored by a `.gitkeep`) so the glob has a valid root from day one. Adds the eleventh code (`specforge.skills.catalog_missing`) to `ToolExceptionMapper`.

- Core-only item. No new MCP tool ships.
- Consumed by `ITEM-005-SKILL-INSTALLER` (the `install_skills` tool reads from this catalog).
- ITEM-012 populates the catalog with the seven canonical skills (catalog amended from six to seven on 2026-05-28); until then the catalog enumerates zero entries — a valid state per DEC-005.

## Problem Slice

This item resolves "how do skill files become part of the published binary, and how does specforge validate them at startup?"

Explicit non-goals (owned by other items):

- `install_skills` tool implementation — `ITEM-005-SKILL-INSTALLER`.
- Authoring the seven SKILL.md bodies (`draft-decision`, `review-decision`, `draft-item`, `review-item`, `impact-assessment`, `validate-spec-graph`, `adopt-existing-project`) — `ITEM-012-SKILL-CONTENT`. (Catalog amended from six to seven on 2026-05-28.)
- Writing skill files to disk — `ITEM-005`.
- Surfacing skill content via MCP Resources — explicitly rejected by DEC-008 ("no Resources in MVP").
- `SpecforgeSkillInstallException` (the filesystem-write failure type) — `ITEM-005`. ITEM-004 only produces the `catalog_missing` type which fires on broken embedded content, not on install-time write errors.

## Terminology Used

- **Embedded resource**: MSBuild concept; a file copied into the assembly's manifest at compile time, addressable at runtime via `Assembly.GetManifestResourceNames()` and `GetManifestResourceStream()`.
- **Logical name**: the string under which an embedded resource is addressable. Set via `<LogicalName>` MSBuild metadata; controls the resource name independent of source path.
- **Frontmatter**: the YAML block delimited by `---` at the top of a `SKILL.md` carrying `name` and `description` keys per DEC-005.
- **Startup validator**: one-shot check that runs at MCP host bootstrap (before stdio loop begins); throws on first broken skill and prevents the server from starting.
- **Empty catalog**: zero embedded skills. A valid state — DEC-005 says "absent the directory, the build embeds zero resources and `install_skills` becomes a no-op". This item respects that.

Glossary cross-references: `../../../shared/glossary.md`.

## Approved Decisions

- `DEC-005-SKILL-INSTALLATION-MODEL` — sections "Source Layout", "Canonical `SKILL.md` Format", "Build-Time Embedding", "Error Types".
- `DEC-005-SKILL-INSTALLATION-MODEL` — section "Consequences": "Specforge.Core ships with embedded skill content. Adding or editing a skill is a source change + rebuild + republish, not a hot-swap." — locks the embedding-at-build-time stance ITEM-004 implements.
- `DEC-003-RUNTIME-AND-ARCHITECTURE` — section "Baseline Libraries": `YamlDotNet` placed in `Specforge.Core` "anticipating skill frontmatter parsing here". ITEM-004 introduces the reference.
- `DEC-003-RUNTIME-AND-ARCHITECTURE` — sections "Error Handling", "Logging and Diagnostics": typed Core exceptions + stderr-only output for startup failure messages.
- `DEC-007-MVP-TOOL-SET` — section "Error-Code Catalog": row `specforge.skills.catalog_missing` defines the envelope shape ITEM-004 must produce when the validator throws.

## Current Code State

After `ITEM-001-SOLUTION-BOOTSTRAP`, `ITEM-002-CONFIG-MODEL`, and `ITEM-003-ID-VALIDATOR` land:

- `Specforge.Core` carries `Configuration/`, `Identifiers/`, `Diagnostics/`, and eight typed exceptions extending `SpecforgeException`.
- `Specforge.Core.csproj` has no `<EmbeddedResource>` entries yet.
- `Directory.Packages.props` does not yet reference `YamlDotNet`.
- `spec/skills/` directory does not exist on disk.
- `Specforge.Mcp`'s `Program.cs` boots the MCP stdio host but does not yet run any skill-catalog validation step.
- `ToolExceptionMapper` covers ten codes (`config.*` ×3, `package.*` ×2, `id.*` ×3, plus `tool.internal_error` and `tool.invalid_argument`). Wait — `tool.invalid_argument` ships with the JSON Schema arg-validator in ITEM-005; ITEM-004 finds only ten codes mapped on entry and adds the eleventh (`skills.catalog_missing`). Adjust if the actual count diverges during implementation.

## Target Behavior

After this item is Done:

1. **`spec/skills/.gitkeep` exists** at the repo root, anchoring the directory in git even when empty. `spec/skills/.gitkeep` itself is NOT embedded (filtered out — see "Code Scope" below).
2. **`Specforge.Core.csproj` embeds every file under `spec/skills/**`** as a manifest resource, with logical name `specforge.skills/<recursive-path>/<filename>`. Example: `spec/skills/draft-decision/SKILL.md` → resource `specforge.skills/draft-decision/SKILL.md`.
3. **`IEmbeddedSkillCatalog.GetSkills()` enumerates** the embedded resources, groups them by skill directory, and returns `IReadOnlyList<EmbeddedSkill>` ordered alphabetically by `Name`. Each `EmbeddedSkill` carries: `Name`, `Description`, `Files` (every non-SKILL.md file inside the skill directory as a `(LogicalPath, OpenStream)` pair).
4. **`SkillFrontmatterParser.Parse(string skillMarkdown)`** extracts the YAML frontmatter (between leading `---` and trailing `---`), parses it with `YamlDotNet`, and returns a `SkillFrontmatter` record. Throws `SpecforgeEmbeddedSkillNotFoundException` on missing frontmatter, malformed YAML, missing `name`, missing `description`, or `description.Length > 200`.
5. **`SkillCatalogValidator.Validate()` runs once at startup**, before the MCP stdio loop accepts traffic. It enumerates the catalog and parses each `SKILL.md`. The first failure throws and the server fails to start; success is silent (or logged at `Information` level to stderr).
6. **Name consistency check**: parsed `name` must equal the skill's directory segment (e.g., a `SKILL.md` under `specforge.skills/draft-decision/` must declare `name: draft-decision` in its frontmatter). Mismatch throws `SpecforgeEmbeddedSkillNotFoundException` with `resourceName` set to the offending logical path.
7. **Empty catalog is valid**: zero embedded resources under `specforge.skills/` produces an empty `GetSkills()` list and `Validate()` succeeds silently. The MCP host starts normally and `install_skills` (when ITEM-005 ships it) reports zero files installed.
8. **Logical-name filter**: only files whose logical name begins with `specforge.skills/` are considered. Other embedded resources (none today, but possible later) are ignored.
9. **`.gitkeep` files are skipped** at enumeration time (the catalog filters out filenames literally equal to `.gitkeep`). They are still embedded as resources by the wildcard glob but contribute nothing to the catalog output.

## Invariants

- **`Specforge.Core` continues to reference no `ModelContextProtocol.*` assembly.** Architecture test from ITEM-001 still passes.
- **`YamlDotNet` is the only new dependency.** No `Markdig`, no `System.Text.Json`-beyond-already-introduced. Markdig arrives in a later item (the first that needs full markdown AST).
- **Catalog enumeration is deterministic**: alphabetical by `Name`. Build-time embed order is not relied upon.
- **Validation is one-shot at startup**, not per `install_skills` call. By the time `install_skills` (ITEM-005) runs, the catalog is already known-good.
- **`SpecforgeEmbeddedSkillNotFoundException` carries `ResourceName`** as required by DEC-007 catalog row (`data: { resourceName: string }`).
- **Frontmatter parsing tolerates unknown keys** (`name`/`description` are the only required keys; `keywords`/`tags`/etc. are passed through without validation) per DEC-005 "Canonical SKILL.md Format".
- **Frontmatter parsing is strict on the required keys**: missing or empty `name` or `description` triggers a throw. `description` >200 chars triggers a throw with a clear message naming the byte count.

## Code Scope

**In scope (created or modified by this item):**

`Specforge.Core` (new files):

- `Skills/EmbeddedSkill.cs` — record `{string Name, string Description, IReadOnlyDictionary<string, object> ExtraFrontmatter, IReadOnlyList<EmbeddedSkillFile> Files}`.
- `Skills/EmbeddedSkillFile.cs` — record `{string LogicalPath, Func<Stream> OpenStream}`. `OpenStream` returns a fresh `Stream` per call (the underlying `Assembly.GetManifestResourceStream` is invoked each time).
- `Skills/SkillFrontmatter.cs` — record `{string Name, string Description, IReadOnlyDictionary<string, object> Extras}`.
- `Skills/SkillFrontmatterParser.cs` — static class with `Parse(string source) -> SkillFrontmatter`. Uses `YamlDotNet.Serialization.Deserializer`.
- `Skills/IEmbeddedSkillCatalog.cs` — interface: `IReadOnlyList<EmbeddedSkill> GetSkills()`.
- `Skills/EmbeddedSkillCatalog.cs` — default implementation; constructor takes `Assembly assembly` and `string resourcePrefix = "specforge.skills/"`. The DI registration defaults to `typeof(SpecforgeConfig).Assembly` — the Core assembly itself.
- `Skills/SkillCatalogValidator.cs` — class with `void Validate()`. Throws on first failure; success is silent.
- `Exceptions/SpecforgeEmbeddedSkillNotFoundException.cs` — `ErrorCode = "specforge.skills.catalog_missing"`, carries `ResourceName`, plus an inner exception when applicable (e.g., a YamlDotNet parse exception).

`Specforge.Core.csproj` (modifications):

- Add a new `<ItemGroup>` block:
  ```xml
  <ItemGroup>
    <EmbeddedResource Include="..\..\spec\skills\**\*.*" Exclude="..\..\spec\skills\**\.gitkeep">
      <LogicalName>specforge.skills/%(RecursiveDir)%(Filename)%(Extension)</LogicalName>
    </EmbeddedResource>
  </ItemGroup>
  ```
  Note the `Exclude` for `.gitkeep` — keeps the directory anchor file out of the embedded set. (Alternative: include it and filter at enumeration time. Both are documented in DEC-005's "Source Layout" section; this item picks the build-time exclude.)
- Add `<PackageReference Include="YamlDotNet" />`.

`Directory.Packages.props` (modifications):

- Add `<PackageVersion Include="YamlDotNet" Version="..." />` — exact version pinned at implementation time; documented in the implementing commit's history-event detail.

`Specforge.Mcp` (modifications):

- `Program.cs` — call `host.Services.GetRequiredService<SkillCatalogValidator>().Validate()` after the DI container is built and before the MCP stdio loop begins. Catch `SpecforgeEmbeddedSkillNotFoundException`, log it to stderr at `Error` level, return non-zero exit code.
- `Tools/ToolExceptionMapper.cs` — add a `case` arm for `SpecforgeEmbeddedSkillNotFoundException` mapping to `specforge.skills.catalog_missing` with `data: { resourceName }`. This is the same code surfaced from runtime tools that may detect catalog inconsistency, even though the primary surfacing is at startup.
- `Hosting/SpecforgeCoreServices.cs` — register `IEmbeddedSkillCatalog → EmbeddedSkillCatalog` and `SkillCatalogValidator` as singletons.

`spec/skills/` (new directory):

- `.gitkeep` — empty file. Keeps the directory present in git so `dotnet build` against a fresh clone finds the include root.

`Specforge.Tests` (new files):

- `Skills/SkillFrontmatterParserTests.cs` — valid frontmatter; missing `---` delimiter; malformed YAML; missing `name`; missing `description`; `description` exactly 200 chars (passes); `description` 201 chars (fails); unknown keys tolerated.
- `Skills/EmbeddedSkillCatalogTests.cs` — uses the test assembly as the resource source with a separate test prefix (`test.skills/`); fixtures include a valid skill, a broken skill, and an empty catalog scenario via a no-resource assembly.
- `Skills/SkillCatalogValidatorTests.cs` — valid skills succeed silently; broken skill throws `SpecforgeEmbeddedSkillNotFoundException` with `ResourceName` populated; empty catalog succeeds.
- `Tools/ToolExceptionMapperTests.cs` — extend with the new exception → envelope mapping.

Test fixtures (new embedded resources in `Specforge.Tests.csproj`):

- `test/Specforge.Tests/SkillFixtures/valid-skill/SKILL.md` — valid frontmatter.
- `test/Specforge.Tests/SkillFixtures/broken-skill/SKILL.md` — malformed frontmatter (missing `description`).
- `test/Specforge.Tests/SkillFixtures/name-mismatch/SKILL.md` — frontmatter `name: wrong-name`.
- Csproj entry embeds these under `test.skills/<recursive-dir>/...` logical names so they don't collide with the production `specforge.skills/` prefix.

**Out of scope (deferred to later items):**

- `install_skills` MCP tool implementation — `ITEM-005-SKILL-INSTALLER`.
- `SpecforgeSkillInstallException` (filesystem write failure type) — `ITEM-005`.
- The seven production SKILL.md files — `ITEM-012-SKILL-CONTENT`.
- Per-agent install target path resolution (`%USERPROFILE%\.claude\skills\...`) — `ITEM-005`.
- Markdig markdown parsing dependency — not introduced here; defer to the first item that needs full AST parsing (likely ITEM-007 if `get_decision` returns parsed sections, otherwise later).

## Test Scope

Four new test classes plus an extension of `ToolExceptionMapperTests`. ~20-25 new tests. xUnit only. Uses embedded fixture resources in the test assembly (the same `<EmbeddedResource>` mechanism the production code uses, but under a different prefix and with a different Assembly).

## Test Plan

1. Add `YamlDotNet` to `Directory.Packages.props` and `Specforge.Core.csproj`; verify the build picks it up.
2. Add the production `<EmbeddedResource>` glob to `Specforge.Core.csproj`; create `spec/skills/.gitkeep`. Verify `dotnet build` succeeds and produces a `Specforge.Core.dll` with zero `specforge.skills/*` resources (since no skills exist yet).
3. Implement `SkillFrontmatter`, `SkillFrontmatterParser`, `EmbeddedSkill`, `EmbeddedSkillFile`, `IEmbeddedSkillCatalog`, `EmbeddedSkillCatalog`, `SkillCatalogValidator`, and the new exception.
4. Author the test fixture SKILL.md files and the test-csproj embedded-resource block.
5. Write the four test classes; run them; iterate on the parser until all pass.
6. Wire the startup validator into `Specforge.Mcp.Program.cs`; manually launch the published binary and confirm it starts cleanly with no embedded skills.
7. Add a broken fixture skill to the production glob temporarily (on a feature branch, not merged); confirm the server fails to start with the expected stderr message; revert.
8. Run `dotnet test test/Specforge.Tests -c Release`; all new tests pass; ITEM-001/002/003 tests continue to pass.

## Test Evidence

- Console output of `dotnet build` (zero warnings).
- Console output of `dotnet test` (all tests pass).
- Architecture test output confirming `Specforge.Core` still has no `ModelContextProtocol.*` reference after the new infrastructure.
- A transcript at `test/Specforge.Tests/Evidence/itm004-startup-fail.txt` capturing the stderr output when a broken-skill fixture is intentionally embedded (commit on feature branch, hash recorded; not merged).
- The empty-catalog smoke run: stderr capture from launching `specforge.exe` with zero skills, showing no validator errors and the MCP host coming up.

## Impact Assessment

| Aspect | Impact | Notes |
|---|---|---|
| Skill packaging | Direct | This item implements the embedding half of DEC-005. |
| Build pipeline | Direct | New embedded-resource glob; new `YamlDotNet` NuGet reference. |
| MCP host bootstrap | Direct | New startup-validation step preceding the stdio loop. |
| Error handling | Direct | New typed Core exception + envelope mapping row. |
| Core library boundary | No impact | New infrastructure is Core-side; architecture test still passes. |
| MCP tool surface | No impact | No new tool ships here. |
| Configuration discovery | No impact | DEC-002 mechanics unchanged. |
| ID scheme | No impact | DEC-004 mechanics unchanged. |
| Schema versioning | No impact | DEC-006 mechanics unchanged. |
| Documentation | No impact | User-visible surface unchanged until ITEM-005 ships `install_skills`. |
| Test coverage scope | Direct | ~20-25 new unit tests; test-assembly fixture pattern established for skill-related items. |
| Performance | No measurable | Validation reads small embedded resources; runs once per server start. |
| Concurrency | No measurable | Startup-only; no runtime concurrency surface. |
| External adoption | No impact | Skills are user-wide artifacts; this item doesn't touch any target project. |
| Maintenance burden | Indirect | The `<EmbeddedResource>` glob is set-and-forget; adding a skill is dropping a directory under `spec/skills/`. |

## Validation

- **Build**: `dotnet build Specforge.sln -c Release` succeeds with zero warnings.
- **Test**: `dotnet test test/Specforge.Tests -c Release` succeeds; new tests pass; prior tests still pass.
- **Boundary**: ITEM-001 architecture test still passes — `Specforge.Core.dll` carries no `ModelContextProtocol.*` reference even after the new infrastructure.
- **Empty catalog**: with zero files under `spec/skills/`, `IEmbeddedSkillCatalog.GetSkills()` returns an empty list and `SkillCatalogValidator.Validate()` is a no-op.
- **Negative**: a deliberately broken SKILL.md fixture (missing `description`) causes the validator to throw `SpecforgeEmbeddedSkillNotFoundException` with `ResourceName` pointing at the offending logical path.
- **Negative**: a SKILL.md whose `name` does not match its directory throws the same exception with a message naming both the declared `name` and the directory.
- **Smoke**: launch `D:\Work\specforge\bin\specforge.exe` against the dogfooded config from ITEM-002; the server starts cleanly (no skills embedded yet, so the validator is a no-op).

## Open Questions

- Pinned version of `YamlDotNet` — chosen at implementation time and recorded in `Directory.Packages.props`. Not blocking.
- Whether to expose the catalog's enumeration via a future read-only MCP tool (e.g., a debug `list_embedded_skills`) — defer; `install_skills(dryRun: true)` from ITEM-005 already covers the "what is embedded?" query.
- Whether `.gitkeep` exclusion should be at MSBuild include time (chosen here) or at enumeration time — chosen at MSBuild because it keeps the resource manifest cleaner; not blocking.
- Whether to validate the `name` frontmatter key against DEC-004's kebab-case regex (`^[a-z][a-z0-9-]*[a-z0-9]$`) explicitly here or defer to the consuming tool — leaning explicit here, so an invalid name surfaces at startup. Finalize during implementation.

## Done Criteria

The item is **Done** (post-Approved) when:

1. All files listed in "Code Scope (In scope)" exist at the specified paths in the repository.
2. `Specforge.Core.csproj` carries the `<EmbeddedResource>` glob with `LogicalName` per DEC-005.
3. `Directory.Packages.props` carries a pinned `<PackageVersion Include="YamlDotNet" />`.
4. `spec/skills/.gitkeep` exists at the repo root.
5. `dotnet build Specforge.sln -c Release` reports zero warnings.
6. `dotnet test test/Specforge.Tests -c Release` runs every new test (~20-25) plus all prior-item tests; all pass.
7. `Specforge.Core.dll` carries no `ModelContextProtocol.*` reference (architecture test).
8. `D:\Work\specforge\bin\specforge.exe` (rebuilt and republished) starts cleanly with the empty catalog; an intentionally-broken fixture (on a feature branch) is observed to crash the server with the expected stderr.
9. A `CMT-NNN` row is appended to `ledger/commits.md` recording the implementation commit's short SHA.
10. A history event is appended to `ledger/history.md` marking the transition.

## Links

- `../decisions/DEC-005-SKILL-INSTALLATION-MODEL.md` (approved) — sections "Source Layout", "Canonical SKILL.md Format", "Build-Time Embedding", "Error Types", "Consequences".
- `../decisions/DEC-003-RUNTIME-AND-ARCHITECTURE.md` (approved) — sections "Baseline Libraries" (YamlDotNet), "Error Handling", "Logging and Diagnostics".
- `../decisions/DEC-007-MVP-TOOL-SET.md` (approved) — section "Error-Code Catalog" (row `specforge.skills.catalog_missing`).
- `./ITEM-001-SOLUTION-BOOTSTRAP.md` (approved) — scaffold this item extends.
- `./ITEM-002-CONFIG-MODEL.md` (approved) — `SpecforgeException` base; MCP host bootstrap point this item hooks into; `ToolExceptionMapper` extended here.
- `./ITEM-003-ID-VALIDATOR.md` (approved) — identifier infrastructure; the skill `name` frontmatter key may invoke parts of it (kebab-case validation) per Open Questions.
- `../../../templates/item_spec.md` — item template.
- `../../../shared/spec_item_contract.md` — required-section contract.
- `../../../shared/document_lifecycle.md` — status states.
- `../../../shared/impact_assessment_checklist.md` — aspects checklist.

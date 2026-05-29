# DEC-005-SKILL-INSTALLATION-MODEL - Skill Installation Model

Status: Approved
Date: 2026-05-28
Owner: AI assistant (drafted)
Review owner: User (approved 2026-05-28)
Covers: skill source location, build-time embedding, install mechanism, target install paths, re-install behavior, scope boundary with `init`
Supersedes: none

## Context

`DEC-001-DISTRIBUTION-AND-TRANSPORT` committed to a two-layer multi-agent strategy: a cross-agent MCP tool layer plus a per-agent skill layer. It locked the supported agents at MVP as **Claude Code** and **Codex CLI**, with skill files living at `~/.claude/skills/<name>/SKILL.md` and `~/.agents/skills/<name>/SKILL.md` respectively. DEC-001's research confirmed the two formats are structurally identical (`SKILL.md` with YAML frontmatter), and explicitly deferred the **packaging and installation mechanics** to this record.

`DEC-003-RUNTIME-AND-ARCHITECTURE` placed `YamlDotNet` in `Specforge.Core` as a baseline library specifically anticipating skill frontmatter parsing here.

This decision defines the *mechanics* of how specforge ships and installs skills onto a developer machine. It does **not** define the skill set or the content of any individual skill — that is the responsibility of `DEC-008-INSTRUCTION-LAYER-DESIGN`.

On 2026-05-28 the user chose, via structured questions:

- skill source-of-truth: **files in the specforge repo, embedded as resources at publish**;
- install trigger: **an explicit MCP tool, `install_skills`**;
- re-install: **always overwrite**;
- scope boundary: **`install_skills` writes user-wide skill directories only**; project-level Codex `agents/openai.yaml` belongs to DEC-007's `init`.

This record formalizes those choices and fills in the mechanics (embedded resource glob, tool contract, target paths, error types, ownership rules).

## Decision

### Source Layout

Skills are authored as markdown files in the specforge repo:

```text
D:\Work\specforge\
  spec\
    skills\
      <skill-name>\
        SKILL.md
        [supporting-files...]
```

- `<skill-name>` is kebab-case (`draft-decision`, `append-ledger`, `impact-assess`), matching what both Claude Code and Codex expect as the skill directory name.
- `SKILL.md` is required in every skill directory.
- Additional supporting files in the directory are copied verbatim alongside `SKILL.md`.
- The repository file path is the single source of truth. The specific list of skill names is *not* enumerated by DEC-005 — that's the property of DEC-008.

### Canonical `SKILL.md` Format

```yaml
---
name: <kebab-case-name>           # required; must equal the directory name
description: <one sentence>       # required; ≤ 200 chars
---

<markdown body>
```

- `name` and `description` are the **intersection** of what both agents require — and therefore the only required keys for MVP.
- Optional keys that both agents tolerate (e.g. `keywords`, `tags`) may appear; the canonical author should stay close to the intersection.
- Per-agent content forks inside one skill file are not supported. If a skill genuinely needs different bodies for Claude Code and Codex, ship two skill directories with distinct names. This is an escape hatch, not an expected MVP pattern.

### Build-Time Embedding

At publish time, every file under `spec/skills/**` is embedded into `Specforge.Core.dll` as an embedded resource. Embedding lives in **Core**, not Mcp, so that an alternate host (CLI, embedded) could install skills without reimplementing the catalog.

`Specforge.Core.csproj` declares:

```xml
<ItemGroup>
  <EmbeddedResource Include="..\..\spec\skills\**\*.*">
    <LogicalName>specforge.skills/%(RecursiveDir)%(Filename)%(Extension)</LogicalName>
  </EmbeddedResource>
</ItemGroup>
```

Resulting resource names look like `specforge.skills/draft-decision/SKILL.md`. A Core service `IEmbeddedSkillCatalog` enumerates them at runtime via `Assembly.GetManifestResourceNames()` and exposes per-skill streams.

The catalog runs a startup validation: every embedded `SKILL.md` must parse and must contain a valid `name`/`description` frontmatter. A broken skill produces a `SpecforgeEmbeddedSkillNotFoundException` at server startup, not at install time — the failure is a build problem, surfaced early.

### Install Tool Contract

A single MCP tool — `install_skills` — performs installation. It lives in `Specforge.Mcp` and delegates to a Core service `ISkillInstaller`.

DEC-007 owns the final MCP envelope; DEC-005 specifies the contract that DEC-007 must honor:

| Argument | Type | Default | Purpose |
|---|---|---|---|
| `agent` | enum: `claude-code` \| `codex` \| `all` | `all` | Restrict installation to one agent. |
| `dryRun` | boolean | `false` | Report planned writes without touching the filesystem. |

Result payload (shape finalized in DEC-007):

- count of files written per agent;
- list of paths written (or planned, when `dryRun`);
- subset of paths that overwrote existing files;
- the specforge binary version that performed the install (diagnostic only; not persisted to disk).

The tool is **idempotent**: every call writes the full embedded skill set. There is no partial-install mode in MVP.

### Target Install Paths

For each enabled agent and each embedded skill `<skill-name>`:

| Agent | Target path on Windows |
|---|---|
| Claude Code | `%USERPROFILE%\.claude\skills\<skill-name>\SKILL.md` (plus supporting files) |
| Codex CLI | `%USERPROFILE%\.agents\skills\<skill-name>\SKILL.md` (plus supporting files) |

- Home-directory resolution uses `Environment.GetFolderPath(SpecialFolder.UserProfile)` — independent of `~` shell expansion.
- Parent directories are created as needed.
- For MVP, Windows x64 is the only platform (DEC-001). macOS/Linux home-directory variants are deferred together with cross-platform support itself.

### Re-install Behavior

Every `install_skills` call writes the full embedded skill set, overwriting any existing files at the target paths.

- No diff, no merge, no prompt.
- No version-comparison manifest in MVP — the binary version *is* the skill version, recoverable from `specforge.exe` itself.
- Files outside the specforge-managed skill names (other tools' skills, user-hand-authored skills under different names) are never touched.
- A user who wants to customize a specforge skill should copy it to a different `<skill-name>` in their own skills directory; specforge does not know about the copy and will not overwrite it.
- If a write fails mid-call (permission denied, disk full, etc.), `install_skills` aborts the affected agent and returns a `SpecforgeSkillInstallException`. Files already written in the same call are NOT rolled back; the result accurately reports the partial state.

### Ownership of the Skill Namespace

specforge implicitly **owns the skill names it ships** inside `~/.claude/skills/` and `~/.agents/skills/`. No manifest is maintained.

- Other tools or hand-authored skills under different names coexist without conflict.
- A name collision between specforge and another tool at the same skill name results in specforge overwriting that name on every `install_skills` call. The risk is small in practice (specforge names are operation-specific, e.g. `draft-decision`) but is documented in user-facing docs.

### Scope Boundary with `init` (DEC-007)

`install_skills` is **strictly user-wide**: it writes to `%USERPROFILE%\.claude\skills\` and `%USERPROFILE%\.agents\skills\` and nowhere else.

Project-level Codex wiring — specifically the `<project>/agents/openai.yaml` file that declares specforge as an MCP dependency for that project — is the responsibility of DEC-007's `init` tool, not DEC-005. The two operations have different lifecycles:

- `install_skills` runs **once per machine** (and again on every specforge upgrade).
- `init` runs **once per repository** (when adopting specforge in a project).

Coupling them into one tool would force `install_skills` to depend on an active package selection (DEC-002), which may not exist on a fresh machine, and would mix user-scoped and project-scoped state into a single command.

### Error Types

New typed exceptions in `Specforge.Core`, following the DEC-003 pattern; MCP envelope mapping is DEC-007's concern:

- `SpecforgeSkillInstallException` — filesystem write failed (permission denied, disk full, locked file, etc.). Carries the failed target path and the inner I/O exception.
- `SpecforgeEmbeddedSkillNotFoundException` — embedded catalog enumeration found no skills, or a referenced supporting file is missing. Indicates a broken build, not a runtime user error.

## Alternatives Considered

| Alternative | Rejection reason |
|---|---|
| Skills as templates rendered at install time | Adds a templating engine and a render step for hypothetical flexibility. Skill content is small and stable; the binary-version-is-skill-version model is simpler and removes template-debugging from the failure surface. |
| Hand-authored per-agent files in parallel (`spec/skills/claude-code/...`, `spec/skills/codex/...`) | Doubles authoring work for a near-zero format difference. The two agents already converge on `SKILL.md` with the same required frontmatter — divergence is not justified. |
| Auto-install on first server start | Silent filesystem writes to `~/.claude/` and `~/.agents/` before any explicit consent. Surprising and hard to opt out of. The explicit tool is a one-time call per machine — friction is negligible, consent is legible. |
| Both — explicit tool *and* auto-install on first start | Inherits the consent problem of auto-install, and makes the tool's role ambiguous (when is calling it necessary?). |
| External CLI `specforge --install-skills` | DEC-001 explicitly excluded a CLI surface from the MVP. MCP-tool-only is the project's interface stance. |
| Skip-if-exists | Preserves local edits but creates silent drift — users won't know if they're running with shipped or stale content. The fork-by-renaming escape hatch handles intentional customization without ambiguity. |
| Version-aware overwrite (manifest tracking) | Adds a side file in `~/.claude/` and `~/.agents/` plus version-comparison logic. Defer to post-MVP if selective upgrade or uninstall emerges as a real need. |
| Prompt-per-conflict | specforge runs as a headless MCP server. Prompts would surface as MCP error round-trips through the host — bad UX. |
| `install_skills` also writes project-level `agents/openai.yaml` | Couples user-wide install with project-level wiring; requires an active package selection at install time. The `init` lifecycle (per-repo) belongs in DEC-007. |
| `install_skills` writes a user-wide `agents/openai.yaml` variant | A user-wide MCP-dep declaration would apply to every Codex project, including ones that don't want specforge. Fragile and surprising. Project-scoped declarations are the correct unit. |
| Pack all skills into a single zip resource | Simpler enumeration (one zip blob) but harder to diff during development and harder to inspect post-publish. Per-file embedded resources cost nothing and make build-time inspection trivial. |
| Maintain an installed-skills manifest in `%APPDATA%\specforge\manifest.json` | Useful for selective upgrade and uninstall, but not needed for the always-overwrite + no-uninstall MVP model. Re-evaluate if uninstall or selective upgrade enters scope. |

## Consequences

- `Specforge.Core` ships with embedded skill content. Adding or editing a skill requires a source change + rebuild + republish; there is no hot-swap path.
- The MVP MCP tool surface gains `install_skills` (DEC-007 finalizes its MCP envelope).
- Users must call `install_skills` once per machine — and again after every specforge upgrade — to materialize skills on disk. The top-level `README.md` and DEC-008's instruction layer must surface this ritual.
- The `spec/skills/` directory becomes a required part of the repo layout. Absent the directory, the build embeds zero resources and `install_skills` becomes a no-op (not an error, but reported in the result as zero files written).
- The skill-name namespace inside `~/.claude/skills/` and `~/.agents/skills/` is implicitly owned by specforge for the names it ships. Collisions are possible but unlikely.
- DEC-008 is free to enumerate any set of skills without DEC-005 re-evaluation, provided each follows the canonical `SKILL.md` format.
- DEC-007's `init` retains exclusive ownership of project-level `agents/openai.yaml`.
- A failed install on one agent does not roll back successful writes on the other; the result documents the partial state.
- Uninstall is not provided in MVP. Manual cleanup of the two skill directories is acceptable because skills are inert markdown files.
- Skill version = binary version. There is no separate skill-versioning scheme. DEC-006 may cross-reference this stance.

## Impact Assessment

| Aspect | Impact | Notes |
|---|---|---|
| Skill packaging | Direct | This decision defines it. |
| Build pipeline | Direct | Embedded-resource glob added to `Specforge.Core.csproj`. |
| MCP tool surface | Indirect | `install_skills` finalized by DEC-007; this decision specifies its inputs and outputs. |
| Skill content | No impact | Content is DEC-008's concern; this decision is mechanics only. |
| Multi-agent strategy | Direct | Realizes DEC-001's per-agent skill layer for Claude Code and Codex CLI. |
| Project-level integration | No impact | Project-level `agents/openai.yaml` belongs to DEC-007 `init`. |
| Error handling | Direct | Two new typed exceptions: `SpecforgeSkillInstallException`, `SpecforgeEmbeddedSkillNotFoundException`. |
| Cross-platform support | Indirect | Win-x64-only today; macOS/Linux path variants deferred with cross-platform support itself. |
| Versioning | Indirect | Skill version equals binary version; no separate scheme. DEC-006 may cross-reference. |
| Documentation | Direct | Top-level README and getting-started must show the `install_skills` one-time call and the upgrade ritual. |
| User customization | Indirect | Documented escape hatch: fork-by-renaming. No first-class customization in MVP. |
| Distribution | Indirect | Skill files travel inside `specforge.exe`; no extra distribution artifact. |
| Maintenance burden | Indirect | New skills are added by dropping a directory under `spec/skills/`; rebuild picks them up. |
| Performance | No measurable | Install is a small set of file writes; runs once per machine per upgrade. |
| Concurrency | No measurable | Install is invoked from a single MCP session; no contention story. |

## Source Links

| Source | Locator | Evidence |
|---|---|---|
| `DEC-001-DISTRIBUTION-AND-TRANSPORT` | section "Multi-Agent Strategy" | Cross-agent MCP tool layer + per-agent skill layer; supported agents Claude Code + Codex CLI; Cursor out of scope. |
| `DEC-001-DISTRIBUTION-AND-TRANSPORT` | research findings on Codex Agent Skills (developers.openai.com/codex/skills) | `~/.agents/skills/<name>/SKILL.md` user-scope path; required frontmatter keys `name` and `description`. |
| `DEC-003-RUNTIME-AND-ARCHITECTURE` | section "Baseline Libraries" | `YamlDotNet` in Core (used here for `SKILL.md` frontmatter parsing). |
| `DEC-003-RUNTIME-AND-ARCHITECTURE` | section "Error Handling" | Typed-Core-exceptions pattern this decision continues. |
| Conversation 2026-05-28, structured questions | AskUserQuestion answers | Files-in-repo + embedded resources; explicit `install_skills`; always-overwrite; user-wide scope only. |

## Related Decisions

- `DEC-001-DISTRIBUTION-AND-TRANSPORT` (approved): defines the multi-agent skill layer this decision packages.
- `DEC-003-RUNTIME-AND-ARCHITECTURE` (approved): embedding lives in `Specforge.Core`; new exception types follow the established pattern.
- `DEC-004-ID-SCHEME-CUSTOMIZATION` (approved): skills do not carry spec-entity IDs; skill names are kebab-case directory names, independent of the ID scheme.
- `DEC-006-SCHEMA-VERSIONING` (planned): may cross-reference the "skill version = binary version" stance.
- `DEC-007-MVP-TOOL-SET` (planned): finalizes the `install_skills` MCP envelope and owns the separate `init` tool for project-level wiring.
- `DEC-008-INSTRUCTION-LAYER-DESIGN` (planned): enumerates the actual skill set and authors `SKILL.md` content for each.

## Related Item Specs

- `ITEM-004-EMBEDDED-SKILL-CATALOG` (planned): implements `IEmbeddedSkillCatalog` in `Specforge.Core` plus the embedded-resource glob in the csproj plus startup validation of all embedded `SKILL.md` files.
- `ITEM-005-SKILL-INSTALLER` (planned): implements `ISkillInstaller` in Core and the `install_skills` MCP tool surface in Mcp.

## Related Tests / Validation

- Embedded catalog enumerates every file under `spec/skills/**` and returns no foreign entries.
- A `SKILL.md` lacking required frontmatter `name` or `description` causes startup validation to throw `SpecforgeEmbeddedSkillNotFoundException` with the offending resource name in the message.
- `install_skills` with default args writes every embedded skill to both `%USERPROFILE%\.claude\skills\` and `%USERPROFILE%\.agents\skills\`, creating parent directories as needed.
- A second `install_skills` call overwrites the previous content with no diff/prompt and returns the overwritten paths in the result.
- `install_skills agent=claude-code` writes to `%USERPROFILE%\.claude\skills\` only; `agent=codex` writes to `%USERPROFILE%\.agents\skills\` only.
- `install_skills dryRun=true` lists planned paths and does not touch the filesystem.
- A user-hand-authored skill under a name not in the embedded catalog is left untouched across `install_skills` calls.
- A simulated write failure on one agent produces a `SpecforgeSkillInstallException` while files already written to the other agent remain in place; the partial state is reported.

## Open Questions

- Whether to ship a `list_skills` companion tool, or fold listing into `install_skills dryRun=true` — likely the latter; final shape decided in DEC-007. Not blocking.
- Whether to provide uninstall in a future DEC — leaning yes once specforge has wider adoption; manual cleanup is acceptable for MVP.
- Whether to introduce a version manifest in `%APPDATA%\specforge\manifest.json` — defer until version-aware overwrite or selective upgrade is requested.
- Whether to detect skill-name collisions with other tools at install time and warn — defer; document the small risk in user-facing docs and revisit on user reports.

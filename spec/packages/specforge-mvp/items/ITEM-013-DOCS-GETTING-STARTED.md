# ITEM-013-DOCS-GETTING-STARTED - Top-Level README Revision and Getting-Started Documentation

Status: Approved
Review owner: User
Depends on: `ITEM-005-SKILL-INSTALLER`, `ITEM-006-INIT-TOOL`, `ITEM-010-VALIDATE-TOOL`, `ITEM-012-SKILL-CONTENT`, `DEC-001-DISTRIBUTION-AND-TRANSPORT`, `DEC-005-SKILL-INSTALLATION-MODEL`, `DEC-006-SCHEMA-VERSIONING`, `DEC-007-MVP-TOOL-SET`, `DEC-008-INSTRUCTION-LAYER-DESIGN`
Updates ledger rows: new `ART-ITEM-013`; new `ART-DOCS-GETTING-STARTED`; revises existing `ART-README-TOP` (content change, status unchanged); new `CMT-NNN` row for implementation commit

## Handoff Summary

Ship the user-facing documentation set that brings a new adopter from "never heard of specforge" to "first decision drafted, first item drafted, baseline validated". Final Stage 1 item. Two documents: (1) revised top-level `README.md` (existing `ART-README-TOP`) — 1-line tagline, 3-sentence "What is specforge?", install snippet, quick links; (2) new `docs/getting-started.md` (new `ART-DOCS-GETTING-STARTED`) — pre-install requirements, full install walkthrough (build-from-source and pre-built binary paths from DEC-001), first-time adoption flow via `init` + `install_skills` + `validate`, the four project-level file outputs from DEC-008 explained, the one-time skill installation explained, the upgrade ritual when a new binary version ships. Content only — no code, no MCP tools, no NuGet packages, no error codes. One new content-drift test (`DocsContentReferenceTests`) walks both docs, extracts backticked tool names and skill names, and asserts each resolves against the live 21-tool catalog from DEC-007 and the 6-skill catalog from DEC-008 / ITEM-012.

- 0 new tools, 0 new NuGet packages, 0 new error codes, 0 new typed exceptions, 0 new embedded-resource globs.
- 1 new documentation artifact (`docs/getting-started.md`); 1 existing artifact revised (`README.md`).
- 2 new descriptive ART rows: `ART-DOCS-GETTING-STARTED` (new file), plus revision metadata on `ART-README-TOP` (no new row — content history goes in `history.md`).
- 1 new content-drift test: `DocsContentReferenceTests` — same two-phase tool-name-check pattern from ITEM-012, plus skill-name validation and file-path-output validation.
- After ITEM-013 approval, Stage 1 is complete (13/13 items Approved); Stage 2 implementation is unblocked.

## Problem Slice

This item resolves "what does a fresh adopter — a human, possibly with AI assist — read to start using specforge productively?" The answer is two documents: a short top-level entry-point and a walkthrough that traverses the bootstrap quartet (`init`, `install_skills`, `use_package`, `validate`).

Explicit non-goals (out of MVP or owned by other items):

- A full reference manual for every tool — every Stage 1 item already publishes its tool's full JSON Schema; the docs link to them via stable file paths but do NOT re-document the schemas (single-source-of-truth principle).
- A skill-content reference — `adopt-existing-project` SKILL.md (owned by ITEM-012) is the AI-facing version of the same flow; the docs reference it but don't duplicate the AI-facing instructions.
- A migration guide from spec-kit or other tools — out of MVP; specforge is a fresh adopt at this stage.
- A best-practices essay (when to draft a DEC vs an ITEM, what's a good Impact Assessment, etc.) — out of MVP. The skill bodies (ITEM-012) already capture the operational knowledge.
- Localization / non-English documentation — English-only in MVP.
- An auto-generated API doc from the C# code — out of MVP. The public surface specforge promises is the 21-tool MCP catalog, not the C# API.
- An auto-generated tool-catalog doc — the docs reference DEC-007 directly; if catalog drift ever causes pain we can revisit.
- A FAQ — defer until real adopter questions surface.

## Terminology Used

- **Bootstrap quartet**: the four-tool sequence (`init` → `install_skills` → `use_package` → `validate`) a new adopter runs to reach a known-clean baseline. Same sequence the `adopt-existing-project` skill (ITEM-012) walks.
- **Four project-level file outputs**: the files DEC-008 names that `init` produces in the adopter's repo: `.specforge.json` (config; DEC-002), `SPECFORGE.md` (always-overwrite project-level instructions; DEC-008), tagged block in `CLAUDE.md` (tagged-block-only edit; DEC-008), tagged block in `AGENTS.md` (tagged-block-only edit; DEC-008). The Codex-specific `agents/openai.yaml` (DEC-005) is a fifth output written only when Codex support is selected via `behavioralFiles=codex-only` or `all`.
- **Upgrade ritual**: the sequence a maintainer runs when a new specforge binary version is published — re-publish/replace the binary; `info` confirms the new `binaryVersion` + `supportedSchemaVersionRange`; `init dryRun=true` previews what would change in the existing repo; `init` (real run) writes the opportunistic schema upgrade per DEC-006 and overwrites the always-overwrite files; `install_skills agent=all` redeploys skills; `validate(aspect=all)` confirms a clean baseline.
- **Content-drift test**: a unit test that parses the Markdown docs at test time, extracts referenced tool/skill names, and asserts each resolves against the live catalog. Same pattern as ITEM-012's `SkillContentReferenceTests`.

## Approved Decisions

- `DEC-001-DISTRIBUTION-AND-TRANSPORT` — sections "Distribution Path" (publish via `dotnet publish -o D:\Work\specforge\bin\` for the developer; pre-built binary install path for adopters), "MCP Registration" (`.mcp.json` snippet shown in docs). The install walkthrough cites the canonical commands from this DEC.
- `DEC-005-SKILL-INSTALLATION-MODEL` — sections "Install Tool Contract", "Target Layout" (`~/.claude/skills/` and `~/.agents/skills/`), "Codex Project YAML" (`agents/openai.yaml`). The docs explain each path.
- `DEC-006-SCHEMA-VERSIONING` — sections "Schema Version Mechanics", "Upgrade Behavior" (in-memory + opportunistic write). The upgrade-ritual section in the docs explains how this manifests to adopters.
- `DEC-007-MVP-TOOL-SET` — sections "MVP Tool Catalog" (21 tools), "Error Envelope" — the docs name every tool the adopter touches during the bootstrap quartet.
- `DEC-008-INSTRUCTION-LAYER-DESIGN` — sections "Project-Level Files", "Tagged-Block Convention", "MVP Skill Catalog". The docs explain the four project-level outputs and the skill catalog by name.

## Current Code State

After ITEM-012 lands:

- `Specforge.Mcp` registers 21 tools (catalog from DEC-007).
- `Specforge.Core/Skills/EmbeddedSkillCatalog` enumerates 6 skill bodies authored by ITEM-012.
- `install_skills agent=all` deploys 6 × 2 = 12 SKILL.md files to user-wide scope.
- `init` writes the four project-level files (and optionally `agents/openai.yaml`) per ITEM-006 + DEC-008.
- `validate(aspect=all)` produces structured findings per ITEM-010.
- Top-level `README.md` exists (per `ART-README-TOP` in artifacts.md) but is the initial bootstrap content from project init — it lacks the install walkthrough, the tool catalog reference, and links to getting-started.
- `docs/` directory does not yet exist at the repo root. ITEM-013 introduces it.
- No content-drift test exists for the top-level docs.

## Target Behavior

After this item is Done:

1. **Top-level `README.md`** (revised; existing `ART-README-TOP`):
   - Tagline: "Spec-driven development MCP server for AI-assisted .NET architecture work."
   - "What is specforge?" — 2-3 sentences explaining: dogfoods its own MVP via decision records + item specs + ledger files; consumed via Claude Code / Codex CLI as an MCP server; 21 tools (decisions / items / ledger / validate / setup).
   - "Quick start" — a 3-line snippet pointing to `docs/getting-started.md` for the full walkthrough; a one-line install command (`dotnet publish src/Specforge.Mcp/Specforge.Mcp.csproj -c Release -o D:\Work\specforge\bin\`) for impatient readers; a one-line MCP registration snippet (`.mcp.json` excerpt).
   - "Requirements" — Windows x64 only for MVP; .NET 10 SDK 10.0.201; Claude Code or Codex CLI host.
   - "Documentation" — bullet links: `docs/getting-started.md`, `spec/packages/specforge-mvp/decisions/` (architecture decisions), `spec/packages/specforge-mvp/items/` (item specs), `spec/shared/` (lifecycle, glossary, contracts, impact-assessment checklist).
   - "License" — placeholder line; license-selection is out of MVP and tracked separately.
   - "Status" — single sentence: "MVP under development (Stage 1 spec complete; Stage 2 implementation in progress)."
2. **`docs/getting-started.md`** (new; new `ART-DOCS-GETTING-STARTED`):
   - Section "Before you start" — requirements list (Windows x64, .NET 10, Claude Code or Codex CLI host), pre-conditions (no existing `.specforge.json` in the project).
   - Section "Install specforge" — two sub-paths:
     - **Build from source**: clone, `dotnet build`, `dotnet publish src/Specforge.Mcp/Specforge.Mcp.csproj -c Release -o <publish-dir>`. Note that `<publish-dir>` must be on `PATH` or in a fixed location the host knows.
     - **Pre-built binary**: pull the release archive from `<release-url-placeholder>` (release tooling is out of MVP; placeholder until publish pipeline is set up); extract to `<publish-dir>`. The binary itself doesn't expose a standalone CLI — running `specforge.exe` directly opens an MCP stdio loop and waits for a host. Verify the install via the host (next section): after registering, the host's tool list should show 21 specforge tools.
   - Section "Register the MCP server" — show the canonical `.mcp.json` excerpt from DEC-001 for Claude Code; show the canonical `agents/openai.yaml` snippet from DEC-005 for Codex.
   - Section "Bootstrap a new project (the bootstrap quartet)":
     1. **`init`** — first call: `init scaffold=true behavioralFiles=all`. Explain that this writes (a) `.specforge.json` with the current `schemaVersion`, (b) `SPECFORGE.md` with the auto-generated standard sections, (c) tagged blocks in `CLAUDE.md` and `AGENTS.md`, (d) optionally `agents/openai.yaml` when `behavioralFiles=all` or `behavioralFiles=codex-only`, (e) optionally a scaffold of `spec/` with `decisions/`, `items/`, `ledger/`, `shared/`, `templates/` directories when `scaffold=true`. Show example output. Mention `dryRun=true` for preview.
     2. **`install_skills`** — `install_skills agent=all`. Explain that this extracts the 7 embedded SKILL.md bodies to `~/.claude/skills/<name>/SKILL.md` and `~/.agents/skills/<name>/SKILL.md`. Note that this is a one-time call per binary version (re-run after binary upgrades for skill content refresh; always-overwrite is safe per DEC-005).
     3. **`use_package`** — `use_package name=<your-package>`. Explain that single-package projects auto-select on first tool call so this can be skipped; multi-package projects must pick. Reference `list_packages` to enumerate.
     4. **`validate`** — `validate aspect=all`. Explain the structured findings payload; the goal is `errorCount=0` for a clean baseline.
   - Section "Author your first decision" — walkthrough of invoking the `draft-decision` skill via Claude Code / Codex; or directly calling `create_decision title="<Plain Title>"`, then iterating the body via the AI, then calling `set_decision_status status="Draft for user review"` and then `set_decision_status status="Approved" reviewer="<name>" notes="<summary>"`. Cross-reference the `draft-decision` and `review-decision` skills.
   - Section "Author your first item" — walkthrough analogous to the decision flow but via `draft-item` skill / `create_item` + dependsOn list of approved decisions, then via `review-item` skill / `set_item_status status="Approved" reviewer=... notes=...` for approval. Cross-reference both the `draft-item` and `review-item` skills.
   - Section "Validate the spec graph" — walkthrough invoking `validate-spec-graph` skill / direct `validate(aspect=all)` call. Explain severity interpretation (error / warning / info). Reference the skill for the fix recipes.
   - Section "Upgrade ritual" — when a new specforge binary version ships:
     1. Re-build or re-download per the install section.
     2. Verify the new version via `info`.
     3. Run `init dryRun=true` against the existing repo; review the planned changes (most fields immutable, but `.specforge.json` schemaVersion may upgrade in memory per DEC-006).
     4. Run `init` (real run); the schema upgrade chain runs in memory, opportunistic write fires when any tool next writes to `.specforge.json` (per DEC-006).
     5. Re-run `install_skills agent=all` to refresh skill bodies (DEC-005 always-overwrite means stale skill content is replaced).
     6. Run `validate(aspect=all)` to confirm the upgrade hasn't introduced inconsistencies.
   - Section "Where to learn more" — bullet links to `spec/packages/specforge-mvp/decisions/` (every DEC's rationale), `spec/packages/specforge-mvp/items/` (every item's implementation plan), `spec/shared/` (lifecycle/glossary/contracts/impact-assessment), the SKILL.md bodies under `spec/skills/`.
3. **Content-drift test `DocsContentReferenceTests`** (new in `Specforge.Tests/Docs/`):
   - Theory `EveryToolNameMentionedIsRegistered` — walks `README.md` and `docs/getting-started.md`, applies the two-phase tool-name check from ITEM-012 (exact-match against the 21-tool catalog + shape-regex fallback for `_`-containing typos), fails on unregistered tool names.
   - Theory `EverySkillNameMentionedIsRegistered` — walks both docs for backticked skill-shaped names (`^[a-z][a-z-]+[a-z]$`, no `_`, length 6-30), asserts each against the 7-skill catalog from ITEM-012 (`draft-decision`, `review-decision`, `draft-item`, `review-item`, `impact-assessment`, `validate-spec-graph`, `adopt-existing-project`). Catalog was amended from six to seven skills on 2026-05-28.
   - Theory `EveryFilePathMentionedIsCanonical` — checks the canonical file paths the docs reference (`.specforge.json`, `SPECFORGE.md`, `CLAUDE.md`, `AGENTS.md`, `agents/openai.yaml`, `~/.claude/skills/`, `~/.agents/skills/`) appear consistently — i.e., if a doc says `.specforge.config` it fails the spell-check against the closed set of canonical paths.

## Invariants

- **No code change.** ITEM-013 is content-only; no `Specforge.Core` or `Specforge.Mcp` files are modified.
- **No spec-graph mutations.** Authoring this item is documentation work, not graph evolution.
- **`docs/` lives at repo root**, not under `spec/`. The `spec/` tree is the dogfood package; `docs/` is the project's user-facing surface. Both are top-level peers per the existing `ART-README-TOP` (which is at repo root) precedent.
- **Tool and skill name spellings are unified with the live catalogs.** The content-drift test enforces this; manual editing of the docs without running the test risks drift.
- **The four project-level file outputs are listed consistently** in both `README.md` and `docs/getting-started.md`: `.specforge.json`, `SPECFORGE.md`, `CLAUDE.md` tagged block, `AGENTS.md` tagged block. `agents/openai.yaml` is listed as a fifth optional Codex-mode output. This list is the single canonical adopter-facing surface for what `init` writes.
- **The upgrade ritual ends with `validate(aspect=all)`** — both as a documented step and as the only post-upgrade verification specforge mandates. No other invariants are documented (CI configuration, IDE setup) because those are adopter-environment-specific.
- **Build-from-source path uses the exact `dotnet publish` command from DEC-001** (`dotnet publish src/Specforge.Mcp/Specforge.Mcp.csproj -c Release -o <publish-dir>`); pre-built binary path leaves `<release-url>` as a placeholder until release tooling is set up (out of MVP).
- **Content-drift test fails the build** on any catalog drift detected after Stage 2 implementation lands; this is the gate that keeps the docs honest as the catalog evolves.

## Code Scope

**In scope (created or modified by this item):**

Content (new) at repo root:

- `docs/getting-started.md` — the walkthrough described in Target Behavior §2.

Content (modified) at repo root:

- `README.md` — rewritten per Target Behavior §1. The current file's content (initial bootstrap from project init) is largely replaced; the file path is unchanged so `ART-README-TOP` keeps its existing identity.

Ledger (modifications):

- `spec/packages/specforge-mvp/ledger/artifacts.md` — append one new descriptive row `ART-DOCS-GETTING-STARTED` for `docs/getting-started.md`. The existing `ART-README-TOP` row's `Last update` cell is refreshed to record the revision; no new row.
- `spec/packages/specforge-mvp/ledger/history.md` — append a `Created` event for `ART-DOCS-GETTING-STARTED` and a `Revised` (or `Content updated`) event for `ART-README-TOP`.

Tests (new):

- `Specforge.Tests/Docs/DocsContentReferenceTests.cs` — single test class with three theories per Target Behavior §3.

**Out of scope (deferred or out of MVP):**

- A full tool-reference page or auto-generated tool-catalog page — defer; the docs cite DEC-007 directly.
- A migration guide from spec-kit / other tools — out of MVP.
- A FAQ — defer until adopter questions surface.
- Localization / translation — out of MVP.
- API reference for the C# code — out of MVP; specforge's public surface is the 21-tool MCP catalog.
- `docs/architecture.md` or similar deep-dive page — defer; the DECs themselves play this role.
- A separate `docs/upgrade.md` file — the upgrade ritual is a section in `docs/getting-started.md` to keep the entry-point doc unified.
- Release-pipeline / publishing documentation — out of MVP (release tooling itself is post-MVP).

## Test Scope

One new test class (`DocsContentReferenceTests`) with three theories. The theories share the body-walking + identifier-extraction logic with ITEM-012's `SkillContentReferenceTests` — a small static helper can be factored if both lands at Stage 2, or each test class can keep its own copy. ~10-20 effective test cases (two docs × multiple identifiers each).

## Test Plan

1. Author `docs/getting-started.md` per Target Behavior §2.
2. Rewrite `README.md` per Target Behavior §1.
3. Append the new `ART-DOCS-GETTING-STARTED` row and refresh `ART-README-TOP`'s Last update cell in `ledger/artifacts.md`.
4. Append the two new history events in `ledger/history.md`.
5. Add `DocsContentReferenceTests` with three theories.
6. Run `dotnet test`; verify the new tests + all prior-item tests + architecture test still pass.
7. Manual smoke: open `README.md` in a Markdown previewer; verify the install snippet + links + tagline render correctly. Open `docs/getting-started.md`; verify the section navigation works (table of contents implicit from headings).
8. Walk the bootstrap quartet against a fresh temp-dir project as documented; assert each step succeeds and the documented output matches the actual output of the tools.

## Test Evidence

- Console output of `dotnet build` (zero warnings).
- Console output of `dotnet test` (all pass, including `DocsContentReferenceTests`).
- Architecture test still green.
- A transcript at `test/Specforge.Tests/Evidence/itm013-bootstrap-walkthrough.txt` showing the bootstrap quartet (`init`, `install_skills`, `use_package`, `validate`) executed against a fresh temp-dir project, with output matching the `docs/getting-started.md` examples.
- A transcript at `test/Specforge.Tests/Evidence/itm013-upgrade-walkthrough.txt` showing the upgrade ritual against a project initially bootstrapped on an earlier `schemaVersion` (fixture), exercising the opportunistic-write path from DEC-006.

## Impact Assessment

| Aspect | Impact | Notes |
|---|---|---|
| MCP tool surface | No impact | No new tools; no behavioral change to existing tools. |
| Spec graph operations | No impact | Pure documentation. |
| Build pipeline | No impact | Docs are not embedded; no glob extension. |
| Core library boundary | No impact | No code change. |
| Error handling | No impact | No new codes. |
| Configuration discovery | No impact | DEC-002 mechanics unchanged. |
| ID scheme | No impact | One descriptive ART row added (`ART-DOCS-GETTING-STARTED`); no new kinds, no new sequential IDs. |
| Schema versioning | Indirect | The docs surface the DEC-006 upgrade-ritual flow to adopters; the mechanics themselves are unchanged. |
| Skill packaging | Indirect | The docs surface the `install_skills` one-time call as part of the bootstrap quartet; the mechanics are unchanged. |
| Lifecycle policy | No impact | Lifecycle of `ART-README-TOP` unchanged; `ART-DOCS-GETTING-STARTED` enters Draft on first publication. |
| Ledger structure | Indirect | One new descriptive ART row; ledger semantics unchanged. |
| Documentation | Direct | This is the documentation item. After ITEM-013, an adopter can self-onboard without conversation context. |
| External adoption | Direct | The `docs/getting-started.md` walkthrough is the primary first-touch surface for any human adopter; the AI counterpart is the `adopt-existing-project` SKILL.md from ITEM-012. |
| Test coverage scope | Indirect | One new test class; the integration test is the runtime gate against drift between docs and live catalogs. |
| Performance | No measurable | Static content. |
| Concurrency | No measurable | Static content. |
| Maintenance burden | Direct (small) | Two docs must be kept in sync with the catalog and the bootstrap-quartet semantics; the content-drift test catches the most common drift class (tool/skill name renames), but file-path renames and prose-substance drift require human review during DEC amendments. |

## Validation

- **Build**: `dotnet build Specforge.sln -c Release` succeeds with zero warnings.
- **Test**: `dotnet test` succeeds; the new `DocsContentReferenceTests` passes; all prior tests + architecture test still pass.
- **Boundary**: architecture test still green (no new MCP/Hosting references in Core; no code change in this item).
- **Tool-name drift**: every backticked tool name in `README.md` and `docs/getting-started.md` is in the 21-tool catalog per the two-phase check from ITEM-012.
- **Skill-name drift**: every backticked skill name in either doc is in the 7-skill catalog from ITEM-012.
- **File-path consistency**: every project-level file path (`.specforge.json`, `SPECFORGE.md`, `CLAUDE.md`, `AGENTS.md`, `agents/openai.yaml`) is spelled identically across both docs.
- **Bootstrap walkthrough reproducibility**: against a fresh temp-dir project, executing the documented bootstrap quartet produces the documented output. (Verified by `itm013-bootstrap-walkthrough.txt`.)
- **Upgrade ritual reproducibility**: against a fixture bootstrapped on an earlier `schemaVersion`, executing the documented upgrade ritual produces the documented behavior. (Verified by `itm013-upgrade-walkthrough.txt`.)
- **Markdown well-formedness**: both docs parse with Markdig without errors (a lightweight test or a manual rendering check; the content-drift test parses both files anyway, so a Markdig parse failure surfaces there).

## Open Questions

- Whether `docs/getting-started.md` should include an "uninstall" section covering `delete_decision` / `delete_item` for accidental creations during onboarding. Leaning yes — small additional subsection; reinforces DEC-007's Delete Semantics. Finalize during authoring.
- Whether the install section should show both PowerShell and Command Prompt invocations of `dotnet publish` (Windows-only per DEC-001 in MVP). Leaning PowerShell only — it's the more modern Windows shell; cmd users can adapt. Reconsider if adopter feedback surfaces.
- Whether the upgrade ritual should mandate a `git commit` step between `init` and `install_skills` so changes are reviewable in git diff. Leaning yes for the documented workflow (an explicit "review in git" sentence between steps) — improves auditability without adding tool semantics.
- Whether to include a "Troubleshooting" section. Leaning no for MVP — defer until specific adopter issues recur. The 21-tool error envelope (DEC-007) already gives the AI structured diagnosis paths.
- Whether `README.md`'s "Status" sentence should be updated automatically by `init` (i.e., a new tagged block) or kept manual. Leaning manual — it changes too rarely to justify another tagged block; document the status update as a manual step in the upgrade ritual.

## Done Criteria

The item is **Done** (post-Approved) when:

1. `docs/getting-started.md` exists at the repo root with the sections enumerated in Target Behavior §2.
2. `README.md` at the repo root carries the revised structure from Target Behavior §1.
3. The `ART-DOCS-GETTING-STARTED` row exists in `<package>/ledger/artifacts.md`.
4. The `ART-README-TOP` row in `<package>/ledger/artifacts.md` has its `Last update` cell refreshed.
5. `dotnet build Specforge.sln -c Release` reports zero warnings.
6. `dotnet test` runs every new test plus all prior-item tests; all pass.
7. `DocsContentReferenceTests` passes all three theories.
8. `Specforge.Core.dll` carries no `ModelContextProtocol.*` reference (architecture test).
9. `Specforge.Mcp` registers twenty-one tools (catalog unchanged from ITEM-010).
10. Both smoke transcripts exist under `test/Specforge.Tests/Evidence/`.
11. A `CMT-NNN` row is appended to `ledger/commits.md`.
12. History events recording the new artifact and the README revision are appended to `ledger/history.md`.
13. **Stage 1 milestone**: with ITEM-013 Approved, all 13 Stage 1 items are Approved; `work_plan.md` Stage 1 marked complete; Stage 2 implementation officially unblocked.

## Links

- `../decisions/DEC-001-DISTRIBUTION-AND-TRANSPORT.md` (approved) — sections "Distribution Path", "MCP Registration".
- `../decisions/DEC-005-SKILL-INSTALLATION-MODEL.md` (approved) — sections "Install Tool Contract", "Target Layout", "Codex Project YAML".
- `../decisions/DEC-006-SCHEMA-VERSIONING.md` (approved) — sections "Schema Version Mechanics", "Upgrade Behavior".
- `../decisions/DEC-007-MVP-TOOL-SET.md` (approved) — section "MVP Tool Catalog" (21 tools), "Error Envelope".
- `../decisions/DEC-008-INSTRUCTION-LAYER-DESIGN.md` (approved) — sections "Project-Level Files", "Tagged-Block Convention", "MVP Skill Catalog".
- `./ITEM-005-SKILL-INSTALLER.md` (approved) — `install_skills` operational details surfaced by the bootstrap walkthrough.
- `./ITEM-006-INIT-TOOL.md` (approved) — `init` operational details surfaced by the bootstrap walkthrough.
- `./ITEM-010-VALIDATE-TOOL.md` (approved) — `validate` operational details surfaced by the baseline-confirmation step.
- `./ITEM-012-SKILL-CONTENT.md` (approved) — the 7 skill bodies the docs reference by name; the content-drift test pattern reused here.
- `../../../shared/document_lifecycle.md` — lifecycle states the docs mention in passing.
- `../../../shared/glossary.md` — terminology the docs reuse without redefinition.
- `../../../../README.md` (revised by this item) — repo-root entry point. (Relative path: items → specforge-mvp → packages → spec → repo root.)
- `../../../../docs/getting-started.md` (created by this item) — walkthrough surface. (Same relative-path depth as the README.)

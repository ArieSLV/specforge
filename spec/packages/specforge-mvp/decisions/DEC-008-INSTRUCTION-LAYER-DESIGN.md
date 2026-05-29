# DEC-008-INSTRUCTION-LAYER-DESIGN - Instruction Layer Design

Status: Approved
Date: 2026-05-28
Owner: AI assistant (drafted)
Review owner: User (approved 2026-05-28)
Covers: instruction layer model, MVP skill catalog (seven narrow action-focused skills — amended from six by ITEM-011 audit on 2026-05-28 closing the `review-item` gap), skill content principles, project-level `SPECFORGE.md` + tagged-block merge into `CLAUDE.md`/`AGENTS.md`, shared-docs surfacing, MCP Resources stance, `init` amendments for project-level files
Supersedes: none
Amends: `DEC-007-MVP-TOOL-SET` (extends `init` to also write `SPECFORGE.md` and update tagged blocks in `CLAUDE.md`/`AGENTS.md`)

## Context

`DEC-001-DISTRIBUTION-AND-TRANSPORT` committed to a two-layer multi-agent strategy: a cross-agent MCP tool layer plus a per-agent skill layer. DEC-005 settled the *mechanics* of skill packaging (where files live, how they install, who owns the namespace). DEC-007 settled the *MCP tool surface* (21 tools, rich descriptions, error envelope).

What remained unwritten — and what this final Stage 0 decision settles — is the **instruction layer**: the human-and-AI-readable content that tells an agent *when and how* to invoke the seventeen + four tools. The mechanics decisions placed the skills in `~/.claude/skills/` and `~/.agents/skills/`; this decision decides what each `SKILL.md` actually carries. The tool decisions made every tool self-describing via JSON Schema; this decision decides which workflows are big enough that a skill (rather than a tool description) is the right home.

A third surface enters here: **project-level behavioral guidance**. A target project (RavenDB-26295, or specforge itself, or a future adopter) needs an at-the-root signal that says "this codebase uses specforge — here's the relevant context for working with it." That signal naturally lives in `CLAUDE.md` (Claude Code's convention) and `AGENTS.md` (Codex's convention). DEC-007's `init` already writes the Codex `agents/openai.yaml` technical-wiring file; DEC-008 extends `init`'s scope to also write/update the *behavioral* files.

On 2026-05-28 the user chose, via structured questions:

- **Skill set scope**: **~6 narrow, action-focused skills**, each covering one multi-tool workflow with a 1-2 page `SKILL.md`.
- **Project-level guidance**: a standalone **`SPECFORGE.md`** at project root, plus a **tagged block** (`<!-- specforge:start --> ... <!-- specforge:end -->`) inserted into `CLAUDE.md` and `AGENTS.md` that references it.
- **MCP Resources**: **not in MVP** — all use cases are covered by the existing tool surface; defer.
- **Shared docs**: skills **reference `spec/shared/*` files by relative path** and rely on the agent's own file-read tool to fetch them on demand; no inline duplication into skill content.

This record formalizes those choices, enumerates the seven MVP skills with their triggers and references (catalog amended from six to seven on 2026-05-28 — see "Amendments" near the bottom of this record), defines the `SPECFORGE.md` content shape and the tagged-block convention, and amends DEC-007's `init` contract to cover the new files.

## Decision

### Instruction Layer Model

Behavioral guidance reaches the AI through four converging surfaces. This decision places content into each at the right layer of generality.

| Layer | Lives in | Authoritative for | Owned by |
|---|---|---|---|
| **Tool descriptions** | JSON Schema attached to each MCP tool (`Specforge.Mcp`) | "What does *this* tool do? What are its args?" | DEC-007 (the rich-schemas rule) |
| **Skills** (this DEC) | `~/.claude/skills/<name>/SKILL.md` and `~/.agents/skills/<name>/SKILL.md` (DEC-005) | "How do I *combine* tools to accomplish a multi-step workflow?" | DEC-008 (this record) |
| **Project-level guidance** | `<project>/SPECFORGE.md` + tagged block in `<project>/CLAUDE.md` and `<project>/AGENTS.md` | "What is *this specific project's* spec layout, packages, conventions?" | DEC-008 (this record); written by `init` (DEC-007 amendment below) |
| **Shared docs** | `spec/shared/*.md` (lifecycle, glossary, impact checklist, contract) | "What are the spec system's invariants and reference data?" | The shared documents themselves; surfaced to AI via skill cross-references |

A skill never duplicates a tool description; it references the tool by name. A skill never duplicates a shared doc; it references the file by relative path. Project-level guidance never duplicates skills; it points at them by name and adds the project-specific context they can't know.

### MVP Skill Catalog

Seven skills ship in MVP (catalog amended from six to seven on 2026-05-28 after the post-Stage-1 spec audit found item-side review had no parallel skill to `review-decision` — see "Amendments" below). Each skill is a multi-tool workflow that benefits from being recognized as a named pattern.

| Skill name | Trigger phrases (description keywords) | Tools it stitches | Shared docs it references |
|---|---|---|---|
| `draft-decision` | "draft a decision", "create a DEC", "author an architectural decision record" | `create_decision` → write Context/Decision body → invoke `impact-assessment` skill → optional `append_history` → `set_decision_status` | `spec_item_contract.md`, `decision_record.md` (template), `impact_assessment_checklist.md`, `document_lifecycle.md` |
| `review-decision` | "review this decision", "approve a DEC", "evaluate a decision record" | `get_decision` → assess against contract / impact coverage / stable locators → propose changes → `set_decision_status(id, ..., reviewer, notes)` (which appends the `REV-DEC-NNN-NNN` row) | `document_lifecycle.md`, `spec_item_contract.md` |
| `draft-item` | "draft an item", "create an ITEM", "author an item spec" | `create_item` → populate sections per the item contract → `set_item_status` | `spec_item_contract.md`, `item_spec.md` (template) |
| `review-item` | "review this item", "approve an ITEM", "evaluate an item spec" | `get_item` → assess against contract / impact coverage / stable locators → propose changes → `set_item_status(id, ..., reviewer, notes)` (which appends the `REV-ITEM-NNN-NNN` row) | `document_lifecycle.md`, `spec_item_contract.md` |
| `impact-assessment` | "impact assessment", "fill the impact table", "what does this affect" | (read-only during fill) → write the Impact Assessment markdown table directly into the decision/item body | `impact_assessment_checklist.md` |
| `validate-spec-graph` | "validate the spec", "check the ledger", "find broken links" | `validate(aspect="all")` → group issues by severity → propose fixes → apply via the appropriate write tools | `document_lifecycle.md` (lifecycle-aspect issues) |
| `adopt-existing-project` | "adopt this project", "onboard specforge into this repo", "set up specforge here" | Survey existing layout → `init` with explicit `packages[]` referencing the existing directory → declare `extraKinds` if the layout uses non-core kinds → `install_skills` if not yet run on this machine | DEC-002 portability stance, DEC-004 `extraKinds` rule |

Each skill is **1-2 pages** of markdown. The full text of each `SKILL.md` is authored under ITEM-012 (see Related Item Specs); DEC-008 fixes the catalog (seven names, seven triggers, seven tool-stitches, seven reference sets) but not the full body text.

### Skill Content Principles

Every `SKILL.md` honors these rules:

1. **YAML frontmatter is the intersection of agent requirements** (DEC-005): `name` (matches directory), `description` (≤200 chars, verb-rich, lists trigger phrases). No specforge-proprietary keys.
2. **Body is action-focused, imperative voice**: "To draft a decision: 1) call `create_decision(title)`; 2) populate the Context section explaining the pressure; 3) ..."
3. **Tools are named exactly as in DEC-007** (e.g. `create_decision`, not "the decision creation tool"). The model can then surface tool calls without ambiguity.
4. **Shared docs are referenced by relative path** (e.g. "see `spec/shared/impact_assessment_checklist.md`"). The model uses its own Read tool to fetch when needed; specforge does not embed the shared content into skill bodies (which would create drift the moment the shared doc changes).
5. **No inline copy of tool input schemas**. Tools self-describe via JSON Schema; the skill names *when* to use them, the tool description names *what* they take.
6. **Cross-skill references are explicit**. `draft-decision` says "after writing the body, invoke the `impact-assessment` skill to fill the impact table." The model is free to chain skills as needed.
7. **No project-specific content** inside a `SKILL.md`. Skills are user-wide (DEC-005) and must work for every project. Project-specific knowledge lives in `SPECFORGE.md` (next section).

### Project-Level Files

Two files at the project root carry project-specific behavioral guidance:

#### `SPECFORGE.md`

A standalone file owned end-to-end by specforge. `init` writes it; `init` overwrites it on re-run (same lock-in as `install_skills`'s always-overwrite rule from DEC-005). Users who want to customize fork the file under a different name and reference it manually.

Content sections (in order):

```markdown
# SPECFORGE — Spec-Driven Workspace for `<project-name>`

## Spec layout
- Packages: <enumerate from .specforge.json>
- Shared docs: `<sharedPath>/`
- Templates: `<templatesPath>/`
- Active package selection: <single-package auto-select | multi-package via `use_package`>

## Identifier scheme
- Core kinds: DEC, ITEM, ART, REV, CMT (per DEC-004)
- Extra kinds declared for this project: <from packages[].extraKinds>
- Bare references resolve inside the active package; cross-package references use `<package>/<id>`.

## Tools available
<the 21 tools, one-line each — generated from DEC-007's catalog>

## Skills available
<the 7 skills, one-line each>

## Lifecycle
States and transitions per `<sharedPath>/document_lifecycle.md`.

## Generated by
`init` on <date>; specforge binary version <version>; config schemaVersion <N>.
```

The exact text is produced by `init` from the live config. `SPECFORGE.md` is therefore always **synchronized with the current `.specforge.json`** — running `init` again refreshes it.

#### Tagged Block in `CLAUDE.md` and `AGENTS.md`

`init` writes (or updates) the following block in `CLAUDE.md` and `AGENTS.md` at the project root:

```markdown
<!-- specforge:start -->
This project uses specforge for spec-driven development.

See `./SPECFORGE.md` for the project's spec layout, identifier scheme, available tools and skills, and lifecycle.

When the user asks to draft, review, validate, or onboard a spec artifact, prefer the matching specforge skill (`draft-decision`, `review-decision`, `draft-item`, `impact-assessment`, `validate-spec-graph`, `adopt-existing-project`) and the underlying MCP tools over freeform file edits.

For multi-package projects, call `use_package` at the start of the session before any package-dependent tool.
<!-- specforge:end -->
```

Rules for the tagged block:

- **Delimiters are exact strings**: `<!-- specforge:start -->` and `<!-- specforge:end -->`. `init` searches for the start delimiter; if absent, prepends the block (with a blank line of separation) at the top of the file; if present, replaces the content between the two delimiters.
- **User content outside the block is untouched**. Re-running `init` is safe — it does not disturb hand-authored guidance in the same file.
- **Block content is generated from a template**, not the live config (the project-specific facts live in `SPECFORGE.md` which the block points to). The block is short and stable across re-runs unless DEC-008 itself amends the template.
- **If `CLAUDE.md` or `AGENTS.md` does not exist**, `init` creates it with only the tagged block as content. The user is then free to add their own guidance below.
- **Both files always carry the same block**. `init` updates them in lockstep regardless of which agent is detected — this matters because users routinely use both Claude Code and Codex against the same repo.

### Shared Docs Surfacing

Skills reference `spec/shared/*` documents by relative path. The agent's own file-read tool fetches them on demand. specforge does not embed these into skill bodies and does not provide a `get_shared_doc` MCP tool.

Rationale: shared docs already live in the project's working tree (`<project>/spec/shared/*`), which is the agent's universe of fileable content. A specforge wrapper would add indirection without adding capability.

When a skill needs to point at a shared doc, it does so explicitly:

> ...details and the full aspect list are in `spec/shared/impact_assessment_checklist.md`.

The agent reads the file when it needs the detail.

### MCP Resources Stance

specforge **does not publish MCP Resources** in MVP.

- `info` exposes the diagnostic data Resources would otherwise carry.
- `list_packages` / `get_decision` / `get_item` expose every content concern.
- The agent's own file-read tool covers raw-file access.
- Resources would duplicate existing surfaces with no observed gain.

This stance is documented here so a future DEC reopening the question has a clear "no, and here's why" to argue against.

### `init` Amendment (DEC-007 extension)

`DEC-007-MVP-TOOL-SET` defined `init` as the bootstrap tool. DEC-008 extends `init`'s contract to cover project-level instruction files.

New `init` responsibilities (additive to DEC-007's enumeration):

1. Write or refresh `<project>/SPECFORGE.md` with content generated from the active `.specforge.json` per the section template above. Always overwrite.
2. Write or update the tagged block in `<project>/CLAUDE.md` (create the file if absent). Only the tagged block is touched.
3. Write or update the tagged block in `<project>/AGENTS.md` (create the file if absent). Only the tagged block is touched.

Existing DEC-007 responsibilities (generating `.specforge.json`, optionally generating a conforming `spec/` layout, writing Codex `agents/openai.yaml`) are unchanged.

`init`'s argument surface gains one optional flag:

| Argument | Type | Default | Purpose |
|---|---|---|---|
| `behavioralFiles` | enum: `all` \| `claude-only` \| `codex-only` \| `none` | `all` | Restrict which behavioral files are written. `all` covers SPECFORGE.md + CLAUDE.md block + AGENTS.md block. `none` skips behavioral files (use when the user has hand-authored guidance and does not want any auto-merge). |

`dryRun` continues to work as before — reports planned writes (including SPECFORGE.md content and the tagged block content) without touching the filesystem.

### Re-`init` Behavior

Running `init` on a project that already has specforge files:

- **`.specforge.json`** — DEC-006 in-memory upgrade applies; the file is rewritten only if its schemaVersion is below current.
- **`agents/openai.yaml`** (DEC-005 boundary) — overwritten; `init` owns it end-to-end.
- **`SPECFORGE.md`** — overwritten; specforge owns it end-to-end. Same model as the user-wide skill files in DEC-005.
- **`CLAUDE.md`** — *only the tagged block* is replaced. Surrounding user content is preserved byte-for-byte.
- **`AGENTS.md`** — same as `CLAUDE.md`.

A user who genuinely wants to detach from specforge's project-level files removes them by hand or runs `init` with `behavioralFiles: none` to suppress future updates.

### Error Types

No new typed exceptions in Core for DEC-008. The new behavioral-file writes use the existing `SpecforgeSkillInstallException`-style filesystem-error pattern, surfaced through `specforge.tool.invalid_argument` (for malformed args) and through a generic write-failure that reuses `specforge.skills.install_failed` semantics:

- Tagged-block parse failure (delimiter found but not properly closed) surfaces as `specforge.tool.invalid_argument` with `argument: "<file>"`, `expected: "balanced <!-- specforge:start --> ... <!-- specforge:end --> block"`, suggesting "remove the partial block or run `init behavioralFiles=none`".

No new error-code rows are added to DEC-007's catalog beyond reusing existing semantics.

## Alternatives Considered

| Alternative | Rejection reason |
|---|---|
| 4 minimal skills (drop `draft-item` and `validate-spec-graph`) | Items are the bulk of Stage 1 work (16 planned); a skill that names the multi-tool flow is worth more than a generic "items are like decisions" transfer instruction. `validate-spec-graph` covers the most error-prone class of work (cross-link integrity) — pulling it out of the catalog would push that knowledge into raw tool-description reading. |
| 8-10 skills with finer granularity (`append-history`, `cross-reference-stable-locators`, `lifecycle-transitions`, `id-scheme-reference` as separate skills) | The finer-grained skills overlap on trigger phrases and compete for keyword-match selection (e.g. `cross-reference-stable-locators` and `id-scheme-reference` would both grab "what's the ID format?"). Better to keep the four reference topics inside the shared docs (`document_lifecycle.md`, `glossary.md`) and let the action skills point at them. |
| 1 mega-skill (`specforge-workflow`) covering everything | Trigger keyword matching becomes one OR of every possible phrase — diluted match quality. Skills win when each one is narrowly named and the model can pick the right one for the prompt. |
| Tagged-block merge only (no standalone `SPECFORGE.md`) | The block would balloon into 5-10 screens of project-specific content inside CLAUDE.md/AGENTS.md — invasive in files the user co-owns. Separating the live project facts into `SPECFORGE.md` keeps the block short and stable. |
| Standalone `SPECFORGE.md` only (no tagged-block merge) | The user has to wire it manually for the agent to discover it. Defeats the deterministic-by-default principle. Without the block, a fresh Claude Code session may never read `SPECFORGE.md`. |
| Nothing project-level — skills carry all guidance | Skills are user-wide and cannot know that *this* repo has packages named X, Y, Z, or uses extra kinds A, B. Project-specific facts must live with the project; the alternative is hardcoding facts into user-wide skills, which breaks the moment the user works on a second project. |
| Skills embed (copy) shared docs inline | Self-contained skills, but every shared-doc edit creates drift between live content and skill copy. Skills get re-installed only on `install_skills`; shared docs evolve continuously. Reference-by-path keeps the surfaces in sync without an installer step. |
| `get_shared_doc(name)` MCP tool | Adds a 22nd tool that wraps file system read with no indirection benefit — the docs already live in the visible file tree. Justified only if shared docs ever moved outside the visible tree; for MVP they do not. |
| MCP Resources for shared docs | Duplicates the file-read path. Resources matter when the server holds state the file system doesn't expose (live computed values, remote data). Shared docs are static files — no Resources advantage. |
| MCP Resources for embedded skill catalog (read-only view) | Lets the model fetch `specforge://skills/<name>/SKILL.md` without installation, but DEC-005 deliberately routes all skill access through `install_skills`. A Resources view would create a shortcut around that intent. |
| No MCP Resources (this decision) — plus future commitment to never add them | Over-commits. The choice today is "no Resources in MVP"; a future DEC may identify a real use case (e.g. a long-running specforge-as-shared-server mode where file access from the agent is restricted). The door stays open. |
| Tagged-block update only (no overwrite of `SPECFORGE.md`) | Half the model — keeps the merge convention but loses the always-current project snapshot. `SPECFORGE.md` exists precisely because re-`init` keeps it in sync; freezing it would defeat the point. |
| Prompt-on-conflict for the tagged block | specforge is a headless MCP server; there is no interactive prompt channel. Same reasoning that rejected prompt-per-conflict for `install_skills` in DEC-005. |

## Consequences

- The MVP ships **seven `SKILL.md` files** under `spec/skills/` (amended from six to seven on 2026-05-28 to add `review-item`). ITEM-012 authors them; the embedded-skill catalog from DEC-005 picks them up automatically at build time.
- `install_skills` from DEC-005 now has a defined payload: those seven skills. The skill catalog grows only by DEC amendment.
- `init` from DEC-007 gains the `behavioralFiles` argument and the responsibility for `SPECFORGE.md` + tagged blocks in `CLAUDE.md` / `AGENTS.md`. ITEM-006 (the init implementation) carries this scope from the start.
- Every project that runs `init` ends up with `SPECFORGE.md` at its root, plus tagged blocks in `CLAUDE.md` and `AGENTS.md`. The block is short and stable; the long-form file is regenerated from the live config.
- The tagged-block delimiter strings (`<!-- specforge:start -->` / `<!-- specforge:end -->`) become a *public contract* — a future DEC changing them would force every adopter to update their existing files. They are documented here as a deliberate stable surface.
- Re-`init` is safe and idempotent for the behavioral files: SPECFORGE.md is overwritten (specforge-owned), CLAUDE.md/AGENTS.md preserve user content outside the block.
- Users who do not want any project-level files run `init behavioralFiles=none`. Documentation surfaces this for users with strong file-ownership preferences.
- Skills reference `spec/shared/*` paths verbatim; if the user customizes their `shared` path via DEC-002's optional override convention, the skill paths still resolve relative to the active package (the agent's file-read tool naturally handles relative resolution from the project root).
- No MCP Resources land in MVP. A future DEC may revisit if a use case emerges.
- The instruction layer model (tool descriptions / skills / project-level / shared docs) becomes the canonical map for where to put any new behavioral guidance. Anything that does not fit one of the four layers prompts re-evaluation of the model.

## Impact Assessment

| Aspect | Impact | Notes |
|---|---|---|
| Instruction layer | Direct | This decision defines it. |
| Skill catalog | Direct | Six MVP skills named and scoped. |
| Skill content authoring | Indirect | Content lives in ITEM-012, written against the rules here. |
| Project-level integration | Direct | `SPECFORGE.md` + tagged-block convention defined. |
| `init` tool surface | Direct | DEC-007 `init` amended with `behavioralFiles` argument and new file outputs. |
| Multi-agent strategy | Direct | Same tagged-block content lands in both `CLAUDE.md` (Claude Code) and `AGENTS.md` (Codex). |
| MCP Resources | Direct | Explicit "no Resources in MVP" stance recorded. |
| Shared docs | Indirect | Surfaced via skill cross-references, no specforge wrapper. |
| Configuration discovery | No impact | DEC-002 mechanics unchanged. |
| Error handling | Indirect | Tagged-block-parse error reuses existing `specforge.tool.invalid_argument` semantics; no new error codes. |
| Documentation | Direct | Top-level README and getting-started must show the layered model and the four files a new project gets (`.specforge.json`, `agents/openai.yaml`, `SPECFORGE.md`, tagged-block CLAUDE.md/AGENTS.md). |
| Target project portability | Direct | `init` produces a uniform set of project-level files regardless of the adopted layout. |
| User customization | Indirect | Tagged-block convention preserves user content outside the block; `behavioralFiles=none` opts out entirely. |
| Maintenance burden | Indirect | Six skill bodies + one tagged-block template; growth gated by DEC amendments. |
| Performance | No measurable | All writes are local; `init` runs rarely. |
| Concurrency | No measurable | Single-session `init`. |
| Future extensions | Indirect | Adding a seventh skill or a new tagged-block template line is a DEC amendment, not a structural change. |

## Source Links

| Source | Locator | Evidence |
|---|---|---|
| `DEC-001-DISTRIBUTION-AND-TRANSPORT` | section "Multi-Agent Strategy" | Two-layer model (cross-agent MCP tools + per-agent skills); supported agents Claude Code + Codex. |
| `DEC-005-SKILL-INSTALLATION-MODEL` | sections "Source Layout", "Canonical SKILL.md Format" | Skill file location and frontmatter intersection rule this decision's skill bodies honor. |
| `DEC-005-SKILL-INSTALLATION-MODEL` | section "Re-install Behavior" | Always-overwrite model, paralleled here for `SPECFORGE.md`. |
| `DEC-007-MVP-TOOL-SET` | section "MVP Tool Catalog" | The 21 tools the seven skills name and stitch. |
| `DEC-007-MVP-TOOL-SET` | section "Setup" | `init` tool this decision amends. |
| `DEC-007-MVP-TOOL-SET` | section "Error Envelope" | Error envelope reused (no new codes here). |
| `DEC-002-CONFIGURATION-AND-DISCOVERY` | sections "Configuration Schema", "Shared and Templates Resolution" | Source of `.specforge.json` facts that `init` projects into `SPECFORGE.md`. |
| `DEC-004-ID-SCHEME-CUSTOMIZATION` | sections "Core Kinds", "Per-Package Customization" | Source of identifier facts surfaced in `SPECFORGE.md`. |
| Conversation 2026-05-28, structured questions (Round 1 for DEC-008) | AskUserQuestion answers | Six narrow skills; `SPECFORGE.md` + tagged-block; no Resources in MVP; shared docs by relative path. |

## Related Decisions

- `DEC-001-DISTRIBUTION-AND-TRANSPORT` (approved): defines the multi-agent skill layer this decision fills with content.
- `DEC-002-CONFIGURATION-AND-DISCOVERY` (approved): `SPECFORGE.md` projects `.specforge.json` facts.
- `DEC-004-ID-SCHEME-CUSTOMIZATION` (approved): `SPECFORGE.md` projects ID-scheme facts.
- `DEC-005-SKILL-INSTALLATION-MODEL` (approved): packaging and install mechanics for the seven skills.
- `DEC-006-SCHEMA-VERSIONING` (approved): no direct interaction; `SPECFORGE.md` mentions the current `schemaVersion` in the footer.
- `DEC-007-MVP-TOOL-SET` (approved): amended here to extend `init`'s file outputs and argument surface.

## Related Item Specs

- `ITEM-006-INIT-TOOL` (planned): implements the amended `init` with `behavioralFiles` arg, `SPECFORGE.md` generation, and the tagged-block merge logic for `CLAUDE.md` / `AGENTS.md`.
- `ITEM-012-SKILL-CONTENT` (planned): authors the seven `SKILL.md` bodies (and any supporting files) under `spec/skills/` per the rules in this decision.
- `ITEM-013-PROJECT-LEVEL-TEMPLATES` (planned, if needed): houses the `SPECFORGE.md` content template and the tagged-block template, if they outgrow being inlined in ITEM-006.

## Related Tests / Validation

- Each shipped `SKILL.md` has YAML frontmatter with valid `name` (matching directory) and `description` (≤200 chars, non-empty). Tested by the `IEmbeddedSkillCatalog` startup validation from DEC-005.
- `install_skills` writes exactly seven skills to each agent directory; no extras, no omissions.
- `init` on a fresh directory produces `.specforge.json`, `agents/openai.yaml`, `SPECFORGE.md`, `CLAUDE.md`, `AGENTS.md` — five files. CLAUDE.md and AGENTS.md contain the tagged block only.
- `init` re-run on a directory with hand-authored content in `CLAUDE.md` preserves all content outside the tagged-block delimiters byte-for-byte.
- `init behavioralFiles=none` skips `SPECFORGE.md`, `CLAUDE.md`, `AGENTS.md` writes entirely; still produces `.specforge.json` and `agents/openai.yaml`.
- `init behavioralFiles=claude-only` writes the tagged block to `CLAUDE.md` but not `AGENTS.md`; still produces `SPECFORGE.md` (the file is agent-agnostic).
- `init dryRun=true behavioralFiles=all` lists every planned file and the planned tagged-block content for each; no filesystem changes.
- A `CLAUDE.md` containing a partial tagged block (start but no end) causes `init` to return `specforge.tool.invalid_argument` per the contract above.
- `SPECFORGE.md` after `init` includes the current `.specforge.json` schemaVersion in its footer and the current binary version.
- Each of the seven skills' trigger phrases (`draft a decision`, `review this decision`, `review this item`, ...) survive a round-trip through the embedded resource pipeline and appear in the installed `SKILL.md` frontmatter `description`.

## Amendments

| Date | Trigger | Change | Rationale |
|---|---|---|---|
| 2026-05-28 | Post-Stage-1 end-to-end spec audit | MVP skill catalog expanded from 6 to 7 by adding `review-item`. New row inserted in the "MVP Skill Catalog" table (between `draft-item` and `impact-assessment`). All occurrences of "six skills" / "6 skills" / "the six" updated to seven. Consequences and Related Tests updated accordingly. | Audit found ITEM-008 referenced `review-item` as "if added", revealing a real asymmetry: decisions had a dedicated review skill but items did not. Adding `review-item` restores symmetry between the DEC and ITEM sides of the spec graph. The new skill mirrors `review-decision` structurally (uses `get_item` + `set_item_status` with reviewer+notes; appends `REV-ITEM-NNN-NNN`). Additive change: existing 6 skills are unchanged. |

## Open Questions

- Whether `SPECFORGE.md` should include a "common workflows" cheat sheet (e.g. "to add a new decision, ask: draft-decision") in addition to the catalog facts — leaning yes; finalize in ITEM-012/ITEM-006. Not blocking.
- Whether the tagged-block content should be parameterized by detected agents (e.g. only mention skills if the agent supports them) — defer; the block is short enough that universal content is the cleaner default.
- Whether to ship localized versions of `SKILL.md` for non-English users — defer; agents handle multilingual prompts and the skill bodies are technical labels rather than user-facing prose.
- Whether to ship a `SPECFORGE.md.template` users can override per project — defer; the always-overwrite model fights customization for a benefit that hasn't been requested.
- Whether `init` should detect existing tagged blocks in *other* locations (CONTRIBUTING.md, README.md) and report on them — defer; assume the convention is `CLAUDE.md`/`AGENTS.md` and document that.
- Whether a future `uninstall` MCP tool covers project-level files (removing the tagged block, deleting `SPECFORGE.md`) — defer alongside DEC-005's uninstall question.

# ITEM-012-SKILL-CONTENT - Six SKILL.md Bodies for MVP Skill Catalog

Status: Approved
Review owner: User
Depends on: `ITEM-004-EMBEDDED-SKILL-CATALOG`, `ITEM-005-SKILL-INSTALLER`, `ITEM-007-DECISION-TOOLS`, `ITEM-008-ITEM-TOOLS`, `ITEM-009-LEDGER-APPEND-TOOLS`, `ITEM-010-VALIDATE-TOOL`, `DEC-005-SKILL-INSTALLATION-MODEL`, `DEC-007-MVP-TOOL-SET`, `DEC-008-INSTRUCTION-LAYER-DESIGN`
Updates ledger rows: new `ART-ITEM-012`; new `ART-SKILL-DRAFT-DECISION`, `ART-SKILL-REVIEW-DECISION`, `ART-SKILL-DRAFT-ITEM`, `ART-SKILL-REVIEW-ITEM`, `ART-SKILL-IMPACT-ASSESSMENT`, `ART-SKILL-VALIDATE-SPEC-GRAPH`, `ART-SKILL-ADOPT-EXISTING-PROJECT`; new `CMT-NNN` row for the implementation commit

## Handoff Summary

Ship the seven MVP SKILL.md bodies that DEC-008 named (catalog amended from six to seven on 2026-05-28 to add `review-item`): `draft-decision`, `review-decision`, `draft-item`, `review-item`, `impact-assessment`, `validate-spec-graph`, `adopt-existing-project`. Content-authoring item only — no code, no tools, no NuGet packages, no error codes. The 7 SKILL.md files live at `spec/skills/<kebab>/SKILL.md`; ITEM-004's existing `<EmbeddedResource>` glob picks them up at build time and ITEM-005's `install_skills` tool deploys them at runtime. ITEM-012 specifies each skill's exact frontmatter (`name` + `description` ≤200 chars per ITEM-004), the canonical body outline (When-to-trigger / Steps / Tools used / Shared docs referenced / Cross-skill references), and the content rules from DEC-008 §"Skill Content Principles" enforced by an integration test that walks every skill body and asserts (a) every tool name mentioned is registered in the live tool catalog per DEC-007, (b) every shared-doc reference is a valid relative path under `spec/shared/`, (c) every cross-skill reference names a skill in the catalog. Seven new descriptive `ART-SKILL-*` rows track the artifacts.

- 0 new tools, 0 new NuGet packages, 0 new error codes, 0 new typed exceptions, 0 new embedded-resource globs (ITEM-004's glob covers the new files).
- 6 new SKILL.md files; 6 new descriptive ART rows in `<package>/ledger/artifacts.md`.
- 1 new integration test: `SkillContentReferenceTests` — walks every SKILL.md body, parses Markdig AST, extracts tool names + relative paths + cross-skill references, asserts each is valid.
- Per DEC-008's 7 content principles, each SKILL.md is portable across packages (no specforge-mvp-specific facts in the bodies).

## Problem Slice

This item resolves "what does the AI actually read when a host triggers one of the seven MVP skills?" — the answer must be seven self-contained Markdown bodies that another AI session in a fresh adopter's repo can follow without conversation context.

Explicit non-goals (out of MVP or owned by other items):

- A skill-authoring tool (`create_skill`) — out of MVP. The 6 MVP skills are authored manually; future additions could justify a tool, but the catalog isn't expected to expand quickly.
- Per-package custom skills — out of MVP. DEC-005 restricts skills to user-wide scope; per-package skills would require a separate scope mechanism deferred to a future DEC.
- Skill-content versioning — handled implicitly by `install_skills`'s always-overwrite semantics (DEC-005); a SKILL.md change ships with the next specforge binary release and the next user-side `install_skills` call.
- A `lint_skill_content` MCP tool — `SkillCatalogValidator` from ITEM-004 already runs at server startup; the integration test introduced here covers the static content invariants. A runtime lint tool would duplicate without value-add at MVP corpus size.
- The `SPECFORGE.md` / tagged-block content authored at adopter sites — that's owned by ITEM-006's `init` tool. ITEM-012's `adopt-existing-project` SKILL.md *references* `init` but doesn't author the project-level files.
- Documentation / README — owned by ITEM-013.

## Terminology Used

- **SKILL.md body**: the Markdown file at `spec/skills/<kebab-name>/SKILL.md` consumed by Claude Code (`~/.claude/skills/<name>/`) and Codex (`~/.agents/skills/<name>/`).
- **Frontmatter intersection**: per DEC-005, the only frontmatter keys both agents require are `name` and `description`. Skills MAY define more keys; only those two are validated by `SkillCatalogValidator`.
- **Trigger condition**: a natural-language description of when the AI should invoke the skill, written in the first paragraph of the body so the host's skill-selection layer can match it. Not a structured field — DEC-008's 7 principles disallow inline schemas.
- **Step**: a numbered imperative sentence telling the AI what to do, optionally with sub-steps. The body's "Steps" section is the operational core.
- **Tools used**: a flat list naming the registered MCP tool names this skill expects to call. The integration test asserts every name appears in the live catalog.
- **Cross-skill reference**: an explicit text reference (e.g., "after drafting, invoke `review-decision`") naming a sibling skill. The integration test asserts every named skill is in the catalog.

## Approved Decisions

- `DEC-005-SKILL-INSTALLATION-MODEL` — sections "Source Layout" (`spec/skills/<kebab-name>/SKILL.md`), "Canonical Frontmatter" (`name` + `description`), "Install Tool Contract" (always overwrite). ITEM-012 produces files at the exact source layout DEC-005 named.
- `DEC-007-MVP-TOOL-SET` — sections "MVP Tool Catalog" (21 tools by name). Every tool a skill mentions must be in this list.
- `DEC-008-INSTRUCTION-LAYER-DESIGN` — sections "MVP Skill Catalog" (the seven skill names + intended triggers), "Skill Content Principles" (the 7 rules), "Tagged-Block Convention" (referenced by `adopt-existing-project`).
- `spec/shared/impact_assessment_checklist.md` — referenced by the `impact-assessment` skill and indirectly by `draft-decision` / `draft-item`.
- `spec/shared/document_lifecycle.md` — referenced by `review-decision` (lifecycle transitions) and `validate-spec-graph` (lifecycle aspect findings).
- `spec/shared/spec_item_contract.md` — referenced by `draft-item` (required sections).

## Current Code State

After ITEM-010 lands (ITEM-011's centralization is helpful but not a strict prerequisite — the integration test can fall back to a hard-coded 21-tool list per DEC-007 if the reflection-based `SpecforgeErrorCode`-style catalog from ITEM-011 isn't yet available):

- `Specforge.Core/Skills/EmbeddedSkillCatalog` reads embedded resources at `specforge.skills/<kebab>/SKILL.md` (the LogicalName transform from ITEM-004).
- `Specforge.Core/Skills/SkillCatalogValidator` runs at server startup; broken frontmatter → fail-fast.
- `Specforge.Mcp/Tools/Skills/InstallSkillsTool` extracts the 7 skills to `~/.claude/skills/<name>/SKILL.md` and `~/.agents/skills/<name>/SKILL.md`.
- `spec/skills/.gitkeep` exists as the directory anchor from ITEM-004.
- **No actual SKILL.md files exist yet.** The 7 skills are referenced by name in DEC-008 §"MVP Skill Catalog" but their bodies have not been written.
- The empty-catalog code path in `SkillCatalogValidator` returns "OK" (per ITEM-004 invariant: empty catalog is legitimate). ITEM-012 transitions the dogfood catalog from empty to 7-skill.

## Target Behavior

After this item is Done:

1. Seven SKILL.md files exist at `spec/skills/<kebab>/SKILL.md`:
   - `spec/skills/draft-decision/SKILL.md`
   - `spec/skills/review-decision/SKILL.md`
   - `spec/skills/draft-item/SKILL.md`
   - `spec/skills/review-item/SKILL.md`
   - `spec/skills/impact-assessment/SKILL.md`
   - `spec/skills/validate-spec-graph/SKILL.md`
   - `spec/skills/adopt-existing-project/SKILL.md`
2. Each file has YAML frontmatter conforming to DEC-005's intersection schema:
   ```yaml
   ---
   name: <kebab-case name matching the directory>
   description: <≤200-char single-sentence trigger description>
   ---
   ```
3. Each body uses the canonical outline:
   - **First paragraph** (no heading): a 1-3 sentence narrative trigger description; this is what the host's skill-selection layer matches against.
   - **`## When to use`**: bullet list of trigger conditions in addition to the first-paragraph narrative.
   - **`## Steps`**: numbered imperative steps. Sub-steps with letters. Use exact tool names in backticks.
   - **`## Tools used`**: flat bullet list of tool names; one bullet per tool; backtick the names. (Redundant with Steps but lets a fast-scanning host preview the toolchain.)
   - **`## Shared docs referenced`**: bullet list of relative paths to `spec/shared/*.md` AND/OR `spec/templates/*.md` files this skill expects to consult. (The section heading retains the short name "Shared docs referenced" for readability; templates are accepted because some skills — notably `draft-decision`/`draft-item` — naturally reference the corresponding template alongside the shared-doc set.)
   - **`## Cross-skill references`**: bullet list naming sibling skills the AI should invoke before, after, or instead.
4. The build-time embedded-resource glob from ITEM-004 picks up all 6 files automatically (no csproj edit needed).
5. The startup `SkillCatalogValidator` from ITEM-004 enumerates 7 skills with zero errors.
6. `install_skills agent=all` deploys 7 skills × 2 agents = 14 files. (`install_skills agent=claude-code dryRun=true` lists 7 target paths.)
7. The new integration test `SkillContentReferenceTests` walks all 6 bodies and verifies:
   - Tool-name reference check (two-phase): walk every backticked identifier in the body. (a) If the identifier is in the closed 21-tool list (sourced either from ITEM-011's `SpecforgeErrorCode`-adjacent tool registry via reflection, or from a hard-coded 21-name list per DEC-007 if ITEM-011's surface isn't yet present), accept — registered tool reference. (b) Otherwise, if the identifier has tool-name shape (regex `^[a-z][a-z_]+[a-z]$`, contains `_`, length 4-30) AND is NOT in the 21-tool list, fail — unregistered tool name (typo or stale rename). (c) Otherwise (likely an ordinary English word like `the`, `init`, `info` when written outside a tool-reference context), skip. The two-phase check accepts the four single-word tool names (`init`, `info`, `validate`, etc.) when they're listed by exact match, and catches `_`-containing typos like `create_decisions` (plural drift). Single-word typos like `infoo` are NOT caught by this test but ARE caught at runtime when the AI invokes the bad name and the server returns `specforge.tool.unknown` (or equivalent).
   - Every relative path matching `spec/shared/[a-z_]+\.md` OR `spec/templates/[a-z_]+\.md` resolves to an existing file in the dogfood repo. (The canonical body section is named `## Shared docs referenced` but the regex accepts both shared and templates entries — see Invariants for the rule.)
   - Every cross-skill name (backticked, in `## Cross-skill references` section) is in the 7-skill set.

## Per-Skill Content Outline

The following seven subsections give a fresh AI session enough detail to author each SKILL.md verbatim. The Stage 2 implementer translates each outline into the canonical body sections above. Wording can polish; structure and substance are locked.

### 1. `draft-decision`

**Frontmatter**:
- `name: draft-decision`
- `description: Use when the user wants to record a new architectural decision or resolve a design fork. Drafts a DEC-NNN via create_decision, populates required sections, and transitions to Draft for review.` (192 chars; ≤200 per ITEM-004 contract)

**First paragraph (narrative)**: "Use this skill when the user wants to record a new architectural decision — typically when a design fork emerges in conversation (which library, which pattern, which lifecycle), or when the user explicitly says 'let's write a DEC'. Produces a single new decision file in the active package's `decisions/` directory, the corresponding `ART-DEC-NNN` ledger row, and a `Created` history event — all in one tool call via `create_decision`."

**When to use** (bullets):
- The user is resolving an architectural fork that affects multiple future items.
- The user explicitly asks for a decision record.
- The current task can't proceed until a design choice is locked.

**Steps**:
1. If unsure which package is active, call `info` to check; call `use_package` if needed.
2. Survey related decisions via `list_decisions status=Approved` and `get_decision <related-id>` for context.
3. Frame the design fork: what's being decided, what alternatives exist, why one is preferred.
4. If the user hasn't decided on a fork, use the host's structured-question facility to elicit choices before drafting. Do not draft against an unresolved fork.
5. Invoke the `impact-assessment` skill to populate the Impact Assessment table.
6. Call `create_decision title="<Plain Title>" status="Draft"` to allocate `DEC-NNN` and scaffold the file.
7. Edit the new file's body sections (Context, Decision, Alternatives Considered, Consequences, Impact Assessment, Source Links, Related Decisions, Open Questions) per the template.
8. Call `set_decision_status id="DEC-NNN" status="Draft for user review"` when the draft is ready.
9. Invoke the `review-decision` skill (the user now reviews).

**Tools used**: `info`, `use_package`, `list_decisions`, `get_decision`, `create_decision`, `set_decision_status`.

**Shared docs referenced**: `spec/shared/document_lifecycle.md`, `spec/shared/impact_assessment_checklist.md`, `spec/templates/decision_record.md`.

**Cross-skill references**: `impact-assessment` (called during step 5); `review-decision` (called at step 9).

### 2. `review-decision`

**Frontmatter**:
- `name: review-decision`
- `description: Use when a decision is Draft for user review or the user requests a review pass. Reads the body, checks required sections and lifecycle, then transitions to Approved or Refined.` (177 chars; ≤200 per ITEM-004 contract)

**First paragraph (narrative)**: "Use this skill when the user wants you to review a decision record — either freshly drafted (`Draft for user review`) or one returning for a second pass. The skill reads the full decision body, verifies the required sections are populated and self-consistent, applies the impact-assessment lens, and either records approval via `set_decision_status` (which appends the REV row) or surfaces actionable feedback so the user can refine."

**When to use** (bullets):
- A decision is in `Draft for user review` status and the user requests review.
- The user wants to formally approve a decision (`set_decision_status status=Approved`).
- The user wants to request changes on an in-flight decision.

**Steps**:
1. Call `get_decision id="DEC-NNN"` to read the full body and metadata.
2. Verify required sections are present and non-empty: Context, Decision, Alternatives Considered, Consequences, Impact Assessment, Source Links, Related Decisions, Open Questions.
3. Run `validate(aspect=lifecycle)` to catch any status mismatches or missing REV evidence for this decision.
4. Cross-check the Impact Assessment table by invoking the `impact-assessment` skill if any aspect is missing or implausible.
5. If approving: call `set_decision_status id="DEC-NNN" status="Approved" reviewer="<name>" notes="<summary>"` — this appends `REV-DEC-NNN-NNN` automatically.
6. If requesting changes: list the issues; suggest `set_decision_status status="Refined"` to record a refinement pass.
7. If withdrawing: suggest `set_decision_status status="Withdrawn"` after confirmation.

**Tools used**: `get_decision`, `set_decision_status`, `validate`.

**Shared docs referenced**: `spec/shared/document_lifecycle.md`, `spec/shared/impact_assessment_checklist.md`.

**Cross-skill references**: `impact-assessment` (called during step 4); `validate-spec-graph` (closely related — `review-decision` calls `validate(aspect=lifecycle)` directly, but `validate-spec-graph` is the right skill if broader validation is needed).

### 3. `draft-item`

**Frontmatter**:
- `name: draft-item`
- `description: Use when the user requests a new item spec for an implementation slice. Drafts an ITEM-NNN record using create_item, populates required sections, and transitions to Draft for user review.` (187 chars; ≤200 per ITEM-004 contract)

**First paragraph (narrative)**: "Use this skill when the user wants to specify a new implementation slice — typically after at least one foundation decision (`DEC-NNN`) is approved. Produces a single new item file in the active package's `items/` directory, the corresponding `ART-ITEM-NNN` ledger row, and a `Created` history event. Items are the unit of Stage 1 work and the granular unit of Stage 2 implementation."

**When to use** (bullets):
- A foundation decision is approved and the user wants to specify the next implementation slice.
- The user explicitly asks for an item spec.
- An existing item spec has grown too large and should be split.

**Steps**:
1. If unsure which package is active, call `info`; call `use_package` if needed.
2. Survey related approved decisions via `list_decisions status=Approved` to enumerate the dependencies for the new item.
3. Survey related items via `list_items` to avoid duplication.
4. Frame the implementation slice: what's being built, what depends on it, what it doesn't cover.
5. Call `create_item title="<Plain Title>" status="Draft" dependsOn=["DEC-NNN", "DEC-MMM", ...]` — `dependsOn` lists the approved decisions the new item realizes.
6. Edit the new file's body sections per `spec/templates/item_spec.md` and the required-section list in `spec/shared/spec_item_contract.md`: Handoff Summary, Problem Slice, Approved Decisions, Current Code State, Target Behavior, Invariants, Code Scope, Test Scope, Test Plan, Impact Assessment, Validation, Done Criteria, plus optional Terminology Used / Open Questions / Links.
7. Invoke the `impact-assessment` skill to populate the Impact Assessment table.
8. Invoke the `validate-spec-graph` skill with `aspect=impact-coverage` to verify all required sections are populated before transitioning.
9. Call `set_item_status id="ITEM-NNN" status="Draft for user review"` when ready.

**Tools used**: `info`, `use_package`, `list_decisions`, `list_items`, `create_item`, `set_item_status`, `validate`.

**Shared docs referenced**: `spec/shared/spec_item_contract.md`, `spec/shared/document_lifecycle.md`, `spec/shared/impact_assessment_checklist.md`, `spec/templates/item_spec.md`.

**Cross-skill references**: `impact-assessment` (step 7); `validate-spec-graph` (step 8); `review-decision` (if user requests review of approved-prerequisite decisions before drafting the item); `review-item` (called when the user wants the freshly-drafted item reviewed and approved).

### 4. `review-item`

**Frontmatter**:
- `name: review-item`
- `description: Use when an item is Draft for user review or the user requests a review pass. Reads the body, checks required sections and lifecycle, then transitions to Approved or Refined.` (179 chars; ≤200 per ITEM-004 contract)

**First paragraph (narrative)**: "Use this skill when the user wants you to review an item spec — either freshly drafted (`Draft for user review`) or one returning for a second pass. The skill reads the full item body, verifies the required sections are populated per `spec/shared/spec_item_contract.md` and self-consistent, applies the impact-assessment lens, and either records approval via `set_item_status` (which appends the REV row when reviewer+notes supplied) or surfaces actionable feedback so the user can refine. Mirrors `review-decision` structurally."

**When to use** (bullets):
- An item is in `Draft for user review` status and the user requests review.
- The user wants to formally approve an item (`set_item_status status=Approved`).
- The user wants to request changes on an in-flight item.

**Steps**:
1. Call `get_item id="ITEM-NNN"` to read the full body and metadata. The returned payload includes `missingRequiredSections` (per ITEM-008's `ItemDocument` shape) — surface this first when present.
2. Verify required sections are present and non-empty per `spec/shared/spec_item_contract.md`: Handoff Summary, Problem Slice, Terminology Used, Approved Decisions, Current Code State, Target Behavior, Invariants, Code Scope, Test Scope, Test Plan, Test Evidence, Impact Assessment, Validation, Open Questions, Done Criteria, Links.
3. Run `validate(aspect=impact-coverage)` to catch any missing-required-section findings against this item; cross-reference the `validate-spec-graph` skill for fix recipes if findings are non-trivial.
4. Cross-check the Impact Assessment table by invoking the `impact-assessment` skill if any aspect is missing or implausible.
5. Inspect the `Depends on:` line; verify each referenced ID exists or is intentionally forward-looking (per ITEM-008 invariant — items can predate their targets transiently).
6. If approving: call `set_item_status id="ITEM-NNN" status="Approved" reviewer="<name>" notes="<summary>"` — this appends `REV-ITEM-NNN-NNN` automatically.
7. If requesting changes: list the issues; suggest `set_item_status status="Refined"` to record a refinement pass.
8. If withdrawing: suggest `set_item_status status="Withdrawn"` after confirmation.

**Tools used**: `get_item`, `set_item_status`, `validate`.

**Shared docs referenced**: `spec/shared/document_lifecycle.md`, `spec/shared/spec_item_contract.md`, `spec/shared/impact_assessment_checklist.md`.

**Cross-skill references**: `impact-assessment` (called during step 4); `validate-spec-graph` (closely related — `review-item` calls `validate(aspect=impact-coverage)` directly, but `validate-spec-graph` is the right skill if broader validation is needed).

### 5. `impact-assessment`

**Frontmatter**:
- `name: impact-assessment`
- `description: Use when populating the Impact Assessment section of a decision or item. Walks the canonical aspect checklist and records No impact / Indirect / Direct per aspect with concrete justification.` (191 chars; ≤200 per ITEM-004 contract)

**First paragraph (narrative)**: "Use this skill when authoring or revising the Impact Assessment section of any decision or item record. The skill walks the canonical aspect list from `spec/shared/impact_assessment_checklist.md`, evaluates each aspect against the current artifact's scope, and records the impact category (`No impact` / `Indirect` / `Direct`) with a single-sentence justification. Always invoked from inside `draft-decision` or `draft-item`; never standalone."

**When to use** (bullets):
- Drafting a new decision record (called from `draft-decision`).
- Drafting a new item spec (called from `draft-item`).
- Revising an existing Impact Assessment section in response to scope changes.

**Steps**:
1. Read the canonical aspect list from `spec/shared/impact_assessment_checklist.md`.
2. For each aspect, classify the artifact's impact:
   - **No impact**: the artifact does not affect this aspect; do not omit the row — record explicitly.
   - **Indirect**: the artifact indirectly influences this aspect (e.g., a new error code affects test coverage scope but not behavior).
   - **Direct**: the artifact changes this aspect's behavior or contract.
3. Write a single-sentence justification per row, referring concretely to the artifact's content. Vague justifications (`"see Decision"`) are not acceptable — the table must stand alone.
4. If a new aspect emerges that isn't in the checklist, propose it to the user; do not silently extend the table.
5. Return to the calling skill (`draft-decision` or `draft-item`).

**Tools used**: (none — this is a pure authoring skill).

**Shared docs referenced**: `spec/shared/impact_assessment_checklist.md`.

**Cross-skill references**: invoked by `draft-decision`, `draft-item`, `review-decision`, `review-item`.

### 6. `validate-spec-graph`

**Frontmatter**:
- `name: validate-spec-graph`
- `description: Use before approving any artifact or before publishing a release. Runs validate(aspect=all), interprets findings by severity, and proposes fixes referencing the appropriate tools.` (179 chars; ≤200 per ITEM-004 contract)

**First paragraph (narrative)**: "Use this skill to confirm the spec graph is internally consistent — every artifact has its required sections, every cross-reference resolves, every lifecycle status agrees between file and ledger, every identifier is allocated cleanly. Invoke before approving any item via `set_item_status status=Approved`, before publishing a binary release, and after any large mass-rename or refactor."

**When to use** (bullets):
- Before approving any decision or item.
- Before publishing a release.
- After a mass rename, schema upgrade, or other cross-cutting edit.
- When investigating an apparent inconsistency the AI surfaced.

**Steps**:
1. Call `validate(aspect=all)`. Inspect `data.findings`.
2. For each finding by severity:
   - **`error`**: must be resolved before continuing. Use the suggestion field for the fix path.
   - **`warning`**: review and decide. Common warnings: dangling `Depends on` (acceptable in a transient state if the target is planned next), Approved-without-REV (record the manual review via `append_review` or amend status via `set_*_status` with reviewer+notes).
   - **`info`**: informational. Common info: Draft item with missing required sections (acceptable — item still in flight).
3. Common error patterns and their fixes:
   - `aspect=ids, message="Identifier gap not explained by a Deleted history event"`: either delete the gap-introducing artifact properly via `delete_*` (which appends the tombstone evidence) or, if the gap is intentional, append the explanatory history event manually via `append_history`.
   - `aspect=links, message="Reference to non-existent artifact"`: create the missing artifact via `create_decision` / `create_item`, or fix the reference, or remove the reference.
   - `aspect=lifecycle, message="Artifact file status (X) differs from ledger row status (Y)"`: call `set_*_status` with the intended status to sync.
   - `aspect=impact-coverage, message="<section> is required but missing"`: edit the artifact to add the section before reattempting `set_*_status status=Approved`.
4. Re-run `validate(aspect=all)`. Loop until `errorCount=0`.
5. Surface warnings and info findings to the user with the option to fix or defer.

**Tools used**: `validate`, `set_decision_status`, `set_item_status`, `create_decision`, `create_item`, `delete_decision`, `delete_item`, `delete_review`, `delete_commit`, `append_history`, `append_review`.

**Shared docs referenced**: `spec/shared/document_lifecycle.md`, `spec/shared/spec_item_contract.md`.

**Cross-skill references**: `review-decision`, `review-item`, `draft-decision`, `draft-item` (all of which call this skill for pre-approval gates).

### 7. `adopt-existing-project`

**Frontmatter**:
- `name: adopt-existing-project`
- `description: Use the first time a project starts using specforge. Walks init, install_skills, and the first validate to bring an empty repo into a known-clean baseline.` (155 chars; ≤200 per ITEM-004 contract)

**First paragraph (narrative)**: "Use this skill when bringing specforge into a project for the first time. Walks the bootstrap quartet: `init` to write `.specforge.json` and the project-level instruction layer, optionally scaffold a `spec/` directory layout, `install_skills` to deploy the seven MVP skills at user-wide scope, and a first `validate(aspect=all)` to confirm the empty repo is a clean baseline. After this, `draft-decision` is the natural next skill."

**When to use** (bullets):
- A fresh project has not yet been bootstrapped with `.specforge.json`.
- The user explicitly asks to onboard the project.
- A first-time installation of the specforge MCP binary.

**Steps**:
1. Run `info` to confirm the binary is reachable and to read `binaryVersion` + `supportedSchemaVersionRange`.
2. Call `init dryRun=true scaffold=true behavioralFiles=all` to preview the planned changes.
3. Surface the preview to the user; confirm scope (which packages, which agents' behavioral files).
4. Call `init dryRun=false scaffold=true behavioralFiles=all` (or per-agent) to write the files. Outputs include `.specforge.json`, optionally a `spec/` scaffold, `SPECFORGE.md`, and tagged blocks in `CLAUDE.md` / `AGENTS.md` per the user's selection.
5. Call `install_skills agent=all dryRun=true` to preview the 7 skills × 2 agents = 14 file paths.
6. Call `install_skills agent=all dryRun=false` to deploy the skills. (Always-overwrite reinstall is safe.)
7. Call `list_packages` to confirm the new package is recognized.
8. Call `use_package name="<new-package>"` to make it active.
9. Invoke the `validate-spec-graph` skill (`aspect=all`) to confirm `errorCount=0` on the empty baseline.
10. Surface to the user: "Baseline clean. Next step is `draft-decision` to record the first foundation decision."

**Tools used**: `info`, `init`, `install_skills`, `list_packages`, `use_package`, `validate`.

**Shared docs referenced**: (none — this skill bootstraps, so shared docs aren't yet present in a typical fresh adopter's repo until `init scaffold=true` writes them).

**Cross-skill references**: `validate-spec-graph` (step 9); `draft-decision` (recommended next at step 10).

## Invariants

- **No project-specific facts in any skill body.** A SKILL.md referring to `specforge-mvp` or DEC-001 etc. is wrong — those are facts of this dogfood package, not the cross-package skill contract. The integration test does not enforce this directly (too hard), but the content principles in DEC-008 are mandatory.
- **Backticked tool-name references are validated against the 21-tool catalog via a two-phase check.** Phase 1: exact-match lookup catches all 21 names including single-word ones (`init`, `info`, `validate`). Phase 2: any identifier with tool-name shape (regex `^[a-z][a-z_]+[a-z]$`, contains `_`, length 4-30) NOT in the catalog fails as unregistered. Identifiers that match neither phase are skipped (likely ordinary English words). Single-word typos slip the regex but get caught at runtime by the server returning a tool-not-found error.
- **Every relative path matching `spec/shared/<name>.md` OR `spec/templates/<name>.md` mentioned in a skill body's `## Shared docs referenced` section (or anywhere else in the body) must resolve to an existing file in the dogfood repo.** The integration test enforces this against the dogfood repo at test time. The section heading "Shared docs referenced" is a short name covering both shared docs and templates.
- **Every cross-skill reference (backticked, in the `## Cross-skill references` section) must name one of the seven MVP skills.** Adding a 7th skill is a DEC-008 amendment plus an ITEM-012 amendment.
- **Frontmatter must conform to DEC-005's intersection (`name` + `description`)** with `name` ≤30 chars matching `^[a-z][a-z0-9-]+$` and `description` ≤200 chars. `SkillCatalogValidator` from ITEM-004 enforces this at server startup.
- **Body section names are stable**: When to use / Steps / Tools used / Shared docs referenced / Cross-skill references. Adding a new section is allowed if cross-skill-consistent; renaming an existing one is a coordinated change across all 6 files.
- **Steps use imperative voice and exact tool names in backticks.** The 7 principles from DEC-008 are mandatory; deviation in the file shipped to users is a content-quality bug, not a build break.

## Code Scope

**In scope (created or modified by this item):**

Content (new) under `spec/skills/`:

- `spec/skills/draft-decision/SKILL.md`
- `spec/skills/review-decision/SKILL.md`
- `spec/skills/draft-item/SKILL.md`
- `spec/skills/impact-assessment/SKILL.md`
- `spec/skills/validate-spec-graph/SKILL.md`
- `spec/skills/adopt-existing-project/SKILL.md`

Tests (new):

- `Specforge.Tests/Skills/SkillContentReferenceTests.cs` — single test class with three theories: (a) `EveryToolNameMentionedIsRegistered` enumerates SKILL.md bodies, walks every backticked identifier through the two-phase check (exact-match against the 21-tool list; if no match, fail when the identifier has tool-name shape `^[a-z][a-z_]+[a-z]$` with `_`; otherwise skip as English word); (b) `EverySharedDocReferenceResolves` enumerates `spec/shared/*.md` and `spec/templates/*.md` relative-path mentions, asserts each file exists; (c) `EveryCrossSkillReferenceIsValid` enumerates the `## Cross-skill references` section of each body, asserts each mentioned skill name is in the 7-skill catalog.

Ledger (modifications):

- `spec/packages/specforge-mvp/ledger/artifacts.md` — append 6 new descriptive `ART-SKILL-*` rows (one per skill).
- `spec/packages/specforge-mvp/ledger/history.md` — append a `Created` event per skill.

**Out of scope (deferred to later items or out of MVP):**

- `lint_skill_content` MCP tool — duplicates the integration test at MVP corpus size.
- A 7th skill — DEC-008 amendment + ITEM-012 amendment required.
- Per-package custom skills — DEC-005 restricts to user-wide scope.
- Localized skill bodies — English-only in MVP.
- Skill-content versioning — DEC-005's always-overwrite is sufficient.

## Test Scope

One new test class with three theory-driven tests; the SkillContentReferenceTests class doubles as the catalog-integration anchor. The startup `SkillCatalogValidator` from ITEM-004 implicitly covers frontmatter validation. ~20-30 effective test cases across the three theories (seven skills × multiple references each).

## Test Plan

1. Author the 7 SKILL.md files per the outlines in this spec. Each frontmatter pair populated; each body section per the canonical outline.
2. Add `SkillContentReferenceTests` with three theories.
3. Append the 7 new `ART-SKILL-*` rows to `ledger/artifacts.md`.
4. Append `Created` history events.
5. Run `dotnet test`; verify the new tests + all prior-item tests + architecture test still pass. The startup `SkillCatalogValidator` now reports 7 skills instead of 0.
6. Manual smoke: `install_skills agent=claude-code dryRun=true` lists 7 skill paths under `~/.claude/skills/`; `install_skills agent=all dryRun=false` writes 14 files (7 × 2 agents). Both succeed without warnings.

## Test Evidence

- Console output of `dotnet build` (zero warnings).
- Console output of `dotnet test` (all pass, including `SkillContentReferenceTests`).
- Architecture test still green.
- A transcript at `test/Specforge.Tests/Evidence/itm012-install-skills.txt` showing `install_skills agent=all dryRun=false` writing 12 paths to a temp-dir-rooted user profile (using ITEM-005's DI-swappable `ISkillInstallTargetResolver`).
- A transcript at `test/Specforge.Tests/Evidence/itm012-skill-bodies.txt` showing the embedded resource names enumerated by `IEmbeddedSkillCatalog` — 6 entries, each with the expected `name` and `description` frontmatter.

## Impact Assessment

| Aspect | Impact | Notes |
|---|---|---|
| MCP tool surface | No impact | No new tools; no behavioral change to existing tools. |
| Spec graph operations | No impact | Content artifacts, not graph operations. |
| Build pipeline | Indirect | The 6 new files are picked up by ITEM-004's existing glob; binary size grows by the SKILL.md byte total. |
| Core library boundary | No impact | Pure content; no code change in Core. |
| Error handling | No impact | No new codes. |
| Configuration discovery | No impact | DEC-002 mechanics unchanged. |
| ID scheme | No impact | Only descriptive `ART-SKILL-*` rows added; no new kinds, no new sequential IDs. |
| Schema versioning | No impact | DEC-006 mechanics unchanged. |
| Skill packaging | Direct | DEC-005's catalog grows from empty to 7 skills; this is the first time the catalog has content. |
| Lifecycle policy | No impact | Skills don't carry lifecycle states (they're embedded resources, not spec-graph artifacts). |
| Ledger structure | Indirect | 6 new descriptive ART rows; no new kinds. |
| Documentation | Indirect | ITEM-013 (docs/README) will reference these 7 skills as the AI-side surface. |
| External adoption | Direct | The `adopt-existing-project` skill is the canonical first-touch flow for new adopters. |
| Test coverage scope | Indirect | One new test class; the integration test is the runtime gate against drift. |
| Performance | No measurable | Skill bodies are short Markdown files; reading them at startup is trivial. |
| Concurrency | No measurable | Static content. |
| Maintenance burden | Indirect | Six bodies must be kept in sync with the tool catalog and shared docs they reference; the integration test catches drift on the tool-name and shared-doc dimensions; cross-skill consistency on substance is human-reviewed. |

## Validation

- **Build**: `dotnet build Specforge.sln -c Release` succeeds with zero warnings.
- **Test**: `dotnet test` succeeds; the new `SkillContentReferenceTests` passes; all prior tests + architecture test still pass.
- **Frontmatter validation**: `SkillCatalogValidator` at server startup reports 7 skills, zero errors.
- **Catalog enumeration**: `install_skills agent=all dryRun=true` returns 14 target paths (7 skills × 2 agents).
- **Tool-name reachability**: every backticked tool name in every SKILL.md body resolves to a registered tool in the 21-tool catalog.
- **Shared-doc reachability**: every `spec/shared/*.md` and `spec/templates/*.md` reference in every body resolves to an existing file.
- **Cross-skill reachability**: every cross-skill reference in every body names one of the seven MVP skills.
- **Description length**: every `description:` frontmatter value is ≤200 chars per ITEM-004 contract.
- **Self-installability**: a fresh temp-dir-rooted `install_skills agent=all dryRun=false` writes 14 files; re-running it overwrites them byte-for-byte (always-overwrite semantics from DEC-005).

## Open Questions

- Whether `adopt-existing-project`'s step 9 should call `validate-spec-graph` skill directly (delegation) or inline the `validate(aspect=all)` call (terseness). Leaning delegation — cross-skill references are how the catalog stays composable; the integration test asserts the cross-skill name resolves. Finalize during authoring.
- Whether the `## When to use` bullet list and the first paragraph narrative are redundant. Leaning keep both — the first paragraph is for the host's selection layer (similarity-matched against user prompts), the bullet list is for the AI's planning step (explicit triggers it can reason over).
- Whether to include a `## Anti-patterns` section per skill (what NOT to do — e.g., "do not draft against an unresolved fork"). Leaning yes for `draft-decision` and `draft-item` where the pitfalls are concrete; the other four skills don't have clear anti-pattern lists. Make `## Anti-patterns` optional in the canonical outline.
- Whether `impact-assessment` should be marked as "called from" rather than a standalone skill (since DEC-008 implied it's almost always sub-invoked). Leaning keep it standalone with a strong "Always invoked from..." sentence in the first paragraph; the host may still match the skill on user prompts like "revisit the impact assessment for DEC-007".
- Whether the integration test should enforce that every tool category from DEC-007 is mentioned at least once across the 7 skills (so no tool is operationally orphaned in the skill layer). Leaning yes — small additional theory; finalize during implementation.

## Done Criteria

The item is **Done** (post-Approved) when:

1. All 7 SKILL.md files exist at `spec/skills/<kebab>/SKILL.md`.
2. Each file's frontmatter validates per `SkillCatalogValidator`.
3. Each body conforms to the canonical outline (first paragraph + When to use + Steps + Tools used + Shared docs referenced + Cross-skill references).
4. `dotnet build Specforge.sln -c Release` reports zero warnings.
5. `dotnet test` runs every new test plus all prior-item tests; all pass.
6. `SkillContentReferenceTests` passes all three theories.
7. `Specforge.Core.dll` carries no `ModelContextProtocol.*` reference (architecture test).
8. `install_skills agent=all dryRun=true` returns 14 target paths.
9. The 6 new `ART-SKILL-*` rows exist in `<package>/ledger/artifacts.md`.
10. Both smoke transcripts exist under `test/Specforge.Tests/Evidence/`.
11. A `CMT-NNN` row is appended to `ledger/commits.md`.
12. History events recording the 7 new artifacts are appended to `ledger/history.md`.

## Links

- `../decisions/DEC-005-SKILL-INSTALLATION-MODEL.md` (approved) — sections "Source Layout", "Canonical Frontmatter", "Install Tool Contract", "Embedding Mechanism".
- `../decisions/DEC-007-MVP-TOOL-SET.md` (approved) — section "MVP Tool Catalog" — every tool name a skill mentions is sourced from this catalog.
- `../decisions/DEC-008-INSTRUCTION-LAYER-DESIGN.md` (approved) — sections "MVP Skill Catalog" (the seven names), "Skill Content Principles" (the 7 rules), "Tagged-Block Convention" (referenced by `adopt-existing-project`).
- `./ITEM-004-EMBEDDED-SKILL-CATALOG.md` (approved) — embedded-resource glob picks up these files; startup validator enforces frontmatter.
- `./ITEM-005-SKILL-INSTALLER.md` (approved) — `install_skills` deploys these files to user-wide scope.
- `./ITEM-007-DECISION-TOOLS.md` (approved) — the decision tools named by `draft-decision` and `review-decision`.
- `./ITEM-008-ITEM-TOOLS.md` (approved) — the item tools named by `draft-item`.
- `./ITEM-009-LEDGER-APPEND-TOOLS.md` (approved) — `append_history`/`append_review` named by `validate-spec-graph` for fix paths.
- `./ITEM-010-VALIDATE-TOOL.md` (approved) — `validate` named by `validate-spec-graph` and `adopt-existing-project`.
- `../../../shared/impact_assessment_checklist.md` — referenced by `impact-assessment` and indirectly by `draft-decision`/`draft-item`.
- `../../../shared/document_lifecycle.md` — referenced by `review-decision`, `draft-item`, `validate-spec-graph`.
- `../../../shared/spec_item_contract.md` — referenced by `draft-item`, `validate-spec-graph`.
- `../../../templates/decision_record.md` — referenced by `draft-decision`.
- `../../../templates/item_spec.md` — referenced by `draft-item`.

# specforge-mvp Work Plan

Status: Draft
Created: 2026-05-25

## Purpose

This document orders the work needed to take specforge-mvp from "specification only" to a working MCP server adoptable by other projects.

It intentionally does not prescribe the full implementation in one pass. Each stage produces small reviewable artifacts.

## Principles

1. Decisions before items. Items before code.
2. Each decision record covers one concrete design fork.
3. Each item spec is self-contained enough to hand off to a fresh AI session.
4. Cross-cutting impact assessment runs on every meaningful decision.
5. Coverage links between decisions, items, and tests are living state, maintained as work proceeds.
6. ASCII diagrams are required when a decision involves multi-step flow or before/after structure.
7. The toolkit's own spec uses the toolkit's own conventions — dogfood discipline.
8. Spec-kit (GitHub) is acknowledged as inspiration but not authority; specforge diverges intentionally on naming, structure, and lifecycle.

## Stages

### Stage 0 — Decision Freeze

Goal: resolve the eight foundational design forks before any item specs or code.

Decision records to draft and approve, in this order:

1. `DEC-001-DISTRIBUTION-AND-TRANSPORT` — MCP transport, server lifecycle, binary distribution, registration model.
2. `DEC-002-CONFIGURATION-AND-DISCOVERY` — How the running server finds and identifies target packages.
3. `DEC-003-RUNTIME-AND-ARCHITECTURE` — .NET version, NuGet stack, Core library + MCP shell split.
4. `DEC-004-ID-SCHEME-CUSTOMIZATION` — Hardcoded IDs vs configurable schemes; default scheme.
5. `DEC-005-SKILL-INSTALLATION-MODEL` — Where skills live, how they couple to the MCP server.
6. `DEC-006-SCHEMA-VERSIONING` — Spec package schema versioning and forward-compatibility contract.
7. `DEC-007-MVP-TOOL-SET` — Final list of MCP tools for MVP with their JSON-schema sketches.
8. `DEC-008-INSTRUCTION-LAYER-DESIGN` — Split of behavioral guidance across tool descriptions, MCP Resources, skills, target-project `CLAUDE.md`, and shipped `AGENTS.md`. Drafted last in Stage 0 because it depends on DEC-005 and DEC-007.

Review gate: all eight decisions approved before Stage 1 begins.

### Stage 1 — Item Specifications

Goal: write one item spec per implementation slice. Each is independently reviewable and independently buildable.

Items reconciled against Stage 0 outputs (DEC-007 mapped 11; DEC-008 added 2). Original 16-item draft from 2026-05-25 is superseded by this list — the original was authored before Stage 0 decisions and did not match the final tool catalog (21 tools across 6 categories), the embedded-skill catalog, or the project-level file outputs.

Items planned (13):

**Foundation (3):**

- `ITEM-001-SOLUTION-BOOTSTRAP` — Scaffold `Specforge.sln`, the three projects (`Specforge.Core`, `Specforge.Mcp`, `Specforge.Tests`), `Directory.Build.props`, `Directory.Packages.props`, `global.json`, and the architecture test enforcing the Core/Mcp boundary. Per DEC-003.
- `ITEM-002-CONFIG-MODEL` — Implements `.specforge.json` discovery (cwd walk-up), parsing, schemaVersion check + in-memory upgrade chain, shape validation. Defines `SpecforgeConfigValidationException`. Implements `list_packages`, `use_package`, `info` tools. Per DEC-002, DEC-006, DEC-007.
- `ITEM-003-ID-VALIDATOR` — Implements DEC-004 regex set, reserved-kind check, kind-exhaustion check, slug rules, composite REV parsing, bare/qualified cross-package reference parsing. Consumed by every tool with an identifier arg.

**Skill machinery (2):**

- `ITEM-004-EMBEDDED-SKILL-CATALOG` — Implements `IEmbeddedSkillCatalog` in Core, the `<EmbeddedResource Include="..\..\spec\skills\**\*.*" />` glob in `Specforge.Core.csproj`, startup validation of every embedded `SKILL.md`. Per DEC-005.
- `ITEM-005-SKILL-INSTALLER` — Implements `ISkillInstaller` in Core and the `install_skills` MCP tool in Mcp (args `agent`, `dryRun`; always-overwrite reinstall; partial-failure semantics). Per DEC-005, DEC-007.

**Bootstrap and authoring tools (4):**

- `ITEM-006-INIT-TOOL` — Implements the `init` tool: generates `.specforge.json` with current `schemaVersion`, optionally generates a conforming `spec/` layout, writes Codex `agents/openai.yaml`, writes `SPECFORGE.md`, writes/updates tagged blocks in `CLAUDE.md`/`AGENTS.md`. Accepts `behavioralFiles: all|claude-only|codex-only|none`. Per DEC-002, DEC-005, DEC-007, DEC-008.
- `ITEM-007-DECISION-TOOLS` — Implements `list_decisions`, `get_decision`, `create_decision`, `set_decision_status`, `delete_decision` (with pre-Approved state check and REV-cascade). Defines `SpecforgeDeleteForbiddenException`. Per DEC-007.
- `ITEM-008-ITEM-TOOLS` — Same shape as ITEM-007 but for items: `list_items`, `get_item`, `create_item`, `set_item_status`, `delete_item`. Per DEC-007.
- `ITEM-009-LEDGER-APPEND-TOOLS` — Implements `append_history`, `append_review`, `append_commit`, `delete_review`, `delete_commit`. Per DEC-007.

**Validation and error envelope (2):**

- `ITEM-010-VALIDATE-TOOL` — Implements `validate(aspect: ids|links|lifecycle|impact-coverage|all)`. `aspect=ids` consults history-event tombstones for legitimate gap detection. Per DEC-007.
- `ITEM-011-ERROR-ENVELOPE` — Centralizes the Core-exception → MCP-code mapping in `Specforge.Mcp` (13 codes from DEC-007 + lifecycle delete). Defines the JSON envelope rendering with `{code, message, suggestion?, data?}`. Per DEC-007.

**Content (2):**

- `ITEM-012-SKILL-CONTENT` — Authors the seven SKILL.md bodies in `spec/skills/` (catalog amended from six to seven on 2026-05-28): `draft-decision`, `review-decision`, `draft-item`, `review-item`, `impact-assessment`, `validate-spec-graph`, `adopt-existing-project`. Per DEC-008.
- `ITEM-013-DOCS-GETTING-STARTED` — User-facing documentation: top-level `README.md` revision, `getting-started.md`, the install + first-`init` walkthrough. Surfaces the 4-file project output, the `install_skills` one-time call, and the upgrade ritual.

Stage 1 dependency order (rough): ITEM-001 → ITEM-002 → ITEM-003 → ITEM-011 (parallel with above three) → ITEM-004, ITEM-005 → ITEM-006 → ITEM-007, ITEM-008, ITEM-009 (parallel) → ITEM-010 → ITEM-012 → ITEM-013.

Review gate: each item independently approved.

### Stage 2 — Implementation

Goal: write code in the order item specs are approved.

Rule: no code merges until its item spec is `Approved`. POC code may exist on a branch but is evidence only.

### Stage 3 — First External Adoption

Goal: adopt specforge to manage at least one external package (e.g., reuse for the next RavenDB ticket).

This validates portability assumptions. Findings drive a `specforge-v2` package, not silent edits to the MVP.

## Operating Rules

- Use shared glossary terms (`../../shared/glossary.md`) consistently.
- Stable locators required (`../../shared/document_lifecycle.md` "Stable Reference Rule"); line numbers are optional convenience only.
- Cross-cutting impact assessment (`../../shared/impact_assessment_checklist.md`) for every non-trivial decision and every item spec.
- ASCII art for multi-step flows.
- Each decision must state Alternatives Considered with concrete rejection reasons.
- Each item must include a Handoff Summary that allows a fresh AI session to act on the spec without conversation context.

## Immediate Next Step

Stage 1 complete (2026-05-28); **Stage 2 complete (2026-05-30)**. All 8 Stage 0 decisions and all 13 Stage 1 items are Approved, and all 13 items are now implemented (ITEM-001..ITEM-013, commits CMT-001..CMT-013). specforge MVP is a runnable self-contained Windows x64 MCP binary: 21 tools across 5 families (setup / decisions / items / ledger / validate), 14 error codes, 13 typed Core exceptions, 7 embedded + installable skills, and read-only spec-graph validation. Dogfood `validate(aspect=all)` is clean (errorCount=0) and 356 tests pass — the toolkit now dogfoods its own completed MVP. **Immediate next step: Stage 3 — first external adoption**: adopt specforge to manage at least one external package (e.g., the next RavenDB ticket) to validate portability assumptions. Findings drive a `specforge-v2` package, not silent edits to the MVP.

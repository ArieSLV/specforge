# DEC-007-MVP-TOOL-SET - MVP MCP Tool Set

Status: Approved
Date: 2026-05-28
Owner: AI assistant (drafted)
Review owner: User (approved 2026-05-28)
Covers: tool surface principles (naming, granularity, read/write separation, schema richness), error envelope (shape, namespacing, suggestion field), error-code catalog, MVP tool catalog (21 tools across 6 categories, including pre-Approved delete tools), DEC-002 selection-error payload, DEC-005/DEC-006 open questions
Supersedes: none
Resolves: DEC-002 open question (selection-error payload); DEC-005 open question (`list_skills` vs `install_skills dryRun`); DEC-006 open question (binary `supportedMax` diagnostic)

## Context

Every prior Stage 0 decision delegates something to this record:

- **DEC-002-CONFIGURATION-AND-DISCOVERY** named `use_package` as a working tool name and deferred the selection-error payload shape.
- **DEC-003-RUNTIME-AND-ARCHITECTURE** placed tools in `Specforge.Mcp` delegating to `Specforge.Core` and left the MCP error envelope to this record.
- **DEC-004-ID-SCHEME-CUSTOMIZATION** required tools to accept bare and qualified identifiers and named three new exception types whose envelope mapping lives here.
- **DEC-005-SKILL-INSTALLATION-MODEL** specified the `install_skills` argument and result contract and deferred the final MCP envelope to this record; flagged `list_skills` as a possible companion tool to resolve here.
- **DEC-006-SCHEMA-VERSIONING** distinguished version errors from shape-validation errors and asked for distinct envelopes; flagged a `supportedMax`-reporting diagnostic tool as an open question to resolve here.

This decision closes all of those, defines the MVP tool catalog, and specifies the error envelope every tool surfaces. It does not define each tool's full input/output schema — those land in Stage 1 item specs (`ITEM-006` … `ITEM-022` or whichever item numbers are assigned). DEC-007 fixes the *contract* every Stage 1 tool implementation must honor.

On 2026-05-28 the user chose, across three structured-question rounds:

- **Surface shape**: `verb_noun` snake_case names; high-level spec-graph-aware multi-effect operations; separate read/write tools with `dryRun` on writes; rich JSON Schema with descriptions, examples, and enum constraints.
- **Error envelope**: structured `{code, message, suggestion?, data?}`; dotted namespaced codes `specforge.<domain>.<reason>`; top-level optional `suggestion`; selection-error payload carries structured `data.availablePackages` plus prose `message` plus `suggestion`.
- **Catalog**: generic `set_decision_status(id, status, reviewer?, notes?)` (and `set_item_status`) covering every transition; single `validate(aspect?)` with aspect enum; full item-side parity with decision-side tools; standalone `append_history` / `append_review` / `append_commit` alongside the multi-effect operations.
- **Delete** (added after a 4th round of structured questions): `delete_decision`, `delete_item`, `delete_review`, `delete_commit`; restricted to pre-Approved states; numbers tombstoned permanently (consistent with DEC-004 "numbers are never reused"); required `confirm: true` argument; cascading deletes for dependent ledger rows.

This record formalizes all of the above plus the open-question resolutions named at the top.

## Decision

### Surface Principles

| Principle | Rule |
|---|---|
| Naming | `verb_noun` snake_case (`list_decisions`, `create_decision`, `use_package`). |
| Granularity | One tool = one spec-graph operation, atomically maintaining cross-file invariants (file, ART row, ledger appends) where applicable. |
| Read/write split | Read tools (`list_*`, `get_*`, `validate`, `info`) are side-effect-free. Write tools (`create_*`, `set_*_status`, `init`, `install_skills`, `append_*`) explicitly mutate and accept `dryRun: boolean = false`. |
| Schema richness | Every parameter carries a JSON Schema `description`; closed sets use `enum`; identifier-shaped parameters use `pattern` matching DEC-004's regexes; representative `examples` accompany non-trivial parameters. |
| Identifier inputs | Tools that take identifiers accept both bare (`DEC-001`, valid only inside the active package's package-scope) and qualified (`<package>/DEC-001`) forms (DEC-004 cross-package referencing rule). |

### Error Envelope

Every tool returns either a success result or a structured error payload of the form:

```json
{
  "error": {
    "code": "specforge.<domain>.<reason>",
    "message": "<human/AI-readable summary>",
    "suggestion": "<actionable next step, optional>",
    "data": { /* code-specific structured payload, optional */ }
  }
}
```

- `code` is the **only** field tools dispatch on. It is stable across `Specforge.Core` exception renames.
- `message` is a single-paragraph human summary the AI can surface verbatim.
- `suggestion` is present whenever a deterministic next action exists; absent otherwise.
- `data` is present when the error carries machine-readable detail beyond `message`/`suggestion`.

The payload is delivered as JSON content inside the MCP tool result (the protocol-level `content` array), tagged with `mimeType: application/json`.

### Error-Code Catalog

This catalog freezes the namespace; future DECs add codes by amendment.

| Code | Triggering Core exception | `data` shape | Default suggestion |
|---|---|---|---|
| `specforge.config.not_found` | `SpecforgeConfigNotFoundException` | `{ searchedPath: string }` | `"run init to generate .specforge.json at the project root"` |
| `specforge.config.schema_version_unsupported` | `SpecforgeSchemaVersionException` | `{ path: string, claimedVersion: int\|string\|null, supportedRange: [int, int] }` | `"upgrade specforge to a version that supports schemaVersion=<claimed> or lower"` |
| `specforge.config.validation_failed` | `SpecforgeConfigValidationException` (defined in ITEM-002) | `{ path: string, errors: [{ pointer: string, message: string }] }` | `"fix the listed fields in .specforge.json"` |
| `specforge.package.not_selected` | `SpecforgePackageNotSelectedException` | `{ availablePackages: [{ name: string, path: string }] }` | `"call use_package with one of the listed names"` |
| `specforge.package.unknown` | `SpecforgeUnknownPackageException` | `{ requestedName: string, availablePackages: [{ name, path }] }` | `"call use_package with one of the listed names"` |
| `specforge.id.invalid` | `SpecforgeInvalidIdentifierException` | `{ given: string, expectedPatterns: [string] }` | `"correct the identifier to match one of the expected patterns"` |
| `specforge.id.kind_reserved` | `SpecforgeReservedKindException` | `{ given: string, reservedKinds: [string] }` | `"choose a non-reserved kind for extraKinds"` |
| `specforge.id.kind_exhausted` | `SpecforgeKindExhaustedException` | `{ kind: string, package: string }` | `"split the package or otherwise reorganize — 999 IDs of one kind reached"` |
| `specforge.skills.install_failed` | `SpecforgeSkillInstallException` | `{ agent: string, path: string, innerType: string, innerMessage: string }` | `"check filesystem permissions on the target directory"` |
| `specforge.skills.catalog_missing` | `SpecforgeEmbeddedSkillNotFoundException` | `{ resourceName: string }` | `"rebuild specforge — the binary is missing embedded skill resources"` |
| `specforge.lifecycle.delete_forbidden` | `SpecforgeDeleteForbiddenException` (defined in ITEM-007/ITEM-008/ITEM-009) | `{ id: string, currentStatus: string, allowedStates: [string] }` | `"transition the artifact to Withdrawn via set_*_status instead, or supersede with a new artifact"` |
| `specforge.tool.invalid_argument` | argument validation in `Specforge.Mcp` (enum out of range, missing required field surviving JSON Schema, missing `confirm` on delete tools, etc.) | `{ argument: string, given: any, expected: string }` | `"correct the argument value"` |
| `specforge.tool.internal_error` | any unhandled exception bubbling out of Core | `{ correlationId: string }` | `"check the specforge stderr log entry tagged with the correlation id"` |

`specforge.tool.internal_error` deliberately does **not** leak exception type or stack — that information is logged to stderr by `Specforge.Mcp` (per DEC-003) under the same `correlationId` for human diagnosis.

### MVP Tool Catalog

Twenty-one tools across six categories. Each tool below names: purpose, principal inputs, principal outputs, side effects. Full JSON Schemas live in the corresponding ITEM spec.

#### Setup (2)

| Tool | Purpose |
|---|---|
| `init` | Generate a `.specforge.json` at the host-provided cwd (or a specified path). Optionally generates a conforming `spec/` layout. For Codex CLI specifically, writes the project-level `agents/openai.yaml` declaring the `specforge` MCP server as a dependency. Always writes the current `schemaVersion`. Accepts `dryRun`. |
| `install_skills` | DEC-005 contract: extract embedded skills to `~/.claude/skills/` and `~/.agents/skills/`. Args `agent: claude-code\|codex\|all`, `dryRun: bool`. Returns per-agent counts, written paths, overwritten paths, binary version. |

#### Configuration (3)

| Tool | Purpose |
|---|---|
| `list_packages` | Enumerate the active config's packages. Returns `[{ name, path, extraKinds? }]`. Read-only. |
| `use_package` | DEC-002 contract: set the session's active package by `name`. Returns the previous and new active package. No file mutation; updates in-process state only. |
| `info` | DEC-006 OQ resolution: report `{ binaryVersion, supportedSchemaVersionRange: [min, max], activePackage: name?, configPath: path? }`. Read-only diagnostic. Safe to call at any session phase, including before config discovery. |

#### Decisions (4)

| Tool | Purpose |
|---|---|
| `list_decisions` | Enumerate decisions in the active package. Optional `status?` filter. Returns `[{ id, title, status, path }]`. Read-only. |
| `get_decision` | Return one decision by `id` (bare or qualified). Returns `{ id, title, status, date, owner, reviewOwner, supersedes?, body, sections: { Context, Decision, ... }, relatedDecisions, relatedItemSpecs }`. Body is delivered as the raw markdown source plus a parsed section map — agents can use either. |
| `create_decision` | Multi-effect write: allocate the next `DEC-NNN` for the active package, write the decision file from `templates/decision_record.md` with `title` slugged, append `ART-DEC-NNN` row to `ledger/artifacts.md`, append a `Created` event to `ledger/history.md`. Args: `title`, optional `status` (default `Draft`; `Approved` and beyond rejected). Accepts `dryRun`. |
| `set_decision_status` | Multi-effect write: change the decision's `Status:` line, update the corresponding `ART-DEC-NNN` status, append history event, and — when `status=Approved` and `reviewer`+`notes` supplied — append the next `REV-DEC-NNN-NNN` to `ledger/reviews.md`. Validates the transition against the lifecycle from `spec/shared/document_lifecycle.md`. Args: `id`, `status`, optional `reviewer`, optional `notes`. Accepts `dryRun`. |
| `delete_decision` | Multi-effect write: remove the decision file, remove the `ART-DEC-NNN` row from artifacts ledger, cascade-delete any `REV-DEC-NNN-*` rows from reviews ledger, append a `Deleted` history event recording the tombstone. Refused on `Approved` and later statuses. Args: `id`, required `confirm: true`, optional `dryRun`. See "Delete Semantics" below. |

#### Items (4)

Same shape as decisions, mirroring the spec-graph parity decided in Round 3.

| Tool | Purpose |
|---|---|
| `list_items` | Enumerate items in the active package. Optional `status?` filter. Read-only. |
| `get_item` | Return one item by `id`. Returns parsed structure + raw body, like `get_decision`. |
| `create_item` | Multi-effect write: allocate the next `ITEM-NNN`, write the item file from `templates/item_spec.md`, append `ART-ITEM-NNN` to artifacts ledger, append history event. Accepts `dryRun`. |
| `set_item_status` | Multi-effect write: same shape as `set_decision_status`, but for items, and writing to the items ledger (`ledger/items.md`, currently placeholder per DEC-002 — promoted to Draft on first call). |
| `delete_item` | Multi-effect write: same shape as `delete_decision` but for items — removes the item file, the `ART-ITEM-NNN` artifact row, any `REV-ITEM-NNN-*` review rows, and appends a `Deleted` history event. Refused on `Approved` and later. Args: `id`, required `confirm: true`, optional `dryRun`. See "Delete Semantics". |

#### Ledger (3)

Free-form events that aren't tied to a spec-graph operation.

| Tool | Purpose |
|---|---|
| `append_history` | Append a row to `ledger/history.md`. Args: `targetId` (LedgerId of the row's subject), `event` (short label), `detail` (free-form prose). Date stamped from system clock. |
| `append_review` | Append a row to `ledger/reviews.md`. Args: `targetId`, `reviewer`, `outcome` (e.g. `Approved`, `Changes requested`), optional `notes`. The composite `REV-<target>-NNN` is allocated by the tool. |
| `append_commit` | Append a row to `ledger/commits.md` (currently placeholder — promoted to Draft on first call). Args: `gitRef` (short SHA), `summary`. The `CMT-NNN` LedgerId is allocated by the tool. |
| `delete_review` | Remove a single `REV-<target>-NNN` row from `ledger/reviews.md` and append a `Deleted` history event. The per-target seq is tombstoned — the next review on that target advances past the deleted seq. Args: `id` (the composite REV identifier), required `confirm: true`, optional `dryRun`. |
| `delete_commit` | Remove a single `CMT-NNN` row from `ledger/commits.md` and append a `Deleted` history event. The `CMT-NNN` is tombstoned. Args: `id`, required `confirm: true`, optional `dryRun`. |

#### Validation (1)

| Tool | Purpose |
|---|---|
| `validate` | Optional `aspect: ids\|links\|lifecycle\|impact-coverage\|all`, default `all`. Returns `{ issues: [{ severity: error\|warning\|info, location: { file, section? }, code: specforge.validate.<aspect>.<reason>, message, suggestion? }] }`. Scales by adding aspect enum values, not by adding tools. |

### Delete Semantics

The four delete tools — `delete_decision`, `delete_item`, `delete_review`, `delete_commit` — share a uniform contract that is documented once here rather than repeated per tool.

**Allowed states.** A delete is accepted only when the target's status is in the pre-Approved set: `Not started`, `Draft`, `Draft for user review`, or `Placeholder`. Approved, Refined, Superseded, and Withdrawn targets reject the delete with `specforge.lifecycle.delete_forbidden` (the response `data` carries `currentStatus` and `allowedStates`; the `suggestion` directs the caller to `set_*_status` with `Withdrawn`, or to supersession). Reviews and commits have no lifecycle status of their own — their delete is always permitted (the state restriction applies only to status-bearing entities).

**Number tombstoning.** Deleting an artifact does **not** free its number. The next `create_decision` after a `delete_decision(DEC-005)` still allocates `DEC-006`. This preserves DEC-004's "numbers are never reused" invariant. Tombstone evidence lives in the `Deleted` history event — `validate(aspect=ids)` consults history to distinguish a tombstoned gap (legitimate) from a skipped allocation (real error).

**Cascading deletes.**

- `delete_decision(id)` cascades to: all `REV-<id>-*` rows in `ledger/reviews.md` (a review of a deleted target is meaningless).
- `delete_item(id)` cascades to: all `REV-<id>-*` rows in `ledger/reviews.md`.
- `delete_review(id)` does not cascade — a review is a leaf row.
- `delete_commit(id)` does not cascade.

**History event.** Every delete (and every cascaded delete) produces a row in `ledger/history.md` recording the action. The audit trail records what was removed, when, and what numbers are tombstoned. The original artifact's existence is recoverable from git history, not from the live spec graph.

**`confirm: true` requirement.** Each delete tool requires an explicit `confirm: true` argument. Without it, the tool returns `specforge.tool.invalid_argument` with `argument="confirm"`, `expected="true"`, `suggestion="pass confirm=true to perform the delete; pass dryRun=true to preview without writing"`. This raises the threshold for an irreversible operation without resorting to the heavier two-phase `previewToken` pattern.

**`dryRun` supported.** Same as every other write tool. A `dryRun: true` call returns the cascade plan (files that would be removed, rows that would be removed, history event that would be appended) without touching the filesystem.

### Example Schema (`create_decision`)

A representative input schema, illustrating the richness convention from Round 1 Q4:

```jsonc
{
  "name": "create_decision",
  "description": "Create a new decision record in the active package. Allocates the next DEC-NNN, writes the file from the decision template, adds the ART-DEC-NNN artifact row, and appends a Created event to history.md. The decision starts in Draft status and is opened for user review through subsequent set_decision_status calls.",
  "inputSchema": {
    "type": "object",
    "required": ["title"],
    "properties": {
      "title": {
        "type": "string",
        "description": "Plain title; uppercased and hyphenated for the filename slug. Must satisfy DEC-004's slug rules after slugification (max 60 chars after, [A-Z0-9-]).",
        "minLength": 1,
        "maxLength": 120,
        "examples": ["Distribution and Transport", "Schema Versioning Policy"]
      },
      "status": {
        "type": "string",
        "enum": ["Draft", "Draft for user review"],
        "default": "Draft",
        "description": "Initial status. Approved and beyond are rejected — a new decision cannot start as approved."
      },
      "dryRun": {
        "type": "boolean",
        "default": false,
        "description": "When true, return the planned file path, allocated id, ledger rows that would be written, but do not touch the filesystem."
      }
    },
    "additionalProperties": false
  }
}
```

Every Stage 1 ITEM that implements a tool publishes a schema of equivalent richness for its arguments and result.

### What's NOT in MVP

- **`list_skills`** — folded into `install_skills` with `dryRun=true`. The dry-run result already lists every embedded skill and target path; an extra tool would duplicate it.
- **`supersede_decision`** — covered by `set_decision_status(id, status="Superseded")`. The `Supersedes:` link in the *new* decision is part of normal authoring (write the new decision's body) — not a tool concern. A future DEC may add a `link_decisions(superseder, superseded)` helper if usage shows the manual step is error-prone.
- **Delete of Approved+ artifacts** — `delete_*` tools exist in MVP (see "Delete Semantics") but are restricted to pre-Approved states. Approved+ artifacts move to `Withdrawn` via `set_*_status` or are superseded; their existence is preserved in the audit trail.
- **`delete_history_event`** — history rows have a composite `(date, target, event)` key with no stable allocator-issued LedgerId. They are rarely wrong, and when they are, the agent's own file editor handles the edit. A delete tool here would add surface for a marginal case.
- **`delete_artifact_row`** — artifact rows are mirrors of their underlying entities (DEC-004 "mirroring artifacts"). Deleting an artifact row independently of its underlying entity would create a dangling state; deletes always cascade through the owning entity (`delete_decision` → its `ART-DEC-NNN` row). Standalone artifact-row deletes for `ART-<DESCRIPTIVE-SLUG>` rows (work plan, glossary, etc.) are deferred — those artifacts evolve, not get removed.
- **`migrate_config`** — DEC-006 makes migration in-memory + opportunistic; no explicit tool.
- **Generic file CRUD** (`read_file`, `write_file`) — the agent's own file tools handle these; specforge tools are spec-graph-aware.
- **Search** (`search_decisions("foo")`) — defer; `list_decisions` + the agent's text search is enough for MVP. Add when corpus grows.
- **Diff / preview tools beyond `dryRun`** — `dryRun` covers preview; richer diffing is a Stage 2+ concern.
- **Two-phase preview/commit via `previewToken`** — `confirm: true` argument plus `dryRun` already cover the safety budget for delete. The `previewToken` idea remains in Open Questions for future revisit.

### Tool Count Summary

| Category | Tools |
|---|---|
| Setup | 2 (`init`, `install_skills`) |
| Configuration | 3 (`list_packages`, `use_package`, `info`) |
| Decisions | 5 (`list_decisions`, `get_decision`, `create_decision`, `set_decision_status`, `delete_decision`) |
| Items | 5 (`list_items`, `get_item`, `create_item`, `set_item_status`, `delete_item`) |
| Ledger | 5 (`append_history`, `append_review`, `append_commit`, `delete_review`, `delete_commit`) |
| Validation | 1 (`validate`) |
| **Total** | **21** |

## Alternatives Considered

| Alternative | Rejection reason |
|---|---|
| `noun_verb` naming (`decision_create`, `package_use`) | Better prefix-grouping when a host lists tools alphabetically, but verb-first reads more naturally as imperative action and matches DEC-002/DEC-005's working names. |
| `noun.verb` dotted naming (`decision.create`) | MCP namespace is flat; the dot is purely visual. Some hosts sanitize dots. No real grouping benefit. |
| `verbNoun` camelCase | Foreign to MCP convention; would create a stylistic clash with `use_package`/`install_skills` precedent. |
| Per-file primitive tools (`write_decision_file`, `add_artifact_row`, `append_history_row` as separate tools) | Maximum flexibility, but every authoring flow requires multi-call coordination by the AI, and a half-done sequence leaves the spec graph inconsistent. The whole point of these tools is to maintain invariants atomically. |
| Pure file CRUD + thin helpers | Thinnest surface, but pushes format-derivation into every skill/AI session. Defeats the spec-graph-awareness rationale for having tools at all. |
| Unified `tools` with `action` argument (`decisions(action="list"\|"create"\|"approve")`) | Fewer tools but every tool grows internal dispatch + per-action schema variants. Loses MCP's read-safety distinction at the protocol layer. |
| No `dryRun` on writes | Forces every write to commit. Cheap-to-provide preview that DEC-005 already established as a convention. |
| Minimal schemas (types only) | Saves a few hundred context tokens per tool; pays back many times that in invocation correction rounds. False economy. |
| Plain-text error message only | Easiest to author; AI re-parses prose on every error. Defeats the structured result channel MCP provides. |
| Exception-class-name as error code | Leaks `Specforge.Core` internals; every C# rename becomes a public-contract break. |
| Flat snake_case codes (no namespace) | Collision risk with other tools' codes if surfaced in unified error logs; no scanning structure. |
| HTTP-style numeric codes | Opaque; requires lookup table for every code. Fits HTTP, not custom domains. |
| Suggestion embedded in `message` only | AI re-parses prose to extract the action. Top-level field is the same author cost and a clean machine surface. |
| Selection-error: prose-only message | Readable but AI parses names out of the string. Structured `data.availablePackages` adds one short array for measurable ergonomic gain. |
| Selection-error: code only, AI calls `list_packages` | Saves an array in the response; pays a round-trip on every multi-package session. Wrong trade. |
| Verb-named per transition (`approve_decision`, `withdraw_decision`, `supersede_decision`) | Cleaner per-transition arg sets, but inflates the catalog and forces each new lifecycle state to add a tool. Generic `set_decision_status` with optional fields scales by adding enum values. |
| Hybrid status (`set_*` for trivial + verb-named for side-effect transitions) | Creates inconsistency: "which transitions get a named tool?" — judgement call every time the lifecycle changes. |
| Validation: per-aspect tools (`validate_ids`, `validate_links`, ...) | Each tool self-documents but the catalog grows with every new validator, and the AI has to know which to call when. Single `validate` scales by enum values. |
| Validation: no tooling in MVP | Loses the determinism win for the most error-prone class of work (cross-link integrity). |
| Items: read+create only | Status changes by direct file editing — the AI re-derives ledger sync each time. Same coordination cost we avoided for decisions. |
| Items: defer all item tools | Punishes Stage 1, which is the bulk of the work. MVP exists for Stage 1 enablement. |
| Standalone ledger appends: none | History contains many entries that aren't tied to a status change (scope clarifications, mass renames, design forks); without append tools, AI re-derives row format each time. |
| Standalone ledger appends: history only, defer review/commit | Inconsistency cost outweighs saving two tools. |
| `list_skills` as a separate tool | Duplicates `install_skills dryRun=true` output. No information delta. |
| `migrate_config` tool | DEC-006's in-memory + opportunistic write rule makes migration automatic at the moment of next write. An explicit tool would add a surface the user has to remember to call. |
| `search` tool in MVP | Premature; `list_*` + AI text search is enough at current corpus size. |
| No delete tools in MVP | Original draft excluded `delete_*` arguing supersession + lifecycle suffice. User pushback identified real MVP scenarios where this is too restrictive: accidental `create_decision`, typo in `create_item` title, wrong reviewer on `append_review`, wrong SHA in `append_commit`. Manual file editing for these forces the AI to re-derive ledger sync each time — the same coordination cost we avoided by having multi-effect tools. Re-included as `delete_*` for the four entities with stable IDs. |
| Delete: decisions only | Items are the bulk of Stage 1 work (16 planned); accidental `create_item` without an undo path is painful. Reviews and commits are short single-line rows but they accumulate as the project moves and typos there demand the same tooling. |
| Delete: universal `delete(kind, id)` tool | Single tool surface, but per-kind side effects (cascade rules, status restrictions, ART-row coupling) differ enough that internal dispatch would dominate the tool body. Per-entity tools are clearer. |
| Delete: allow Approved+ with `force` flag | Audit trail for an approved decision is supposed to be inviolate. A `force` flag invites accidents and provides an end-run around what `set_*_status` + `Withdrawn` already model. If a truly catastrophic situation arises, the user can delete the row by direct file edit and own the consequences — specforge does not need to make it easy. |
| Delete: reuse numbers (highest-allocated only, or always) | Directly conflicts with DEC-004's "numbers are never reused" invariant. Tombstoning costs nothing and preserves an inviolate cross-reference promise: `DEC-005` is always the *single* `DEC-005` that ever existed in this package's history. |
| Delete: no special confirmation, treat as ordinary write | An ordinary `dryRun: false` write on a delete is irreversible at the spec-graph level. Requiring an additional `confirm: true` argument is the cheapest mechanism that raises the threshold without inventing a stateful preview/commit dance. |
| Delete: two-phase via `previewToken` | Truly safe but adds session state and TTL handling for a single use case. `confirm: true` + `dryRun` deliver the same safety without the complexity. Revisit if `previewToken` becomes useful for other tools. |
| `delete_history_event` in MVP | Composite key, no stable LedgerId; rare need. Defer. |
| `delete_artifact_row` in MVP | Artifact rows mirror their entities; deleting one independently is incoherent. Standalone descriptive artifact rows (`ART-WORK-PLAN` etc.) evolve, they don't get removed. |

## Consequences

- **Tool surface is fixed at 17 for MVP**. Adding a new tool is a DEC amendment to DEC-007 (or a superseding DEC). Adding an enum value to an existing tool (a new `validate` aspect, a new `set_decision_status` `status`) is additive and does not require a DEC.
- **Every Stage 1 ITEM that ships a tool publishes its full JSON Schema** following the example richness above. Schemas live in the implementing item spec.
- **Error envelope is fixed**. Future error types add a row to the error-code catalog by DEC amendment. No tool ships an ad-hoc error format.
- **`Specforge.Mcp` carries argument validation and envelope construction**; `Specforge.Core` raises typed exceptions per DEC-002/003/004/005/006. Mapping table from Core exception to MCP code lives in one place in `Specforge.Mcp`.
- **`init` is the bootstrap for both single-project and external-adoption use cases** (DEC-002 Consequences). It always writes the current `schemaVersion` (DEC-006).
- **Codex `agents/openai.yaml` is owned by `init`** (DEC-005 boundary): generated when `init` runs in a Codex-aware mode (auto-detected from the host or specified by an `--include-codex` flag inside the tool args).
- **Multi-effect operations make the AI's authoring loop one tool call per spec-graph event**. The previously-implicit ledger sync becomes a tool guarantee.
- **`dryRun` is universal across writes**, enabling safe preview from the AI without commit.
- **`info` is the diagnostic anchor** for every "which version supports what?" question (DEC-006 OQ resolved).
- **Per the no-`list_skills` decision**, the canonical "what skills are available?" query is `install_skills(dryRun: true)`.
- **`get_decision` and `get_item` return both raw and parsed forms** — agents pick whichever is appropriate for the prompt size. The raw form is byte-for-byte from disk; the parsed form is a section map.
- **The `Supersedes:` link in a new superseding decision is authored manually** (decision body). Set the old decision's status with `set_decision_status(old_id, status="Superseded")`. A linker tool may follow in a future DEC if manual link errors prove frequent.
- **`delete_*` tools exist for the four entities with stable IDs** (decisions, items, reviews, commits), restricted to pre-Approved states for status-bearing entities. Numbers are tombstoned permanently — DEC-004's "numbers are never reused" invariant is preserved.
- **`validate(aspect=ids)` consults `ledger/history.md` for tombstone evidence** when checking the sequential-numbering rule from DEC-004. A gap whose disappearance is explained by a `Deleted` history event is legitimate; an unexplained gap is a validation error. This implementation detail is mandated here and finalized in ITEM-010.
- **Each delete cascades only one level** — deleting a decision/item removes its dependent reviews but does not chase further. Reviews and commits are leaves; nothing depends on them.

## Impact Assessment

| Aspect | Impact | Notes |
|---|---|---|
| MCP tool surface | Direct | Defined here. |
| Configuration discovery | Indirect | `use_package`, `list_packages`, selection-error payload finalized. |
| ID scheme | Indirect | Tools accept bare and qualified identifiers per DEC-004. |
| Skill packaging | Indirect | `install_skills` envelope finalized; `list_skills` resolved to dry-run subsumption. |
| Schema versioning | Indirect | `info` exposes `supportedSchemaVersionRange`; version-error and validation-error envelopes formally separated. |
| Error handling | Direct | Envelope, code namespace, suggestion convention, and full mapping table all defined here. |
| Stage 1 item specs | Direct | Every Stage 1 ITEM implementing a tool publishes its full JSON Schema against this contract. |
| Documentation | Direct | Top-level README and getting-started must mention `init`, `use_package`, `install_skills`, and `info` as the first-touch quartet. |
| User customization | Indirect | The seventeen tools form a closed set for MVP; extension is by DEC amendment. |
| External adoption | Indirect | `init` shape and `use_package` selection-error payload finalize the contract a target project sees. |
| Lifecycle policy | Indirect | `set_*_status` validates against `spec/shared/document_lifecycle.md`; `delete_*` enforces a pre-Approved-only state restriction; `validate(aspect=ids)` consults history-event tombstones. |
| Maintenance burden | Indirect | Single envelope, single code namespace, generic status transitions — the catalog scales by enum value, not by tool count. |
| Performance | No measurable | All tools are local file operations on a small corpus. |
| Concurrency | No measurable | Single-session per process (DEC-001); no cross-tool contention. |
| Test coverage scope | Direct | Each tool needs unit tests (Core service) plus an in-proc Mcp test asserting the envelope shape for both success and error paths. |

## Source Links

| Source | Locator | Evidence |
|---|---|---|
| `DEC-002-CONFIGURATION-AND-DISCOVERY` | section "Package Selection", "Open Questions" | `use_package` working name; selection-error payload deferred to DEC-007. |
| `DEC-003-RUNTIME-AND-ARCHITECTURE` | sections "Solution and Project Layout", "Error Handling" | Tools live in `Specforge.Mcp`, delegate to `Specforge.Core`; Core throws typed exceptions, Mcp envelope-maps them. |
| `DEC-004-ID-SCHEME-CUSTOMIZATION` | section "Cross-Package Referencing", "Error Types" | Bare and qualified identifier rule; new exception types whose codes are catalogued here. |
| `DEC-005-SKILL-INSTALLATION-MODEL` | sections "Install Tool Contract", "Open Questions" | `install_skills` argument/result contract; `list_skills` resolution. |
| `DEC-006-SCHEMA-VERSIONING` | sections "Error Types", "Open Questions" | `SpecforgeSchemaVersionException` vs `SpecforgeConfigValidationException` envelope separation; `supportedMax`-exposing tool. |
| Conversation 2026-05-28, three structured-question rounds | AskUserQuestion answers (12 questions, all recommended) | Surface principles, error envelope, tool catalog choices. |
| Conversation 2026-05-28, fourth structured-question round (user pushback on no-delete) | AskUserQuestion answers (4 questions, all recommended) | Delete scope = decisions + items + reviews + commits; pre-Approved states only; tombstone forever; required `confirm: true`. |

## Related Decisions

- `DEC-001-DISTRIBUTION-AND-TRANSPORT` (approved): tools run inside the MCP server distributed per DEC-001.
- `DEC-002-CONFIGURATION-AND-DISCOVERY` (approved): config tools (`use_package`, `list_packages`) realize DEC-002 semantics; selection-error payload finalized here.
- `DEC-003-RUNTIME-AND-ARCHITECTURE` (approved): Core/Mcp split this decision lives across.
- `DEC-004-ID-SCHEME-CUSTOMIZATION` (approved): identifier acceptance rule for tool args.
- `DEC-005-SKILL-INSTALLATION-MODEL` (approved): `install_skills` contract finalized here; `init` writes Codex `agents/openai.yaml` per the boundary set there.
- `DEC-006-SCHEMA-VERSIONING` (approved): `info` exposes `supportedSchemaVersionRange`; version vs validation envelope distinction implemented here.
- `DEC-008-INSTRUCTION-LAYER-DESIGN` (planned): skill content references the seventeen tools by name; the catalog above is the authoritative list skills may name.

## Related Item Specs

(IDs use the 3-digit form mandated by DEC-004.)

- `ITEM-001-SOLUTION-BOOTSTRAP` (planned): scaffolds the solution and the Mcp host that registers the seventeen tools.
- `ITEM-002-CONFIG-MODEL` (planned): defines `SpecforgeConfigValidationException`; implements `list_packages`, `use_package`, `info`.
- `ITEM-003-ID-VALIDATOR` (planned): used by every tool with an identifier arg.
- `ITEM-004-EMBEDDED-SKILL-CATALOG` (planned): backs `install_skills`.
- `ITEM-005-SKILL-INSTALLER` (planned): implements `install_skills`.
- `ITEM-006-INIT-TOOL` (planned): implements `init`, including the Codex `agents/openai.yaml` mode.
- `ITEM-007-DECISION-TOOLS` (planned): implements `list_decisions`, `get_decision`, `create_decision`, `set_decision_status`, `delete_decision` (including the pre-Approved state check and the REV-cascade); defines `SpecforgeDeleteForbiddenException`.
- `ITEM-008-ITEM-TOOLS` (planned): implements `list_items`, `get_item`, `create_item`, `set_item_status`, `delete_item` (including the pre-Approved state check and the REV-cascade).
- `ITEM-009-LEDGER-APPEND-TOOLS` (planned): implements `append_history`, `append_review`, `append_commit`, `delete_review`, `delete_commit`.
- `ITEM-010-VALIDATE-TOOL` (planned): implements `validate` with the four MVP aspects (`ids`, `links`, `lifecycle`, `impact-coverage`); `validate(aspect=ids)` consults history-event tombstones when checking the sequential-numbering rule.
- `ITEM-011-ERROR-ENVELOPE` (planned): centralizes the Core-exception → MCP-code mapping in `Specforge.Mcp`, including the `specforge.lifecycle.delete_forbidden` code.

## Related Tests / Validation

- For every tool: input-schema-violation tests assert `specforge.tool.invalid_argument` with the offending argument name in `data.argument`.
- For every tool that requires an active package: missing-package tests assert `specforge.package.not_selected` with a populated `data.availablePackages`.
- For tools accepting identifiers: invalid-identifier tests assert `specforge.id.invalid` with `data.expectedPatterns`.
- `init` smoke test: writes `.specforge.json` with `schemaVersion: <current>`, optionally writes `agents/openai.yaml`, exits with non-error result.
- `install_skills(dryRun: true)` returns the embedded skill catalog without filesystem writes; immediately followed by `install_skills(dryRun: false)` produces the exact paths reported by the dry run.
- `create_decision`/`create_item` smoke tests: file + ART row + history event all present after success; none of them present after a `dryRun: true`.
- `set_decision_status(... status="Approved", reviewer=..., notes=...)`: status updated, ART updated, new REV row appended, history event recorded; `set_decision_status` without `reviewer` on an approval transition is accepted (no review row written) — review can be appended later with `append_review`.
- `validate(aspect: "ids")` against a deliberately broken identifier in a test fixture surfaces an `issues[]` entry with `code` matching `specforge.validate.ids.<reason>`.
- `validate(aspect: "ids")` against a fixture with a tombstoned gap (missing `DEC-003` plus a `Deleted` history event referencing it) reports no issue — the gap is legitimate.
- `validate(aspect: "ids")` against a fixture with a tombstoned gap but no history event reports an unexplained-gap issue.
- `info` returns sane values before and after `use_package`.
- Error envelope shape test: every documented code produces the exact `{code, message, suggestion?, data?}` shape; no tool ships a plain-text error.
- `delete_decision` against a Draft target with `confirm: true`: file removed, `ART-DEC-NNN` row removed, any `REV-DEC-NNN-*` rows removed, `Deleted` history event appended. Subsequent `create_decision` allocates the next number (not the deleted one).
- `delete_decision` against an Approved target returns `specforge.lifecycle.delete_forbidden` with `data.currentStatus="Approved"` and `data.allowedStates=["Not started","Draft","Draft for user review","Placeholder"]`.
- `delete_decision` without `confirm: true` returns `specforge.tool.invalid_argument` with `argument="confirm"`.
- `delete_decision(... dryRun: true, confirm: true)` returns the planned cascade (files/rows that would be removed, history event that would be appended) without filesystem changes.
- `delete_review`/`delete_commit` on existing rows remove them and tombstone their identifiers — the next allocator call advances past the deleted seq.

## Open Questions

- Exact slug-collision policy in `create_decision` and `create_item` when two titles slug to the same string in the same package (extremely rare given title diversity, but possible) — leaning: reject with `specforge.tool.invalid_argument` and `suggestion="amend the title to disambiguate"`. Finalize in ITEM-007/ITEM-008. Not blocking.
- Whether `dryRun` results carry a stable serialization (so a follow-up real call can be matched to a previous preview) — the `previewToken` idea. Could replace the `confirm: true` pattern on deletes if it ever lands. Defer to ITEM-011. Not blocking.
- Whether `validate` should expose a `fix` mode that auto-corrects safe issues (e.g., a stale status line in an ART row) — defer; today every validation surfaces issues only, the AI applies fixes via `set_*_status` or direct edits.
- Whether to introduce a `link_decisions(superseder, superseded)` helper if manual `Supersedes:` link errors become frequent — defer, watch usage.
- Future `search_decisions(query)` tool — defer; revisit when corpus growth makes `list_*` plus AI text search inadequate.
- Whether a `delete_history_event` tool becomes warranted (when wrong history rows are appended often enough that direct file edit gets annoying) — defer, watch usage.
- Tombstone bookkeeping: in MVP `validate(aspect=ids)` scans `history.md` linearly to find `Deleted` events. If the history grows large enough to make this expensive, ITEM-010 may introduce a per-package tombstone index. Not a public contract; an implementation optimization.

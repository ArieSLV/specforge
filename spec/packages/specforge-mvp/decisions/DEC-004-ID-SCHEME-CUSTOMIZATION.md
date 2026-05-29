# DEC-004-ID-SCHEME-CUSTOMIZATION - ID Scheme and Customization

Status: Approved
Date: 2026-05-28
Owner: AI assistant (drafted)
Review owner: User (approved 2026-05-28)
Covers: identifier format and stability, kind taxonomy, number width, per-package customization, composite review and commit IDs, title-slug rules, cross-package referencing
Supersedes: none
Amends: `DEC-002-CONFIGURATION-AND-DISCOVERY` (extends config schema v1 with an optional `extraKinds` field per package — backward-compatible)

## Context

The spec graph already uses identifiers informally — `DEC-001`, `ITEM-02`, `ART-DEC-001`, `REV-DEC-001-001`. They were introduced ad hoc as artifacts were created; the format is partly consistent (3-digit DEC, 2-digit ITEM) and the customization story is undefined.

Three pressures make a formal scheme necessary now, before Stage 1 items start landing:

1. **Stage 1 will explode the population.** Sixteen items are planned, plus their per-item artifact rows, plus future commit and review events. Inconsistent widths and shapes compound across hundreds of identifiers.
2. **External adoption** (DEC-002 made this an explicit goal). RavenDB-26295 already uses kinds outside the core set (`RDM-001`, `BSP-001`, `SE-001`, `EH-001`). The scheme has to admit them without forcing a rename in the target repository.
3. **Tooling boundary** (DEC-003 placed ID validation in `Specforge.Core`). The validator needs a concrete regex and a precise customization surface, not a free-form convention.

On 2026-05-28 the user chose, via structured questions:

- number width: **uniform 3-digit** across every kind;
- customization: **additive only** (core kinds fixed; packages may declare extras);
- review IDs: **composite** `REV-<target>-<seq>`;
- commit IDs: **sequential `CMT-NNN`** with a separate git-ref column.

This record formalizes those choices and fills in the mechanics that do not require adjudication (regex, slug rules, immutability, cross-package referencing, schema amendment to DEC-002).

## Decision

### Identifier Form

The atomic identifier of a spec entity is:

```text
<KIND>-<NUMBER>
```

- `<KIND>` is 2–6 uppercase ASCII letters from the core kind set, or from the package's declared extra kinds.
- `<NUMBER>` is exactly **3 digits**, zero-padded, in the range `001`..`999`.
- The identifier is the unit of cross-reference: documents, ledger rows, and source links cite the bare identifier (`DEC-001`).

Regex (canonical):

```text
^[A-Z]{2,6}-[0-9]{3}$
```

Composite identifiers (review events, currently the only composite kind):

```text
REV-<TARGET-KIND>-<TARGET-NUMBER>-<SEQ>
```

- `<TARGET-KIND>-<TARGET-NUMBER>` is the identifier of the artifact being reviewed.
- `<SEQ>` is a 3-digit per-target counter starting at `001` and advancing monotonically for every review event on that target.
- Composite regex: `^REV-[A-Z]{2,6}-[0-9]{3}-[0-9]{3}$`.

### Core Kinds

The following kinds are reserved across every package. Their semantics are fixed; their counters are per-package per-kind.

| Kind | Identifier shape | Counter scope | Purpose |
|---|---|---|---|
| `DEC` | `DEC-NNN` | per-package | Decision record. |
| `ITEM` | `ITEM-NNN` | per-package | Item spec (Stage 1 unit of work). |
| `ART` | `ART-NNN` _or_ `ART-<KIND>-NNN` | per-package | Artifact ledger row. See "Artifact ID Form" below. |
| `REV` | `REV-<target>-NNN` | per-target | Review event. |
| `CMT` | `CMT-NNN` | per-package | Commit ledger row, paired with a `git-ref` column carrying the short SHA. |

History ledger rows are intentionally *not* a kind: a history row is keyed by `(date, target LedgerId, event)` and carries no identifier of its own. The composite key is enough; adding `HIST-NNN` would inflate the schema without callers ever referencing those rows by ID.

### Artifact ID Form

Artifacts come in two flavors, and the LedgerId reflects which:

- **Mirroring artifacts** — the artifact *is* a primary entity, and its ID mirrors that entity's identifier:
  - `ART-DEC-001` (the file storing `DEC-001`),
  - `ART-ITEM-005` (the file storing `ITEM-005`).
  Such IDs match `^ART-[A-Z]{2,6}-[0-9]{3}$`. They are not a separate counter — they reuse the underlying entity's number.
- **Standalone artifacts** — the artifact is not a primary entity (top-level README, work plan, shared docs, templates). It gets a descriptive ID, conventionally `ART-<DESCRIPTIVE-SLUG>`:
  - `ART-WORK-PLAN`, `ART-LIFECYCLE`, `ART-GLOSSARY`, `ART-TEMPLATE-DECISION`.
  Such IDs match `^ART-[A-Z][A-Z0-9-]*[A-Z0-9]$` and do not consume a number from any counter.

Validators must accept both shapes for `ART-`. Tooling that lists "all artifacts" walks the artifacts ledger directly — it never needs to enumerate by counter.

### Per-Package Customization (Additive)

A package may declare additional kinds via an optional `extraKinds` array in its config entry. Core kinds remain fixed.

Config amendment (extends DEC-002's schema v1 with an optional, backward-compatible field):

```json
{
  "schemaVersion": 1,
  "shared": "spec/shared",
  "templates": "spec/templates",
  "packages": [
    { "name": "specforge-mvp", "path": "spec/packages/specforge-mvp" },
    {
      "name": "ravendb-26295",
      "path": "artifacts/RavenDB-26295/specification",
      "extraKinds": ["RDM", "BSP", "SE", "EH"]
    }
  ]
}
```

Rules for `extraKinds`:

- Each entry is 2–6 uppercase ASCII letters, regex `^[A-Z]{2,6}$`.
- An entry **must not** equal any core kind (`DEC`, `ITEM`, `ART`, `REV`, `CMT`). Validation error otherwise: `SpecforgeReservedKindException`.
- No duplicates within the array.
- Entries are **not required** to be unique across packages. Different packages may both declare `RDM` — each carries its own counter, and cross-package references disambiguate via the package name (see "Cross-Package Referencing").
- A kind declared in `extraKinds` participates in the standard `<KIND>-NNN` shape, per-package counter, and 3-digit width — it gets no extra capabilities.
- A package may **not** rename core kinds. If a target repository uses `ADR` instead of `DEC`, the migration is a one-time rename of files in that repo, not a renaming feature in specforge.

### File-Naming Convention

Files for `DEC` and `ITEM` use the identifier plus a title slug:

```text
<KIND>-<NUMBER>-<TITLE-SLUG>.md
```

- `<TITLE-SLUG>` is the human title, uppercased, words joined with single hyphens.
- Title slug regex: `^[A-Z][A-Z0-9-]*[A-Z0-9]$` (must start and end with `A-Z` or `0-9`; no leading/trailing/consecutive hyphens).
- Maximum length: **60 characters**.
- Examples: `DEC-001-DISTRIBUTION-AND-TRANSPORT.md`, `ITEM-005-CONFIG-DISCOVERY-WALKUP.md`.

Files for other kinds do not use this pattern — they live as rows in ledger files, not as standalone documents.

### Identifier Stability

- Once an identifier is **assigned to a non-`Not started` artifact**, it is immutable. A `DEC-002` stays `DEC-002` forever, even if the decision is later superseded.
- A withdrawn or superseded entry **does not free its number**. `DEC-002` cannot be reused for a new, unrelated decision; the next decision after `DEC-008` is always `DEC-009`.
- A `Not started` placeholder may be renumbered before its status advances (e.g., dependency reordering during planning). After it moves to `Draft` or beyond, the number is frozen.
- The title slug in a filename is **soft**: it tracks the current human title and may be corrected for typos or wording at any time without supersession. Substantive content changes after `Approved` go through supersession, not renaming.

### Counter Advancement

- Counters advance monotonically, starting at `001` per package per kind.
- No gaps for new entries: `DEC-001` → `DEC-002` → `DEC-003`. Skipping numbers is a validation error in the ledger.
- Supersession consumes a number: if `DEC-009` supersedes `DEC-005`, both `DEC-005` and `DEC-009` are taken; `DEC-005`'s status moves to `Superseded`, not `Withdrawn`.
- The current next-number for each kind is recoverable by scanning the artifacts ledger; it is not stored in config.

### Cross-Package Referencing

- Within a single package, identifiers are bare: `DEC-001`, `ITEM-005`.
- Across packages, identifiers are qualified with the package name: `specforge-mvp/DEC-001`, `ravendb-26295/RDM-014`.
- The package name is the `name` field of the entry in `.specforge.json`, not the path.
- A reference is "in" a package when it lives inside that package's directory tree. Source links in shared documents (`spec/shared/*`) use qualified references when they need to point into a package; bare references in shared documents are an error.

### Reserved Kinds and Validation

The validator enforces:

- Core-kind list: `{DEC, ITEM, ART, REV, CMT}`. These cannot appear in any package's `extraKinds`.
- Numeric range: `001`..`999`. Hitting `999` for any kind in any package triggers a "kind exhausted" error and forces a package split — not a width bump.
- Slug character set: `[A-Z0-9-]` only, with the boundary rules above.
- Composite REV form: target identifier must exist; seq must be the next free per-target seq.

## Alternatives Considered

| Alternative | Rejection reason |
|---|---|
| Uniform 2-digit numbers (`DEC-01`, `ITEM-01`) | Caps each kind at 99 per package; would force a width migration mid-project. Existing `DEC-001..DEC-003` would need renumbering — small but real cost for no upside. |
| Per-kind ad hoc widths (`DEC-001`, `ITEM-01`) | Matches current informal state but bakes inconsistency into validation, ledger sorting, and visual scanning forever. No benefit. |
| No per-package customization | Blocks adoption of layouts that already use kinds like `RDM`, `BSP`, `SE` — would force a wholesale rename in the target repo. Contradicts DEC-002's portability stance. |
| Full per-package customization (rename core kinds) | Maximum flexibility but every package diverges. Skills and instructions (DEC-008) lose the ability to say "the DEC" — they'd have to consult per-package config. Cost outweighs benefit. |
| Pure sequential `REV-NNN` with a Target column | Uniform with DEC/ITEM scheme, but loses the at-a-glance "which artifact is this review about?" benefit. The reviews ledger is read by target far more often than by sequence. |
| Both REV shapes available | Two ID styles for one kind — tooling has to handle both, choice becomes a style debate per package. Composite is strictly more readable; pick one. |
| Git short SHA as commit LedgerId | Zero indirection but short SHAs can collide, and a `git reset`/`rebase` invalidates the ID, breaking every cross-reference. Identity must survive history rewrites. |
| Date+seq commit IDs (`CMT-2026-05-28-001`) | Verbose; sorts well but adds nothing over plain sequential when paired with a date column. Useful only if commits are very rare. |
| Per-kind globally unique number space (`DEC-001` unique across all packages) | Makes cross-package refs slightly shorter but turns the counter into a shared resource that all packages have to coordinate on. Per-package counters are independent and trivial to manage. |
| Storing the next-number per kind in config | Persists state that is trivially recoverable by scanning the ledger. Adds drift risk (config diverges from filesystem). Recompute on demand instead. |
| Giving history rows their own `HIST-NNN` IDs | History rows are never cross-referenced by ID; the composite `(date, target, event)` key suffices. Adding an ID inflates the schema for no caller. |

## Consequences

- **One-time rename**: 28 occurrences of `ITEM-NN` across 6 files (`work_plan.md`, both ledger files, three DEC files) are rewritten to `ITEM-NNN` as part of this decision's approval propagation. No items have been drafted yet, so no file renames are needed.
- **Config schema amendment**: `.specforge.json` schema v1 now formally includes an optional `extraKinds` array per package entry. Backward-compatible — configs without the field continue to validate. DEC-006 (planned) will catalog all v1 fields in one place.
- **Validation surface in `Specforge.Core`**: the ID validator implements the regexes above plus the slug rules, the per-target REV seq rule, the reserved-kind check, and the kind-exhausted check. Lives next to the config parser from DEC-002.
- **External adoption playbook**: a repo like RavenDB-26295 adopts specforge by (a) writing a `.specforge.json` with its `specification/` directory as the package path, and (b) declaring `extraKinds: ["RDM", "BSP", "SE", "EH"]`. No file renames in the target repo.
- **Cross-package refs are qualified**: every multi-package operation in DEC-007's tool surface must accept both bare (within active package) and qualified (`package/ID`) references. The qualifier resolution rule is fixed here.
- **No HIST kind**: history rows stay composite-keyed; the artifacts ledger remains the authoritative target index.
- **Number exhaustion is a hard error, not a soft warning**: hitting `999` of any kind in any package forces a package split. This is intentional — a package with 999 decisions has structural problems that a 4-digit width would mask.
- **REV seq is per-target**: tooling that creates a review event must scan all existing REVs for that target to find the next free seq. Cheap (reviews ledger is small).
- **Title slug edits are free during the document's life**: only the identifier number is immutable. This matches the spec-evolution reality where titles get sharpened during review.

## Impact Assessment

| Aspect | Impact | Notes |
|---|---|---|
| ID scheme | Direct | This decision defines it. |
| Configuration schema | Direct | Adds optional `extraKinds` to `.specforge.json` v1. |
| Per-package grouping | Direct | Counter scope locked to per-package per-kind. |
| External adoption | Direct | Additive customization is the explicit adoption lever. |
| MCP tool surface | Indirect | DEC-007 tools accept bare and qualified references per rules above. |
| Spec graph linking | Direct | Cross-package qualifier rule defined here. |
| Validation logic | Direct | Regex set, reserved-kind check, kind-exhaustion check all sourced here. |
| Schema versioning | Indirect | DEC-006 will inventory v1 fields; `extraKinds` is part of v1. |
| Ledger structure | Direct | Composite REV form and CMT + git-ref column locked. |
| Documentation | Direct | Decisions/README, items/README, and ledger/README must reflect the rules. |
| Tooling for renaming | Indirect | A one-time `ITEM-NN → ITEM-NNN` rewrite is executed at approval propagation; future renames go through supersession. |
| Performance | No measurable | Validation is regex + small set lookups. |
| Concurrency | No measurable | Counters are recomputed per session from the ledger. |
| Error handling | Direct | New typed exceptions: `SpecforgeReservedKindException`, `SpecforgeKindExhaustedException`, `SpecforgeInvalidIdentifierException`. Envelope mapping in DEC-007. |
| Target project portability | Direct | RavenDB-26295 adopts via `extraKinds`, no file renames. |
| Future ID kinds | Indirect | Adding a new core kind is itself a DEC; declaring per-package extras is a config change. |

## Source Links

| Source | Locator | Evidence |
|---|---|---|
| `DEC-002-CONFIGURATION-AND-DISCOVERY` | section "Configuration Schema (version 1)" | Existing v1 schema this decision amends with optional `extraKinds`. |
| `DEC-002-CONFIGURATION-AND-DISCOVERY` | section "Alternatives Considered" — "Glob `packages/*`..." | Portability of existing non-conforming layouts is the design driver for additive customization. |
| `DEC-003-RUNTIME-AND-ARCHITECTURE` | section "Error Handling" | New typed exceptions defined here follow the same Core-throws / Mcp-envelopes pattern. |
| `artifacts/RavenDB-26295/specification/` | existing identifier population | Example of `RDM`, `BSP`, `SE`, `EH` extra kinds that additive customization must admit. |
| Conversation 2026-05-28, structured questions | AskUserQuestion answers | Uniform 3-digit; additive customization; composite REV; sequential CMT + git-ref column. |

## Related Decisions

- `DEC-001-DISTRIBUTION-AND-TRANSPORT` (approved): no direct interaction.
- `DEC-002-CONFIGURATION-AND-DISCOVERY` (approved): amended by adding optional `extraKinds` field.
- `DEC-003-RUNTIME-AND-ARCHITECTURE` (approved): validation logic lives in `Specforge.Core`; new exception types follow the established pattern.
- `DEC-005-SKILL-INSTALLATION-MODEL` (planned): skill packaging treats identifiers as opaque strings — no skill-side changes.
- `DEC-006-SCHEMA-VERSIONING` (planned): catalogs v1 schema fields including `extraKinds`; defines the policy for further additive vs breaking changes.
- `DEC-007-MVP-TOOL-SET` (planned): every tool that accepts an identifier parameter follows the bare/qualified rule defined here; error envelope covers the new exception types.
- `DEC-008-INSTRUCTION-LAYER-DESIGN` (planned): instructions reference identifiers in their canonical 3-digit form.

## Related Item Specs

- `ITEM-003-ID-VALIDATOR` (planned): implements the regex set, reserved-kind check, kind-exhaustion check, slug rules, and composite REV/qualified-reference parsing inside `Specforge.Core`.
- `ITEM-002-CONFIG-MODEL` (planned): parses the optional `extraKinds` array per package entry.

## Related Tests / Validation

- Identifier regex accepts every valid form (DEC-001, ITEM-001, ART-DEC-001, ART-WORK-PLAN, REV-DEC-001-001, CMT-001) and rejects every invalid form (DEC-1, DEC-01, dec-001, DEC-0001, DEC-001-, --DEC-001).
- Reserved-kind check rejects `extraKinds: ["DEC"]` with `SpecforgeReservedKindException`.
- Per-target REV seq advances monotonically across reviews on the same target; the first review on a new target starts at 001.
- A composite REV form pointing at a non-existent target identifier fails validation.
- Bare reference inside a `spec/shared/*` document fails validation; qualified reference passes.
- Two packages each declaring `extraKinds: ["RDM"]` both validate; their RDM-001 entries are independent.
- One-time rename test: after the approval propagation, no `ITEM-\d{2}\b` matches remain in `spec/`.

## Open Questions

- Whether `extraKinds` should also support per-kind human-readable descriptions in config (e.g. `{"kind": "RDM", "description": "Requirements Data Model"}`) — leaning no for MVP (the package's own README can describe its extras). Not blocking; revisit if external adopters ask.
- Whether non-ASCII slugs (Cyrillic, accented Latin) should be permitted in title slugs — leaning no; titles are technical labels not prose. Not blocking; can relax later without breakage.

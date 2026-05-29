# DEC-006-SCHEMA-VERSIONING - Schema Versioning Policy

Status: Approved
Date: 2026-05-28
Owner: AI assistant (drafted)
Review owner: User (approved 2026-05-28)
Covers: versioning style, what carries a `schemaVersion`, additive-vs-breaking definitions, schema v1 inventory, unsupported-version behavior, migration mechanism, bump procedure
Supersedes: none

## Context

`.specforge.json` already carries `schemaVersion: 1` (introduced in `DEC-002-CONFIGURATION-AND-DISCOVERY`). `DEC-004-ID-SCHEME-CUSTOMIZATION` then amended schema v1 with an optional `extraKinds` array per package, declaring the change backward-compatible — but the policy that lets us call a change "backward-compatible" has not been written down anywhere.

Three pressures make a policy necessary now, before Stage 1 items implement the config parser:

1. **DEC-002 and DEC-004 already touched the schema, and DEC-007 will touch it again** when `init` writes the file. Without a policy, every author has to re-derive what counts as a bump.
2. **DEC-003 placed config parsing in `Specforge.Core` and named `SpecforgeSchemaVersionException` as a placeholder type to be defined here.** The implementation needs concrete semantics for the version mismatch case.
3. **External adopters (RavenDB-26295 and beyond) need a forward-compatibility guarantee.** A user adopting specforge today writes a v1 config. They need to know what happens when they upgrade specforge in 2027.

This decision establishes the policy. It does **not** introduce a new schema version — v1 is still the current version. It documents the rules under which v2 (if/when it appears) will be introduced and read.

On 2026-05-28 the user chose, via structured questions:

- versioning style: **plain integer**;
- scope: **`.specforge.json` only** (with a forward-looking rule for any future externally-parsed format);
- unsupported-version behavior: **hard refuse with explicit error and suggested action**;
- migration: **in-memory upgrade, opportunistic write**.

This record formalizes those choices, defines additive-vs-breaking precisely, catalogs the current v1 fields, and specifies the bump procedure.

## Decision

### Versioning Style

A `schemaVersion` is a **non-negative integer** — exactly the type and shape DEC-002 already used.

- The integer increments by 1 on every breaking change. There is no skipping.
- Additive changes do not bump the version.
- The version field is named `schemaVersion` (camelCase, consistent with the rest of the config).
- No semver, no date strings, no compound version objects. Integer comparison is the entire version-comparison protocol.

### What Carries a `schemaVersion`

**Rule**: any format that specforge **parses externally** carries a `schemaVersion`. Everything else does not.

| Artifact | Carries `schemaVersion`? | Reason |
|---|---|---|
| `.specforge.json` | **Yes** | Parsed by every specforge invocation; written by users (via `init`) and tools. External contract. |
| `SKILL.md` (frontmatter) | No | Specforge writes every `SKILL.md` fresh on each `install_skills` (DEC-005). Skill version equals binary version. Adding a `specforgeSchemaVersion` key would pollute a frontmatter contract specforge does not own. |
| Ledger files (`artifacts.md`, `items.md`, etc.) | No | Read-mostly by humans in MVP; row shapes evolve by DEC, not by version. No tool parses them in MVP. |
| Document templates (`decision_record.md`, `item_spec.md`) | No | Templates are author guidance, not machine schemas. Evolution is recorded in DECs that change the contracts. |
| ID format (DEC-004) | No | Identifier shape is locked by DEC-004 itself; tooling that changes the shape would be a new DEC, not a version of the existing scheme. |

Forward-looking rule (binding on future DECs):

> When a future decision introduces a new format that specforge **parses externally** (config-like, lock-file-like, etc.), that format carries its own `schemaVersion` independent of `.specforge.json`'s.

This generalizes the choice without committing to a versioned ledger or templates today.

### Additive vs Breaking

| Change | Verdict |
|---|---|
| Add a new **optional** field with a documented default | Additive — no bump. |
| Add a new enum value that older binaries can safely ignore | Additive — no bump. |
| Relax a constraint (broaden a range, loosen a regex) | Additive — no bump. |
| Add documentation, comments, or examples | Additive — no bump (no schema impact at all). |
| Remove a field | **Breaking** — bump. |
| Rename a field | **Breaking** — bump. |
| Change a field's JSON type (string → number, object → array) | **Breaking** — bump. |
| Change a field's semantics while keeping its name and type | **Breaking** — bump. |
| Tighten a constraint (narrow a range, stricter regex) | **Breaking** — bump. |
| Add a new **required** field with no migration default | **Breaking** — bump. |
| Change a field's default value | **Breaking** — bump. |

A change is additive **only if** every binary that supports the previous version can read a file authored under the new rules without misinterpreting any value. Doubt resolves to breaking: when in doubt, bump.

### Schema v1 Inventory

This section is the authoritative catalog of `schemaVersion: 1`. Every additive change to v1 amends this section. Future v2 introduces its own inventory section.

| Field | Type | Required | Introduced | Notes |
|---|---|---|---|---|
| `schemaVersion` | integer | yes | DEC-002 | Always `1` in v1. |
| `shared` | string | yes | DEC-002 | Repo-level shared directory path, relative to the config file. |
| `templates` | string | yes | DEC-002 | Repo-level templates directory path, relative to the config file. |
| `packages` | array of objects | yes | DEC-002 | Explicit package list; never globbed. |
| `packages[].name` | string | yes | DEC-002 | Package identifier; used in cross-package references per DEC-004. |
| `packages[].path` | string | yes | DEC-002 | Package directory path, relative to the config file. |
| `packages[].extraKinds` | array of strings | no | DEC-004 (additive amendment) | Per-package additional ID kinds; rules in DEC-004. |

Subsequent additive amendments to v1 (e.g., a future optional `tracing` field) are appended to this table with their introducing DEC noted.

### Unsupported-Version Behavior

Let `binary.supportedMax = N` be the highest schema version this binary knows how to read. Let `file.schemaVersion = M` be the version declared in the config.

| Situation | Behavior |
|---|---|
| `M ∈ [1..N]` | Read normally. If `M < N`, the in-memory representation is upgraded (see Migration below). |
| `M > N` | **Hard refuse**: throw `SpecforgeSchemaVersionException` carrying `claimedVersion=M`, `supportedRange=[1..N]`, `suggestion="upgrade specforge to a version that supports schemaVersion=M or lower"`, and the config file path. |
| `M < 1`, `M` non-integer, or `schemaVersion` missing | **Hard refuse**: throw `SpecforgeSchemaVersionException` with `suggestion="invalid schemaVersion; expected positive integer in [1..N]"`. |

specforge never silently degrades. There is no best-effort mode, no warn-and-continue, no field-dropping. Spec data is too important to corrupt through ambiguity.

### Migration: In-Memory Upgrade + Opportunistic Write

When `binary.supportedMax = N` and `file.schemaVersion = M` with `M < N`:

1. specforge reads the v_M file.
2. specforge populates defaults for every field added in v_{M+1}, v_{M+2}, …, v_N (defaults come from the migration registry; see "Bump Procedure" below).
3. The in-memory representation is always **v_N** — the rest of the runtime sees a fully upgraded config.
4. specforge does **not** rewrite the file on read. Read-only sessions leave the v_M file on disk untouched.
5. The **next tool that mutates the config** (e.g., `init`, future `add_package`, future config-editing tools) serializes the upgraded representation as v_N. This is "opportunistic write": the upgrade is realized on disk exactly when the user is already producing a config change, so there is no surprise git diff from a read-only session.

This avoids two failure modes:

- **Eager rewrite on read** would create unexpected file changes after a `list_packages` call. The user opens git status and sees `.specforge.json` modified for no apparent reason.
- **Manual migration** (no automatic upgrade) would force the user to edit a file by hand for a transformation specforge knows how to do deterministically — the opposite of what the tool is for.

When the binary supports an older `supportedMax = N` than the file's `M > N` declared version, no upgrade happens — the hard-refuse rule above applies and execution stops before any state is loaded.

### Bump Procedure

When a future DEC needs to introduce v2 (or any future v_K):

1. The DEC's `Decision` section names the new fields, removed fields, renamed fields, and changed semantics, with rationale.
2. The DEC defines a **migration function** v_{K-1} → v_K: for every field added with no in-file value, what default to populate; for every field removed, what to do with the value (typically: discard); for every rename, the source field; for every semantic change, the transformation.
3. `Specforge.Core` maintains a chain of migration functions v_1 → v_2 → … → v_K. Reading any v_M produces v_K via the chain.
4. The new DEC adds a Schema v_K Inventory section (modeled on the v1 inventory above) and links to the v_{K-1} inventory for diff context.
5. The DEC enumerates breaking-change rationales — every bump should be a considered last resort.

A single DEC may not introduce more than one schema bump. If a DEC's design naturally requires two bumps, split it.

### Read-Back Compatibility

For MVP: **every binary version reads every prior schema version**, all the way back to v1. The migration chain grows by one entry per bump.

This is cheap because schema bumps are rare and the schema is small. When the chain becomes burdensome (say, >5 versions) a future DEC will define a deprecation policy. Until then, no version is dropped.

### Error Types

New typed exception in `Specforge.Core` (placeholder named in DEC-003; now fully defined):

- `SpecforgeSchemaVersionException` — file claims a `schemaVersion` outside the binary's supported range, or the field is missing/malformed. Carries: `Path` (config file), `ClaimedVersion` (object: either an int, the offending non-int value, or null), `SupportedRange` (`[1..N]`), `Suggestion` (free-text action). MCP envelope mapping is DEC-007's concern.

A separate, pre-existing concern stays separate: **config validation errors** (version is supported but file shape is wrong — missing required field, invalid type) throw a different typed exception, conventionally `SpecforgeConfigValidationException`, which DEC-007 envelope-maps differently from version errors. DEC-006 does not define that type; it is implied by ITEM-002 (config model) and finalized there.

## Alternatives Considered

| Alternative | Rejection reason |
|---|---|
| Semantic versioning (`schemaVersion: "1.1.0"`) | Adds string-parse logic where integer comparison suffices. MAJOR.MINOR.PATCH is meaningful for APIs with frequent additive releases; for a spec schema that bumps once every 12+ months, MINOR and PATCH would always be `.0.0`. |
| Date-based versioning (`schemaVersion: "2026-05"`) | Self-locating in time but couples a bump to a calendar boundary, and creates ambiguity (timezone, "which 2026-05?"). Integer comparison is clearer and dependency-free. |
| Per-section versions (e.g., separate `packagesSchemaVersion`) | Adds N times the comparison logic for a problem that does not exist — the config file is small and evolves as a whole. |
| Versioning every structured format (config + SKILL.md + ledger + templates) | Adds version markers to artifacts no tool reads externally in MVP. Pre-emptive overhead. The forward-looking rule generalizes the choice without paying the cost today. |
| `specforgeSchemaVersion` key inside SKILL.md frontmatter | Pollutes a frontmatter contract owned by Claude Code and Codex. Specforge writes every SKILL.md fresh on each `install_skills`, so the migration scenario doesn't exist. |
| Best-effort parse + warn on unsupported versions | A v2-aware file read by a v1 binary could be silently misinterpreted (same field name, new semantics). Safety beats friendliness for spec data. |
| Silent ignore of unknown fields | Quietly throws away semantically meaningful new fields on every save. Lossy by design. |
| Automatic file rewrite on read | Friendly until `git status` after a read-only operation shows config changes. Surprising and hard to opt out of without a per-call flag. |
| Explicit `migrate` MCP tool | Auditable but forces the user to remember to call it. Binary still has to read pre-migration data at runtime — same logic plus an extra tool the user has to discover. |
| No automatic migration (manual edit only) | Punts a deterministic transformation to the human. Opposite of the tool's purpose. |
| Drop support for old versions immediately | Saves migration-function maintenance but breaks any user who hasn't upgraded their config. MVP cost of keeping every version is near zero. |
| Bumping on additive changes | Inflates the version counter and conditions every future binary on knowing the difference between v1.0 and v1.1 fields. Plain "additive = no bump" is the convention for most successful integer-versioned formats. |

## Consequences

- The `Specforge.Core` config parser implements two distinct checks at load time: schema-version check (`SpecforgeSchemaVersionException` on mismatch) and shape validation (`SpecforgeConfigValidationException` on wrong type/missing required field). These cannot be collapsed without losing diagnostic clarity.
- Every future schema bump is itself a DEC, with explicit migration rules and rationale.
- Schema v1 is open-ended for additive growth: future DECs can add optional fields without bumping, and they amend the Schema v1 Inventory table here.
- Read-only sessions never modify `.specforge.json`. Users can trust that running list-style tools is safe and idempotent on disk.
- Upgrading specforge after a v2 bump: a v1 file is silently accepted, read into v2 in memory, and rewritten as v2 the first time a tool mutates the config. The user notices the bump only when they would already be writing to the file.
- Older binary + newer file: specforge refuses with a precise error pointing the user at the upgrade path. No silent partial behavior.
- The migration chain accumulates one function per bump for the binary's lifetime. With current bump frequency expectations (every 12+ months), the maintenance cost is negligible.
- DEC-007's `init` tool always writes the **current** `schemaVersion` (no flag to author an older version). Round-tripping a `.specforge.json` through `init` upgrades it as a side effect — which is consistent with the opportunistic-write rule.
- Tooling can determine "this binary supports schemaVersion N" by reading a constant in `Specforge.Core`; that constant is the single source of truth and is exposed via a future read-only tool if needed.

## Impact Assessment

| Aspect | Impact | Notes |
|---|---|---|
| Schema versioning | Direct | This decision defines it. |
| Configuration discovery | Indirect | DEC-002 mechanics gain a version check at parse time. |
| `init` tool behavior | Indirect | DEC-007 `init` always writes the current `schemaVersion`. Round-tripping upgrades old files. |
| ID scheme | No impact | DEC-004's rules are locked there; not versioned through this scheme. |
| Skill packaging | No impact | DEC-005 makes skill version = binary version. SKILL.md never carries a specforge version. |
| Error handling | Direct | `SpecforgeSchemaVersionException` fully defined; `SpecforgeConfigValidationException` implied for ITEM-002 to finalize. |
| MCP tool surface | Indirect | DEC-007 envelope-maps the two error types differently. |
| Migration logic | Direct | In-memory upgrade chain + opportunistic write rule locked here. |
| Backward compatibility commitment | Direct | "Every binary reads every prior version" stated as the MVP-era commitment, with revisit trigger documented. |
| Future schema bumps | Direct | Bump procedure (one DEC per bump, migration function, inventory amendment) defined. |
| Documentation | Direct | Top-level README and getting-started must mention the `.specforge.json schemaVersion` field and the upgrade behavior. |
| Performance | No measurable | Version check is one integer comparison per load. |
| Concurrency | No measurable | Single-session config load; no contention. |
| Target project portability | Indirect | A v1 file in a target project will keep working as specforge evolves. |
| Maintenance burden | Indirect | One migration function per bump; bumps are rare. |

## Source Links

| Source | Locator | Evidence |
|---|---|---|
| `DEC-002-CONFIGURATION-AND-DISCOVERY` | section "Configuration Schema (version 1)" | `schemaVersion: 1` integer field introduced here. |
| `DEC-004-ID-SCHEME-CUSTOMIZATION` | section "Per-Package Customization (Additive)" | `extraKinds` field added to v1 additively; first concrete example of an additive amendment to a versioned schema. |
| `DEC-003-RUNTIME-AND-ARCHITECTURE` | section "Error Handling" | `SpecforgeSchemaVersionException` named as a placeholder type to be defined here. |
| `DEC-005-SKILL-INSTALLATION-MODEL` | section "Re-install Behavior" | Skill version = binary version; no SKILL.md-side versioning. |
| Conversation 2026-05-28, structured questions | AskUserQuestion answers | Plain integer; `.specforge.json` only; hard refuse on unsupported; in-memory + opportunistic write migration. |

## Related Decisions

- `DEC-002-CONFIGURATION-AND-DISCOVERY` (approved): introduced `schemaVersion`; this DEC formalizes the policy for it.
- `DEC-003-RUNTIME-AND-ARCHITECTURE` (approved): placed `SpecforgeSchemaVersionException` as a placeholder; this DEC fully defines it.
- `DEC-004-ID-SCHEME-CUSTOMIZATION` (approved): made the first additive amendment to v1; sets the precedent.
- `DEC-005-SKILL-INSTALLATION-MODEL` (approved): clarifies why SKILL.md is not versioned.
- `DEC-007-MVP-TOOL-SET` (planned): envelope-maps schema-version vs config-validation exceptions; `init` always writes the current version.

## Related Item Specs

- `ITEM-002-CONFIG-MODEL` (planned): implements the schema-version check, the in-memory upgrade chain (v1 → v_N), and the opportunistic-write rule. Defines `SpecforgeConfigValidationException` for the shape-validation case.
- `ITEM-003-ID-VALIDATOR` (planned): consumes the validated config but does not itself participate in versioning.

## Related Tests / Validation

- A `.specforge.json` with `schemaVersion: 1` is parsed by every binary version >= MVP.
- A `.specforge.json` with `schemaVersion: 2` against a binary whose `supportedMax = 1` throws `SpecforgeSchemaVersionException` with `claimedVersion=2`, `supportedRange=[1..1]`, and a suggestion mentioning specforge upgrade.
- A `.specforge.json` with `schemaVersion: 0`, `schemaVersion: -1`, `schemaVersion: "1"` (string), or no `schemaVersion` field throws `SpecforgeSchemaVersionException` with the "invalid" suggestion.
- A `.specforge.json` with `schemaVersion: 1` and a future-additive optional field that was missing in the source v1 (none currently — placeholder for once v1 grows) reads cleanly with the default populated.
- After a hypothetical v2 introduction: a v1 file read by a v2 binary populates v2 defaults in memory and remains unchanged on disk through pure read operations; the first config-mutating tool call rewrites the file as v2.
- `SpecforgeConfigValidationException` (shape error, version OK) is thrown distinctly from `SpecforgeSchemaVersionException` (version error) — they map to different MCP error envelopes per DEC-007.

## Open Questions

- Whether to expose a read-only MCP tool that reports `binary.supportedMax` (so users can diagnose "do I need to upgrade?") — likely yes; final shape decided in DEC-007. Not blocking.
- Whether the Schema v1 Inventory should be auto-generated from the C# model (so it cannot drift) or hand-maintained — leaning hand-maintained for MVP with a future test that asserts they match. Not blocking.
- Whether to provide a `--strict` mode where non-current-version files refuse on read instead of upgrading in memory — defer; current users have no need for it.
- When to introduce a deprecation policy for the oldest supported versions — defer until the migration chain exceeds 5 entries; document the trigger here as a reminder.

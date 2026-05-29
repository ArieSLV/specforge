# Glossary

Status: Draft

Normative terminology for specforge specifications. New terms MUST be added here before being used broadly.

## Core Terms

| Term | Meaning |
|---|---|
| `specforge` | The toolkit being specified. |
| `toolkit` | Synonym for specforge. |
| `target project` | A project that adopts specforge to manage its specifications. The toolkit itself is one such target (self-adoption / dogfood). |
| `package` | A unit of specification grouping inside a target project. A target project may have one or many packages (e.g., one per ticket or one per initiative). |
| `spec package` | The on-disk directory and files that make up one package. |
| `MCP server` | The compiled long-running process exposing toolkit functionality as MCP tools. |
| `MCP tool` | A single callable function exposed by the MCP server (e.g., `refs`, `validate`). |
| `Core library` | The C# library containing parsing, validation, and mutation logic. Reused by the MCP shell and any future shell (e.g., CLI). |
| `MCP shell` | The thin program layer that hosts Core library as an MCP server. |
| `spec graph` | The in-memory graph built by parsing a spec package: nodes are artifacts (decisions, items, ledger rows), edges are references. |
| `target host` | The MCP-aware client that launches the MCP server (Claude Code, Claude Desktop, Cursor, etc.). |

## Specification Artifact Terms

| Term | Meaning |
|---|---|
| `decision record` | A standalone file under `decisions/` documenting one design choice. ID prefix `DEC-`. |
| `item spec` | A standalone file under `items/` documenting one implementation slice. ID prefix `ITEM-`. |
| `ledger row` | A row in one of the `ledger/*.md` files tracking artifact, commit, validation, review, blocker, or history events. |
| `artifact ID` | Stable identifier for any tracked file or row (`ART-*`, `DEC-*`, `ITEM-*`, `BLOCK-*`, `Q-*`, etc.). |
| `stable locator` | A reference that survives routine edits: stable ID + section + evidence quote. Line numbers alone are not stable locators. |
| `handoff summary` | The opening section of an item spec that allows a fresh AI session to act on the slice without conversation context. |

## Process Terms

| Term | Meaning |
|---|---|
| `impact assessment` | Cross-cutting checklist applied to every meaningful decision and item spec. Each listed aspect is scored `No impact`, `Direct impact`, `Indirect impact`, `Needs follow-up`, `Needs decision`, or `Needs architect review`. |
| `validation pattern` | The toolkit's read-only cross-artifact audit producing a severity-tiered report (`CRITICAL` / `HIGH` / `MEDIUM` / `LOW`). |
| `dogfood` | The act of using specforge's own methodology to specify the toolkit itself. |
| `authority order` | Source-of-truth ordering for resolving disagreements between documents. |
| `review gate` | A stage boundary that requires reviewer approval before downstream work proceeds. |

## ID Scheme (Default)

Default ID prefixes used in the `specforge-mvp` package:

- `DEC-NNN-TITLE` — decision records
- `ITEM-NN-TITLE` — item specs
- `ART-TITLE` — artifact ledger rows
- `BLOCK-TITLE` — blocker ledger rows
- `Q-TITLE` — open question register rows

Customization of ID schemes for target projects is covered by `DEC-004-ID-SCHEME-CUSTOMIZATION` (planned).

## Forbidden / Avoided Terms

| Term | Why avoided |
|---|---|
| `feature spec` | Overloaded by spec-kit's product-feature framing; use `item spec` instead. |
| `task` (in spec-kit's `tasks.md` sense) | specforge does not generate task lists; this is left to the consuming workflow. |
| `constitution` | Overloaded by spec-kit; use the more descriptive `shared/` files instead. |
| `feature directory` | Overloaded by spec-kit's `specs/<feature>/`; use `package` instead. |

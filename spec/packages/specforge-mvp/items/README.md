# Item Specs — specforge-mvp Package

Status: Draft

Item specs are the unit of Stage 1 work. Each one is independently reviewable and self-contained enough to hand off to a fresh AI session.

## File Naming

`ITEM-NNN-CONCISE-TITLE.md` where:

- `NNN` is a zero-padded 3-digit sequential number (per `DEC-004-ID-SCHEME-CUSTOMIZATION`).
- Title is uppercase-hyphenated, max 60 chars, `[A-Z0-9-]` (per DEC-004 slug rules).

Use `../../../templates/item_spec.md` as the starting structure. Required sections live in `../../../shared/spec_item_contract.md`.

## Lifecycle

See `../../../shared/document_lifecycle.md` for status values and transitions.

An item is `Approved` only when:

- Its required sections are populated (per the item contract).
- Its `Impact Assessment` covers every non-`No impact` aspect.
- It uses stable locators in `Source Links`.
- It includes a `Handoff Summary` allowing a fresh AI session to act on it.
- It cites approved decisions for every architectural commitment it makes.

## Stage 1 Item List

See `../work_plan.md` Stage 1 for the planned 13 items. Each comes from Stage 0 outputs (DEC-007 mapped 11, DEC-008 added 2).

Dependency order: ITEM-001 first (solution scaffold); then ITEM-002, ITEM-003, ITEM-011 in parallel; then ITEM-004, ITEM-005; then ITEM-006; then ITEM-007, ITEM-008, ITEM-009 in parallel; then ITEM-010; then ITEM-012; then ITEM-013.

## Current Items

| ID | Title | Status |
|---|---|---|
| `ITEM-001` | Solution Bootstrap | Approved |
| `ITEM-002` | Config Model | Approved |
| `ITEM-003` | ID Validator | Approved |
| `ITEM-004` | Embedded Skill Catalog | Approved |
| `ITEM-005` | Skill Installer | Approved |
| `ITEM-006` | Init Tool | Approved |
| `ITEM-007` | Decision Tools | Approved |
| `ITEM-008` | Item Tools | Approved |
| `ITEM-009` | Ledger Append Tools | Approved |
| `ITEM-010` | Validate Tool | Approved |
| `ITEM-011` | Error Envelope | Approved |
| `ITEM-012` | Skill Content | Approved |
| `ITEM-013` | Docs / Getting Started | Approved |

**Stage 1 complete (2026-05-28): all 13 items Approved.** Stage 2 implementation is unblocked.

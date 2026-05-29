# Decision Records — specforge-mvp Package

Status: Draft

Decision records under this directory are the unit of architectural commitment for the MVP.

## File Naming

`DEC-NNN-CONCISE-TITLE.md` where:

- `NNN` is a zero-padded sequential number (001, 002, ...).
- Title is uppercase-hyphenated.

Use `../../../templates/decision_record.md` as the starting structure.

## Lifecycle

See `../../../shared/document_lifecycle.md` for status values.

A decision is `Approved` only when:

- Its `Alternatives Considered` section names concrete alternatives with concrete rejection reasons.
- Its `Impact Assessment` lists every non-`No impact` aspect with a short note.
- It uses stable locators in `Source Links`.
- The user (and architects, when needed) has accepted it.

## Current Decisions

| ID | Title | Status |
|---|---|---|
| `DEC-001` | Distribution and Transport | Approved |
| `DEC-002` | Configuration and Discovery | Approved |
| `DEC-003` | Runtime and Architecture | Approved |
| `DEC-004` | ID Scheme and Customization | Approved |
| `DEC-005` | Skill Installation Model | Approved |
| `DEC-006` | Schema Versioning Policy | Approved |
| `DEC-007` | MVP MCP Tool Set | Approved |
| `DEC-008` | Instruction Layer Design | Approved |

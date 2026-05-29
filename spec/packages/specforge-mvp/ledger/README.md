# Ledger Schema — specforge-mvp Package

Status: Draft

The ledger is split across multiple files so it can scale in length (many rows) and width (new metadata fields) without rewriting one huge table.

`../work_ledger.md` is dashboard only. Detailed data lives here.

## Files

| File | Purpose |
|---|---|
| `artifacts.md` | One row per document artifact in the package. |
| `items.md` | One row per implementation item spec. |
| `commits.md` | Append-only commit / push provenance, keyed by `LedgerId`. |
| `reviews.md` | User and architect review events, approvals, and requested changes. |
| `history.md` | Append-only timeline of status transitions and content events. |

Validations and blockers files MAY be added when needed; not part of MVP minimum.

## Core Row Fields

| Field | Meaning |
|---|---|
| `LedgerId` | Stable artifact or item ID. Join key across all ledger files. |
| `Artifact` | File path. |
| `Status` | Lifecycle status. |
| `Depends on` | Other artifacts or decisions. |
| `Owner / reviewer` | Human or role. |
| `Last update` | Date and short change description. |
| `Next action` | The next concrete step. |
| `Blocking question` | Empty unless blocked. |

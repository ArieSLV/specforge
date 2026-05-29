# specforge

MCP-based toolkit for engineering-grade specification work with AI agents.

Status: Pre-MVP (specification phase only — no code yet)

## Purpose

specforge supports projects that need:

- Decision records with cross-cutting impact assessment
- Item specs that hand off cleanly to fresh AI sessions
- A ledger for artifact, commit, validation, review, and blocker provenance
- Stable cross-references between decisions, items, and tests
- Per-package grouping for multi-project repositories
- Read-only cross-artifact validation with severity tiers

It targets engineering work on complex existing systems, not greenfield product feature development. For the latter, GitHub's spec-kit may fit better.

## Status

The toolkit is being specified before being built. Specification lives under `spec/`. Source code does not exist yet and will not be written until the relevant decisions and item specs are approved.

## Layout

| Path | Purpose |
|---|---|
| `spec/packages/specforge-mvp/` | Specification for the MVP of the toolkit |
| `spec/shared/` | Normative material shared across all packages |
| `spec/templates/` | Reusable document templates |
| `src/` (future) | Toolkit source code |

## Entry Points

1. `spec/packages/specforge-mvp/README.md`
2. `spec/packages/specforge-mvp/work_plan.md`
3. `spec/shared/document_lifecycle.md`
4. `spec/shared/glossary.md`
5. `spec/packages/specforge-mvp/work_ledger.md`

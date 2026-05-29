# specforge-mvp Package

Status: Draft
Created: 2026-05-25

This is the specification package for the MVP of the specforge toolkit. It is intentionally dogfooded: specforge will be the first consumer of specforge's own conventions.

## Authority Order

When implementing the toolkit:

1. Approved decisions under `decisions/`
2. Shared normative material under `../../shared/`
3. Approved items under `items/`
4. Ledger entries under `ledger/`
5. POC code (when it exists) is evidence only, never authority

If a design or implementation fork is not answered by an approved decision, it must be raised through a new decision record before implementation continues.

## Layout

| Path | Purpose |
|---|---|
| `work_plan.md` | Stage-by-stage plan for building the toolkit |
| `work_ledger.md` | Dashboard summary of artifact, decision, and item status |
| `decisions/` | Decision records (DEC-NNN-TITLE.md) |
| `items/` | Implementation item specs (ITEM-NN-TITLE.md). Populated after relevant decisions are approved. |
| `ledger/` | Detailed ledger files (artifacts, items, commits, reviews, history) |

## Scope of MVP

In scope:

- One MCP server, stdio transport, .NET compiled
- Core library + MCP shell architecture (no CLI shell)
- MCP tools: `refs`, `validate`, `lifecycle`, `ids-next`, `ledger-add`, `dashboard-sync`, `init`
- Skills for decision drafting, item drafting, ASCII diagramming, source classification
- Per-package directory structure for target projects
- Schema version 1 for spec package format

Explicitly out of scope for MVP (deferred to later packages):

- CLI shell (Core library is reusable for one later if needed)
- HTTP transport for the MCP server
- Mac and Linux builds
- Schema migration tooling between toolkit versions
- Hosted / SaaS deployment
- Agent-specific slash command file generation (MCP gives multi-agent compatibility natively)

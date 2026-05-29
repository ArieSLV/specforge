# Cross-Cutting Impact Assessment Checklist

Status: Draft

Every meaningful decision and every item spec must be evaluated against this checklist. This prevents drift between concerns and keeps each decision scoped.

## Assessment Values

| Value | Meaning |
|---|---|
| `No impact` | The decision does not touch this aspect. |
| `Direct impact` | The decision changes behavior or code in this aspect. |
| `Indirect impact` | The decision changes assumptions this aspect depends on. |
| `Needs follow-up` | The decision exposes a separate task or item spec. |
| `Needs decision` | The decision opens a fork that must be resolved. |
| `Needs architect review` | The impact is material enough to require architect-level input. |

## Aspect List

| Aspect | What to check |
|---|---|
| MCP tool surface | New tools added, existing tool signatures changed, JSON-schema implications. |
| Skill surface | New skills, changed skills, skill discovery and load order. |
| Spec package schema | Layout of `decisions/`, `items/`, `ledger/`, `shared/`, template shape. |
| Spec graph parser | Markdown parsing assumptions, frontmatter parsing, ID extraction rules. |
| Validation rules | Severity tiers, new rules, retired rules, false-positive risk. |
| ID scheme | Default prefixes, configurability, uniqueness rules, collision behavior. |
| Stable locator policy | Reference format, breakage detection, migration of line-only references. |
| Lifecycle states | Status values, transition rules, approval gates. |
| Per-package grouping | Single-package vs multi-package projects, cross-package references. |
| Configuration discovery | How the server finds the target package and its config. |
| Distribution | Binary packaging, `.mcp.json` registration, install location. |
| Schema versioning | Backward compatibility, migration tooling, version detection. |
| Performance | Cold start, graph build time, cache invalidation. |
| Memory footprint | In-memory graph size for large packages. |
| Concurrency | Multiple MCP sessions against the same package. |
| Error handling | Tool-call error format, diagnostic content, debuggability. |
| Logging / observability | Server-side logging, what is exposed to the host. |
| Multi-agent compatibility | Behavior across different MCP hosts (Claude Code, Cursor, etc.). |
| Target project portability | Hardcoded assumptions that block reuse outside specforge itself. |
| Skill / tool coupling | Whether skills require specific tool versions; version negotiation. |
| Documentation | Getting-started impact, reference documentation impact. |
| Testing | Unit, integration, and dogfood-validation impact. |
| Build / dev loop | Build step changes for toolkit developer (not user). |
| Dependencies | NuGet packages added or upgraded, license implications. |

Every decision entry should include only the aspects that are not `No impact`, plus a short note explaining the impact. If too many aspects are affected, split the decision into smaller decisions.

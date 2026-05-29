# DEC-001-DISTRIBUTION-AND-TRANSPORT - Distribution and Transport

Status: Approved
Date: 2026-05-25
Owner: AI assistant (drafted)
Review owner: User
Approved on: 2026-05-25 by User
Covers: MCP transport choice, server lifecycle, binary distribution model, registration mechanism, multi-agent strategy
Supersedes: none (foundational record)

## Context

specforge is a toolkit for engineering-grade specification work. Its primary consumer is an AI agent (Claude Code initially, potentially others) that calls specforge tools while drafting decision records, item specs, and ledger updates.

Before any code is written, several foundational distribution choices must be locked in because they ripple through all later decisions:

- What protocol does the AI agent use to call specforge tools?
- How is the specforge binary launched and terminated?
- How does the AI host (Claude Code, etc.) find and register the specforge server?
- How is the binary packaged and shipped?
- Does specforge generate agent-specific configuration files (as spec-kit does), or does it rely on a single protocol that works across hosts?

These questions resolve before `DEC-002-CONFIGURATION-AND-DISCOVERY` because that decision assumes a running server and asks how it identifies the target package. Without an answer to DEC-001, DEC-002 has nothing to attach to.

This record is intentionally narrow. The .NET runtime version, NuGet stack, and Core-library / MCP-shell architecture split are covered by `DEC-003-RUNTIME-AND-ARCHITECTURE`. The exact MCP tool list is covered by `DEC-007-MVP-TOOL-SET`. The behavioral guidance layer — how tool descriptions, MCP Resources, skills, target-project `CLAUDE.md`, and shipped `AGENTS.md` cooperate to give the AI agent procedural understanding of the toolkit — is covered by `DEC-008-INSTRUCTION-LAYER-DESIGN`.

## Decision

### Transport

specforge uses **MCP stdio transport only** for the MVP.

- The MCP host (e.g., Claude Code) launches the specforge server as a child process.
- Communication uses JSON-RPC over stdin / stdout per the MCP standard.
- No HTTP transport. No network listener. No remote access.

### Server Lifecycle

specforge follows the standard MCP **host-spawned, session-scoped** lifecycle:

```text
  host process (Claude Code, ...)
        |
        | session start
        v
  spawn specforge.exe   ----------+
        |                         |
        | JSON-RPC over stdio     |
        | (tool calls, results)   |
        |                         |
        | session end             |
        v                         |
   host disconnects               |
        |                         |
        v                         |
   specforge process exits  <-----+
```

- The MCP host spawns one specforge process per session at session start.
- The process lives for the duration of the session.
- The process terminates when the host disconnects.
- No daemon. No background service. No system-tray entry.
- Server-side caches (e.g., parsed spec graph) live in per-session process memory and are discarded on session end.

### Binary Distribution

specforge is distributed as a **self-contained single-file .NET executable**, Windows x64 only for MVP.

- Built via `dotnet publish` with `--self-contained true`, `-p:PublishSingleFile=true`, runtime identifier `win-x64`.
- The publish step uses `-o D:\Work\specforge\bin\` to direct output into a stable, framework-agnostic path. Without `-o`, `dotnet publish` defaults to `bin\<Configuration>\<TargetFramework>\<RuntimeIdentifier>\publish\` (for example `bin\Release\net8.0\win-x64\publish\`), which would force `.mcp.json` updates whenever DEC-003 changes the target framework.
- The end user does not need a .NET SDK or runtime installed.
- Mac and Linux builds are out of MVP scope (deferred to a later package).
- Size budget: ~50 MB acceptable; if the binary exceeds ~80 MB, `-p:PublishTrimmed=true` is to be evaluated during ITEM-003 implementation.

### Installation Location

For MVP:

- Binary lives at a fixed absolute path on the developer machine: `D:\Work\specforge\bin\specforge.exe`.
- This path is stable across framework choices because the publish step uses `-o` to direct output here (see Binary Distribution above).
- No installer. No PATH manipulation. No per-user copy.
- The user references this absolute path in their MCP host configuration.

A standardized install location (`%LOCALAPPDATA%\specforge\` or similar) is deferred to v2 once usage patterns are clear.

### Registration with MCP Host

specforge is registered **once, user-wide**, in the host's user-level MCP configuration file.

- For Claude Code, this is `~/.claude/.mcp.json` (or the host's equivalent user-scope config).
- Per-project `.mcp.json` files are NOT required and SHOULD NOT be used for specforge — one global registration makes the toolkit available in every project the host opens.

Illustrative registration entry shape (exact key names follow the MCP SDK at implementation time):

```text
"specforge": {
  "command": "D:\\Work\\specforge\\bin\\specforge.exe",
  "args": []
}
```

The server identifies the target package per tool call, not via launch arguments. The discovery mechanism is the subject of `DEC-002-CONFIGURATION-AND-DISCOVERY`.

### Multi-Agent Strategy

specforge has two layers of consumer-facing artifacts, each with a different agent strategy.

**MCP tool layer — cross-agent.** The MCP server itself is protocol-agnostic. Any MCP-aware host (Claude Code, Claude Desktop, Codex CLI, Zed, others) can register and call specforge tools without per-agent generation. specforge does NOT generate per-agent slash-command files like spec-kit's `.claude/commands/foo.md`, `.cursor/commands/foo.md`, `.codex/commands/foo.md` — those duplicate the same logical command across hosts. This is an intentional divergence from spec-kit's 13-agent matrix (see `D:\Work\spec-kit\AGENTS.md`, section "Current Supported Agents"); that matrix is a pre-MCP workaround that MCP eliminates.

**Instruction / skill layer — per-agent but structurally aligned.** Procedural know-how (decision drafting, item drafting, ASCII diagrams, source classification) is delivered as Agent Skills. Both supported hosts use a `SKILL.md` directory format with YAML frontmatter (`name`, `description`); paths and a few optional metadata items differ. MVP supports two agents because the toolkit owner uses both heavily:

- Claude Code: `~/.claude/skills/<name>/SKILL.md`
- Codex CLI: `~/.agents/skills/<name>/SKILL.md` (Codex skills MAY additionally declare MCP-tool dependencies via optional `agents/openai.yaml`)

Codex's older `~/.codex/prompts/` (Custom Prompts) mechanism is **deprecated** per OpenAI's 2026 documentation in favour of Agent Skills, so specforge does not target it. spec-kit's `.codex/commands/` table entry reflects that deprecated path.

Cursor is explicitly NOT supported because the toolkit owner's organization does not use it; excluding it removes the temptation to add a third skill flavour. Other hosts (Windsurf, Gemini CLI, Kilo, Roo, opencode, Auggie, CodeBuddy, Amazon Q, Qwen, GitHub Copilot) are out of MVP scope. A future package may add per-agent skill ports if there is a concrete consumer.

Skill content is logically the same across the two supported agents (the procedure for drafting a decision does not change), and because both use the `SKILL.md` directory shape, the host-specific wrapping is mostly path-level. Concrete maintenance cost is "two install locations" rather than "two file formats".

Detailed skill packaging and discovery strategy is the subject of `DEC-005-SKILL-INSTALLATION-MODEL` and `DEC-008-INSTRUCTION-LAYER-DESIGN`.

Summary for MVP: one cross-agent MCP server plus skills for Claude Code and Codex CLI.

## Alternatives Considered

| Alternative | Rejection reason |
|---|---|
| HTTP transport instead of stdio | Adds network listener and authentication surface for zero benefit in single-user local use. stdio is the standard MCP pattern for local tools. |
| Both stdio and HTTP simultaneously | Doubles surface area, testing burden, and documentation. May be added in a future package if remote access becomes a genuine requirement. |
| Long-running daemon (always on, shared across sessions) | Non-standard MCP pattern. Adds lifecycle complexity (start, stop, restart, crash recovery). No significant performance advantage given how lightweight individual tool calls are. |
| Spawn per tool call (process per call) | Adds startup overhead to every call, defeating any caching opportunity. The session-scoped model amortizes parse cost across many calls in one session. |
| Framework-dependent .NET deployment | Fragile when the user's installed .NET runtime version drifts from the build target. Self-contained publish removes this class of failure entirely. |
| NuGet global tool (`dotnet tool install -g specforge`) | Real virtues: clean version management, no manual binary copy, public discoverability. Rejected for MVP because (1) it requires the user to have a .NET SDK installed, (2) it requires hosting a NuGet feed, (3) the self-contained single-exe path is simpler for bootstrap. May be reconsidered for v2. |
| Per-project `.mcp.json` registration | Forces the user to register specforge in every project. With one global registration, specforge is available everywhere automatically. |
| Generate agent-specific slash command files like spec-kit | Bets against the MCP protocol's cross-agent guarantee and creates an N-host maintenance matrix. MCP gives specforge multi-agent reach natively; spec-kit's approach is a workaround for the pre-MCP era. |
| Build for Mac / Linux at MVP | Toolkit author is on Windows; cross-platform builds are not free (testing matrix, path handling, file system case sensitivity). v2 will add Linux when there is a concrete consumer. |

## Consequences

- specforge is one Windows x64 self-contained executable for MVP.
- The user registers specforge once in their user-wide MCP config and forgets about it.
- Every MCP-aware AI host the user uses gets specforge tools for free (no per-host setup). The skill layer supports Claude Code and Codex CLI in MVP; users of other MCP-aware hosts get tools but not the curated procedural skills.
- Server starts cold on every new session; per-session in-memory caches are acceptable.
- Mac and Linux users cannot use the MVP. This is a known and accepted limitation.
- The "how does the running server figure out which target package to operate on" question is intentionally left to DEC-002.
- The "what runtime and NuGet packages does the .NET app use" question is intentionally left to DEC-003.
- Hosts that lack MCP support cannot use specforge tools at all. This is an accepted tradeoff: the cost of maintaining a pre-MCP slash-command matrix (the spec-kit pattern) outweighs the benefit of supporting niche pre-MCP hosts.
- At the skill layer, Claude Code and Codex CLI are supported in MVP. Cursor is explicitly out of scope (organization does not use it). Other hosts are deferred to a future package and are a known, accepted gap.
- Each skill is authored once and delivered to two install locations (`~/.claude/skills/<name>/SKILL.md` and `~/.agents/skills/<name>/SKILL.md`). Because both agents use the same `SKILL.md` directory shape, the cost is closer to copying than to porting. DEC-008 will pick between (a) single-source SKILL.md plus a tiny install script that places copies in both locations, and (b) parallel files. Single-source is plausible because the formats are nearly identical.
- Codex's optional `agents/openai.yaml` allows formally declaring `mcp` tool dependencies for a skill; this is a Codex-only refinement specforge will exploit where it improves Codex behaviour, without requiring a parallel Claude Code construct.

## Impact Assessment

| Aspect | Impact | Notes |
|---|---|---|
| Distribution | Direct | Self-contained single-exe is the MVP shape. |
| Multi-agent compatibility | Direct | Bet on MCP protocol; no per-agent files. |
| Documentation | Direct | User-facing install and register instructions required at MVP. |
| Target project portability | Direct | One global server serves any number of target projects; no per-project install. |
| Dependencies | Direct | Self-contained publish adds 30-80 MB depending on trimming. |
| Configuration discovery | Indirect | Sets the constraint that DEC-002 must work per-call, not per-launch. |
| Schema versioning | Indirect | Binary version is one component of overall versioning; full policy is DEC-006. |
| Build / dev loop | Indirect | Compile + publish workflow during toolkit development (not user-facing). |
| Skill / tool coupling | Indirect | Skills will call MCP tools by name; transport stability matters but is independent of skill design. |
| Performance | Indirect | Cold start per session is acceptable; cache lives in process memory. |
| Concurrency | Indirect | Multiple host sessions = multiple specforge processes, each with its own in-memory state. DEC-002 will need to address shared on-disk state. |

## Source Links

| Source | Locator | Evidence |
|---|---|---|
| MCP specification | Transport section, "stdio" subsection | stdio is the canonical local-tool transport. |
| Anthropic MCP SDK documentation | "Local server with stdio transport" examples | Confirms stdio + host-spawn is the recommended pattern. |
| GitHub Spec Kit `AGENTS.md` | `D:\Work\spec-kit\AGENTS.md`, section "Current Supported Agents" | 13-agent maintenance matrix shown; example of the pattern specforge intentionally avoids. |
| Conversation 2026-05-25 between user and AI assistant | Sections "Why MCP здесь действительно подходит" and "Чего стоит признать честно" | User and assistant agreed on MCP as primary surface, Windows-first scope, self-contained binary distribution. |
| Conversation 2026-05-25 between user and AI assistant | Section "оба нет" | User confirmed no CI / pre-commit and no shell-automation requirement, removing CLI shell from MVP. |
| OpenAI Codex documentation 2026 | `https://developers.openai.com/codex/skills` | Codex CLI Agent Skills: directory layout with `SKILL.md`, USER scope at `~/.agents/skills/`, YAML frontmatter `name` + `description`, optional `agents/openai.yaml` with `dependencies.tools` of `type: mcp`, progressive disclosure model. |
| OpenAI Codex documentation 2026 | `https://developers.openai.com/codex/custom-prompts` | Confirms Custom Prompts at `~/.codex/prompts/` are deprecated in favour of Agent Skills; specforge does not target the deprecated path. |
| OpenAI Codex documentation 2026 | `https://developers.openai.com/codex/guides/agents-md` | AGENTS.md is durable project guidance for working agreements and repository expectations; separate concept from skills, not a substitute. |

## Related Decisions

- `DEC-002-CONFIGURATION-AND-DISCOVERY` (planned): how the server identifies the target package per tool call.
- `DEC-003-RUNTIME-AND-ARCHITECTURE` (planned): .NET version, NuGet stack, Core library + MCP shell split.
- `DEC-006-SCHEMA-VERSIONING` (planned): how binary and spec-package versions interact.
- `DEC-007-MVP-TOOL-SET` (planned): exact list of MCP tools exposed.
- `DEC-008-INSTRUCTION-LAYER-DESIGN` (planned): how tool descriptions, MCP Resources, skills, and per-project / shipped documents combine to guide the AI agent.

## Related Item Specs

- `ITEM-003-MCP-SERVER-BOOTSTRAP` (planned): the program entry point, tool registration, stdio loop. Implements the transport / lifecycle side of DEC-001.

## Related Tests / Validation

- Manual: spawn the published executable, verify it speaks MCP stdio against a known good MCP host harness.
- Automated: smoke test that confirms `tools/list` returns at least one tool after the server starts.
- Validation: register the published exe in the user's `~/.claude/.mcp.json`, confirm Claude Code lists specforge tools.

## Open Questions

None blocking. Deferred questions all live in later decisions (DEC-002, DEC-003, DEC-006, DEC-007).

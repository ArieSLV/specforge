# DEC-003-RUNTIME-AND-ARCHITECTURE - Runtime and Architecture

Status: Approved
Date: 2026-05-28
Owner: AI assistant (drafted)
Review owner: User (approved 2026-05-28)
Covers: .NET runtime version, language version, solution/project layout, dependency direction, publishing mode, baseline library selection, logging/diagnostics convention, error-handling stance
Supersedes: none

## Context

`DEC-001-DISTRIBUTION-AND-TRANSPORT` fixed the outer shape: MCP stdio transport, host-spawned per-session lifecycle, self-contained single-file Win x64 binary published to `D:\Work\specforge\bin\` and registered user-wide via `.mcp.json`.

`DEC-002-CONFIGURATION-AND-DISCOVERY` then defined the runtime contract for finding `.specforge.json` and resolving packages, and stated explicitly that "the Core library implements config parsing and resolution." That phrasing implied — but did not formalize — a library/host split.

This record formalizes the runtime and architectural foundation that every subsequent Stage 0 decision (DEC-004..DEC-008) and every Stage 1 item spec will sit on:

- which .NET we build on,
- how the solution is split into projects,
- which direction dependencies flow,
- how the binary is published,
- which baseline libraries we adopt,
- how logging and errors are handled given that stdio is owned by the MCP protocol.

On 2026-05-28 the user chose, via structured questions:

- runtime: **.NET 10 LTS**;
- layout: **Core + Mcp + Tests** (three projects in one solution);
- publish mode: **self-contained single-file**, no trimming, no AOT.

This record formalizes those choices and fills in the surrounding mechanics that do not require user adjudication (library selection, logging stance, error propagation).

## Decision

### Runtime

- Target framework: **.NET 10** (LTS, GA Nov 2025, support through Nov 2028).
- Language version: **C# latest** (matches .NET 10 release).
- `global.json` at repo root pins the SDK with `"rollForward": "latestFeature"` so minor SDK updates do not break builds.
- `Nullable: enable`, `ImplicitUsings: enable`, `TreatWarningsAsErrors: true` set in a `Directory.Build.props` at repo root and inherited by every project.

### Solution and Project Layout

One solution at the repo root: `Specforge.sln`.

```text
D:\Work\specforge\
  Specforge.sln
  Directory.Build.props
  global.json
  src\
    Specforge.Core\
      Specforge.Core.csproj         (library — no MCP dependency)
    Specforge.Mcp\
      Specforge.Mcp.csproj          (executable — MCP host)
  test\
    Specforge.Tests\
      Specforge.Tests.csproj        (xUnit)
  bin\                               (publish output target, gitignored)
```

- **`Specforge.Core`** is a pure library. It owns: config discovery and parsing, package enumeration, shared/template resolution (DEC-002), document model (decision records, item specs, ledger rows), ID validation (DEC-004), markdown parsing and rendering helpers, append-only ledger logic, history events. It must not reference any MCP package, any HTTP package, or `Microsoft.Extensions.Hosting`.
- **`Specforge.Mcp`** is the executable host. It wires the official MCP C# SDK to stdio, registers tools, and delegates every tool body to `Specforge.Core`. The output assembly is named `specforge.exe` (`<AssemblyName>specforge</AssemblyName>` in the csproj) so that DEC-001's `.mcp.json` registration path `D:\Work\specforge\bin\specforge.exe` is satisfied without renames.
- **`Specforge.Tests`** is a single xUnit assembly that covers Core directly and Mcp via in-proc method invocation (no real stdio handshake needed for the bulk of testing).

### Dependency Direction

```text
Specforge.Tests  ──►  Specforge.Mcp  ──►  Specforge.Core
                ╰────────────────────────►  Specforge.Core
```

- `Specforge.Core` depends only on: BCL, `Markdig`, `YamlDotNet`, `Microsoft.Extensions.Logging.Abstractions`.
- `Specforge.Mcp` depends on: `Specforge.Core` + the official MCP C# SDK (`ModelContextProtocol` package family) + `Microsoft.Extensions.Logging.Console` (for stderr logging).
- `Specforge.Tests` depends on: both projects + xUnit + test SDK.
- A build-time check (architecture test in `Specforge.Tests`) asserts that `Specforge.Core` carries zero reference to any `ModelContextProtocol.*` assembly. This freezes the boundary against accidental drift.

### Publishing

Single canonical publish command, framework-agnostic on the output side:

```bash
dotnet publish src/Specforge.Mcp -c Release -r win-x64 ^
  --self-contained true ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -o D:\Work\specforge\bin\
```

- Self-contained: framework bundled — no .NET install required on the target machine.
- Single-file: produces `specforge.exe`.
- No trimming, no AOT for MVP. Both Markdig and the MCP SDK use reflection heavily; enabling trim/AOT now would require either trim warnings to be suppressed unsafely or `[DynamicallyAccessedMembers]` annotations we cannot guarantee for third-party code. Revisit in a later DEC if startup time or binary size becomes a measured constraint.
- Output path `D:\Work\specforge\bin\` is fixed by DEC-001's `.mcp.json` registration. The publish command above uses `-o` to keep the path framework-version-independent (no `bin\Release\net10.0\win-x64\publish\` leak).

### Baseline Libraries

| Concern | Package | Project |
|---|---|---|
| Markdown parsing/AST/rendering | `Markdig` | Core |
| YAML frontmatter (for `SKILL.md` packaged by DEC-005) | `YamlDotNet` | Core |
| Logging contract | `Microsoft.Extensions.Logging.Abstractions` | Core |
| Logging implementation (stderr) | `Microsoft.Extensions.Logging.Console` | Mcp |
| MCP transport / tool registration | `ModelContextProtocol` (official C# SDK) | Mcp |
| Test framework | `xUnit` + `Microsoft.NET.Test.Sdk` + `xunit.runner.visualstudio` | Tests |

Exact versions are pinned at implementation time in each csproj; a `Directory.Packages.props` centralises version management. No other runtime libraries are introduced without an item-spec amendment.

### Logging and Diagnostics

stdio is owned by the MCP protocol: stdout is the protocol channel, stdin is the inbound message stream. Logging therefore must not touch stdout.

- All log output goes to **stderr**. The console logger is configured with `LogToStandardErrorThreshold = LogLevel.Trace` so every level routes to stderr.
- Default minimum level: `Information`. Overridable per process via environment variable `SPECFORGE_LOG_LEVEL` (`Trace|Debug|Information|Warning|Error|Critical`).
- Optional log file: if `SPECFORGE_LOG_FILE` is set, logs are tee'd to that file in addition to stderr. Implementation is a simple file logger inside `Specforge.Mcp` (no Serilog/NLog dependency for MVP).
- Core never writes to console directly; it logs through `ILogger<T>` only.
- No telemetry, no metrics, no OpenTelemetry exporter in MVP.

### Threading and Async

- Every public Core API that performs I/O (file read, file write, ledger append) returns `Task` or `ValueTask`.
- The MCP host is single-process, single-session (DEC-001), so there is no cross-request concurrency story. Per-request handlers may be async-await; there is no shared mutable state that requires locking for MVP.
- Cancellation tokens are accepted by every async Core API and threaded through to file I/O.

### Error Handling

- Core throws **typed exceptions** for documented failure modes:
  - `SpecforgeConfigNotFoundException` (DEC-002 missing-config case),
  - `SpecforgePackageNotSelectedException` (DEC-002 multi-package no-selection case),
  - `SpecforgeUnknownPackageException` (selection by unknown name),
  - `SpecforgeSchemaVersionException` (DEC-006 territory; placeholder type defined here),
  - additional types added by their owning DECs.
- `Specforge.Mcp` catches these at the tool-handler boundary and converts them to MCP error responses. The exact MCP envelope shape is the property of DEC-007.
- Unexpected exceptions are logged at `Error` level on stderr and returned to the host as a generic `Internal` MCP error; nothing is silently swallowed.

## Alternatives Considered

| Alternative | Rejection reason |
|---|---|
| .NET 8 LTS | Support ends Nov 2026 (~6 months from this decision); would force a runtime migration mid-MVP. No upside, since specforge ships its own SDK pin. |
| .NET 9 STS | STS lifecycle ending May 2026 — already at or past EOL on the decision date. Wrong choice for a new project. |
| Single project (Core + Mcp merged) | Simpler tree, but bakes MCP into the library boundary. Defeats DEC-001's reusability stance and makes a future CLI/embedded host a refactor instead of a new project. |
| Per-project test assemblies (`Core.Tests` + `Mcp.Tests`) | Many tests naturally span both layers (e.g. "config discovery surfaces the right MCP error"). Single test assembly removes a class of "which project does this test live in" decisions for marginal benefit. |
| Self-contained + Trimming | Markdig and the MCP SDK rely on reflection; trim would require either unsafe warning suppression or upstream annotation work we cannot do. Revisit later if size is measured to be a problem. |
| Self-contained + Native AOT | Best startup, but tooling for Markdig / MCP SDK / YamlDotNet is still maturing in 2026 and would force handwritten serialization. Premature for MVP. |
| Roll our own MCP transport | The official C# SDK is maintained and covers stdio + tool registration. Reimplementing is pure cost. |
| Serilog or NLog for logging | More features than MVP needs; abstraction (`ILogger<T>`) lets us swap in either later without touching Core call sites. |
| NUnit or MSTest | xUnit is the de facto .NET community standard, has clean parallel execution, and matches the user's day-to-day environment (RavenDB). No reason to deviate. |
| `Specforge.Abstractions` interface project | Adds a fourth project for a benefit (mockability) that xUnit + concrete classes already give us for the MVP. Re-evaluate if a second host (CLI) appears. |
| Publish output under `bin\Release\net10.0\win-x64\publish\` | Path leaks the framework version into DEC-001's `.mcp.json` registration. Already rejected during DEC-001 review; using `-o D:\Work\specforge\bin\` instead. |

## Consequences

- Build is a single `dotnet build Specforge.sln -c Release`. Publish is a single `dotnet publish` invocation with explicit `-o` (above). No build orchestration tooling required for MVP.
- `Specforge.Core` is reusable: a future CLI host, an embedded host, or a different transport (HTTP MCP, named pipes) can take a project reference and wire the same operations without code churn in Core.
- The Core ↔ Mcp boundary is enforced by an architecture test — drift produces a red build, not a silent regression.
- Adding trim or AOT later is a configuration change on `Specforge.Mcp.csproj`, conditional on third-party libraries supporting it. Not blocked by this decision.
- Every subsequent DEC (DEC-004 ID scheme, DEC-005 skill packaging, DEC-006 schema versioning, DEC-007 MVP tools, DEC-008 instruction layer) places its implementation surface in known projects: model and rules in Core, MCP-facing surface in Mcp.
- DEC-005's skill packaging can rely on `YamlDotNet` already being present in Core for SKILL.md frontmatter.
- Logging goes to stderr only — this constraint must be honored by every future feature; writing to `Console.Out` would corrupt MCP messages.
- Test runs are `dotnet test` against a single assembly, simple to wire into CI later.
- Binary size will be larger than a framework-dependent build (self-contained .NET 10 is ~60-80 MB single-file). Acceptable for a developer-machine tool; revisit if it becomes friction.

## Impact Assessment

| Aspect | Impact | Notes |
|---|---|---|
| Distribution | Direct | Locks publish command, output layout, runtime independence. |
| Runtime requirements | Direct | .NET 10 SDK to build; nothing required on user machine to run. |
| Core library boundary | Direct | Pure-library status enforced by architecture test. |
| MCP tool surface | Indirect | Tools live in `Specforge.Mcp`; their shape and naming are DEC-007's concern. |
| Configuration discovery | Indirect | DEC-002 mechanics implemented in `Specforge.Core`. |
| ID scheme | Indirect | DEC-004 implementation lives in `Specforge.Core`. |
| Skill packaging | Indirect | DEC-005 uses Core for content generation; YamlDotNet already available. |
| Schema versioning | Indirect | DEC-006 policy implemented in `Specforge.Core`. |
| Performance | Indirect | Startup ~200-500 ms typical for self-contained single-file; acceptable for per-session spawn. |
| Concurrency | Indirect | Per-session process; no multi-tenant concerns. Async APIs in place for I/O. |
| Logging / observability | Direct | stderr-only convention locked here. |
| Error handling | Direct | Typed Core exceptions; MCP envelope mapping in DEC-007. |
| Testing | Direct | Single xUnit project; architecture test enforces Core/Mcp boundary. |
| Documentation | Indirect | Top-level README must document the build/publish command. |
| Maintenance burden | Indirect | Three projects, one solution, no orchestration tooling — minimal. |
| Future portability | Direct | Core is host-agnostic; alternative hosts (CLI, embedded, HTTP MCP) are additive, not destructive. |
| Schema migration tooling | Indirect | Lives in Core when DEC-006 specifies it. |

## Source Links

| Source | Locator | Evidence |
|---|---|---|
| `DEC-001-DISTRIBUTION-AND-TRANSPORT` | sections "Distribution Format", "Registration with MCP Host" | Self-contained single-file Win x64, `D:\Work\specforge\bin\specforge.exe` registration. |
| `DEC-002-CONFIGURATION-AND-DISCOVERY` | sections "Decision — Configuration Schema", "Related Item Specs" | "Core library implements config parsing and resolution" — formalized here. |
| Conversation 2026-05-28, structured questions | AskUserQuestion answers | .NET 10 LTS; Core + Mcp + Tests; self-contained single-file. |
| .NET 10 LTS support policy | dotnet.microsoft.com / lifecycle | GA Nov 2025, support through Nov 2028. |
| Official MCP C# SDK | `ModelContextProtocol` NuGet package family | stdio transport, tool registration primitives. |

## Related Decisions

- `DEC-001-DISTRIBUTION-AND-TRANSPORT` (approved): distribution shape this decision implements.
- `DEC-002-CONFIGURATION-AND-DISCOVERY` (approved): configuration semantics implemented in `Specforge.Core`.
- `DEC-004-ID-SCHEME-CUSTOMIZATION` (planned): rules live in Core.
- `DEC-005-SKILL-INSTALLATION-MODEL` (planned): packaging hooks built atop Core.
- `DEC-006-SCHEMA-VERSIONING` (planned): policy implemented in Core; placeholder exception type already defined here.
- `DEC-007-MVP-TOOL-SET` (planned): tool surface lives in `Specforge.Mcp`; finalizes MCP error envelope.
- `DEC-008-INSTRUCTION-LAYER-DESIGN` (planned): no runtime impact, but ships skills generated by Core utilities.

## Related Item Specs

- `ITEM-001-SOLUTION-BOOTSTRAP` (planned): scaffolds the solution, three projects, `Directory.Build.props`, `Directory.Packages.props`, `global.json`, and the architecture test that guards the Core/Mcp boundary.
- `ITEM-002-CONFIG-MODEL` (planned): implements DEC-002 inside `Specforge.Core`.

## Related Tests / Validation

- `dotnet build Specforge.sln -c Release` produces all three assemblies without warnings.
- `dotnet publish src/Specforge.Mcp -c Release -r win-x64 -p:PublishSingleFile=true --self-contained -o D:\Work\specforge\bin\` produces `D:\Work\specforge\bin\specforge.exe`.
- Architecture test in `Specforge.Tests` asserts `Specforge.Core` references zero `ModelContextProtocol.*` assemblies.
- Smoke test: launch the published binary, send a minimal MCP initialize handshake over stdio, observe a valid response on stdout and nothing on stderr at the default log level.
- Logging test: forcing an error path produces output on stderr and never on stdout.

## Open Questions

- Exact NuGet versions for `Markdig`, `YamlDotNet`, `ModelContextProtocol`, xUnit — pinned at item-spec implementation time in `Directory.Packages.props`. Not blocking this decision.
- Whether `Directory.Packages.props` Central Package Management is enabled from the start or introduced later — leaning yes from the start; not blocking.

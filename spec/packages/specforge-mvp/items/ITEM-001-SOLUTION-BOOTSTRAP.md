# ITEM-001-SOLUTION-BOOTSTRAP - Solution Bootstrap

Status: Approved
Review owner: User (approved 2026-05-28)
Depends on: `DEC-001-DISTRIBUTION-AND-TRANSPORT`, `DEC-003-RUNTIME-AND-ARCHITECTURE`, `DEC-007-MVP-TOOL-SET`
Updates ledger rows: new `ART-ITEM-001`; new `CMT-NNN` rows for the bootstrap commit(s)

## Handoff Summary

Bootstrap the .NET solution that every subsequent specforge code item depends on. Create `Specforge.sln` at the repository root with three projects (`Specforge.Core`, `Specforge.Mcp`, `Specforge.Tests`) per the layout fixed by `DEC-003-RUNTIME-AND-ARCHITECTURE`, plus the centralized build configuration (`Directory.Build.props`, `Directory.Packages.props`, `global.json`), plus the architecture test that enforces the Core/Mcp dependency boundary. No tool logic, no MCP server logic, no domain types — just the scaffold and the boundary guard.

- Three projects, one solution, one build command (`dotnet build Specforge.sln -c Release`).
- `Specforge.Mcp` outputs as `specforge.exe` via `<AssemblyName>specforge</AssemblyName>` so DEC-001's `.mcp.json` registration path stays valid.
- Architecture test (xUnit) asserts `Specforge.Core` references zero `ModelContextProtocol.*` assemblies.
- Publish command per DEC-003 produces `D:\Work\specforge\bin\specforge.exe`; the binary is allowed to be a no-op at this stage — runtime behavior lands in subsequent items.

## Problem Slice

This item resolves "where does specforge code live and how does it build?" — the structural bedrock that every subsequent item compiles against.

Explicit non-goals (each is owned by a later item):

- Configuration discovery / parsing → `ITEM-002-CONFIG-MODEL`.
- ID validation → `ITEM-003-ID-VALIDATOR`.
- Embedded skill resource glob and catalog → `ITEM-004-EMBEDDED-SKILL-CATALOG`.
- The MCP server's stdio loop, tool registration, and any tool body → `ITEM-005` and later.
- Any feature that ships in `Specforge.Core` beyond an empty namespace.

## Terminology Used

- **Solution / project**: standard .NET concepts.
- **Architecture test**: an xUnit test that inspects compiled assemblies' metadata to assert dependency rules; fails the build if `Specforge.Core` ever takes an MCP reference.
- **Central package management**: `Directory.Packages.props` at the solution root carries all `<PackageVersion>` entries; project files use `<PackageReference Include="..." />` without versions.
- **AssemblyName**: the MSBuild property that sets the output filename, independent of the project name.

Glossary terms: see `../../../shared/glossary.md` ("artifact ledger", "stable locator" — used here for the architecture-test design).

## Approved Decisions

- `DEC-001-DISTRIBUTION-AND-TRANSPORT` — section "Distribution Format", section "Registration with MCP Host": output binary at `D:\Work\specforge\bin\specforge.exe`; self-contained single-file Win x64 publish.
- `DEC-003-RUNTIME-AND-ARCHITECTURE` — sections "Runtime", "Solution and Project Layout", "Dependency Direction", "Publishing": .NET 10 LTS; three projects under `src/` and `test/`; architecture-test-enforced Core/Mcp boundary; publish command pinned with `-o`.
- `DEC-007-MVP-TOOL-SET` — section "Tool Count Summary": informs that `Specforge.Mcp` will host 21 tools in later items; ITEM-001 only scaffolds the project, not any tool.

## Current Code State

The repository at `D:\Work\specforge\` contains **only specification files** under `spec/` (markdown) and this item spec under `spec/packages/specforge-mvp/items/`. There is no `Specforge.sln`, no `src/`, no `test/`, no `bin/`, no `Directory.Build.props`, no `global.json`. The `bin/` path referenced by DEC-001 does not yet exist on disk.

A `.gitignore` may or may not be present at the repo root — implementation must check and amend rather than overwrite.

## Target Behavior

After this item is Done:

1. `dotnet build Specforge.sln -c Release` from the repo root succeeds with **zero warnings** (DEC-003 mandates `TreatWarningsAsErrors: true`).
2. `dotnet test test/Specforge.Tests -c Release` runs the architecture test and it passes.
3. `dotnet publish src/Specforge.Mcp -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o D:\Work\specforge\bin\` produces `D:\Work\specforge\bin\specforge.exe` (the file exists and is launchable; it may exit immediately with no observable behavior — that is acceptable for ITEM-001).
4. Repo file tree matches the layout fixed in DEC-003 ("Solution and Project Layout" diagram).

## Invariants

- `Specforge.Core` references no `ModelContextProtocol.*` assembly (asserted by the architecture test on every build).
- All three projects target `net10.0` exactly; no multi-targeting.
- `Nullable: enable`, `ImplicitUsings: enable`, `TreatWarningsAsErrors: true` apply to every project via `Directory.Build.props`.
- The output assembly of `Specforge.Mcp` is named `specforge` (not `Specforge.Mcp`) so the published binary is `specforge.exe`.
- Central package management is in effect: project files contain no `<PackageReference>` with an inline `Version=` attribute; all versions live in `Directory.Packages.props`.
- The `global.json` SDK pin uses `"rollForward": "latestFeature"` per DEC-003.

## Code Scope

**In scope (created by this item):**

- `D:\Work\specforge\Specforge.sln`
- `D:\Work\specforge\Directory.Build.props`
- `D:\Work\specforge\Directory.Packages.props`
- `D:\Work\specforge\global.json`
- `D:\Work\specforge\src\Specforge.Core\Specforge.Core.csproj`
- `D:\Work\specforge\src\Specforge.Core\AssemblyInfo.cs` (or a `Placeholder.cs` ensuring the assembly compiles)
- `D:\Work\specforge\src\Specforge.Mcp\Specforge.Mcp.csproj` (with `<AssemblyName>specforge</AssemblyName>`, `<OutputType>Exe</OutputType>`)
- `D:\Work\specforge\src\Specforge.Mcp\Program.cs` (minimum entry point — `return 0` is sufficient; will be rewritten by ITEM-005 onward)
- `D:\Work\specforge\test\Specforge.Tests\Specforge.Tests.csproj`
- `D:\Work\specforge\test\Specforge.Tests\Architecture\BoundaryTests.cs`
- `D:\Work\specforge\.gitignore` (created if absent; if present, amend to add `bin/`, `obj/`, `*.user`, etc.)

**Out of scope (owned by later items):**

- Any class in `Specforge.Core` carrying domain logic.
- Any MCP host wiring in `Specforge.Mcp` (the official MCP C# SDK is **not yet referenced**; it is added by the first item that actually registers a tool — likely ITEM-005 or ITEM-006).
- Any embedded-resource `<EmbeddedResource>` glob in `Specforge.Core.csproj` (added by ITEM-004).
- Markdig, YamlDotNet, MS.Extensions.Logging packages — added by the items that first use them (ITEM-002 brings Markdig + logging; ITEM-004 brings YamlDotNet).
- Any test beyond the boundary architecture test.

## Test Scope

One test class, two test cases:

- `BoundaryTests.SpecforgeCore_ReferencesNo_McpAssemblies` — loads the compiled `Specforge.Core.dll` via metadata reflection (`Assembly.GetReferencedAssemblies()` against a `MetadataLoadContext` rooted at the test bin dir) and asserts no referenced assembly's `Name` starts with `ModelContextProtocol`.
- `BoundaryTests.SpecforgeCore_ReferencesNo_HttpHosting` — same shape but asserts no reference to `Microsoft.AspNetCore.*` or `Microsoft.Extensions.Hosting`. DEC-003 explicitly forbids these in Core; the test catches drift early.

Test infrastructure:

- xUnit only (no FluentAssertions in MVP per DEC-003 baseline-library decision unless explicitly added later).
- Test category attribute is not used yet (specforge has no test-category taxonomy — only the RavenDB target project does).
- Tests run via standard `dotnet test`; no special runner flags.

## Test Plan

1. Implement the two test methods in `BoundaryTests.cs`.
2. Run `dotnet test test/Specforge.Tests -c Release` locally; both assertions must pass.
3. Add a deliberate negative test in a feature branch (temporarily reference `ModelContextProtocol` from `Specforge.Core`); confirm `BoundaryTests.SpecforgeCore_ReferencesNo_McpAssemblies` fails with a clear message naming the violating reference. Revert.
4. Confirm `dotnet build Specforge.sln -c Release` reports zero warnings on a clean checkout.
5. Run the publish command from "Target Behavior" item 3; confirm `D:\Work\specforge\bin\specforge.exe` exists and is launchable (`& "D:\Work\specforge\bin\specforge.exe"` returns immediately).

## Test Evidence

Captured into the commits ledger as the implementation lands:

- `dotnet build` output excerpt confirming zero warnings.
- `dotnet test` output excerpt showing both architecture tests passing.
- File system snapshot showing `D:\Work\specforge\bin\specforge.exe` after publish (size + last-modified timestamp suffice).
- Deliberate-negative-test commit hash (on a feature branch, not merged) demonstrating the architecture test catches drift.

## Impact Assessment

| Aspect | Impact | Notes |
|---|---|---|
| Distribution | Direct | Creates the publishable binary that DEC-001 registers. |
| Build pipeline | Direct | Single `dotnet build` command for the whole solution. |
| Core library boundary | Direct | Architecture test guards DEC-003's Core/Mcp invariant from inception. |
| Runtime requirements | Direct | `global.json` pins .NET 10 LTS SDK; published binary is self-contained. |
| MCP tool surface | No impact | Tools come in later items; ITEM-001 ships zero tool implementations. |
| Configuration discovery | No impact | DEC-002 mechanics implemented by ITEM-002. |
| Schema versioning | No impact | DEC-006 mechanics implemented by ITEM-002. |
| ID scheme | No impact | DEC-004 mechanics implemented by ITEM-003. |
| Skill packaging | No impact | DEC-005 mechanics implemented by ITEM-004/005. |
| Error handling | No impact | DEC-007 envelope implemented by ITEM-011. |
| Test coverage scope | Direct | Test project scaffolded; first tests authored (boundary architecture). |
| Documentation | Indirect | Top-level `README.md` should gain a "Build" section once this item lands (handled in ITEM-013). |
| Maintenance burden | Indirect | Adds a `.sln`, three `.csproj` files, three project-level config files to the repo. Trivial maintenance after creation. |
| Performance | No measurable | Build time only. |
| Concurrency | No measurable | Single-process build. |
| External adoption | No impact | This item produces specforge's own binary, not any target-project artifact. |

## Validation

- **Build**: `dotnet build Specforge.sln -c Release` exits 0 with zero warnings.
- **Test**: `dotnet test test/Specforge.Tests -c Release` exits 0 with both architecture tests passing.
- **Publish**: the canonical publish command produces `D:\Work\specforge\bin\specforge.exe`.
- **Layout**: `Specforge.sln`, `Directory.Build.props`, `Directory.Packages.props`, `global.json` all exist at repo root. `src/Specforge.Core/`, `src/Specforge.Mcp/`, `test/Specforge.Tests/` directories all exist with their `.csproj` files.
- **Boundary**: `Specforge.Core.csproj` contains no `<ProjectReference>` and no `<PackageReference>` referencing any `ModelContextProtocol*` package.
- **AssemblyName**: `Specforge.Mcp.csproj` contains `<AssemblyName>specforge</AssemblyName>`.
- **Dogfood (cross-check with DEC-002 mechanics, not implemented yet)**: when `ITEM-002` later adds the config parser to `Specforge.Core`, the architecture test continues to pass — confirming the parser does not accidentally pull in MCP transitively.

## Open Questions

None expected. DEC-001/003/007 lock every architectural choice ITEM-001 needs. Implementation-time questions about specific package versions or csproj details defer to the items that first introduce those packages (ITEM-002 onward). If a genuinely unanticipated fork emerges during implementation, raise it as a new DEC or item amendment rather than improvising.

## Done Criteria

The item is **Done** (post-Approved) when:

1. All files listed in "Code Scope (In scope)" exist at the specified paths in the repository.
2. `dotnet build Specforge.sln -c Release` from the repo root reports zero warnings.
3. `dotnet test test/Specforge.Tests -c Release` runs both architecture tests and both pass.
4. The canonical publish command produces `D:\Work\specforge\bin\specforge.exe`.
5. A `CMT-NNN` row is appended to `ledger/commits.md` (which transitions from `Placeholder` to `Draft` on first append per DEC-007) recording the bootstrap commit's short SHA.
6. The corresponding history event is appended to `ledger/history.md` recording the transition.

## Links

- `../decisions/DEC-001-DISTRIBUTION-AND-TRANSPORT.md` (approved) — section "Distribution Format", "Registration with MCP Host".
- `../decisions/DEC-003-RUNTIME-AND-ARCHITECTURE.md` (approved) — sections "Runtime", "Solution and Project Layout", "Dependency Direction", "Publishing", "Baseline Libraries".
- `../decisions/DEC-007-MVP-TOOL-SET.md` (approved) — section "MVP Tool Catalog" (informs that `Specforge.Mcp` will host 21 tools; none implemented here).
- `../../../templates/item_spec.md` — item template structure.
- `../../../shared/spec_item_contract.md` — required-section contract.
- `../../../shared/document_lifecycle.md` — status states this item moves through.
- `../../../shared/impact_assessment_checklist.md` — aspects checklist consulted for the Impact Assessment table above.

# Commit Ledger — specforge-mvp

Status: Draft

Append-only commit / push provenance. Promoted from Placeholder → Draft on 2026-05-29 when the first implementation commit (ITEM-001) landed.

| LedgerId | Commit SHA | Linked artifacts/items | Branch | Date | Note |
|---|---|---|---|---|---|
| `CMT-001` | `13a306d` | `ART-ITEM-001` | `master` | 2026-05-29 | ITEM-001 solution bootstrap. Initial repo commit: full Stage 0/1 spec + 3-project .NET 10 scaffold (Specforge.Core, Specforge.Mcp [AssemblyName=specforge], Specforge.Tests) + Directory.Build.props/Packages.props + global.json + xUnit architecture boundary test. Build 0 warnings; 2 tests pass; publish produces `bin/specforge.exe`. Deliberate-negative-test confirmed the boundary test catches drift (reverted). |
| `CMT-002` | `284e3be` | `ART-ITEM-002` | `master` | 2026-05-29 | ITEM-002 config model + MCP host + first 3 tools (`list_packages`, `use_package`, `info`). Core config pipeline (discovery/parse/validate/version-gate per DEC-002/006), 5 typed exceptions, `SessionState`, `BinaryInfo`. Mcp stdio host on `ModelContextProtocol.Core` 1.3.0 (low-level surface chosen over the `ModelContextProtocol` meta-package to avoid a `Microsoft.Extensions.Hosting` dependency, per DEC-003), `IMcpTool` pattern, `ToolExceptionMapper` (5 codes + `internal_error`), stderr-only logging. Dogfood `.specforge.json`; smoke evidence `itm002-smoke.txt`. Build 0 warnings; 35 tests pass. |
| `CMT-003` | `0605d29` | `ART-ITEM-003` | `master` | 2026-05-29 | ITEM-003 identifier infrastructure (Core-only): `IdParser`/`IdRegexes` (4 DEC-004 forms), `IdValidator`, `TitleSlug`, `KindRegistry` (reserved-kind check), `IIdAllocator`/`IdAllocator` + `ILedgerReader`/`LedgerReader`. 3 new typed exceptions (`specforge.id.*`) + mapper arms. Retrofit: `ConfigValidator.EnforceReservedKinds` rejects reserved `extraKinds` at load time. Build 0 warnings; 114 tests pass (79 new). Evidence `itm003-parser.txt`. |

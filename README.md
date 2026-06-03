# specforge

Spec-driven development MCP server for AI-assisted .NET architecture work.

## What is specforge?

specforge is an MCP server that turns architecture work into a reviewable spec graph — decision records, item specs, and append-only ledger files — and dogfoods its own MVP through exactly that structure under `spec/`. It is consumed through an MCP-aware host (Claude Code or Codex CLI), not as a standalone CLI. It exposes 21 tools across five families: decisions, items, ledger, validate, and setup.

## Quick start

See `docs/getting-started.md` for the full walkthrough — install, MCP registration, and the bootstrap quartet (`init` → `install_skills` → `use_package` → `validate`). For the impatient:

Publish the binary (PowerShell). This bare form is the fast path for a machine that already has the .NET 10 runtime; the guide's [build-from-source](docs/getting-started.md) section gives the full `-r win-x64 --self-contained true -p:PublishSingleFile=true` flags for a portable single-file build:

```powershell
dotnet publish src/Specforge.Mcp/Specforge.Mcp.csproj -c Release -o D:\Work\specforge\bin\
```

Register it once, user-wide, in your host's MCP config (`~/.claude/.mcp.json` for Claude Code):

```json
{
  "mcpServers": {
    "specforge": { "command": "D:\\Work\\specforge\\bin\\specforge.exe", "args": [] }
  }
}
```

Then open the project in your host and run `init` to bootstrap. The full sequence is in the getting-started guide.

## Requirements

- **Windows x64** only for the MVP.
- **.NET SDK 10.0.201** (compatible 10.0.x feature band per `global.json`) — to build from source.
- **Claude Code or Codex CLI** — an MCP-aware host to call the tools.

## Documentation

- `docs/getting-started.md` — install, register, bootstrap, author your first decision and item, validate, and the upgrade ritual.
- `spec/packages/specforge-mvp/decisions/` — architecture decisions (the `DEC-NNN` records).
- `spec/packages/specforge-mvp/items/` — item specs (the `ITEM-NNN` implementation slices).
- `spec/shared/` — lifecycle policy, glossary, spec-item contract, and impact-assessment checklist.

## License

License selection is out of MVP scope and tracked separately. _(placeholder)_

## Status

MVP under development (Stage 1 spec complete; Stage 2 implementation in progress).

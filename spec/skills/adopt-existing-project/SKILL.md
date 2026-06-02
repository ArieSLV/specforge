---
name: adopt-existing-project
description: Use the first time a project starts using specforge. Walks init, install_skills, and the first validate to bring an empty repo into a known-clean baseline.
---

Use this skill when bringing specforge into a project for the first time. It walks the bootstrap quartet: `init` to write the configuration and project-level instruction layer (optionally scaffolding a spec directory layout), `install_skills` to deploy the MVP skills at user-wide scope, and a first `validate` run to confirm the empty repo is a clean baseline. After this, `draft-decision` is the natural next skill.

## When to use

- A fresh project has not yet been bootstrapped with a specforge configuration.
- The user explicitly asks to onboard the project.
- A first-time installation of the specforge MCP binary.

## Steps

1. Run `info` to confirm the binary is reachable and to read its version and supported schema-version range.
2. Call `init` with `dryRun=true` and `scaffold=true` to preview the planned changes.
3. Surface the preview to the user; confirm scope (which packages, which agents' behavioral files).
4. Call `init` with `dryRun=false` to write the files. Outputs include the configuration file, an optional spec scaffold, the generated instruction document, and the tagged blocks per the user's selection.
5. Call `install_skills` with `dryRun=true` to preview the per-agent skill target paths.
6. Call `install_skills` with `dryRun=false` to deploy the skills. Reinstall is always-overwrite and therefore safe to re-run.
7. Call `list_packages` to confirm the new package is recognized.
8. Call `use_package` to make the new package active.
9. Invoke the `validate-spec-graph` skill (`aspect=all`) to confirm a zero-error baseline.
10. Tell the user the baseline is clean and that `draft-decision` is the next step to record the first foundation decision.

## Tools used

- `info`
- `init`
- `install_skills`
- `list_packages`
- `use_package`
- `validate`

## Shared docs referenced

- (none — this skill bootstraps, so the shared docs are not present until `init` with `scaffold=true` writes them)

## Cross-skill references

- `validate-spec-graph`
- `draft-decision`

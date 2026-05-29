# Standalone Item Spec Contract

Status: Draft

Each file under `items/` in any package MUST be self-contained enough that a new AI session can act on it given only:

- the item spec file itself
- the target project's checked-out repository
- the shared material under `spec/shared/`
- relevant approved decisions

The item spec MUST NOT depend on chat history or undocumented assumptions.

## Required Sections

```text
# <Item ID> - <Title>

Status:
Review owner:
Depends on:
Updates ledger rows:

## Handoff Summary
## Problem Slice
## Terminology Used
## Approved Decisions
## Current Code State
## Target Behavior
## Invariants
## Code Scope
## Test Scope
## Test Plan
## Test Evidence
## Impact Assessment
## Validation
## Open Questions
## Done Criteria
## Links
```

## Section Notes

- **Handoff Summary**: One paragraph plus a short bullet list, written as if briefing a colleague who has read nothing but the title.
- **Problem Slice**: A precise narrowing of which engineering question this item resolves and which it explicitly does not.
- **Terminology Used**: Lists glossary terms that carry weight in this slice.
- **Approved Decisions**: Direct links to DEC-* records that govern this item.
- **Current Code State**: What exists in the repository today, with stable locators.
- **Target Behavior**: What the code must do after the item is implemented.
- **Invariants**: Conditions that must hold during and after implementation.
- **Code Scope**: Files in scope. Files explicitly out of scope.
- **Test Scope / Plan / Evidence**: What is tested, how, where evidence lives.
- **Impact Assessment**: Per the checklist; only non-`No impact` aspects.
- **Validation**: How completion is verified (commands, manual checks, dogfood scenarios).
- **Open Questions**: Items needing a separate decision before this item can be `Approved`.
- **Done Criteria**: Explicit conditions for the item to move from `Approved` to `Done`.
- **Links**: All stable references — to decisions, related items, source files, test files, ledger rows.

## Reference Rule

Item specs MUST use stable locators per `document_lifecycle.md` "Stable Reference Rule":

- Stable IDs preferred over file paths.
- File paths preferred over line numbers.
- Line numbers MUST NOT be the only locator.

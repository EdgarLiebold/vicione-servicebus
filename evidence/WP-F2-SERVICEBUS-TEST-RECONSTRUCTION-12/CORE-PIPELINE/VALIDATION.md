# Core pipeline replacement validation

## Frozen subject

- Final technical commit: `ea262b869a67b3b095ef064653eeb3b1e0a5a900`
- Final technical tree: `b4e6129795f800d7e5b1667e84d9245e7631b2ad`
- Branch: `test/servicebus-xunit4-mtp2-a-plus-v2`
- The local and remote branch both pointed to this commit before evidence generation.
- Evidence generation did not modify product, test, build, package, solution, or workflow inputs.

## Inherited closure

- The technical replacement removed exactly the 13 inherited files under
  `tests/ViciOne.ServiceBus.Tests/Pipeline/`; the empty directory was removed.
- Git history retains every retired byte.
- `INHERITED_BEHAVIOR_DISPOSITION.json` contains exactly 34 unique rows,
  `OBL-R0-CORE-B-0426` through `OBL-R0-CORE-B-0459`, without a gap or duplicate.
- Every row has disposition `REPLACED_EXECUTING`, a native requirement variant, a concrete test
  method, and profile `UnitArchitecture`.
- The replacement is semantic, not line-for-line: deterministic state and causality assertions
  replace inherited timing and fixture coupling.

## Stationary positive gates

All commands ran locally from the repository root against the frozen technical tree with .NET SDK
10.0.302. The exact argument vectors and raw hashes are bound by `POSITIVE_EXECUTION.json`.

| Gate | Result | Bound artifacts |
|---|---:|---|
| Unit solution locked restore | exit 0 | `final-unit-locked-restore.binlog` |
| Engineering Release build, non-incremental | exit 0; 0 warnings; 0 errors | `final-engineering-build.binlog` |
| Native Unit/Architecture solution | 1735/1735 passed; 0 failed; 0 skipped | 13 files under `positive/` |
| LocalIntegration with run-scoped PostgreSQL and Azurite | 17/17 passed; 0 failed; 0 skipped | 3 files under `local-integration/` |

The unit run is the native xUnit/MTP solution and does not execute the retired VSTest fixture.
LocalIntegration is a separate profile and does not substitute for any hermetic core assertion.

## Negative proof

`MUTATION_RECIPES.json`, `MUTATION_EXECUTION.json`, `MUTATION_MANIFEST.md`, and the 16 raw CTRF
files under `mutations/` form one reproducible mutation proof. Fifteen mutants change product
behavior and one removes a requirement-projection row. All 16 built successfully and were killed by
their intended native tests with test exit code `2`; no mutation survived and no test was skipped.

The matrix covers constructor and collaborator boundaries, synchronous and asynchronous filtering,
both retry-cancellation locations, exact transaction options, active/inactive context ownership,
external ownership, commit, rollback, null factory results, both async-flow overloads, negative
timeout validation, and fail-closed requirement projection.

## Workflow state

All four GitHub Actions workflows are manually disabled at repository level while the local test
reconstruction continues. Their versioned YAML remains in Git. This operational pause is not used
as test evidence and does not weaken any local gate; re-enablement and hosted-CI qualification are a
later explicit step after the local native suite is complete.

# RabbitMQ address validation

## Frozen subject

- Final technical commit: `5fd962f927c7f0fe8de0d3240f2a3129449ef41b`
- Final technical tree: `d8f50b9995b8a457067396d49c7c4969acf0e9aa`
- Branch: `test/servicebus-xunit4-mtp2-a-plus-v2`

The final technical commit contains every transport, test, build and documentation correction.
Final gates ran with source, test, project and build inputs byte-identical to this commit; only the
generated evidence outputs were uncommitted while those commands ran. No product behavior, timeout,
retry or assertion was relaxed.

## Inherited closure

- Retired fixture SHA-256 before deletion: `9264bfe2e2e763ba683b7bd1afc6473a4d3d18a9090105454c7f21751843ba48`
- Frozen R0 ledger SHA-256: `5b7134255db42f7fdb4276598b4790660059d006e13bd184db8fe5cf6bf51a66`
- `INHERITED_BEHAVIOR_DISPOSITION.json` maps exactly 46 unique obligations,
  `OBL-R0-BRK-0215` through `OBL-R0-BRK-0260`, without gaps, duplicates or unrelated IDs.
- The retired fixture remains byte-recoverable from Git history.

## Stationary positive gates

All final commands ran from the repository root against the final technical tree with .NET SDK
`10.0.302`. Builds were serial and build servers were disabled. Test profiles used the repository's
native xUnit 4 / MTP 2 command form.

| Gate | Exact result | Raw evidence |
|---|---:|---|
| Unit solution locked restore | exit 0; all locked graphs current | `final-locked-restore.txt` |
| Engineering Release build | exit 0; 0 warnings; 0 errors | `final-engineering-build.txt` |
| RabbitMQ executable, unfiltered | 107/107 passed; 0 failed; 0 skipped | `final-rabbitmq-test.json` |
| UnitArchitecture solution, unfiltered and serial by module | 1698/1698 passed; 0 failed; 0 skipped | `final-unit-test.txt` |
| LocalIntegration with run-scoped PostgreSQL and Azurite | 17/17 passed; 0 failed; 0 skipped | `final-local-integration-test.txt` |
| Focused Saga causal barrier | 10/10 consecutive invocations passed | command verdict |
| Changed-file formatting and `git diff --check` | exit 0 | command verdict |
| Generated Apache 2.0 section 4(b) change list | 7647/7647 entries matched | command verdict |

The RabbitMQ cohort itself is hermetic and opens no broker, socket or container. LocalIntegration is
the separate repository-wide required profile and uses the canonical fixture runner; it does not
stand in for address behavior.

## Product-path corrections

- Query parsing preserves everything after the first raw `=` and decodes it exactly once at the
  owning address boundary.
- Short queue and exchange names decode, validate and render symmetrically, including Unicode.
- TTL reaches RabbitMQ as a numeric AMQP queue argument.
- Topology destination addresses use the final configured host for both public overloads and retain
  no temporary `localhost` snapshot. Public and formatter-produced exchange names remain data:
  address syntax cannot silently become topology configuration.
- Every direct-constructor entity-name input has the same character and UTF-8 byte-limit contract.
- Telemetry waits for the exact provider-backed idle deadline and proves the just-before boundary.
- Saga observation waits on actual consumption before reading state written during `Consume`.

## Negative proof

`MUTATION_MANIFEST.md` binds 17 product mutants and one requirement-projection sabotage.
`MUTATION_RECIPES.json` makes every edit byte-reproducible from the final technical tree: exact
target, baseline hash, unique old/new bytes, occurrence and mutant hash. Every recipe resolves to
its declared mutant hash. `MUTATION_EXECUTION.json` binds the exact build and native MTP argument
vectors, exit codes and raw-result hashes from one worktree detached at that commit. Every test
command exited `2` for its intended assertion and no mutation survived. Old prose-only mutation
logs from rejected candidates remain only in Git history.

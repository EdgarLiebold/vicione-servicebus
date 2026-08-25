# RabbitMQ address validation

## Frozen subject

- Final review commit: `838c883500a2290e871e686a34a6c93f5e9dd215`
- Final review tree: `73a8970def7491f06604a94df104bdeeef40e162`
- Final executable commit: `8939c04827a4e35ef37561acbabd704a9ba910dc`
- Final executable tree: `e74d17e781120315a32a4f1432f30dba611244d0`
- RabbitMQ correction parent: `03b375879f887a2bdab4f378fbad12b24d3db668`
- Branch: `test/servicebus-xunit4-mtp2-a-plus-v2`

The RabbitMQ correction parent contains all transport product, test, build, documentation and
disposition corrections. The final executable commit adds one causal consumption barrier to a
pre-existing Saga test that an additional unfiltered diagnostic run exposed. No product behavior,
timeout, retry or assertion was relaxed. The focused Saga test passed 10/10 consecutive runs after
the correction. The final review commit changes only `.testagent/status.md` to record these completed
facts; its executable graph is byte-identical to the validated commit.

## Inherited closure

- Retired fixture SHA-256 before deletion: `9264bfe2e2e763ba683b7bd1afc6473a4d3d18a9090105454c7f21751843ba48`
- Frozen R0 ledger SHA-256: `5b7134255db42f7fdb4276598b4790660059d006e13bd184db8fe5cf6bf51a66`
- `INHERITED_BEHAVIOR_DISPOSITION.json` maps exactly 46 unique obligations,
  `OBL-R0-BRK-0215` through `OBL-R0-BRK-0260`, without gaps, duplicates or unrelated IDs.
- The retired fixture remains byte-recoverable from Git history.

## Stationary positive gates

All final commands ran from the repository root on the final executable commit with .NET SDK
`10.0.302`. Builds were serial and build servers were disabled. Test profiles used the repository's
native xUnit 4 / MTP 2 command form. The subsequent review commit changed no executable input.

| Gate | Exact result | Raw evidence |
|---|---:|---|
| Unit solution locked restore | exit 0; all locked graphs current | `final-locked-restore.txt` |
| Engineering Release build | exit 0; 0 warnings; 0 errors | `final-engineering-build.txt` |
| RabbitMQ executable, unfiltered | 105/105 passed; 0 failed; 0 skipped | `final-rabbitmq-test.json` |
| UnitArchitecture solution, unfiltered and serial by module | 1696/1696 passed; 0 failed; 0 skipped | `final-unit-test.txt` |
| LocalIntegration with run-scoped PostgreSQL and Azurite | 17/17 passed; 0 failed; 0 skipped | `final-local-integration-test.txt` |
| Focused Saga causal barrier | 10/10 consecutive invocations passed | command verdict |
| Changed-file formatting and `git diff --check` | exit 0 | command verdict |
| Generated Apache 2.0 section 4(b) change list | 7642/7642 entries matched | command verdict |

The RabbitMQ cohort itself is hermetic and opens no broker, socket or container. LocalIntegration is
the separate repository-wide required profile and uses the canonical fixture runner; it does not
stand in for address behavior.

## Product-path corrections

- Query parsing preserves everything after the first raw `=` and decodes it exactly once at the
  owning address boundary.
- Short queue and exchange names decode, validate and render symmetrically, including Unicode.
- TTL reaches RabbitMQ as a numeric AMQP queue argument.
- Topology destination addresses use the final configured host for both public overloads and retain
  no temporary `localhost` snapshot.
- Every direct-constructor entity-name input has the same character and UTF-8 byte-limit contract.
- Telemetry waits for the exact provider-backed idle deadline and proves the just-before boundary.
- Saga observation waits on actual consumption before reading state written during `Consume`.

## Negative proof

`MUTATION_MANIFEST.md` binds 14 product mutants and one requirement-projection sabotage. Each entry
contains the baseline hash, exact mutation, mutant hash, native filter, minimum count, raw CTRF hash
and post-restore contract. Every test command exited `2` for its intended assertion and no mutation
survived. Old prose-only mutation logs from the rejected candidate were removed; Git history retains
them, but they are not active evidence.

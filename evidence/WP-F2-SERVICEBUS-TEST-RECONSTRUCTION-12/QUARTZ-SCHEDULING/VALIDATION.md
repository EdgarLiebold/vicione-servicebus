# Quartz scheduling reconstruction validation

## Frozen technical subject

- Technical commit: `4f83f3b47f812e1b3d7589bcb98b8d9abd5c549a`
- Technical tree: `cfabc206368d3ac8da81ce434112a4f78e51ddaa`
- Parent: `debba8919088959d85fe43f4033932f632e6506b`
- Runtime: .NET SDK `10.0.302`
- Test stack: xUnit 4 on Microsoft Testing Platform 2
- Configuration: `Release`

The parent contains the complete Quartz reconstruction. The technical commit strengthens one
Cancellation causality test after the first mutation attempt proved that the original end-to-end
carrier did not exercise the `RedeliveryRetryFilter` boundary directly enough. No product code was
changed by that correction.

## Positive verdicts

| Gate | Verdict |
|---|---|
| Locked restore of `ViciOne.ServiceBus.Engineering.slnx` | exit 0 |
| Engineering Release build | exit 0, 0 warnings, 0 errors |
| UnitArchitecture | 1822/1822 passed, 0 failed, 0 skipped |
| Quartz focused | 85/85 passed, 0 failed, 0 skipped |
| LocalIntegration with run-scoped PostgreSQL and Azurite | 17/17 passed, 0 failed, 0 skipped |

`POSITIVE_EXECUTION.json` binds the exact commands, floors, results and raw-artifact hashes. The
broker runner generated credentials per run, published only loopback endpoints to the child process,
collected the broker logs under ignored `artifacts/run-output`, and removed the fixtures after the
test verdict.

## Inherited behavior closure

`INHERITED_BEHAVIOR_DISPOSITION.json` contains exactly the 87 inherited obligations
`OBL-R0-PER-0200` through `OBL-R0-PER-0286`, without duplicates:

- 85 `REPLACED_EXECUTING` obligations;
- 1 `UPSTREAM_FRAMEWORK_COMPATIBILITY` obligation for pure Quartz.NET behavior;
- 1 `INVALID_DUPLICATE_RETIRED` obligation whose source did not implement the behavior claimed by
  its name.

`REQUIREMENT_PROJECTION.json` is the independent 64-entry requirement/variant-to-test-method
projection. Theory expansion produces the 85 executed Quartz cases. The focused CTRF is the runtime
proof; the two JSON files are the semantic and inherited-source closure.

## Mutation verdicts

Twelve byte-exact, one-cause mutations were built successfully and killed by their intended owner
tests. Each raw CTRF has exit code 2, at least one causally failed assertion and zero skipped tests.

| ID | Injected defect | Causal result |
|---|---|---|
| M01 | Replace the configured `TimeProvider` with system time | configured-time assertion failed |
| M02 | Remove every recurring prefix instead of only the leading prefix | exact schedule ID failed |
| M03 | Replace the immutable queue snapshot with the default queue | settings identity failed |
| M04 | Override the application-owned `TimeProvider` during registration | exact DI instance failed |
| M05 | Cast serialized metadata instead of deserializing it | metadata reconstruction failed |
| M06 | Omit explicit trace headers at the raw scheduling boundary | raw-JSON trace continuity failed |
| M07 | Treat an equal but unrequested cancellation token as caller cancellation | redelivery timed out |
| M08 | Cast serialized transport properties instead of deserializing them | property reconstruction failed |
| M09 | Replace the immutable hosted-service snapshot with a constant | hosted setting failed |
| M10 | Replace the immutable endpoint queue snapshot with the default queue | endpoint name failed |
| M11 | Accept a zero prefetch count at the public boundary | exact validation contract failed |
| M12 | Restore the process-global durable-job cache | deleted job was not recreated |

`MUTATION_EVIDENCE.json` is the byte-level authority. For every mutation it binds the target,
baseline hash, exact old and new bytes, occurrence count/index, mutant hash, complete build and test
argument vectors, exit codes, result counts and raw CTRF hash. The disposable worktree was restored
after every run and ended clean at the frozen technical commit.

## Environment diagnosis

The first locked restore in the filesystem sandbox stalled during package graph evaluation. The
same locked command completed immediately outside the sandbox. Native MTP execution is likewise run
outside that sandbox because its runner IPC has already reproduced an environment-only permission
failure. This is treated as an execution-environment diagnosis, not as a product defect: no package,
MSBuild, runner or source workaround was introduced.

## Verdict

**PASS.** The Quartz product path, native test replacement, inherited behavior closure, exact test
floors, local infrastructure integration and the twelve highest-risk regression boundaries are green
and reproducibly bound to the frozen technical commit.

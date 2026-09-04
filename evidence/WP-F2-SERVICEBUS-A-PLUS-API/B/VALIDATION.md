# Work package B validation

## Reproducible execution environment

Final commands used SDK 10.0.400 and runtime 10.0.11 with a task-scoped CLI home,
`DOTNET_MULTILEVEL_LOOKUP=0`, the existing NuGet package cache, disabled MSBuild node reuse, disabled
build servers, and serial MSBuild for the Engineering graph. Microsoft Testing Platform was executed
outside the filesystem sandbox because sandboxed local IPC fails with `SocketException (13)`.

The first packaged-Journey attempt used the script's system-CLI default and blocked during its third
pack step. The process was terminated without modifying a final artifact. Re-execution with the
task-scoped .NET CLI outside the sandbox completed in approximately 45 seconds. This is the previously
diagnosed repository-specific execution remedy; no source, project, or test was excluded.

## Final acceptance results

| Gate | Result |
|---|---|
| Product Release build, warnings as errors | exit 0; 0 warnings; 0 errors; 55.17 s |
| Engineering Release build, warnings as errors | exit 0; 0 warnings; 0 errors; 1m 45.72 s |
| Unit Release build, warnings as errors | exit 0; 0 warnings; 0 errors; 8.06 s |
| Engineering `dotnet format --verify-no-changes` | exit 0; no changes |
| Unit `dotnet format --verify-no-changes` | exit 0; no changes |
| UnitArchitecture run 1 | 3,500 total; 3,500 passed; 0 failed; 0 skipped; 2m 59.035s |
| UnitArchitecture run 2 | 3,500 total; 3,500 passed; 0 failed; 0 skipped; 2m 53.536s |
| UnitArchitecture run 3 | 3,500 total; 3,500 passed; 0 failed; 0 skipped; 2m 52.362s |
| Developer Journeys | 14 scenarios; 8 freshly packed packages; exit 0 |
| Packed public API baseline | 8 assemblies; 20,812 lines; SHA-256 `3ef4b2dee702570baabead980ecc593126cf0dd21a0516c987fce193b147ff83` |
| Vulnerable packages | exit 0; none in all 22 product projects |
| Product projects with nullable enabled | 25 of 25 |
| Product `#nullable disable` | 0 files |
| Direct product process-clock accesses | 0 |
| Embedded JSON test-reference mismatches | 0 |
| Changes below `review/` | 0 |
| `git diff --check` | exit 0 |

## API inventory

The final inventory covers all 25 named product assemblies with no missing assembly or load error.

| Metric | Actual | Target |
|---|---:|---:|
| Task-like public methods without `Async` | 0 | 0 |
| Non-exempt async public methods without cancellation token | 0 | 0 |
| Cancellation tokens not last | 0 | 0 |
| Cancellation tokens with a nonstandard name | 0 | 0 |
| Classified callback/context exceptions | 721 | explicit classification |
| Public `DateTime` signatures | 0 | 0 |
| Application send shapes | 3 | at most 3 |
| Application publish shapes | 2 | at most 3 |
| `ConsumeContext<T>` members | 24 | at most 25 |
| Consume-context completion shapes | 46 | at most 60 |

Inventory SHA-256:
`b985321505579701aa5e9e9174fe4aede14f8822805934bad19c8aff6c80763a`.

## Complete profile command

The following command was run three times; only the results-directory suffix changed.

```text
dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx -c Release --no-build --no-restore \
  --results-directory artifacts/test-results/a-plus-api-b-run-<n> \
  --minimum-expected-tests 3500 --max-parallel-test-modules 1
```

The hard floor was raised to the observed final count. No test was filtered, skipped, or removed.

## Focused regression closure

- Entity Framework Core: 121/121 passed after composite consume-context proxies and server-bounded,
  SQLite-compatible chronological quarantine ordering were added.
- Quartz 4.0.0: 89/89 passed after advanced send-endpoint test composition and 83 embedded executable
  target identities were synchronized.
- Central core tests: 1,735/1,735 passed after cancellation-order, runtime-object dispatch, DST,
  one-shot scheduling, and deterministic barrier corrections.
- ActiveMQ: 138/138 passed after runtime proxy member names were synchronized.

## Evidence integrity

The bidirectional Red Team report SHA-256 is
`5c020998cef2c7719f4b0e33099bd183a941ddfc6061741ff34cc94fa0c66633`.
The three complete-run log SHA-256 values are recorded by the repository handoff and can be recomputed
with `shasum -a 256 evidence/WP-F2-SERVICEBUS-A-PLUS-API/B/unit-run-*.log`.

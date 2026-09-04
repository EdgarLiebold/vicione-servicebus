# Work package A validation

## Reproducible execution environment

All final .NET commands used SDK 10.0.400 and runtime 10.0.11 with a task-scoped CLI home,
`DOTNET_MULTILEVEL_LOOKUP=0`, the existing user NuGet cache, MSBuild node reuse disabled, and build
servers disabled. The builds and tests were run outside the filesystem sandbox because MSBuild worker
processes repeatedly stalled inside it. Serial MSBuild (`-m:1`) was used for the Engineering graph.

This is the repository-specific remedy for the previously diagnosed local build stall; it is an
execution-environment correction, not a source or test exclusion.

## Final acceptance results

| Gate | Result |
|---|---|
| Locked Engineering restore | exit 0 |
| Product Release build with `-warnaserror` | exit 0; 0 warnings; 0 errors; 5.10 s |
| Engineering Release build with `-warnaserror` | exit 0; 0 warnings; 0 errors; 14.98 s |
| Unit Release build with `-warnaserror` | exit 0; 0 warnings; 0 errors; 9.49 s |
| Engineering `dotnet format --verify-no-changes` | exit 0; no source changes |
| Unit `dotnet format --verify-no-changes` | exit 0; no source changes |
| Unit run 1 | 3,496 total; 3,496 passed; 0 failed; 0 skipped; 3m 29.114s |
| Unit run 2 | 3,496 total; 3,496 passed; 0 failed; 0 skipped; 3m 47.797s |
| Unit run 3 | 3,496 total; 3,496 passed; 0 failed; 0 skipped; 3m 27.328s |
| Vulnerable packages | exit 0; none in all 22 product projects |
| Normalized public API inventory | before and after SHA-256 both `d55ddd98ce68d0429bc9ea0cdd50e0e03bd77e8510b4a5094c6e4f7287f9ddc7` |
| Forbidden comment vocabulary | 0 lines |
| Product `#nullable disable` | 0 files |
| `.testagent` directories | 0 |
| `git diff --check` | exit 0 |

The formatter emitted its generic workspace-load warning while returning success. Diagnostic review
identified the build-only analyzer project reference for `ViciOne.ServiceBus.Analyzers.csproj`; all
projects loaded for formatting, both verification commands returned zero, and neither changed a file.

The macOS test host also printed `MallocStackLogging` environment notices between several module
processes. They did not originate from compilation, did not change any module verdict, and all three
aggregate processes returned zero.

## Unit command

The following command was run three times. Only the results-directory suffix changed from 1 to 3.

```text
dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx -c Release --no-build --no-restore \
  --results-directory evidence/WP-F2-SERVICEBUS-A-PLUS-API/A/final-run-<n> \
  --minimum-expected-tests 3496 --max-parallel-test-modules 1
```

No test was filtered, skipped, or removed. The minimum was raised from the assignment floor of 3,490
to the observed current count of 3,496.

## Public API inventory

The final inventory was generated after the last product build. It covers 22 product assemblies with
no missing assembly and no load error. Aggregate values are 4,077 public types, 4,000 top-level public
types, 77 nested public types, 1,326 interfaces, 1,777 extension methods, 2,847 asynchronous methods,
1,192 root-namespace public types, 1,474 root-namespace extension methods, 38 send shapes, 20 publish
shapes, 54 declared consume-context members, and 273 consume-context completion shapes.

The runtime-specific `generatedAtUtc` and `binRoot` fields were removed before canonical JSON sorting
and hashing. The remaining before/after documents are byte-identical. The broader baseline differs
from the narrower figures quoted by the assignment because the repository tool includes nested public
types and the complete declared application surface; the same checked-in tool produced both sides.

## Failed diagnostic runs retained in the account

The first pre-final full run exposed five deterministic projection/path failures plus one failure in
the durable-send consumer-failure scenario. The path and projection defects were corrected. The
durable-send scenario then passed six isolated repetitions and every later complete run; it remains
scheduled for the package-B determinism audit rather than being hidden as an accepted failure.

After those corrections, three full runs passed. They were deliberately invalidated when the outbox
oracle was strengthened to prove that the pending collection is empty after the first drain.

The next full run exposed the saga repository barrier race documented in `DEVIATIONS.md`: 3,495 of
3,496 passed and the cancel observation timed out. That run was not counted. After the deterministic
task-state barrier was added, the affected test passed ten isolated repetitions and the three final
complete runs recorded above.

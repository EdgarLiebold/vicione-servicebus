# Work package F validation

## Reproducible execution environment

Final commands used .NET SDK 10.0.400 and runtime 10.0.11 with a task-scoped CLI home,
`DOTNET_MULTILEVEL_LOOKUP=0`, the existing NuGet cache, disabled MSBuild node reuse, disabled build
servers, and serial MSBuild. Restore, Microsoft Testing Platform, Roslyn format, package-consumer, API-
inventory, and vulnerability commands ran outside the filesystem sandbox because the sandbox blocks
the required NuGet/network or local IPC operations.

After the new capability projects and consumer references were introduced, one unlocked restore
updated their dependency locks. The final `--locked-mode --force-evaluate` Engineering restore then
completed with exit 0 in 7.072 seconds.

## Final acceptance results

| Gate | Result |
|---|---|
| Engineering locked restore | exit 0; all lock graphs accepted; 7.072s |
| Product Release build, warnings as errors | exit 0; 0 warnings; 0 errors; 20.05s |
| Engineering Release build, warnings as errors | exit 0; 0 warnings; 0 errors; 24.31s |
| Unit Release build, warnings as errors | exit 0; 0 warnings; 0 errors; 9.00s |
| Engineering `dotnet format --verify-no-changes --severity warn` | exit 0; no file diagnostics |
| Unit `dotnet format --verify-no-changes --severity warn` | exit 0; no file diagnostics |
| Main unit module, serial | 1,858 total; 1,858 passed; 0 failed; 0 skipped; 1m 05.263s |
| Architecture module, serial | 202 total; 202 passed; 0 failed; 0 skipped; 2m 07.931s |
| `IConsumerKind` focused final repetition | 2 total; 2 passed; 0 failed; 0 skipped; 2.280s |
| UnitArchitecture accepted run 1 | 3,703 total; 3,703 passed; 0 failed; 0 skipped; 3m 29.277s |
| UnitArchitecture accepted run 2 | 3,703 total; 3,703 passed; 0 failed; 0 skipped; 3m 39.888s |
| UnitArchitecture accepted run 3 | 3,703 total; 3,703 passed; 0 failed; 0 skipped; 3m 37.547s |
| Seven capability packages | Sagas, Courier, Futures, JobService, Mediator, Initializers, and EntityFrameworkCore.Sagas each packed successfully |
| Developer Journeys | 14 scenarios; 15 freshly packed packages; 0 warnings; 0 errors; exit 0 |
| Packed public API baseline | 13 assemblies; 20,888 lines; SHA-256 `e74e4cedd8955cd31be601acccf32c9e00718b43f07761e5260f82e82caaa79d` |
| SuiteComposition runtime | limits, SQLite reliable messaging, consumer, request, and schedule passed; exit 0 |
| Compiled API inventory | 31 assemblies; no missing assembly; no load error; all enforced convention counts green |
| NuGet vulnerability inventory | 0 direct/transitive findings; 0 unresolved paths; exit 0 |
| `git diff --check` | exit 0 |

The three accepted complete-profile commands were identical except for the results directory:

```text
dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx -c Release --no-build --no-restore \
  --results-directory /private/tmp/vicione-sb-f-unit-run-<n> \
  --minimum-expected-tests 3703 --max-parallel-test-modules 1 --progress off --no-ansi
```

No accepted UnitArchitecture command used a filter. The class-filtered executions are reported only as
focused extension-point and mutation evidence.

## Requirement-to-evidence map

| Requirement | Executing or structural evidence |
|---|---|
| Core consumer remains operational | ordinary consumer tests in the 1,858-test module and all three complete runs |
| Registered kinds drive `ConfigureEndpoints` | custom-kind end-to-end test plus built-in saga, activity, future, and job registration suites |
| Complete third-party kind contract | `ConsumerKindExtensionTests`: name, discovery, explicit-owner precedence, definition, endpoint definition, primary/companion names, configuration, harness observation, and real delivery |
| Unsupported custom observation fails closed | exact negative custom-kind configuration test |
| Core owns no capability type | `CapabilityPackageArchitectureTests.CapabilityTypes_AreOwnedOnlyByTheirDedicatedAssemblies` |
| One-way package graph and EF split | `CapabilityProjects_HaveOnlyTheApprovedProductDependencies` plus all seven package builds |
| SuiteComposition has only three product dependencies | `SuiteComposition_HasExactlyTheApprovedProductDependencies` and runtime loaded-assembly assertion |
| Packages work for real consumers | package-only Developer Journey gate against 15 fresh NUPKGs |
| No convention regression | compiled before/after API inventory and both full format gates |
| No dependency security regression | generated direct/transitive vulnerability inventory |
| Stable complete profile | three consecutive 3,703/3,703 runs with zero skips |

## Compiled API inventory

The before record is the final work-package-E inventory at commit `120714c4`; the after record loads the
seven new assemblies as separate products. Assemblies increase from 24 to 31 and public types from
4,111 to 4,153 because capability boundaries expose their explicit registration surfaces. The enforced
values remain:

- async without suffix: 0;
- async without a cancellation token outside the exception catalog: 0;
- cancellation token not last or wrongly named: 0/0;
- `DateTime` in public signatures: 0;
- public internals, obsolete members, and `EditorBrowsable(Never)`: 0 each;
- extension methods in the application root namespace: 0;
- send/publish shapes: 3/2;
- `ConsumeContext` members/completions: 24/24;
- application-builder members: 11, reduced from 20.

The complete records are `api-inventory-before.json` and `api-inventory-after.json`.

`review/**`, `LICENSE.txt`, `NOTICE`, `COPYRIGHT`, and the package-G `CHANGELIST.md` were not modified or
staged by package F. This evidence makes no claim of independent Red Team or Lead acceptance.

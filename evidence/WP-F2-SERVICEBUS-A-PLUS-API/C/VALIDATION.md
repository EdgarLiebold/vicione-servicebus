# Work package C validation

## Reproducible execution environment

Final commands used SDK 10.0.400 and runtime 10.0.11 with a task-scoped CLI home,
`DOTNET_MULTILEVEL_LOOKUP=0`, the existing NuGet package cache, disabled MSBuild node reuse, disabled
build servers, and serial MSBuild for the Engineering graph. Microsoft Testing Platform was executed
outside the filesystem sandbox because sandboxed local IPC fails with `SocketException (13)`.

One sandboxed package attempt stopped producing progress while evaluating the graph. It was abandoned
without accepting any artifact. The complete pack and Developer Journey gates were rerun outside the
sandbox with the established task-scoped environment and completed successfully. No project, source,
test, or package was excluded.

An early `dotnet test` invocation used VSTest-style argument placement against the Microsoft Testing
Platform solution and discovered zero tests, returning exit code 5. That invocation is discarded and
is not acceptance evidence. Every accepted run used the repository's MTP command form and a hard
minimum expected-test count.

One post-namespace Journey recheck set `DOTNET_ROOT` but omitted the script's explicit `DOTNET_CLI`
selector, so the script started with the installed SDK 10.0.302. It was interrupted after the first
package, its temporary output was discarded, and it is not acceptance evidence. Both accepted final
Journey runs set `DOTNET_CLI=/private/tmp/vicione-dotnet-10-current/dotnet` and used SDK 10.0.400.

## Final acceptance results

| Gate | Result |
|---|---|
| Product Release build, warnings as errors | exit 0; 0 warnings; 0 errors; 4.29 s |
| Engineering Release/no-incremental build, warnings as errors | exit 0; 0 warnings; 0 errors; 2m 54.69 s |
| Unit Release/no-incremental build, warnings as errors | exit 0; 0 warnings; 0 errors; 2m 29.72 s |
| Engineering `dotnet format --verify-no-changes` | exit 0; no changes |
| UnitArchitecture run 1 | 3,502 total; 3,502 passed; 0 failed; 0 skipped; 2m 56.634s |
| UnitArchitecture run 2 | 3,502 total; 3,502 passed; 0 failed; 0 skipped; 2m 49.510s |
| UnitArchitecture run 3 | 3,502 total; 3,502 passed; 0 failed; 0 skipped; 2m 50.262s |
| Post-format complete-profile rebind | 3,502 total; 3,502 passed; 0 failed; 0 skipped; 3m 01.184s |
| Developer Journeys | 14 scenarios; 8 freshly packed packages; 0 warnings; 0 errors; exit 0 |
| Packed public API baseline | 8 assemblies; 20,769 lines; SHA-256 `1a2dd8d8fd7ed98b77b5d920c480356bd162580ef0391dfecc7a06340d68bf71` |
| Canonical package output | 22 packages; filename-bound manifest SHA-256 `c5308fc80a3f2e2bf8818787e63ae754747f933373d0da680af78577b6266181` |
| Namespace migration dry run | 0 rewritten files; 0 renamed entries; exit 0 |
| Public transport selector discovery | 9 source entry points; all in `ViciOne.ServiceBus.Configuration` |
| Product CS1591 suppressions | 0 |
| Changes below `review/` | 0 tracked changes; PO-owned directory remains untracked |
| `git diff --check` | exit 0 |

## API inventory

The final inventory covers all named product assemblies with no missing assembly or load error.

| Metric | Actual | Target |
|---|---:|---:|
| Public types | 4,091 | recorded |
| Top-level / nested public types | 4,022 / 69 | recorded |
| Public interfaces | 1,332 | recorded |
| Public extension methods | 1,866 | recorded |
| Root Application namespace types | 111 | exact baseline |
| `application-api.txt` API entries | 111 | equal to root types |
| Extension methods in root namespace | 0 | at most 120 |
| Public types in `.Internals` namespaces | 0 | 0 |
| Obsolete declarations | 0 | 0 |
| `EditorBrowsable(Never)` declarations | 0 | 0 |
| Application builder members | 20 | at most 20 |
| Task-like public methods without `Async` | 0 | 0 |
| Non-exempt async public methods without cancellation token | 0 | 0 |
| Cancellation tokens not last | 0 | 0 |
| Cancellation-token parameters with a nonstandard name | 0 | 0 |
| Public `DateTime` signatures | 0 | 0 |
| Application send / publish shapes | 3 / 2 | at most 3 / at most 3 |
| `ConsumeContext<T>` members / completion shapes | 24 / 24 | at most 25 / at most 60 |

Inventory SHA-256:
`36721d4ac0ec06b356dc1895a204c8082ba22c0ebac22faa9f5954b32f1451f5`.

Application baseline SHA-256:
`287103e7c178d339bfa7e24bc015ba4578a20d7595a8839d5f12619ae98d28df`.

## Package identities

The accepted output directory contains the core and Abstractions packages; Analyzers, MessagePack,
SignalR, and StateMachineVisualizer; ActiveMq, AmazonSqs, AzureServiceBus, EventHubs, RabbitMq,
SqlTransport.PostgreSql, and SqlTransport.SqlServer; AmazonS3, Azure.Storage, Azure.Table, DynamoDb,
and EntityFrameworkCore; Quartz; plus AzureServiceBus.Testing, EventHubs.Testing, and RabbitMq.Testing.
Every package uses the canonical §3.1 identity.

## Focused architecture closure

- `ApplicationApi_IsExactlyTheVersionedRootNamespaceBaseline` binds the 111 root types to the checked-in baseline.
- `FiveApiLayers_HaveEnforcedDiscoveryAndPackageBoundaries` binds the layer boundaries, builder ceiling, shipping/testing split, and all nine discovered public transport selectors.
- The transport-selector source discovery is pattern-based, so a future public `Using…` or `CreateUsing…` selector outside the Configuration namespace fails the gate.
- The direct CS1591 scan, heritage scan, root-extension scan, migration dry run, and diff check are clean.

## Full-profile command

The following Microsoft Testing Platform command was executed three times; only the results-directory
suffix changed:

```text
dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx -c Release --no-build --no-restore \
  --results-directory artifacts/test-results/a-plus-api-c-run-<n> \
  --minimum-expected-tests 3502 --max-parallel-test-modules 1
```

No test was filtered, skipped, removed, or accepted through a zero-test result.

The formatter changed only import ordering in 34 generated/global-using files and two Azure Service
Bus local-integration tests. The subsequent strict Engineering build, complete-profile rebind, and
fresh reflection inventory bind those non-semantic source changes to the accepted snapshot.

## Evidence integrity

The bidirectional internal Red Team record is
`red-team-async-bidirectional-after-rename.md`, SHA-256
`53d8b40170b19eec2984b2a70d8fd0f25a07818d7c085c54fed7e50f47b6f02f`. It binds the final C# source
aggregate `a3eef2d4dbd6982745cc9469d2de4c2178edf1e397d5c89e3db472f83c316714`. The report is internal
adversarial evidence, not independent acceptance.

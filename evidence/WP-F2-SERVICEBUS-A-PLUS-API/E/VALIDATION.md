# Work package E validation

## Reproducible execution environment

Final commands used .NET SDK 10.0.400 and runtime 10.0.11 with a task-scoped CLI home,
`DOTNET_MULTILEVEL_LOOKUP=0`, the existing NuGet cache, disabled MSBuild node reuse, disabled build
servers, and serial MSBuild. Microsoft Testing Platform, Roslyn, package-consumer, vulnerability, and
Docker-backed commands ran outside the filesystem sandbox because they require reliable local IPC or
network/container access.

The first sandboxed locked restore correctly distinguished stale lockfiles from unavailable NuGet
advisory metadata. Four dependency graphs affected by renamed Testing project references were refreshed
once with `--force-evaluate`; the following `--locked-mode` restore passed.

## Final acceptance results

| Gate | Result |
|---|---|
| Engineering locked restore | exit 0; all projects up to date |
| Product Release build, warnings as errors | exit 0; 0 warnings; 0 errors; 33.79s |
| Engineering Release build, warnings as errors | exit 0; 0 warnings; 0 errors; 1m 36.46s |
| Unit Release build, warnings as errors | exit 0; 0 warnings; 0 errors; 7.55s |
| Engineering `dotnet format --verify-no-changes` | exit 0; no changes |
| Unit `dotnet format --verify-no-changes` | exit 0; no changes |
| UnitArchitecture accepted run 1 | 3,696 total; 3,696 passed; 0 failed; 0 skipped; 3m 17.880s |
| UnitArchitecture accepted run 2 | 3,696 total; 3,696 passed; 0 failed; 0 skipped; 3m 16.293s |
| UnitArchitecture accepted run 3 | 3,696 total; 3,696 passed; 0 failed; 0 skipped; 3m 16.206s |
| Developer Journeys | 14 scenarios; 8 freshly packed packages; 0 warnings; 0 errors; exit 0 |
| Packed public API baseline | 8 assemblies; 20,970 lines; SHA-256 `d37877060e2f4e1524686c2abc378a0083dd17f6a338a17653d5ef510eba0743` |
| RabbitMQ LocalIntegration | real Docker broker; 27/27 passed; 0 skipped; 11.661s |
| SQLite reliable-messaging migration | 3/3 passed; 0 skipped; 1.081s |
| PostgreSQL reliable-messaging migration | real Docker database; 1/1 passed; 0 skipped; 1.253s |
| NuGet vulnerability inventory | 0 direct/transitive findings; 0 unresolved paths; exit 0 |
| Retired entry-point and nullable static gates | 0 matches |
| Process-vocabulary comment gate | 0 matches |
| `git diff --check` | exit 0 |

The three accepted complete-profile commands were identical except for the results directory:

```text
dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx -c Release --no-build --no-restore \
  --results-directory artifacts/test-results/a-plus-api-e-accepted-run-<n> \
  --minimum-expected-tests 3696 --max-parallel-test-modules 1
```

No accepted UnitArchitecture command used a filter. Filtered executions are reported only as focused
mutation or provider evidence.

## Ownership and API gates

The executing architecture inventory finds exactly one durable-row delivery implementation,
`ReliableMessagingDeliveryService<TBus>`, and exactly one public quarantine/operations contract,
`IReliableMessagingOperations<TBus>`. Source and sample scans find zero occurrences of
`UseInMemoryOutbox`, `AddEntityFrameworkOutbox`, or `UseBusOutbox`.

The only public `Use…Scheduler` choices are `UseTransportScheduler`, `UseQuartzScheduler`, and
`UseInMemoryScheduler`, all in `ViciOne.ServiceBus.Configuration` and reachable through the
reliable-messaging configurator. Journeys 06, 07, and 11 compile solely against the freshly packed
unified API.

## Compiled API inventory

The after-inventory loaded every expected product assembly with no missing assembly and no load error.
It reports 4,111 public types, 1,882 extension methods, and 2,894 async methods. The enforced totals are:

- async without suffix: 0;
- async without a cancellation token outside the callback/context exception catalog: 0;
- cancellation token not last: 0;
- cancellation token with a wrong name: 0;
- `DateTime` in public signatures: 0;
- public internals, obsolete members, and `EditorBrowsable(Never)`: 0 each;
- extension methods in the application root namespace: 0;
- send/publish shapes: 3/2;
- `ConsumeContext` members/completions: 24/24;
- application-builder members: 20.

The complete before/after records are `api-inventory-before.json` and `api-inventory-after.json`.

## Durable transport and migration acceptance

The RabbitMQ tests prove that a confirmed result is persistent, mandatory, and publisher-acknowledged;
the stored message survives sender shutdown, unroutable publication raises a return instead of success,
and a cancelled attempt neither publishes nor reports acceptance. A source-bound architecture test
guards the three transport flags and the awaited send-before-result ordering.

The SQLite migration cases validate schema creation, provider SQL, and retained-state mapping. The real
PostgreSQL case starts from legacy outbox/inbox rows, applies the supplied script twice, and verifies
retry, quarantine, due time, identity, inbox attempts, capacity ledger, and source-row preservation.

## Acceptance defects corrected

- The journey gate initially failed after all 14 scenarios built because its reflection tool's
  NativeAOT lock graph depended on the SDK patch. Disabling AOT for the dynamic assembly loader removed
  that accidental dependency; the complete gate then passed.
- The first complete real RabbitMQ profile passed the three new acceptance tests but found four older
  fault-redrive setups without mandatory message limits (23/27). Their shared setup was modernized and
  the complete profile then passed 27/27. No product rule, test expectation, timeout, or skip was relaxed.

`review/**` was never modified or staged. This evidence is implementation-team evidence and makes no
claim of independent Red Team or Lead acceptance.

# Product-wide coverage profile at a0931a6bb

## Exact scope and collection

- Product/test HEAD: `a0931a6bb2442c254a7b6c5b6494acd9af00d150`.
  The tracked `src`/`tests` diff was empty before and after collection. User-owned
  untracked `TestResults/` and `review/` were not included.
- Thirty-six fresh Cobertura reports under
  `artifacts/coverage-a-plus-20260923-a0931a6bb/raw/` cover all 32 product
  assemblies: 22 Unit, 13 local-provider, and one Abstractions run with
  `DOTNET_EnableAVX2=0`. The associated 36 successful executions total
  11,124 passed tests, zero failures and skips. The separately run Release
  Unit/Architecture gate passed 10,243/10,243, zero failures and skips.
- The local-provider solution's locked restore and Release build completed
  with zero warnings and errors. The two Cron phase documents record focused
  tests and a zero-warning Release Unit build for each phase's source revision.
- `analysis-36/summary.json`, `methods.json`, and `provenance.json` record
  report hashes, exact HEAD, execution logs, collection scripts, 75 product
  binaries, and nine run-scoped fixtures. Their SHA-256 values are
  `cf14a8c6241bc5bd6c59f88d74bdee79964b2c8f2fd7eca186dbadf1e8a07d03`,
  `c5f46f01c4bd1f5ee145621f420932752842876a9a92800f0479278ab8f69cd1`,
  and `1d38c1ed27aca8085d589de52098e7ac7d20a2658a35d801ff82afd37b8a658a`.
  All nine `fixture-findings.json` files list no infrastructure findings.

## Product-wide result

| Measure | `a0931a6bb` | Previous `c073a5e30` |
| --- | ---: | ---: |
| Line coverage | 83,771 / 93,431 = 89.6608% | 83,795 / 93,439 = 89.6788% |
| Branch observation, conservative merge | 29,966 / 36,620 = 81.8296% | 30,015 / 36,672 = 81.8472% |
| Branch observation, capped sum | 32,435 / 36,620 = 88.5718% | 32,487 / 36,672 = 88.5880% |
| Methods with CRAP > 30 | 55 / 25,957 | 56 / 25,953 |

The named Cron parser entry point moved from CRAP 66 to 10.05 in this merged
profile. Its four extracted named-value helpers score 16, 16, 8, and 8.
`GetMonthNumber` moved from CRAP 48 to 4. These structural changes remove
two methods from the CRAP > 30 list. `ActiveMq.ConnectionContextFactory.CreateConnection`
enters that list at 45.81 because its observed coverage in this run is 10/22
lines; no ActiveMQ production source changed in these commits. The small
global coverage decrease is a measured change in the exact test execution,
not assigned solely to the Cron edits.

Cobertura gives branch counts by source line without stable branch identities.
The conservative merge uses the maximum hit count per branch location; the
capped sum adds observations up to the maximum valid count. These percentages
are observations, not formal lower or upper bounds for combined coverage.
CRAP is `complexity² × (1 − method line coverage)³ + complexity`.

## Attempts excluded from the 36 successful reports

- A sandboxed Unit collection attempt stopped at a blocked .NET named pipe;
  the same script succeeded with the required local execution permission.
- The first local-provider build omitted its absolute MSBuild coverage
  overlay, so its first test command discovered zero tests. The provider
  solution was restored and built again with the absolute overlay. Its
  corrected build had zero warnings and errors.
- A direct broad-provider attempt lacked the canonical `LocalIntegration`
  fixture profile and failed in Azure Table. It was discarded.
- The six-broker fixture successfully completed six provider reports, then
  one ActiveMQ/Artemis request-response case timed out under the full suite.
  The failed ActiveMQ XML was discarded. A fresh fixture passed the targeted
  three variants, and another fresh fixture passed the complete 100-test
  ActiveMQ suite with a new report. The sporadic failure's cause remains
  unknown. The test counts handler entry before `RespondAsync`, so the failed
  run's 100 handler entries do not establish that responses were sent.
- A shortened Event Hubs fixture omitted Azurite; its attempted test could
  not reach `localhost:10000`. The runner cleaned its containers after the
  attempt was stopped. A fresh Azurite plus Event Hubs fixture passed 61/61
  and supplied the counted report.

The persisted failed fixture attempts and corrected reruns remain in the
profile's execution logs. The blocked sandbox and omitted-overlay attempts
were discarded before this exact-commit collection.
Every counted XML comes from one of the 36 successful test executions.

## Remaining A+ work

Global A+ remains open: line and branch observations are below the requested
target, and 55 methods still exceed CRAP 30. The leading measured hotspots
are `PropertyAccessorFactory.IsCompilationFailure` (72, unexecuted), Azure
Service Bus receiver exception handling (57.95), and
`CronExpression.ProgressNextFireTimeDayOfWeek` (53.38). The intermittent
Artemis request-response failure needs an independent behavior investigation
before it can be considered closed.

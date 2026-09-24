# Product-wide coverage profile at a8c79a588

## Measured scope

- Product/test HEAD: `a8c79a588924fb79f8abc61a224961737e113e81`.
  The tracked `src`/`tests` diff was empty at analysis time (SHA-256
  `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855`).
- The unfiltered Unit/Architecture gate passed 10,332/10,332 tests with no
  skips; its Release build had zero warnings and errors. The 488 JobService
  tests and the direct cross-message partitioning test also passed.
- The 36 fresh successful Microsoft CodeCoverage Cobertura reports in
  `artifacts/coverage-a-plus-20260924-a8c79a588/raw/` observe all 32 product
  assemblies: 22 Unit, 13 local-provider, and one Abstractions run with
  `DOTNET_EnableAVX2=0` (786/786 passed). All 11 broker-bound local-provider
  projects passed, along with the direct Abstractions local project and the
  Azure Service Bus emulator. The 12 fixture records have empty findings.
- The reports, method analysis, binary inventory and fixture records are
  recorded in `analysis-36/summary.json`, `methods.json`, and
  `provenance.json`. Their SHA-256 values are respectively
  `e6fed0c9a4fc73ed38ee2769a19c9995ff4f7ddded35745f33e5b03fe3922280`,
  `6ed209982bac291b3888fef2ff0b90a436de4ae28aa4e975d401cfb6cba98819`,
  and `a81fbabce543bffd987abc8ba0c9e0aac134e95850f2e600c76c9ed83428f52c`.

## Product-wide result

| Measure | `a8c79a588` | Previous `6352ef3e9` |
| --- | ---: | ---: |
| Line coverage | 83,918 / 93,514 = 89.7384% | 83,935 / 93,515 = 89.7557% |
| Branch observation, conservative merge | 30,083 / 36,664 = 82.0505% | 30,091 / 36,666 = 82.0679% |
| Branch observation, capped sum | 32,566 / 36,664 = 88.8228% | 32,570 / 36,666 = 88.8289% |
| Methods with CRAP > 30, exact arithmetic | 40 / 25,979 | 41 / 25,974 |

The JobService receive-endpoint lambda dropped from CRAP 44 (complexity 44,
28/28 lines) to CRAP 6 (complexity 6, 9/9 lines). Its four new registration
helpers are fully line-covered with CRAP 8–10. The existing regression checks
the exact 18 message-type registrations, one coordinator, limit 2, same-job
serialization across message types, and progress in another partition. It
executes two of the 18 selector delegates; the other selector expressions
were moved without alteration. The changed method boundaries and test
observations affect aggregate coverage, so the small percentage decrease is
not evidence by itself of a product behavior regression.

Cobertura supplies per-line branch counts without stable branch identities.
The conservative merge takes the largest covered count at each source
location; the capped sum adds observations up to the largest valid count.
Neither is an exact union of branch identities. CRAP is
`complexity² × (1 − method line coverage)³ + complexity`.
The previous archived analyzer counted one mathematically exact CRAP 30 as
`> 30` because binary floating-point arithmetic produced
`30.000000000000004`. Re-evaluating its stored method counts with exact
integer arithmetic gives 41 rather than the archived 42. This profile uses
exact rational arithmetic for CRAP and an exact integer threshold check.

## Excluded attempts and evidence limits

- A combined filtered `dotnet test` build was cancelled after it stopped
  producing output. Separate Release builds then succeeded with zero warnings
  and errors, and the exact filtered test passed.
- The first instrumented Core run failed its outbound-HTTP boundary test after
  observing `https://dc.services.visualstudio.com/v2/track`. The test host
  transitively loads `Microsoft.Testing.Extensions.Telemetry` and
  `Microsoft.ApplicationInsights`; the installed Microsoft Testing Platform
  documentation names `TESTINGPLATFORM_TELEMETRY_OPTOUT`. With that variable
  and `DOTNET_CLI_TELEMETRY_OPTOUT` set to `1`, the boundary test passed alone
  and the complete instrumented Core project passed 6,383/6,383. The failed
  report is under `excluded/`; the successful isolated boundary report is
  under `supplementary/`, outside the 36-report aggregation.
- A fresh locked restore for the extra measurement-only Unit overlay exited 1
  without a diagnostic after a long wait. The previous profile's isolated
  `unit-sdk` and `provider-sdk-net` outputs were copied to new directories,
  then their affected projects and all local-provider solutions were rebuilt
  with `--no-restore` at `a8c79a588`, with zero warnings and errors. The
  JobService product binary in each new output root carries revision
  `+a8c79a588`. Unchanged assemblies may retain older informational
  revisions; their source was unchanged by this commit. The post-build binary
  inventory does not independently prove which binary each earlier test host
  loaded.
- Successful command exits were observed in this session, but execution logs
  were not persisted. Test totals and the AVX2 setting therefore have session
  evidence rather than independently replayable logs. Report, script, binary,
  and fixture hashes are persisted in the ignored artifact directory.
- Existing user-owned untracked `TestResults/` and `review/` were left intact.

## Next code areas

- Azure Service Bus `ConnectionContextFactory.CreateConnection` is the largest
  remaining hotspot: CRAP 50, complexity 44, 41/48 covered lines. Exercise
  credential modes and mixed caller-owned clients through discriminating
  behavior tests before simplifying its branches.
- Azure Service Bus `ServiceBusHostConfigurator.ParseEndpoint` remains CRAP
  42.19, complexity 40, 40/45 covered lines. Its host, authentication and
  custom-port contracts already have strong unit and emulator regression
  tests; parsing stages can be split with those tests as guards.
- The three Abstractions `ConsumeObserverConverter<T>` callbacks each have
  CRAP 42 and 0/9 covered lines. Determine whether a supported runtime path
  still uses the converter before testing or removing that public type.

The A+ line, branch, and CRAP objective remains open. The complete method
ranking is in `analysis-36/methods.json`.

# Product-wide coverage profile at c4b3faeda

## Scope and evidence

- Source and test commit: `c4b3faeda0b36a6fb1708a0f59591defcac791a4`.
  The tracked `src`/`tests` diff was empty during aggregation, with SHA-256
  `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855`.
- The 36 fresh Cobertura reports under
  `artifacts/coverage-a-plus-20260923-c4b3faeda/raw/` comprise 22
  Unit/Infrastructure reports, 13 local-provider reports, and one supplementary
  Abstractions run with `DOTNET_EnableAVX2=0`. All counted test commands exited
  successfully; the complete fresh-build serial Unit/Architecture gate passed
  10,211/10,211 tests before the source/test commit, on the same content.
  Architecture tests are outside the instrumented aggregate because their
  assembly-ownership assertion is affected by instrumentation.
- All 32 product assemblies in the preceding complete profile occur in the
  reports. Report hashes, 77 product-binary hashes, the exact HEAD, and four
  fixture records are in the artifact-local `provenance.json`; method details
  are in `analysis-36/methods.json`. The four counted fixture runs have empty
  findings lists. The multi-broker fixture used the required ActiveMQ outage
  control; SQL Server, RabbitMQ, and Azure Service Bus used separate fixtures.
- Earlier setup attempts are retained outside `raw/`. One direct provider run
  lacked the local fixture profile; a later multi-broker run lacked ActiveMQ
  outage control. Neither is counted. An Azure Service Bus emulator run had one
  `MessageLockLost` while completing a dead-letter message (27/28 passed). A
  complete new-fixture rerun passed 28/28; only that successful report is in
  `raw/`. The first failure remains a test-stability observation, pending the
  separate adversarial assessment recorded with this phase.
- Locked restores and Release builds used separate artifact roots for the
  supplementary Unit and provider projects. Coverage used
  `tools/ci/coverage.settings.xml` and Microsoft CodeCoverage.
- Binary and report hashes describe collected files but do not alone prove a
  report-to-executed-binary or build-to-commit chain. The exact-HEAD attribution
  additionally relies on the observed build/test sequence and unchanged
  tracked source/test content. In particular, the 18 direct Unit modules used
  binaries built by the complete gate immediately before the `c4b3faeda`
  commit.

This profile is the historical source/test state at `c4b3faeda`. A subsequent
hardening of an Azure Service Bus emulator test requires a new source/test
commit and a fresh product-wide profile before another current-state claim.

## Product-wide result

| Measure | Current `c4b3faeda` | Previous `a087aaa64` |
| --- | ---: | ---: |
| Line coverage | 83,646 / 93,414 = 89.5433% | 83,636 / 93,404 = 89.5422% |
| Branch observation, conservative merge | 29,917 / 36,688 = 81.5444% | 29,946 / 36,737 = 81.5145% |
| Branch observation, capped sum | 32,400 / 36,688 = 88.3123% | 32,442 / 36,737 = 88.3088% |
| Methods with CRAP > 30 | 66 / 25,943 | 67 / 25,920 |

`RabbitMqEndpointAddress(Uri, Uri)` now has 47/47 measured lines and CRAP 6
in the global aggregate; the preceding complete profile reported 98/99 and
CRAP 67. The extracted parsing methods have 49/49 measured lines in total and
maximum CRAP 4. Other measured line changes across reports are descriptive,
since test execution and instrumentation can vary.

Cobertura supplies per-line branch counts without branch identities. The
aggregator merges unique source lines across reports. Its conservative branch
merge takes the largest observed covered count per location; its capped sum
adds observed counts up to the largest valid count. Reports can disagree on
the valid count at one location, as the previous profile documented for
`MessagePackMessageSerializer.cs:194`. Consequently these observations are
not formal bounds on exact merged branch coverage. CRAP uses
`complexity² × (1 − method line coverage)³ + complexity`.

The Microsoft `coverage-analysis` skill guided collection and risk review.
Its PowerShell scripts could not run because `pwsh` is unavailable here; the
artifact-local Python aggregator applies the documented formula. Coverage and
CRAP remain below the requested product-wide A+ target.

## Next code areas

- `PropertyAccessorFactory.IsCompilationFailure`: CRAP 72 and 0/1 covered
  lines. Confirm a reachable public failure path before adding a test.
- `CronExpression.StoreExpressionGeneralValue`: CRAP 66 with full measured
  line coverage; behavior-led simplification is needed.
- JobService registration and correlation methods: CRAP 62 with full measured
  line coverage; inspect configuration contracts and split complexity only
  where the same hard assertions can protect semantics.
- Azure Service Bus receiver exception handling and endpoint parsing:
  uncovered failure paths still carry high CRAP and need failure oracles.

The exact method ordering and source locations are in `analysis-36/methods.json`.

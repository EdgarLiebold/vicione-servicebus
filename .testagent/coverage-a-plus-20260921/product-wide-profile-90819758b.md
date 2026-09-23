# Product-wide coverage profile at 90819758b

## Scope and evidence

- Source and test commit: `90819758bf2ed70e981853bf3fd7d6529a85dab9`.
  The tracked `src`/`tests` diff was empty during aggregation, with SHA-256
  `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855`.
- The 36 fresh Cobertura reports under
  `artifacts/coverage-a-plus-20260923-90819758b/raw/` comprise 22
  Unit/Infrastructure reports, 13 local-provider reports, and one supplementary
  Abstractions run with `DOTNET_EnableAVX2=0`. Every counted test command exited
  successfully. The complete stable, serial Unit/Architecture gate passed
  10,211/10,211 tests with no failures or skips. Architecture tests are outside
  the instrumented aggregate because instrumentation affects their
  assembly-ownership assertion.
- All 32 product assemblies in the preceding complete profile occur in the
  reports. Report hashes, 77 product-binary hashes, exact HEAD, and four
  fixture records are in the artifact-local `provenance.json`; method details
  are in `analysis-36/methods.json`. All four counted fixture runs have empty
  findings lists. The multi-broker fixture used ActiveMQ outage control;
  SQL Server, RabbitMQ, and Azure Service Bus used separate fixtures.
- A first Unit/Architecture gate was run while direct Unit coverage read the
  same SDK output during a build. Six Quartz cases failed immediately with
  `BadImageFormatException`. Those 18 direct coverage reports were removed
  from `raw/` and retained under `failed-concurrent-unit/`. The stable serial
  gate and all 18 direct reports were then rerun, successfully and without a
  concurrent build. The four supplementary Unit reports used a separate SDK
  output root and were unaffected.
- Locked restores and Release builds used separate artifact roots for the
  supplementary Unit and provider projects. Coverage used
  `tools/ci/coverage.settings.xml` and Microsoft CodeCoverage.
- Binary and report hashes describe collected files but do not alone prove a
  report-to-executed-binary or build-to-commit chain. The exact-HEAD
  attribution also relies on the observed build/test sequence and unchanged
  tracked source/test content. The 18 direct Unit modules used binaries from
  the complete gate built on this content immediately after the commit.

## Product-wide result

| Measure | Current `90819758b` | Previous `c4b3faeda` | Before parser phase `a087aaa64` |
| --- | ---: | ---: | ---: |
| Line coverage | 83,649 / 93,414 = 89.5465% | 83,646 / 93,414 = 89.5433% | 83,636 / 93,404 = 89.5422% |
| Branch observation, conservative merge | 29,918 / 36,688 = 81.5471% | 29,917 / 36,688 = 81.5444% | 29,946 / 36,737 = 81.5145% |
| Branch observation, capped sum | 32,401 / 36,688 = 88.3150% | 32,400 / 36,688 = 88.3123% | 32,442 / 36,737 = 88.3088% |
| Methods with CRAP > 30 | 66 / 25,943 | 66 / 25,943 | 67 / 25,920 |

`RabbitMqEndpointAddress(Uri, Uri)` has 47/47 measured lines and CRAP 6 in
the current global aggregate; the profile before this phase reported 98/99
and CRAP 67. The extracted parser steps have 49/49 measured lines in total
and maximum CRAP 4. The Azure Service Bus emulator test now exercises an
exact body assertion after confirmed dead-letter settlement; a separate
fresh-fixture run also passed 28/28. Other measured line changes across
reports are descriptive because execution and instrumentation can vary.

Cobertura supplies per-line branch counts without branch identities. The
aggregator merges unique source lines across reports. Its conservative branch
merge takes the largest observed covered count per location; its capped sum
adds observed counts up to the largest valid count. At
`MessagePackMessageSerializer.cs:194`, the current MessagePack Unit report
records 2/2 while several provider reports record 0/4, so these observations
are not formal bounds on exact merged branch coverage. CRAP uses
`complexity² × (1 − method line coverage)³ + complexity`.

The Microsoft `coverage-analysis` skill guided collection and risk review.
Its PowerShell scripts could not run because `pwsh` is unavailable here; the
artifact-local Python aggregator applies the documented formula. Coverage
and CRAP remain below the requested product-wide A+ target.

## Next code areas

- `PropertyAccessorFactory.IsCompilationFailure`: CRAP 72 and 0/1 measured
  lines. Confirm a reachable public failure path before adding a test;
  synthetic reflection state would not establish product behavior.
- `CronExpression.StoreExpressionGeneralValue`: CRAP 66 with full measured
  line coverage; behavior-led simplification is needed.
- JobService registration and correlation methods: CRAP 62 with full measured
  line coverage; inspect configuration contracts and use hard assertions
  before changing structure.
- Azure Service Bus receiver exception handling and endpoint parsing:
  uncovered failure paths still carry high CRAP and need failure oracles.

The exact method ordering and source locations are in `analysis-36/methods.json`.

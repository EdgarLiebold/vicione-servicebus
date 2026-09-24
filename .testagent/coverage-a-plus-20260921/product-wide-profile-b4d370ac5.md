# Product-wide coverage profile at b4d370ac5

## Exact scope

- Product/test HEAD: `b4d370ac5f9d875d0524c805cde37817eb781e37`.
  The tracked `src`/`tests` diff was empty throughout collection (SHA-256
  `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855`).
  Existing untracked `TestResults/` and `review/` were not included.
- Thirty-six fresh Microsoft CodeCoverage Cobertura reports under
  `artifacts/coverage-a-plus-20260924-b4d370ac5/raw/` cover all 32 product
  assemblies: 22 Unit, 13 local-provider, and one supplementary Abstractions
  run with `DOTNET_EnableAVX2=0`. Every counted test command exited
  successfully. The supplementary run passed 786/786 tests.
- The Unit/Architecture solution and instrumented local-provider solution
  built in Release with zero warnings and errors. The separate SQL Server,
  RabbitMQ, and Azure Service Bus solutions also built in Release with zero
  warnings and errors. The first complete Unit/Architecture gate at this HEAD
  passed 10,262/10,263 tests; the sole failure was a global DiagnosticListener
  positive control that also captured a concurrent request. Its class passed
  4/4 in isolation. The test class now uses the existing nonparallel global
  listener collection and the positive control accepts only its own request.
  The corrected class passed 4/4; the renewed full gate passed 10,263/10,263.
  This test-only correction occurred after coverage collection and is not part
  of the measured source/test snapshot.
- `analysis-36/summary.json`, `methods.json`, and `provenance.json` record
  report hashes, 75 product-binary hashes, exact HEAD, and nine fixture
  records. Their SHA-256 values are respectively
  `32b5231ab18090c1f5ccd506dc19c161e33014313139c0accc89f4e82681a052`,
  `f5bfa9c49434e2e45be61bf0ce25fc829aedfbbc36c9033f70b1df34936596d1`,
  and `5a88258e359b15ce98d67ff7237b8d96f67ed41c7ef82c902b2912245719b9bf`.
  All nine counted fixture records have empty `findings` lists.

## Product-wide result

| Measure | `b4d370ac5` | Previous `4e7b54c22` |
| --- | ---: | ---: |
| Line coverage | 83,851 / 93,466 = 89.7128% | 83,843 / 93,474 = 89.6966% |
| Branch observation, conservative merge | 30,008 / 36,616 = 81.9532% | 30,005 / 36,628 = 81.9182% |
| Branch observation, capped sum | 32,475 / 36,616 = 88.6907% | 32,474 / 36,628 = 88.6589% |
| Methods with CRAP > 30 | 45 / 25,971 | 48 / 25,971 |

The three fewer high-CRAP methods include the extracted bus startup validator
and two removed, unused internal Abstractions helpers. Changes in aggregate
line and branch denominators reflect the changed source and instrumented
method boundaries; they must not be attributed only to added tests.

Cobertura provides per-line branch counts without stable branch identities.
The conservative merge takes the largest observed covered count at each
source location. The capped sum adds observations up to the largest valid
count. These are observations, not formal bounds for the merged test suite.
CRAP is `complexity² × (1 − method line coverage)³ + complexity`.

## Excluded attempts and limits

- The first local-provider build against the new isolated artifact directory
  had no restore assets (`NETSDK1004`). Locked restore then succeeded, followed
  by a zero-warning Release build.
- Azure Table attempts with the default UnitArchitecture profile and then
  with LocalIntegration but without a run-scoped Azurite account failed before
  product behavior could be assessed. Their reports were moved to `excluded/`.
  The canonical Azurite fixture run passed all 27 tests and supplied the
  counted report.
- An ActiveMQ run without the required broker-outage controller passed 98/100
  tests; the two recovery tests rejected that fixture configuration. Its
  report was moved to `excluded/`. The canonical runner with
  `--allow-broker-outage activemq` passed all 100 tests and supplied the
  counted report.
- Fixture endpoints and credentials are generated per run. The report XML
  does not encode these settings; `provenance.json` and the nine fixture
  records bind the successful runs to broker logs and empty cleanup findings.
- The 36 successful command exits and zero-warning builds were observed in
  this session, but `provenance.json` contains no persisted execution logs.
  Its product-binary hashes are a post-build inventory; they do not independently
  prove the exact binary loaded for each earlier report. Exact source/test HEAD,
  clean source/test diff at collection, successful run output, report hashes,
  and fixture records establish the scope within that limit.
- The renewed full gate rebuilt files under `artifacts/sdk/` after collection;
  30 current binary hashes therefore differ from the historical collection
  inventory in `provenance.json`. The report, analysis, fixture, and provider
  binary hashes remain a verifiable collection snapshot. The manifest has
  deliberately not been regenerated against the later test-only edit.

## Next code areas

- JobService endpoint configuration remains CRAP 44 at 28/28 covered lines.
  Its 18 JobId partition selectors need strong behavior evidence before
  extraction; the existing cross-message serialization test covers two types
  and proves shared coordinator identity.
- Azure Service Bus `ConnectionContextFactory.CreateConnection` remains CRAP
  42.08 at 33/42 covered lines. Credential modes and mixed preconfigured
  clients need behavioral oracles before refactoring.
- The three `ConsumeObserverConverter<T>` callbacks remain CRAP 42 each and
  0/9 covered lines. Confirm whether the public observer path still uses the
  adapter, then test actual observer dispatch or remove only proven dead code.
- The product-wide A+ goal remains open. `analysis-36/methods.json` contains
  the complete method ranking.

## Later correction to the CRAP threshold count

The archived analyzer used binary floating-point arithmetic for the strict
`CRAP > 30` count. One method with mathematically exact CRAP 30 rounded
slightly above the threshold in each compared profile. Exact integer
threshold arithmetic over the unchanged archived method data gives **44**
for `b4d370ac5` and **47** for comparison `4e7b54c22`, rather than archived
45 and 48. The archived reports and hashes remain unchanged.

# Product-wide coverage profile at 151bf0dc1

## Exact scope and collection

- Product/test HEAD: `151bf0dc141e13734cb6d3167a1d03906edddb90`.
  The tracked `src`/`tests` diff was empty during collection; its SHA-256 is
  `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855`.
  User-owned untracked `TestResults/` and `review/` were not included.
- Thirty-six fresh Microsoft CodeCoverage Cobertura reports under
  `artifacts/coverage-a-plus-20260924-151bf0dc1/raw/` cover all 32 product
  assemblies: 22 Unit, 13 local-provider, and one supplementary Abstractions
  run with `DOTNET_EnableAVX2=0`. Every counted test command exited
  successfully. The separately run Release Unit/Architecture gate passed
  10,256/10,256 tests with zero failures and skips.
- The Release Unit solution and local-provider solution builds completed with
  zero warnings and errors. The four supplementary Unit projects and three
  separately built provider solutions also completed their Release builds
  with zero warnings and errors.
- `analysis-36/summary.json`, `methods.json`, and `provenance.json` record the
  36 report hashes, 75 product-binary hashes, exact HEAD, eight fixture
  records, and method inventory. Their SHA-256 values are respectively
  `6b34608eebe9c8861327a8fa1bd0ec1944d9c25e6664c06144bd64cfa1fca3cd`,
  `98550d86cbdf524c7255e2f669386c26df390a62adc56c32c5c97d7a37786538`,
  and `a4954e13ebc587d9b2ef63c1afac4bf873fa36493459c611ce4f65bf36208afb`.
  All eight counted fixture records have empty findings lists.

## Product-wide result

| Measure | `151bf0dc1` | Previous `a0931a6bb` |
| --- | ---: | ---: |
| Line coverage | 83,796 / 93,457 = 89.6626% | 83,771 / 93,431 = 89.6608% |
| Branch observation, conservative merge | 29,978 / 36,624 = 81.8534% | 29,966 / 36,620 = 81.8296% |
| Branch observation, capped sum | 32,451 / 36,624 = 88.6058% | 32,435 / 36,620 = 88.5718% |
| Methods with CRAP > 30 | 52 / 25,965 | 55 / 25,957 |

`MessageTypeCache<T>.CheckIfValidMessageType` moved from CRAP 39.35 in the
previous complete profile to CRAP 8 with 8/8 covered lines and complexity 8.
Its extracted checks score 12, 6, 6, and 6.40. The source phase and adversarial
review are documented in `message-type-cache-validation-phase.md`. The global
hotspot count fell by three; the measured changes in other assemblies cannot
be assigned solely to this source edit because execution and instrumentation
can vary between complete profiles.

Cobertura gives per-line branch counts without stable branch identities.
The conservative merge takes the largest observed covered count at each
source location. The capped sum adds observations up to the largest valid
count. These are observations, not formal bounds for the merged test suite.
CRAP is `complexity² × (1 − method line coverage)³ + complexity`.

## Excluded attempt

The first ActiveMQ/Artemis fixture run omitted the canonical
`--allow-broker-outage activemq` option required by two recovery tests. It
failed 2/100 with the missing fixture-control diagnostic. Its XML report was
removed before aggregation. A new fixture with outage control passed the
complete 100/100 test suite and supplied the counted report. No failed XML
is among the 36 reports.

## Next code areas

- `PropertyAccessorFactory.IsCompilationFailure` remains CRAP 72 with 0/1
  measured lines. Investigate a reachable public property-access failure
  before adding a test; a synthetic exception alone would not establish a
  product contract.
- Azure Service Bus receiver exception handling remains CRAP 57.95 with
  29/34 covered lines and complexity 50. Its failure and cancellation paths
  need behavior-level oracles and a focused adversarial review.
- Azure Service Bus subscription validation remains CRAP 46.44 with 5/9
  covered lines. JobService endpoint configuration is CRAP 44 despite full
  measured line coverage and needs behavior-preserving complexity reduction.
- The remaining global A+ gap is substantial: line and branch observations
  remain below the requested level, and 52 methods still exceed CRAP 30.
  The complete ranked inventory is in `analysis-36/methods.json`.

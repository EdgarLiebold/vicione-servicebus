# Cumulative product coverage profile at e51c446f2

## Scope and validation

- Product/test HEAD: `e51c446f2988aee535ae2e6245fcdba0eb59ecd7`.
  No `src` file changed since the preceding `e8976fc62` profile. This commit
  adds a source-owned Amazon SQS topology probe contract test, its requirement
  mapping, and a changelog entry.
- The Release Unit/Architecture solution build was observed to pass with zero
  warnings and errors; its console output was not persisted. The serial full
  gate passed **10,351/10,351**, with zero failures and
  skipped tests. Its log is
  `artifacts/coverage-a-plus-20260924-e51c446f2/unit-architecture-gate-final.log`
  (SHA-256 `3c14d7e7e83cfd945da6f180e08be1fbd4f0db16af2e0c0ea39d0d4b33347063`).
  An earlier gate command incorrectly passed an MSBuild option to Microsoft
  Testing Platform and discovered zero tests. That unsuccessful invocation is
  preserved as `unit-architecture-gate.log`; it does not count as a test result.
- The fresh Amazon SQS project run under Microsoft CodeCoverage was observed
  to pass **212/212**; its test console output was not persisted. The Cobertura report is SHA-256
  `10be702db2a3569c0e7f37f4beba0fe2ea3e293235360468271501c2a900db9d`.
  The newly added topology test was also observed to pass in the focused
  **4/4** class run; that console output was not persisted either. The saved
  full-gate log establishes the aggregate 10,351 result, while the Cobertura
  file itself contains no test count.
- `artifacts/coverage-a-plus-20260924-e51c446f2/raw/` contains 40 parseable
  Cobertura reports for 32 product assemblies. The 39 inherited reports are
  individually hash-identical to the preceding profile; one Amazon SQS report
  was added at this HEAD. These are cumulative observations, not 40 executions
  at this HEAD. All 12 broker fixture records are inherited; no new broker run
  occurred in this iteration.

## Product-wide cumulative result

| Measure | `e51c446f2` | Previous `e8976fc62` |
| --- | ---: | ---: |
| Line coverage | 84,039 / 93,480 = 89.9005% | 84,015 / 93,480 = 89.8748% |
| Conservative branch observation | 30,132 / 36,660 = 82.1931% | 30,126 / 36,660 = 82.1768% |
| Methods with CRAP > 30, exact arithmetic | 33 / 25,989 | 34 / 25,989 |

`AmazonSqsBrokerTopology.IProbeSite.Probe` moves from CRAP 42 with zero of 24
covered lines to CRAP 6 with 24 of 24. The contract test builds two topics,
two queues, and three asymmetric SNS-to-SQS subscriptions, then checks the
public probe result's exact entity names, durable and auto-delete flags, and
source/destination pairs. It is independent of the product's projected values.
The adversarial Red Team reviewed the test and requirement/changelog mapping
and returned PASS.

## Merge method and limits

- The raw reports remain unchanged. The existing JobSaga source overlay still
  excludes that changed file from exactly eleven older reports and takes its
  current classes only from the fresh Core report in an earlier profile. No
  product source changed in this iteration, so the Amazon SQS observation can
  be merged with the inherited reports by source location and method identity.
- `analysis-40/summary.json`, `methods.json`, `provenance.json`, and
  `overlay-policy.json` record counts, report hashes, method threshold
  decisions, source continuity, and the fixture chain. Their SHA-256 values
  are respectively `ff12ed408fe7f40e30e0e7083a366fc2193cafa2e376090fc3d63e838c4844a2`,
  `96d1a44d6a3d7f80c8427132cfda8733b5e634a27fb5eaf262739d6e0d41b535`,
  `bd9c1d385240fca14cda60fe4b122147106615ddc6f8ee01e52ef7f58e102819`,
  and `fc2f148f747d643f2808193e3de84fdec50f6a116865c2116db8aac3d3e0cff9`.
- Cobertura lacks stable branch identities. The conservative merge takes the
  largest observed covered count at each source location. The capped sum can
  overstate coverage across repeated observations and is excluded from the
  quality comparison. CRAP uses method complexity and merged method line
  coverage: `complexity + complexity² × (1 − coverage)³`.
- The Microsoft `code-testing-agent`, `run-tests`, and `coverage-analysis`
  skills guided the test design, execution, and risk measurement.

Global A+ remains open: line and branch coverage are below A+, and 33 methods
remain above CRAP 30.

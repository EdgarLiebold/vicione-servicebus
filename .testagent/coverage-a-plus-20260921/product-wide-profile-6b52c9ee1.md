# Cumulative product coverage profile at 6b52c9ee1

## Scope and validation

- Product/test HEAD: `6b52c9ee1fca851c1135f82853806346941b65a2`.
  The only changed `src` file since `d415d2cbf` is
  `src/ViciOne.ServiceBus.Abstractions/Util/RequestRateAlgorithm.cs`.
  The commit also adds six source-owned test cases, four requirement mappings,
  and a changelog entry. An adversarial read-only Red Team identified and
  helped close missing cancellation accounting, disposal, rate-permit
  refund, rate-window, and already-waiting-request oracles. Its final code
  review returned PASS.
- The exact-commit Release Unit/Architecture build passed with zero warnings
  and errors; its log SHA-256 is
  `00872fbcbdbab35a6d4d645992635dc6504c82060bafc27e03b20837d95b8537`.
  Both fresh Abstractions product and test DLLs embed the full `6b52c9ee1`
  revision. The serial full gate passed **10,363/10,363**, with zero failures
  and skips; its log SHA-256 is
  `94ed9fdad2bb9ba507554d1a14b2364309e3b66a1b0bbbdc5e5b48be8e99a0db`.
- Microsoft CodeCoverage with `tools/ci/coverage.settings.xml` passed the
  Abstractions suite **792/792**, with zero failures and skips. Its log
  SHA-256 is `bce3d5f6fc4e0b0d4401abb3af0910754362c7018ccc4545eecb19c5ca84cc38`;
  its Cobertura SHA-256 is
  `10a0da5eec89094417254c98b377c3e7036f1ba08991c1cbbefa10fd253d720c`.
- `artifacts/coverage-a-plus-20260924-6b52c9ee1/raw/` contains 43 parseable
  Cobertura reports for 32 product assemblies. Forty-two inherited reports
  are individually hash-identical to the preceding profile; the Abstractions
  report is new at this HEAD. These are cumulative observations, not 43
  executions at this HEAD. All 12 broker fixture records are inherited; no
  new broker run occurred in this iteration.

## Product-wide cumulative result

| Measure | `6b52c9ee1` | Previous `d415d2cbf` |
| --- | ---: | ---: |
| Line coverage | 84,070 / 93,507 = 89.9077% | 84,053 / 93,487 = 89.9088% |
| Conservative branch observation | 30,149 / 36,670 = 82.2171% | 30,143 / 36,664 = 82.2142% |
| Methods with CRAP > 30, exact arithmetic | 30 / 25,994 | 31 / 25,992 |

The `RequestRateAlgorithm` constructor previously had CRAP 34 despite full
line coverage. Validation is now a separate method; the fresh report shows
the constructor at **25/25** lines, CRAP **12**, and `ValidateOptions` at
**15/16** lines, CRAP **22.12**. `BeginRequestAsync` is at **31/32** lines,
CRAP **16.01**. The new `ReturnRatePermit` is **8/8**, CRAP **2**.

The boundary test reproduced an integer overflow that made a maximum-prefetch
request wait forever for zero result capacity. The fix calculates the
rounded-up request limit without overflow and caps the default result capacity
at `int.MaxValue`. Tests also reproduce and prevent leaked active-request
counts and semaphore leases when cancellation or disposal occurs while result
capacity is unavailable. Fake-clock tests verify that an unused rate permit
is returned and wakes an existing waiter in the same interval, is not
returned after a window reset, and does not inflate the next window's quota.
The focused class passed **31/31** after the red runs; its exact-commit log
SHA-256 is `e33e04882e3de6c09c2b15b1eacbd85840f3766482c4a93cdee8dedb0fb0d4eb`.

## Merge method and limits

- `RequestRateAlgorithm.cs` changed, so exactly 39 inherited reports
  containing that source file are excluded for it. Only the new Abstractions
  report contributes that file's classes, lines, branches, and methods. The
  previous JobSaga overlay still excludes eleven stale reports; the
  Serialization and Retry overlays each exclude 35 stale reports. All other
  product source files are unchanged since `d415d2cbf`.
- `analysis-43/summary.json`, `methods.json`, `provenance.json`, and
  `overlay-policy.json` record the exact merge, exclusions, report and binary
  hashes, fixture inheritance, and CRAP decisions. Their SHA-256 values are
  respectively `9ea2f8a9b94e9252e6e3afa5d310df59a4a844361a767f6af81653d556f70400`,
  `dcf01390d789ce5a3e6fb49fcee4c82ccb7d0766726b36881240814482efd3ba`,
  `4d023ec1ae04e95e1efad7ce2d5b1b1f4d1b9620b912703fcdc143dc627013c3`,
  and `396b8ff32e14cef2b920d4743b85dd87bd8627f435e529f949e41d7f6d70685d`.
- Cobertura lacks stable branch identities. The conservative merge takes the
  largest observed covered count at each source location. The capped sum can
  overstate repeated observations and is not used for the quality comparison.
  CRAP is `complexity + complexity² × (1 − method line coverage)³`.
- The Microsoft `code-testing-agent`, `run-tests`, `coverage-analysis`,
  `test-gap-analysis`, and `assertion-quality` skills informed the boundary
  tests, test commands, hotspot selection, mutation review, and assertions.
  The final tests assert exact active counts, result capacities, rate quota,
  token identity, timeout boundaries, and cleanup rather than mere execution.

Global A+ remains open: line and branch coverage are below A+, and 30 methods
remain above CRAP 30.

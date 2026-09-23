# Product-wide coverage profile at b6ffcfbdf

## Scope and evidence

- Exact product/test HEAD: `b6ffcfbdfe4cbdfa87a20fbccf0aff6986aaa171`.
  The tracked `src`/`tests` diff at aggregation was empty (SHA-256
  `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855`).
- All 36 fresh Cobertura reports under
  `artifacts/coverage-a-plus-20260923-b6ffcfbdf/raw/` are parseable and
  observe all 32 product assemblies: 22 Unit/Infrastructure, 13 local-provider,
  and one supplementary Abstractions run with `DOTNET_EnableAVX2=0`.
  The 36 counted test commands passed 11,102 executions, zero failures and
  skips. The portability command repeats 768 Abstractions tests.
- The four successful provider fixtures were `vicione-4a6d1dd7b46e` (six
  brokers with ActiveMQ outage control), `vicione-2d769fb19707` (SQL Server),
  `vicione-479717e19768` (RabbitMQ), and `vicione-2634e3e8376c` (Azure
  Service Bus). Their findings lists are empty. The local-provider graph built
  with zero warnings and errors. The complete Engineering Release build and
  final Unit/Architecture gate (10,227/10,227) immediately before this
  collection are documented in `quartz-suspect-manual-schedule-phase.md`.
- `analysis-36/summary.json`, `methods.json`, and `provenance.json` contain
  the aggregation, method list, exact HEAD, and SHA-256 hashes of all 36
  reports, seven execution logs, two analysis files, nine collection scripts,
  77 product binaries, four fixture records, and their broker logs. The
  no-AVX2 test runner placed its report under `TestResults/` because the
  initial output argument was relative. A verified copy is included in this
  profile's `raw/no-avx2/` directory; the original is left untouched.

## Product-wide result

| Measure | Current `b6ffcfbdf` | Previous `7e184ded0` |
| --- | ---: | ---: |
| Line coverage | 83,703 / 93,411 = 89.6072% | 83,702 / 93,411 = 89.6061% |
| Branch observation, conservative merge | 29,947 / 36,668 = 81.6707% | 29,947 / 36,668 = 81.6707% |
| Branch observation, capped sum | 32,426 / 36,668 = 88.4313% | 32,426 / 36,668 = 88.4313% |
| Methods with CRAP > 30 | 63 / 25,943 | 63 / 25,943 |

The net one-line difference is run variability, not evidence that the S3
region test covered a previously missed product line. Five source lines were
newly hit and four previously hit lines were missed across telemetry,
batching, EF saga, PostgreSQL, Abstractions, and Amazon SQS paths. The new
`RegionEndpoint`-only test and the deterministically corrected regionless
test strengthen the S3 startup contract regardless of this aggregate count.
No production source changed in this phase.

Cobertura supplies per-line branch counts without stable branch identities.
The conservative observation takes the largest hit count at each branch
location; the capped sum adds observations up to the largest valid count.
Neither is a formal lower or upper bound on true combined branch coverage.
CRAP is `complexity² × (1 − method line coverage)³ + complexity`, following
the Microsoft `coverage-analysis` skill. The environment setting on the
no-AVX2 command is recorded by the invocation, not independently proven by
its XML. Hashes preserve files but do not alone prove that each report came
from the recorded binary or commit; attribution also relies on the observed
ordered commands and unchanged tracked source/test files. The three changed
Unit test binaries were built from the eventual commit-identical bytes before
the commit was written; their hashes and the empty source/test diff do not
cryptographically prove that relationship.

The A+ goal remains open for line and branch coverage and for the 63 methods
above CRAP 30. The next bounded product-contract slice should be selected
from these measured gaps, with independent adversarial review before claiming
any method-level improvement.

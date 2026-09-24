# Cumulative product coverage profile at 70bb7e3ac

## Exact-commit evidence

- Product/test commit: `70bb7e3acb0d361586efc57f2c262a65d470e8cc`.
  Against `6ecd8e617`, exactly one product source changed:
  `src/ViciOne.ServiceBus/Middleware/OutputPipeFilter.cs`. Fault-observer
  failures and errors from diagnostic logging no longer replace the original
  dispatch failure or prevent notification of the other observer group.
  The tests hold all six typed and untyped observer stages asynchronously,
  then exercise typed and untyped fault-observer failures without a configured
  logger and with a throwing logger. Requirement mapping and changelog
  accompany the fix.
- The serial Release Unit/Architecture build completed with zero warnings and
  errors (`build.log` SHA-256
  `454078271b38319b2dd604525cb1ca6c7b22bb20511a76fc031bf314a0a8ede3`).
  The Core test DLL and nine product DLLs in its fresh report embed the full
  product/test revision.
- The serial Unit/Architecture gate passed **10,405/10,405** with zero
  failures and skips (`gate.log` SHA-256
  `4b1f1c4c9c91f6a8569361743f8b3b2fe43208c97eb100a5d39cd9f2456b9c6f`).
- The fresh Core Microsoft CodeCoverage run passed **6,412/6,412** with zero
  failures and skips (`core-coverage.log` SHA-256
  `80998c4b85ad7405c9165c6724af8265fb5e0688bee7fe89a7c663727eb55a37`;
  Cobertura SHA-256
  `b4e78d6a2832f736964ff1bf3984cb2461db842fd70640319d501eec8aa6ef92`).
- Read-only adversarial Red Team twice found real counterexamples in the
  first versions: a failing fault observer could replace the original error,
  and a throwing diagnostic logger could still do so. Both were fixed and
  covered before Red Team returned **PASS**. A first full gate also caught
  two Task-returning local test helpers without an `Async` name; the names
  were corrected and the full gate was rerun successfully at this commit.

## Cumulative result

| Measure | `70bb7e3ac` | Previous `6ecd8e617` |
| --- | ---: | ---: |
| Line coverage | 84,244 / 93,522 = 90.0793% | 84,225 / 93,509 = 90.0715% |
| Conservative branch observation | 30,233 / 36,676 = 82.4327% | 30,221 / 36,670 = 82.4134% |
| Methods with CRAP > 30 | 21 / 25,996 | 21 / 25,995 |

`OutputPipeFilter.SendToOutputAsync` moves from 23/30 measured lines and
CRAP 34.59 to 37/38 lines and CRAP 30.02. Its remaining uncovered line is
a closing-brace sequence point; this method still counts above the strict
CRAP 30 threshold. The extracted diagnostic helper is covered 5/5 with
CRAP 2. The new tests distinguish callback order, pending operation state,
exact context and original exception identity, outer notification after a
typed observer failure, and logger failure isolation.

## Merge and limits

`artifacts/coverage-a-plus-20260924-70bb7e3ac/raw/` contains 52 parseable
reports for 32 product assemblies: 51 byte-verified inherited reports and
one fresh Core report. Twelve broker fixture records are inherited; no broker
fixture ran in this iteration. For the changed `OutputPipeFilter.cs`, 43 older
reports are excluded and only the fresh Core report is used. The JobSaga,
Serialization, Retry, RequestRate, QoS, and Azure batching overlays remain in
force. Their stale-report counts are respectively 17, 43, 43, 48, 43, and 3,
including the new Core report where applicable. Cobertura has no stable branch
identities, so the conservative result takes the largest covered count per
branch location. The capped-sum estimate is not used as the quality gate.

The merge is recorded in `analysis-52/summary.json` (SHA-256
`a77d1f5b6f457737df339c726c4345e66181ce12e8dd985ce01dc7e02b9d63a8`),
`analysis-52/methods.json` (`86ddfa08ab28ec8d605b90cdef6f503dab2392d1a76307592206677992b0d0a6`),
`provenance.json` (`560ca78394825b0dc266fd7ec8f8e2eedc1a1b5722a7586e832fc0e2f6dc9f72`),
and `overlay-policy.json` (`df922b9a9ba436e77d3fbf0662424e55c6b1ccfd519635bb42ea80567030f689`).

The Microsoft `code-testing-agent`, `coverage-analysis`, `test-gap-analysis`,
`assertion-quality`, and `run-tests` skills informed test design, adversarial
review, commands, and measurement. Global A+ remains open: branch coverage
is below A+, and 21 methods remain above CRAP 30.

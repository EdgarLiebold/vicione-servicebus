# Cumulative product coverage profile at 330681999

## Exact-commit evidence

- Product/test commit: `330681999d109dcf54f32d03210a064b1e2d3407`.
  Against `c28c3c9d3`, no `src` file changed. The new Abstractions tests
  exercise all three consume-observer converter stages with exact task,
  context, message contract, fault, input validation, and cancellation
  assertions. Requirement mapping and changelog accompany the tests.
- The first full gate at `164dab313` found two new test helper methods that
  violated the repository's bidirectional async naming convention. They were
  renamed to `InvokeAsync` and `RecordAsync` in `330681999`; the failed gate
  is not used as passing evidence or as a coverage source.
- The serial Release Unit/Architecture build at `330681999` completed with
  zero warnings and errors (`build.log` SHA-256
  `990e280019882094bf42290fdc914be6db3fe5b201e29ef3527f7b644ef68981`).
  The Abstractions test DLL and its covered product DLL both embed the full
  product/test revision.
- The serial Unit/Architecture gate passed **10,385/10,385** with zero
  failures and skips (`gate.log` SHA-256
  `f34f895bf5ee44557904c29c7ed9c1b7a09b62590cdf3cef2173a26f35985d66`).
- The fresh Abstractions Microsoft CodeCoverage run passed **801/801** with
  zero failures and skips (`abstractions-coverage.log` SHA-256
  `f3af143c537c315bbf03051a70f59ee4366f6822926a73fc5f9f1f6fc4464bc1`;
  Cobertura SHA-256
  `e76974cc7dcab3094918c2bf1a8bbac5bed6c26ecc574564609840af9d666109`).
- Read-only adversarial Red Team found a surviving post-dispatch cancellation
  mutant. The forwarding test was strengthened with a live caller token,
  exact observer-task identity, cancellation after notification, and pending
  state until the observer completes. Focused and complete Abstractions runs
  passed; Red Team re-review returned **PASS**.

## Cumulative result

| Measure | `330681999` | Previous `c28c3c9d3` |
| --- | ---: | ---: |
| Line coverage | 84,196 / 93,509 = 90.0405% | 84,169 / 93,509 = 90.0117% |
| Conservative branch observation | 30,210 / 36,670 = 82.3834% | 30,192 / 36,670 = 82.3343% |
| Methods with CRAP > 30 | 22 / 25,995 | 25 / 25,995 |

`ConsumeObserverConverter<T>`'s Pre, Post, and Fault methods each move from
0/9 measured lines and CRAP 42 to 9/9 lines and CRAP 6. The tests verify
that each stage delegates once to the matching generic observer method with
the exact context and fault, returns the observer's original asynchronous
task, propagates its failure unchanged, and rejects invalid inputs before
calling the observer.

## Merge and limits

`artifacts/coverage-a-plus-20260924-330681999/raw/` contains 50 parseable
reports for 32 product assemblies: 49 byte-verified inherited reports and
one fresh Abstractions report. Twelve broker fixture records are inherited;
no broker fixture ran in this iteration. All product source files are
unchanged from the previous profile. The JobSaga, Serialization, Retry,
RequestRate, and QoS source overlays remain in force; their stale-report
counts are respectively 16, 41, 41, 46, and 41. Cobertura has no stable
branch identities, so the conservative result takes the largest covered
count per branch location. The capped-sum estimate is not used as the quality
gate.

The merge is recorded in `analysis-50/summary.json` (SHA-256
`9d9e173d1cc2b98b1666fabbf29c6dde0ccdfde0571ce3a7dae2b93e2deebade`),
`analysis-50/methods.json` (`9ba17e8fa55faf400bf9568da9bbab8cfadc8a94718194d0cb9e5c5ca68a80f4`),
`provenance.json` (`cf59e62eeebf0bf047dc80648313b694c835e21d9fdbfd8ddd5953e9aca369df`),
and `overlay-policy.json` (`cb0d9d97302de9f54bf13cae3dd5c0fb732e9781848545b72d90d0c19007cc30`).

The Microsoft `code-testing-agent`, `coverage-analysis`, `test-gap-analysis`,
`assertion-quality`, and `run-tests` skills informed test design, adversarial
review, commands, and measurement. Global A+ remains open: branch coverage
is below A+, and 22 methods remain above CRAP 30.

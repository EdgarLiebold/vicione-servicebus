# Cumulative product coverage profile at 643b5ab4c

## Exact-commit evidence

- Product/test commit: `643b5ab4ca325e2a95fdd7161527f6e31f5ed9a4`.
  Against `70bb7e3ac`, exactly two product sources changed:
  `ConsumeContextOutputMessageTypeFilter.cs` and `OutputPipeFilter.cs`.
  Consume-output fault observers now preserve the original dispatch failure
  even when a typed or outer observer, or diagnostic logging, fails. The
  output-pipe fault notification was moved unchanged into a focused private
  method. New tests use a distinct untyped source and a real
  `MessageConsumeContext<T>` projection; they verify asynchronous observer
  order, context identity, original failure identity, and logger isolation.
  Requirement mapping and changelog accompany the fix.
- The serial Release Unit/Architecture build completed with zero warnings and
  errors (`build.log` SHA-256
  `4ccbd1336b8e6b82763c7f92dc3b00e3516d39fd6dfd3c46f6d6de22af01d0d6`).
  A preceding parallel build returned exit 1 without compiler diagnostics;
  its log is retained as `build-parallel-failed.log` and is not acceptance
  evidence. The Core test DLL and nine product DLLs in the fresh report embed
  the full product/test revision.
- The serial Unit/Architecture gate passed **10,415/10,415** with zero
  failures and skips (`gate.log` SHA-256
  `2f40b456383a0596bb8b7c7b1da16783fed4bf2b9a05afc74a7165c43eb181a0`).
- The fresh Core Microsoft CodeCoverage run passed **6,422/6,422** with zero
  failures and skips (`core-coverage.log` SHA-256
  `357dc412ad2ab7e74714a728d71c6e69276cce58ddcc2aa1589bd277b7a803db`;
  Cobertura SHA-256
  `fa4a48553163ad03bc271369456b85952f6e8038cfb582c5edfea6291332d0e7`).
- Read-only adversarial Red Team found that the first test fixture reused one
  proxy as both untyped source and typed context, allowing a wrong direct-cast
  implementation to pass. The fixture was corrected to return a distinct real
  typed projection. The focused tests passed **10/10**, and Red Team re-review
  returned **PASS** with no concrete remaining product or test gap.

## Cumulative result

| Measure | `643b5ab4c` | Previous `70bb7e3ac` |
| --- | ---: | ---: |
| Line coverage | 84,267 / 93,537 = 90.0895% | 84,244 / 93,522 = 90.0793% |
| Conservative branch observation | 30,245 / 36,682 = 82.4519% | 30,233 / 36,676 = 82.4327% |
| Methods with CRAP > 30 | 19 / 25,999 | 21 / 25,996 |

The consume-output `SendToOutputAsync` moves from 24/31 measured lines and
CRAP 33.78 to 23/23 lines and CRAP 18. The output-pipe counterpart moves
from 37/38 lines and CRAP 30.02 to 22/22 lines and CRAP 18. Each new fault
notification helper measures 17/17 lines with CRAP 12. The tests distinguish
the real typed projection from its untyped source and check that dispatch
failures remain primary when either observer group or its logger fails.

## Merge and limits

`artifacts/coverage-a-plus-20260924-643b5ab4c/raw/` contains 53 parseable
reports for 32 product assemblies: 52 byte-verified inherited reports and
one fresh Core report. Twelve broker fixture records are inherited; no broker
fixture ran in this iteration. For each changed filter source, 44 older reports
are excluded and only the fresh Core report is used. The JobSaga,
Serialization, Retry, RequestRate, QoS, and Azure batching overlays remain in
force. Their stale-report counts are respectively 18, 44, 44, 49, 44, and 3,
including the new Core report where applicable. Cobertura has no stable branch
identities, so the conservative result takes the largest covered count per
branch location. The capped-sum estimate is not used as the quality gate.

The merge is recorded in `analysis-53/summary.json` (SHA-256
`3bb83fd8d1c37972da79ad4bf998ba6ea0fa942a155d2505885d0e4da87eb520`),
`analysis-53/methods.json` (`f969d15c5f6ff098ada5f5fc725ca1fe98386e4eeb6fba97ef308f3976df739f`),
`provenance.json` (`62d917a7f06200094d2200d4f404e27571eff0a43432bcaff949079f8afb56bb`),
and `overlay-policy.json` (`7264c2cf8cb9dfe7901fcda2b054eb0b43bd43054b5bb1022a014ac217ae58b8`).

The Microsoft `code-testing-agent`, `coverage-analysis`, `test-gap-analysis`,
`assertion-quality`, and `run-tests` skills informed test design, adversarial
review, commands, and measurement. Global A+ remains open: branch coverage
is below A+, and 19 methods remain above CRAP 30.

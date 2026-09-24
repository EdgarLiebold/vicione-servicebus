# Cumulative product coverage profile at 9eea1cb5f

## Exact-commit evidence

- Product/test commit: `9eea1cb5fc4b976bccdded60472f18d472529ad9`. Against `643b5ab4c`, one product source
  changed: `src/ViciOne.ServiceBus.Sagas/Saga/InMemoryRepository/IndexedSagaDictionary.cs`.
  Its public `Add` method still acquires the dictionary lock, captures all keys,
  validates admission and publishes registrations in the original order. The
  unchanged publication and reverse rollback block is now a focused private
  method. Two new transaction tests assert exact callback order, original
  exception identity and ordered cleanup failures. Requirement mappings and
  changelog accompany the change.
- The serial Release Unit/Architecture build completed with zero warnings and
  errors (`build.log` SHA-256
  `08a44b56f923071afc6d1c2b5a6975352da138e1be07f6fa1ca04b9bd6b60c05`).
  The Core test DLL and nine product DLLs in the fresh report embed the full
  product/test revision.
- The full Unit/Architecture gate passed **10,417/10,417**, zero failures and
  skips (`gate.log` SHA-256
  `526114046a793f8c8320d0d163514e755cf7b8cc348f3bac964aa9d68f1550c8`).
- The fresh Core Microsoft CodeCoverage run passed **6,424/6,424**, zero
  failures and skips (`core-coverage.log` SHA-256
  `fb534b6b020a1134d98f9d0a88f9db2ee4ed7c268a2651c7696400136546ca29`;
  Cobertura SHA-256
  `910fd5531e1b0445ebbc7b317cdd2a1dfba962d7cfb17f2d81f45e57a99304cf`).
- Read-only adversarial Red Team first found that existing tests never reached
  a failed later publication after earlier successful registrations. The two
  new tests close that gap. Its re-review returned **PASS**. The unchanged
  lock position has no new dedicated test; the synchronous helper call remains
  visibly inside the same dictionary lock. Focused saga tests passed **69/69** before the
  additions, and the updated deep-contract class passed **19/19**.

## Cumulative result

| Measure | `9eea1cb5f` | Previous `643b5ab4c` |
| --- | ---: | ---: |
| Line coverage | 84,282 / 93,540 = 90.1026% | 84,267 / 93,537 = 90.0895% |
| Conservative branch observation | 30,251 / 36,682 = 82.4682% | 30,245 / 36,682 = 82.4519% |
| Methods with CRAP > 30 | 18 / 26,000 | 19 / 25,999 |

`IndexedSagaDictionary.Add` moves from 25/37 measured lines and CRAP 38.51 to
23/23 lines and CRAP 14. The new `ApplyRegistrations` measures 17/17 lines
and CRAP 8. The tests exercise both unwrapped primary failure and aggregated
cleanup failures; they verify reverse rollback and continued cleanup with
exact exception identity and order.

## Merge and limits

`artifacts/coverage-a-plus-20260924-9eea1cb5f/raw/` contains 54 parseable
reports for 32 product assemblies: 53 byte-verified inherited reports and
one fresh Core report. Twelve broker fixture records are inherited; no broker
fixture ran in this iteration. For the changed saga-index source, 31 older
reports are excluded and only the fresh Core report is used. Previous JobSaga,
Serialization, Retry, RequestRate, QoS, Azure batching, OutputPipeFilter and
consume-output overlays remain in force. Their stale-report counts are
respectively 19, 45, 45, 50, 45, 3, 45 and 45, including the new Core report
where applicable. Cobertura lacks stable branch identities; the conservative
result uses the largest observed covered count per branch location. The capped
sum is not used as the quality gate.

The merge is recorded in `analysis-54/summary.json` (SHA-256
`280d746424bfb4c51dc798242faad4d03cb7c6e1714903f49a82b4bc265509b8`),
`analysis-54/methods.json` (`ea3e37de83f2fb6e8a774d5d607d70bffeb53dcd8ecfbc3e845128ee5a25a44f`),
`provenance.json` (`37912d2a825b1a6cbbf8a95c0c5968b5266241e8a18e5b090ccca2287a1adc81`),
and `overlay-policy.json` (`496cf09259fbbf26014d152277deff6c687c1893caf819d7c4989d23c199afb6`).

The Microsoft `code-testing-agent`, `run-tests`, `coverage-analysis`,
`test-gap-analysis` and `assertion-quality` skills informed test design,
execution and review. Global A+ remains open: branch coverage is below A+,
and 18 methods remain above CRAP 30.

# State-machine schedule activity contract slice

## Scope

- Source and tests: `ScheduleActivity<TSaga, TMessage>` and `ScheduleActivity<TSaga, TMessage, T>`.
- Thirteen attributed methods execute fifteen focused cases. The Core requirement projection has
  one entry for each attributed method.
- The 36-report product-wide baseline remains `ac363722c`; this slice is not a new global profile.

## Product defects corrected

- Both public activity constructors now reject missing schedule, time provider, and message
  factory immediately with the exact parameter name. Previously, the two-parameter constructor
  accepted all three invalid values; the three-parameter constructor accepted the latter two.
- Both activities now reject an already-canceled context before saga or scheduler work and pass
  the context cancellation token into `ContextMessageFactory.UseAsync`. Previously, cancellation
  during schedule-token lookup could still invoke the message factory.
- The old null-conditional schedule-token assignment is a direct call after the constructor guard.

## Behavior checked

- A new schedule stores the provider-accepted token before continuation and does not cancel an
  absent predecessor. Replacing a schedule waits for both provider acceptance and prior-schedule
  cancellation before continuation; destination, due time, message, pipe, and cancellation token
  reach the scheduler unchanged.
- The scheduling header is read with the exact key, `Guid` type, and null default. The activity
  retains a schedule delivered by the current token, but cancels a different predecessor.
- Scheduling failure leaves the previous token and suppresses continuation. Cancellation failure
  preserves the newly accepted token, propagates the exact failure, and suppresses continuation.
- Both activity types reject pre-canceled work and cancellation that occurs during token lookup,
  before message construction, provider dispatch, or continuation. The latter cases specifically
  fail if the cancellation token is omitted from `UseAsync`.
- The typed activity covers no predecessor, own delivery, and foreign delivery, including order
  and exact scheduler arguments.

## Verification

- The new constructor and pre-cancellation tests failed against the old product code as expected.
- Final focused Microsoft CodeCoverage run: 15/15 passed, zero failures and skips. Cobertura:
  `artifacts/coverage-a-plus-20260923-ac363722c/core-scheduling/saga-activity-focused-final.cobertura.xml`,
  SHA-256 `cf919591e96a2ad3e95b0b27852596e10cf07a45a697569750991e3ce2c45d37`.
- Complete final-byte Core CodeCoverage run: 6,371/6,371 passed, zero failures and skips.
  Cobertura:
  `artifacts/coverage-a-plus-20260923-ac363722c/core-scheduling/saga-activity-core-final.cobertura.xml`,
  SHA-256 `8635fc848b2db909fd5b272a5dac9b95b48caaced3e3508066b0bdb729a88a11`.
- Complete final-byte Unit/Architecture solution: 10,184/10,184 passed, zero failures and skips.
  The first run exposed eight local asynchronous test helpers without `Async` suffixes;
  their names were corrected and the entire solution gate passed on the final bytes.
- Adversarial read-only review found four actionable gaps across its iterations: a permissive
  header proxy, missing cancel-failure outcome, constructor and early-cancellation defects, and
  missing interleaving tests for cancellation-token forwarding. All were resolved; final verdict
  was PASS with no concrete surviving mutant in the claimed scope.

## Focused method result

| Activity async schedule path | Previous product-wide CRAP | Complete Core lines | Reported branches | Current CRAP |
| --- | ---: | ---: | ---: | ---: |
| Two-parameter activity | 72 | 12/12 | 100% | 6 |
| Three-parameter typed activity | 42 | 13/13 | 100% | 6 |

The previous scores are from the full `ac363722c` profile, and the current scores are from the
complete Core report at this slice's source and test bytes. Product-wide A+ remains open.

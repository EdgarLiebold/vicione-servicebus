# Reliable-messaging retry-jitter phase (2026-09-22)

## Product findings and fixes

- `RetryJitterFraction = NaN` passed public DI options validation because range comparisons do not reject NaN. `ValidateAndFreeze` now requires a finite fraction. The invalid-policy regression matrix previously started with invalid store limits, so every case could fail on the same earlier setting. It now starts with valid limits and verifies the rejected property's name.
- `ValidateAndFreeze` kept all configuration branches in one method: the previous product-wide report measured 34/45 lines, a conservative 25/40 branches, complexity 40, and CRAP 63.37. Required configuration, capacity/retry, and timing checks now run in the same order through separate methods.
- At `TimeSpan.MaxValue`, floating-point jitter bounds could exceed `long` and produce an invalid interval. An intermediate saturated calculation selected the lower bound for a maximum seed; a first integer-only correction then collapsed the distribution in a two-tick window. Decimal interval bounds and an inclusive `UInt128` offset now preserve both outcomes and keep the result within the configured maximum.
- A valid large retry delay could make `now + delay` overflow before the failed intent was persisted. The retry due date now saturates at `DateTimeOffset.MaxValue`.

## Behavior evidence

- `Registration_InvalidRuntimePoliciesFailClosedWhenTheTypedSenderMaterializes`: every invalid setting starts from a valid policy and produces an options error naming that setting.
- `Registration_NonfiniteRetryJitterFailsBeforeTheTypedSenderMaterializes`: NaN and both infinities are rejected at the public DI materialization boundary. Before the fix, NaN was accepted in the valid baseline case.
- `Registration_InclusiveRetryJitterBoundsAreFrozenForDelivery`: 0 and 0.50 are admitted and retained in the frozen runtime policy after the mutable options change.
- `RetryDelay_WithMaximumRepresentableDurationRemainsDeterministicAndBounded`: stable, decorrelated delays at `TimeSpan.MaxValue`, including the seed that selects the upper bound and a tiny positive fraction. The red test against the intermediate calculation observed a lower-bound result for that maximum seed.
- `RetryDelay_TwoTickWindowSelectsBothRepresentableDelays`: zero and midpoint seeds choose both possible delays in a two-tick window.
- `RetryDelay_BeforeTheCeilingKeepsTheConfiguredJitterRange`: zero and maximum seeds choose exactly 80 and 120 ticks when the initial delay is 100 ticks, the maximum is 1,000 ticks, and jitter is 0.20.
- `TransportFailure_WithMaximumRetryDelayRetainsTheIntentAtTheLastRepresentableDueDateAsync`: one real transient dispatch failure persists a retry at `DateTimeOffset.MaxValue`, retains the record, and does not make it due the following day. Before the date fix, the delivery raised `ArgumentOutOfRangeException` before storage.

## Verification

- Release build of `ViciOne.ServiceBus.Tests.Unit.slnx`: 0 warnings, 0 errors in the direct command output. The build result was not separately archived. The no-build Microsoft Testing Platform Unit/Architecture solution gate passed **10,031/10,031**, with no failures or skips; its log is `artifacts/coverage-a-plus-20260922-jitter/unit-final-no-build-escalated.log`. The full Core Microsoft CodeCoverage run passed **6,280/6,280**; its log is `artifacts/coverage-a-plus-20260922-jitter/core-final-current.log`.
- Final Core Cobertura: `artifacts/coverage-a-plus-20260922-jitter/core-final-current.cobertura.xml` (SHA-256 `395538c3abd6b68d7bb493b5989f595f7c015dc64b5c8c6185c111a8896e28bc`); matching Core test DLL SHA-256 `8c2831cbbc192ea501418d9b797d5dd34699c81817587ad18dc17802966fcd05`. These ignored artifacts remain local.

| Measured method | Lines | Reported branches | Complexity / CRAP |
| --- | ---: | ---: | ---: |
| `ValidateAndFreeze` | 16/16 | n/a (0/0) | 1 / 1 |
| `ValidateRequiredConfiguration` | 7/7 | 6/6 | 6 / 6 |
| `ValidateCapacityAndRetry` | 15/15 | 24/24 | 24 / 24 |
| `ValidateTiming` | 13/13 | 12/12 | 12 / 12 |
| `CalculateRetryDelay` | 27/27 | 12/12 | 12 / 12 |

The source/test diff received an independent read-only adversarial PASS. The review found the NaN gap, the weak invalid-policy baseline, the maximum-seed and tiny-fraction arithmetic cases, the two-tick distribution regression, and the due-date overflow; each has a behavior test. Persistence of `DateTimeOffset.MaxValue` has been verified through the in-memory store, not each database provider. The prior 36-report product-wide profile at `a95505227` remains the latest complete aggregate; global A+ is still open.

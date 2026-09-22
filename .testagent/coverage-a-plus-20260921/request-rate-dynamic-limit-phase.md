# Request-rate admission and dynamic limit correction

## Product contract and defects

Eight new tests exercise public request admission with a controlled clock.
They cover current-window increases, exhausted-window decreases, cancellation
after partial reservation, concurrent decreases, interaction with adaptive
request concurrency, disposal of active and queued changes, invalid operations,
and a request canceled before it can acquire a concurrency slot. A waiting
request is present during a decrease and at the exact reset boundary. The
tests assert both newly admitted requests and requests that must stay blocked.

The first independent adversarial review exposed four real product defects:
partial reservation survived cancellation, concurrent changes counted the same
old limit twice, adaptive request-count changes wrote into `_rateLimit`, and
disposing the algorithm left pending changes waiting. Two strengthened tests
failed on the original implementation, then passed after correction.
Subsequent review found that the old rate semaphore separated admission from
the timer's count reset; a reset between those operations lost one interval's
capacity. The implementation now grants capacity and increments the interval
count under the same lock used by reset. A pending decrease reserves returned
capacity before newly waiting requests can consume it. An existing catch path
also returned a request-concurrency permit that a pre-canceled call had never
acquired; the new test verifies that concurrent admission remains at one.

`SemaphoreSlim.Dispose()` was removed from the synchronous algorithm disposal
path because pending `WaitAsync` continuations could otherwise remain stuck
after cancellation. Disposal now signals the shared cancellation source and
stops the rate timer. No `SemaphoreSlim.AvailableWaitHandle` is used.

## Validation on current source and test bytes

- Release build of `ViciOne.ServiceBus.Abstractions.Tests` completed with zero
  warnings and errors. The complete Abstractions suite passed 759/759, zero
  failures and skips, with Microsoft CodeCoverage and canonical settings. It
  includes the compiled Requirements projection for all eight mappings.
- The generated `ChangeRateLimitAsync` state machine has 53/56 covered lines,
  87.5% of its instrumented branches, complexity 24, and CRAP approximately
  24.089 in the final full-suite report. The new `WaitForRatePermitAsync` state
  machine has 12/13 covered lines and complexity 6. The previous 0/17 lines,
  CRAP 110 baseline is a different method body; the percentages are not a
  controlled before/after comparison. These are targeted measurements, not a
  new product-wide A+ result.
- Full-suite report SHA-256:
  `ce690b4d9637234abde7c84be3b5aecbf53026406712908e9a243efdc51232e5`.
  Test log and report are in
  `artifacts/coverage-a-plus-20260922-4488b29fe/rate-limit-phase/full/`.
  Failed and superseded reports are isolated in named sibling directories and
  do not contribute to this result.
- The final Unit/Architecture gate passed 10,005/10,005 with zero failures
  and skips on the current source/test bytes. Its log SHA-256 is
  `6ac7a64290f243457f16354258525c9de4ce2305843d2a09eb21c98649d07cb6`.
  A pre-final run passed 10,004/10,004 but started before the last source and
  test edits; it is retained as an intermediate run, not final evidence.

## Adversarial review

The final independent read-only review returned PASS: no further concrete
product or test defect was found after the acquisition, fairness, and Dispose
corrections. It independently matched the final report hash, 759/759 test log,
53/56 line count, 87.5% branch rate, complexity 24, and CRAP approximately
24.089. It does not prove fairness for every possible adversarial schedule.

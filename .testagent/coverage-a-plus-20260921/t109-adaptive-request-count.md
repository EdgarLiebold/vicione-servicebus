# T109 — adaptive request-count transaction

Implementation commit: `8142f0b29`.

T97 `3cb94a285` remains the latest product-wide Line/Branch/CRAP
checkpoint. This packet makes no new global grade claim.

In the previous algorithm, an empty completion at four permitted requests
published a new limit of two before it had drained two semaphore permits.
The completing request released one permit, so the shrink took that one and
waited for another. If the completion token canceled then, the count stayed
at two and the physical capacity later rose to three. The new test
`CanceledAdaptiveShrink_RestoresTheOriginalConcurrentRequestCapacityAsync`
failed 1/1 on the unchanged product (expected count 4, observed 2).

Adaptive count adjustments now use one serial gate. Each completion computes
its target from the latest committed count. A decrease publishes the count
only after all required permits are drained and refunds any partial drain if
it is canceled or otherwise faults. A queued change or a permit drain honors
caller and algorithm disposal cancellation. An uncontended no-change or
growth completion keeps the prior behavior when its token was already
canceled. The no-change regression test failed against the first correction
and passed after that path was restored.

Four manually authored behavioral tests and their requirement tuples cover:

- `CanceledAdaptiveShrink_RestoresTheOriginalConcurrentRequestCapacityAsync`:
  canceled partial drain restores the four-request limit and four physical
  permits, blocks a fifth request and preserves active accounting.
- `OverlappingEmptyCompletions_SerializeTheAdaptiveCapacityChangesAsync`:
  two overlapping decreases commit in order, then only one request can run.
- `DisposingAlgorithm_CancelsAnInProgressAdaptiveShrinkWithoutLeakingTheLeaseAsync`:
  disposal ends a waiting drain and all acquired leases settle without a
  negative count.
- `AlreadyCanceledCompletionToken_WhenRequestLimitCannotChange_StillCompletesAsync`:
  a no-change completion remains successful, releases its permit, and admits
  a healthy successor.

Microsoft test-gap and assertion-quality review: the early-publication
counterexample was executed on the original source; the no-change token
counterexample was executed against the intermediate correction. A missing
partial-drain refund would strand the fourth recovery request; omitted
serialization would allow concurrent changes to use a stale count. New
assertions span exceptions, published limits, actual blocking, active lease
counts and recovery. None is assertion-free or coverage-only.

The final build passes with zero warnings/errors. Focused
`RequestRateAlgorithmTests` pass 39/39; the complete Abstractions test project
passes 962/962 with zero failures/skips on the exact implementation commit.
Final independent read-only Red Team review: **PASS**, no remaining concrete
P1/P2. Its review did not rerun tests. The product-wide 33-profile measurement
follows the grouped work-volume cadence.

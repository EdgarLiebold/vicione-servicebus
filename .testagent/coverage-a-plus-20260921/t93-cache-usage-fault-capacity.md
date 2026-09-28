# T93 — resource-cache usage faults and capacity release

Exact test commit: `9a947a4c8`.

## Product contracts checked

`UsageDetachFailure_DuringCapacityEvictionReleasesTheResourceAndAdmitsItsSuccessorAsync`
puts a throwing usage-event removal on a capacity-one cache's eviction path.
The old subscriber is removed, its resource is disposed exactly once, its
index key disappears, and a successor is admitted and later disposed exactly
once. A linked, timed cancellation token bounds the capacity wait.

`UsageSubscriptionAndCompensationFailures_DoNotLeakSubscriptionOrStrandCapacityAsync`
puts distinct exceptions in usage-event registration and its compensating
removal. The cache keeps the committed resource usable, removes the partial
subscription, logs both distinct failures with their exact exception
identities, and can evict that resource to admit a successor.

## Verification

- Focused `ResourceCacheLifecycleTests`: 15/15 passed, no failures or skips.
- Complete Core project on exact commit `9a947a4c8`: 6,965/6,965 passed,
  no failures or skips.
- An isolated diagnostic counterprobe logged the original subscription
  exception for compensation. The new test failed on exception identity.
- An isolated capacity counterprobe left `_retiringEntries` unreleased. Both
  new successor admissions failed by their bounded cancellation after ten
  seconds; neither test hung. The production file was restored with no Git
  diff and the focused class passed again.
- Read-only Red Team found two P2 oracle weaknesses: an unbounded underlying
  capacity wait and undifferentiated diagnostic exceptions. Both were fixed.
  A further check found that the outer timeout alone left the underlying
  operation active, so the AddAsync token was bounded too. Final independent
  re-review: PASS with no remaining P1/P2 finding.
- The two new requirement variants are projected in `CoreRequirements.json`.

## Measurement boundary

No product source changed. T85 remains the latest complete 33-profile
Line/Branch/CRAP checkpoint. Global A+ remains open; complete measurement
follows the agreed larger packet cadence.

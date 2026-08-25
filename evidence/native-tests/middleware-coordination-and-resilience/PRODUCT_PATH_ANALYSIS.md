# Middleware coordination and resilience

## Decision boundary

This cohort preserves useful middleware behavior rather than the historical fixture shape or API
accidents. The complete product paths for latest-value observation, one-time setup, cancellation,
forking, nested pipes, partitioning, rate and concurrency limiting, filter observation, rescue,
retry and circuit breaking were read together because their ordering and failure semantics compose.

## A+ product contract

- latest-value observation publishes the most recently entered context with safe cross-thread
  visibility, including an entry whose downstream continuation later fails;
- one-time setup is single-flight, preserves cancellation, permits a healthy retry after terminal
  failure, lets an already queued healthy caller complete, and cannot be evicted while running;
- cancellation can win without leaving a later source failure unobserved;
- forked branches start independently, join before completion and preserve both failures;
- equal partition keys serialize while different partitions may run concurrently; all public key
  providers reject null rather than silently routing it as an empty key;
- rate and concurrency limits support repeated changes in both directions and roll back partial
  permit acquisition when an adjustment is cancelled;
- rescue preserves the exact diagnostic exception, respects handle/ignore filters, rejects a null
  rescue-context result and never hides failure of the rescue pipe itself;
- distinct retry policies retain their independent attempt budgets;
- a circuit breaker reads its current state lock-free on the message path, serializes only rare
  state transitions, assigns every timer and timeout enumerator to exactly one state and uses the
  configured `TimeProvider` for all time and timer behavior.

## Product defects corrected

1. A failed or cancelled one-time setup no longer poisons the context permanently; queued callers,
   retry and eviction now have explicit state transitions.
2. `OrCanceled` now validates inputs, returns an already-cancelled task instead of throwing during
   invocation, and observes a source task that faults after cancellation won.
3. `LatestFilter` now publishes and reads its reference with volatile semantics.
4. Rate and concurrency filters now apply repeated down/up changes to the current limit and return
   every permit acquired by a cancelled decrease.
5. Rate limiting and circuit breaking now accept the standard .NET `TimeProvider`; no production
   test depends on wall-clock delays.
6. Partition construction and configuration reject invalid inputs, including null runtime string
   and byte keys.
7. Fork and rescue construction reject null dependencies; rescue rejects a null factory result.
8. Circuit-breaker transitions no longer allocate losing-state timers before a compare/exchange.
   A small transition lock now determines the sole successor and resource owner, while each send
   retains the state that admitted it for its complete callback lifecycle.
9. Circuit-breaker implementation states are internal details rather than accidental public API.
10. Cache cleanup no longer loses a request that arrives while the current cleanup completes. The
    scheduling flag is reset while holding the same lock that protects cleanup requests, so a
    concurrent caller either joins the active pass or schedules the next one.

## Preserved intentional behavior

The circuit breaker's active threshold remains an activation boundary: it opens only after the
number of attempts exceeds that threshold. This is deliberate in the inherited implementation and
fixture, and is distinct from the failure-percentage threshold. Rescue matches an
`AggregateException` by its base exception but passes the complete aggregate to the rescue context
so diagnostic information is not lost.

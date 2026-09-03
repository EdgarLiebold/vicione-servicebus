# V5 durable sender assertion and gap review

Date: 2026-09-03

The review covers the 57 package-owned xUnit 4 methods across Abstractions, Core, EF, real InMemory integration, and
Architecture.

| Metric | Result |
|---|---:|
| Test methods | 57 |
| Assertion calls | 335 |
| Average assertions per method | 5.88 |
| Assertion-free methods | 0 |
| Trivial-only methods | 0 |
| Self-referential/tautological methods | 0 |
| Distinct assertion APIs | 18 |
| Meaningful assertion categories | 11/12 |

The suite uses equality, boolean, null, exception, runtime type, string, collection, comparison, negative, state/side-
effect, and structural/deep assertions. Approximate numeric assertions are intentionally absent because the package owns
exact byte/count/time/state contracts rather than floating-point approximation.

The strongest cases observe more than return values: atomic capacity state after concurrent writes, exact persisted
failure evidence, lease/generation fencing, state after early and late completion races, absence of payload/control data
from diagnostics and wire headers, DI ownership conflicts, connection-state restoration, real SQLite rows and recovery,
and real InMemory logical-consumer completion.

Pseudo-mutation analysis found three provider-level weaknesses and all were closed before acceptance:

1. EF count and byte capacity were correlated. The owner now runs independent concurrent count-limited and byte-limited
   cohorts, killing removal of either server predicate.
2. EF exact idempotence did not directly vary metadata. The lifecycle now rejects the same id with changed metadata.
3. EF lease fencing did not directly present a stale lease after takeover. The lifecycle now asserts the stale owner is
   rejected before the current owner advances.

One source-order swap in the InMemory completion filter was behaviorally equivalent because the existing downstream pipe
already awaited receive-owned work. It is not presented as a behavioral survivor. The architecture test deliberately
retains the explicit `ReceiveCompleted`-before-callback source contract as defense in depth against later pipeline changes.
All 33 sampled high-risk behavioral mutations are empirically killed after the three improvements.

# T105 — RabbitMQ quorum configuration and broker topology

Implementation commit: `fbb25b940`. T97 `3cb94a285` remains the latest
product-wide Line/Branch/CRAP checkpoint; this packet makes no new global
grade claim.

Calling `SetQuorumQueue(3)` and then `SetQuorumQueue()` left the old
`x-quorum-initial-group-size` in the declaration. A later call without a size
now clears it. Invalid factors are rejected before mutating a prior valid
declaration.

The broker builder already made quorum queues durable, but could still emit
`AutoDelete=true` and, for direct declarations, `Exclusive=true`. It now
normalizes those queue flags to false with or without `x-expires`. The
exchange's configured durability and auto-delete behavior remain observable
and unchanged. The focused tests also exercise final delivery-mode arguments,
combined invalid batch limits and recovery, and built-host address ownership.

The stale group-size test failed red-first, as did the auto-delete case
without expiration. The final 32 focused tests and complete RabbitMQ unit
project (525/525, no failures or skips) pass on the implementation commit.
Test-gap and assertion review found concrete settings, topology, binding
destination, error and recovery oracles; the new cases are not coverage-only.

The first read-only Red Team review found an invalid-repeat gap and identified
the auto-delete broker defect after ruling out an incorrect durability
concern. Its final review found two assertion/claim gaps and one documentation
mismatch; all were corrected. Final read-only Red Team review: **PASS**, no
remaining concrete P1/P2 finding. This is packet seven after T97. The global profile remains on the
20–30-packet cadence or an earlier cross-cutting change.

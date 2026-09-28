# T101 — classic EF transactional outbox session provenance

Implementation commit: `2957da1d2`. Latest product-wide checkpoint remains
T97 `3cb94a285`; no new global Line/Branch/CRAP grade is claimed here.

## Product faults and behavioral evidence

- The old classic outbox treated `OutboxState.State == Unchanged` as proof of a
  database save. `AcceptAllChanges` or manually changing the tracked state
  could signal delivery and start a new batch without durable intent. The new
  session owns exact State and Message entries, validates them before any
  SaveChanges, and completes only after the EF save event includes the whole
  batch.
- A detached staged message could let the State persist alone. The preflight
  rejects this before a partial write and preserves the session for abort.
- Mutating `Status` to Delivered or `LastSequenceNumber` to `long.MaxValue`
  could make the delivery worker delete or skip unsent messages. All initial
  delivery-state fields are now checked before persistence.
- Abort previously searched by a mutable OutboxId, which could leave an owned
  message attached and detach a foreign message. It now detaches exact owned
  references; a controlled counterprobe restoring the ID condition made the
  ownership test fail, then was reverted.
- A failed first factory call left an empty active State. Message creation now
  precedes State attach. If EF `ChangeTracker.Tracked` throws after attaching
  State or Message, reference-based compensation removes the partial entry.
  Both throw positions failed red-first and pass after the fix; healthy retry
  persists exactly one batch and signals once.
- A successful external `SaveChanges(false)` now accepts only owned classic
  Outbox entries and supports repeat commit plus a new batch while preserving
  caller-owned business tracker state.

Seven new T101 requirement variants and one corrected existing variant map to
the focused tests. The class passed 22/22; the complete EF project passed
368/368 with no failures or skips on exact commit `2957da1d2`. Independent
read-only adversarial re-review: **PASS**, no concrete P1/P2 remains.

The caller-owned outer transaction and SaveChanges-interceptor trust boundary
are as documented for T100 in `docs/reliability.md`. This packet did not run
the 33-profile global aggregate; the agreed multi-packet interval applies.

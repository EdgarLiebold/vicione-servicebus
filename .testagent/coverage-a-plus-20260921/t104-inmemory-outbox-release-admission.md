# T104 — in-memory outbox release admission

Implementation commit: `d954933e6`. T97 `3cb94a285` remains the latest
product-wide Line/Branch/CRAP checkpoint; this packet makes no new global
grade claim.

The in-memory outbox checked release before acquiring its deferred-operation
lock. A producer could observe unreleased state, wait for that lock, then
enqueue after release and the final drain, leaving an acknowledged send
stranded. Deferred scheduler cancellation had the same race. Admission now
checks the cancellation token and release state while holding the queue lock.
The scheduler retains its checkpoint lock for atomic admission but invokes
immediate provider callbacks only after leaving both internal locks.

The two original send and scheduler races failed red-first (0/2). Five new
behavioral tests now force the lock-wait sequence, assert exact one-time
delivery and an empty queue, and verify cancellation at both sides of release.
They also reject a token canceled while the producer waits for admission,
preserve that exact token, prove zero forbidden side effects, and observe that
the provider callback runs outside the scheduler lock. Focused tests pass
5/5. Assertion and test-gap review found no count-only, assertion-free, or
coverage-only test. The scheduler proxy checks destination, token ID,
cancellation token, call count, and lock state.

The complete Core project passes 7,043/7,043, with no failures or skips,
on exact implementation commit `d954933e6`.

The first read-only Red Team review found two P2 issues: provider callback
under `_listLock` and a missing cancellation recheck after lock wait. Both
were corrected with the additional deterministic tests. Final read-only
Red Team re-review: **PASS**, no remaining concrete P1/P2.

This is packet six after the T97 global checkpoint (T98, T100, T101, T102,
T103, T104; T99 was review-only). The global profile remains due on the
agreed 20–30-packet cadence or earlier after a cross-cutting change.

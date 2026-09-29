# T106 — reliable inbox distinct cancellation

Implementation commit: `2882773e6`. T97 `3cb94a285` remains the
latest product-wide Line/Branch/CRAP checkpoint. This packet makes no new
global grade claim.

Both reliable inbox providers now link the consume-context delivery token
with the explicit operation token across acquisition, waiting, processing,
and completion. They preserve the original canceled source token in the
propagated exception. The in-memory consumed fence checks delivery
cancellation even when the caller passes another live token. EF checks both
tokens while saving the consumed fence and during its final transaction
commit; canceled work rolls back the business state and outgoing intent.

The new tests exercise pre-canceled sources, cancellation after acquisition,
a provider-side Busy wait, cancellation after a successful EF
`SetConsumedAsync` before transaction commit, and a real transport context
that buffers a uniquely identified outgoing send before delivery
cancellation. They inspect the exact exception token, callback count, lease
attempt, retry and quarantine state, database rows, durable send store, and
external delivery. The original delivery-cancellation cases failed red-first.
An artificial context could not publish, so the outgoing-send case uses a
real transport context with an independently cancellable delivery view.

Focused Core cases pass 3/3 and focused EF cases 5/5. Complete Core passes
7,046/7,046; complete EF passes 376/376, with no failures or skips. Both
project builds pass with zero warnings and zero errors. The EF suite ran on
the final source content before the implementation commit; no EF file
changed afterward. Read-only Red Team initially found three cancellation
and proof gaps, then two coverage gaps, and finally an unbounded Busy-wait
test. All were corrected. Final re-review: **PASS**, no concrete P1/P2.

The next global coverage and CRAP measurement follows the larger
work-volume cadence. A+ Line and Branch targets remain open.

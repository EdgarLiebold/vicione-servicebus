# T107 — receive-side outbox cancellation ownership

Implementation commit: `b200c1148`.

T97 `3cb94a285` remains the latest product-wide Line/Branch/CRAP
checkpoint. This packet makes no new global grade claim.

Classic InMemory and EF receive outboxes now keep the delivery token
authoritative when the caller supplies a different live operation token.
State transitions and outgoing admission reject a canceled delivery. The
InMemory factory preserves the original source token before acquisition and
while waiting for an inbox lock. The EF factory links both sources through
the final transaction commit, rolls back a canceled consumed fence, and
remaps cancellation from the internal linked token. The EF context also
remaps cancellation during save, ordered load, delete, and outgoing write.

Reliable InMemory and EF outgoing admission likewise checks or links the
delivery token when a distinct send token is active. Cancellation during
serialization or a blocked EF outbox write rejects the outgoing intent.

Tests exercise pre-cancel, inbox lock wait, six classic state operations per
provider, cancellation during SQLite save, ordered query and delete,
cancellation immediately before EF commit, healthy retry, a second reliable
EF intent canceled during serialization after a first intent is staged, a
blocked scoped outbox write, and cancellation during both classic and reliable
InMemory serialization. The gated and serialization cases exercise both
delivery and operation cancellation sources. They
assert the exact source token, unchanged persisted rows and outgoing store,
absence of external delivery, and appropriate inbox attempt state. The
original consumed-fence and linked-token bugs failed red-first. A subsequent
adversarial review found three in-flight proof gaps; dedicated gated tests
now exercise each formerly bypassed protection.

Final read-only adversarial re-review: **PASS**, with no remaining concrete
P1/P2. The reviewer confirmed that the second EF intent reaches the tracked
capacity path and that both cancellation sources are exercised at the gated
and serialization boundaries.

The focused cases and builds pass with zero warnings and errors. Complete
Core passes 7,060/7,060 and EF passes 394/394 with no failures or skips.
Global Line and Branch A+ remain open; the next 33-profile measurement
follows the grouped work-volume cadence.

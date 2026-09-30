# T162 inbox and outbox identity across serializer admission

## Product defect

The classic EF outbox factory read a send context's message ID before a direct
serializer callback but did not verify that callback left it unchanged. Its two
productive callers, the transactional scoped bus context and the receive-side
outbox context, performed bounded payload admission before calling the factory.
The first factory-only guard therefore did not protect either productive path:
the serializer could change ID A to B during `Admit`, and the caller could stage
B. The in-memory reliable inbox likewise used the post-admission ID for its
buffered send instead of preserving the incoming send identity.

## Red-first behavioral evidence

- Direct EF factory: both serializer changes, A to another GUID and A to
  `Guid.Empty`, returned an outbox message without the required rejection.
- Productive EF `AddSendAsync`: four variants across transactional and receive
  outboxes failed. A to another GUID passed admission; A to `Guid.Empty` failed
  for the wrong reason after serialization. The test checks the resulting
  context identity, absence of tracked outbox message and state, and absence of
  a persisted outbox row.
- Registered in-memory inbox pipeline: A to another GUID committed an outgoing
  send; A to `Guid.Empty` failed only at durable-record validation. The test
  checks the underlying rejection, an empty durable outbox and the retained
  inbox retry state using a fixed clock.

## Correction and verification

The productive EF callers now capture and validate the ID before bounded
admission and compare it after admission, before factory creation or EF tracking.
The factory separately checks the direct and deferred body path. The in-memory
inbox checks the initial ID before admission and compares it after envelope and
metadata capture, before appending a buffered send.

- EF direct factory regression: 2/2 green.
- EF productive-path regression: 4/4 green.
- In-memory inbox regression: 2/2 green.
- Complete EF suite: 409/409 green.
- Complete Core suite: 7,389/7,389 green.
- Read-only adversarial review found a P2 in the first factory-only fix. After
  both productive EF callers and their tests were corrected, the second review
  found no concrete P1/P2 in the bounded diff.
- The last exact-commit 33-profile Line, Branch and CRAP baseline remains
  `product-wide-profile-096e8ecfa.md`. No new product-wide measurement was
  taken for this defect packet; these new tests exercise product identity and
  persistence behavior rather than a coverage target.

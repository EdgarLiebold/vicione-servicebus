# T172 durable metadata admission boundary

## Confirmed defects

- A scheduled typed durable send could serialize its body, then let a header
  getter change `CorrelationId` while replay metadata was captured. The changed
  value reached durable admission. The new test failed against the prior code
  because admission was invoked instead of rejecting the drift.
- Both EF outbox families could form a persisted intent after header metadata
  capture changed `CorrelationId`. The transactional regression initially
  staged an outbox record; the reliable staging regression covers its separate
  `DurableSendRecord` path. A serializer `ContentType` getter could also change
  `DestinationAddress` during admission after EF had selected the old local
  destination.
- Header fingerprinting itself can execute application converters/getters.
  Checking scalar metadata before those callbacks allowed a mutation during
  the guard to escape the same guard.

## Correction and behavioral evidence

- `TypedDurableSender` now includes body read, payload proof, and replay metadata
  capture in the shared transport stability check.
- `EntityFrameworkScopedBusContext` holds its expected metadata from before
  admission through complete record creation. `OutboxMessageFactory.CreateAdmitted`
  applies the same boundary to classic transactional and receive outboxes.
- The shared body guard and journal check header callbacks before checking
  scalar metadata; the body guard restores mutations even when preflight
  throws. The body getter checks headers before its final metadata check.
- Typed durable and classic EF tests failed against the prior behavior. The
  final tests assert rejection, restored context, and absent durable admission
  or EF tracking. Separate tests cover typed durable send, classic EF staging,
  reliable EF staging, and receive outbox.
- Complete EF Unit project: 413/413 passed. Complete Unit solution:
  12,322/12,322 passed, zero failures or skips, including Core, MessagePack,
  EF, RabbitMQ, Azure Service Bus, and architecture tests. The solution run
  exited 0 after 6m 34s; `git diff --check` is clean.

The exact product-wide coverage/CRAP profile for this change is still open.
The last accepted profile is T171 at `15411740a`; it reported eight methods
with CRAP > 30. A read-only adversarial review of this T172 change found no
remaining concrete P1/P2 after the reported pre-admission destination drift
was corrected.

# T163 classic in-memory receive outbox identity

## Defect

`InMemoryOutboxConsumeContext<TMessage>.AddSendAsync` checked only whether the
outgoing `MessageId` was present. It then invoked the public serializer and read
`MessageId` again while constructing the stored record. The serializer could
change the ID immediately in `GetMessageBody` or later when the returned
`MessageBody` was converted to transport text. An empty ID was also accepted.

## Red-first behavioral test

`ReceiveOutboxMessageIdentityTests.AddSend_RejectsImmediateAndDeferredMessageIdChangesWithoutAppendingAsync`
uses the real consume context and inbox entry. Four variants change the original
ID to a different GUID or `Guid.Empty`, immediately or during transport-text
materialization. Before the fix all four completed without the required
exception. The test requires a `MessageException`, proves the serializer ran,
checks that no outgoing record was appended or consumption committed, then
sends a valid message through the same context and verifies its exact ID and
body in the single stored record.

## Correction and verification

The context now requires a nonempty ID before serialization, materializes the
transport body before checking identity drift, and builds the stored record
from the original ID. The append and cancellation checks remain after the
guard.

- Focused regression: 4/4 green.
- Complete Core suite: 7,393/7,393 green.
- Read-only adversarial review: no concrete P1/P2; the direct consume context
  fixture is sufficient for this synchronous append contract.
- The prior exact-commit 33-profile Line, Branch and CRAP baseline remains
  `product-wide-profile-096e8ecfa.md`. No new product-wide measurement was
  taken for this bounded product-defect iteration.

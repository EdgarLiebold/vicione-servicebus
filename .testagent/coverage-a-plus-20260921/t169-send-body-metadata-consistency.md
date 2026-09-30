# T169 serialized send metadata consistency

The accepted product-wide coverage and CRAP baseline remains
`product-wide-profile-096e8ecfa.md`. This packet targets confirmed behavior
defects without repeating the 33-profile measurement.

## Red-first defects

- A body callback could change RequestId, CorrelationId, ConversationId,
  InitiatorId, ScheduledMessageId, an address, TimeToLive, or SupportedMessageTypes
  after the payload was created. The transport could then deliver bytes and
  metadata describing different messages. An InMemory bus regression reproduced
  each field group, and a RabbitMQ provider regression reproduced correlation
  drift at the published frame.
- A PreSend observer could cache the body and then change CorrelationId before
  the provider's first materialization. A serializer could also mutate
  CorrelationId or SupportedMessageTypes within the first body getter after
  creating an envelope. Real bus tests reproduced both cases.
- ActiveMQ chose its native ReplyDestination before body materialization. A
  callback could change that destination afterward, leaving the context and
  actual route inconsistent. The provider test reproduced an accepted send.
- The first correction rejected Azure Service Bus's legitimate broker-assigned
  ScheduledMessageId and MessagePack's legitimate forwarding type rebinding.
  Separate tests reproduced both regressions before their corrections. A
  public serializer decorator around MessagePack forwarding reproduced a
  further false rejection and is covered by the same parameterized test.

## Resolution

The send context binds stable metadata and contract types to the serialized
body. The common transport materializer compares and restores them before and
after body reads; the outgoing journal does the same for subsequent captures.
ActiveMQ also compares its native reply destination around the read. Azure
Service Bus explicitly accepts its broker-confirmed scheduling token after
successful scheduling. The internal JSON and MessagePack forwarding serializers
authorize their intentional initial contract-type rebinding on the send context,
including when wrapped by a public serializer decorator; later mutations are
rejected. The public API has no new member.

## Verification

- Red-first failures observed for identity, route and expiry, cached body,
  getter-time correlation and contract types, ActiveMQ native routing, Azure
  broker scheduling, and MessagePack forwarding.
- Focused MessageJournal integration class: 34/34 passed.
- Focused MessagePack forwarding class: 7/7 passed.
- Focused ActiveMQ temporary destination class: 13/13 passed.
- Focused Azure Service Bus metadata class: 5/5 passed before the final
  internal-access adjustment.
- Complete EntityFrameworkCore project: 409/409 passed after legacy tests were
  updated to assert restored, rather than rejected, message identities.
- The complete Core project reached 7,425/7,426 after the first integration
  pass; its sole remaining test asserted the old payload-admission wording.
  The product behavior and context restoration already passed, so that test
  now asserts the stable MessageId rejection instead.
- Final complete Unit solution: 12,299/12,299 passed, zero failed or skipped,
  including Core 7,426/7,426, EntityFrameworkCore 409/409, architecture,
  Azure Service Bus, ActiveMQ, RabbitMQ, JSON and MessagePack tests. The first
  broad run exposed legacy assertions and the new test's async naming; those
  were corrected before this complete green rerun.
- Generated Apache change-list evidence matches the working tree (16,837
  entries); `git diff --check` is clean.
- Final read-only adversarial review: no remaining reproducible P1/P2 in the
  T169 diff. Its direct MessagePack, decorated forwarding, ActiveMQ native
  route, and Azure broker-token findings were resolved and regression-tested.

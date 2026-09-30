# T170 native send metadata consistency

The accepted product-wide coverage and CRAP baseline remains
`product-wide-profile-096e8ecfa.md`. This packet closes confirmed provider
behavior defects without repeating the 33-profile measurement.

## Red-first defects

- A RabbitMQ body callback could change the exchange or routing key after
  serialization and publish the bytes through the new route. A cached body
  could similarly be published after the route changed.
- For a RabbitMQ send carrying a durable acceptance requirement, a body
  callback could change Delay, Durable, Mandatory or AwaitAck after the
  initial acceptance validation. The send could then be marked accepted
  despite publishing with different delivery guarantees.
- An Event Hubs body callback or cached body could change PartitionId or
  PartitionKey before batch creation. The provider could send the bytes to a
  different partition route. The cached-body regression also exposed a
  conversation identity assigned after the first body read.
- Adversarial review found that RabbitMQ's legitimate mandatory-routing
  requirement from a distinct PublishContext was applied after native
  metadata had been bound. A successful publish then caused a false mutation
  on a later body read or journal capture. The augmented existing provider
  test reproduced the false rejection before correction. The first correction
  moved the union before materialization, but a second adversarial pass found
  that an earlier observer could already have cached the body. The same test
  reproduced this second false rejection before the final correction.

## Resolution

The shared send-context body binding, transport materializer and outgoing
journal capture compare and restore provider-native metadata through an
internal contract. RabbitMQ binds exchange, routing key and delivery
guarantees; Event Hubs binds partition route. RabbitMQ applies the distinct
PublishContext mandatory requirement before body materialization and, when a
body was cached earlier, rebinds only this validated provider update. Event Hubs
assigns ConversationId when constructing the send context so the first body
and later send use the same identity. No public API member was added.

## Verification

- RabbitMQ send-context class: 55/55 passed after the final correction.
  The new throwing-serializer regression verifies exception identity,
  restored AwaitAck, no acceptance and no publish.
- Event Hubs batch-sender class: 18/18 passed after the final correction.
- Read-only adversarial review found both mandatory-routing false positives;
  the expanded regression failed before and passed after each correction.
- The complete Unit solution and exact-commit receipt are recorded in the
  commit or subsequent review-preparation packet.

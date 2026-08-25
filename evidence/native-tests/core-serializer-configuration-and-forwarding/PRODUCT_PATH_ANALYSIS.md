# Core Serializer Configuration and Forwarding Product-Path Analysis

Date: 2026-08-24

## Files read completely

The analysis followed the public forwarding overloads through send-context construction, context
projection, serializer selection, body creation, transport header materialization, envelope update,
deserialization, and interface-to-concrete dispatch. It included the System.Text.Json envelope and
raw serializers, MessagePack forwarding serializer, mediator serializer context, copy-body fallback,
configuration collection, JSON option callbacks, `MessageSendContext`, in-memory transport, delayed
and scheduled redelivery, and all implementations of `SerializerContext.GetMessageSerializer()`.

## Product defects

Three previously discovered serializer-configuration defects are already fixed in product commit
`07bed594a0e1ce685a1ade9719d5ee9cc98e03d7`:

- named but unregistered serializer and default-deserializer media types no longer validate clean;
- per-message JSON configuration uses the callback's returned options and rejects null;
- global JSON configuration receives a defensive copy, uses the returned options, and rejects null
  without replacing the live options.

This review found and directly corrected two additional unambiguous defects:

- probing a `ForwardMessagePipe<T>` without an optional child pipe dereferenced null;
- `PipeExtensions.IsEmpty()` returned false for `Pipe.Empty<T>()` and therefore contradicted both its
  name and its complement for every non-null pipe.

Both corrections have ordinary source-parallel xUnit coverage and deliberate regression mutations.

## Why forwarding has three consecutive preservation stages

The three stages are not alternative transports or competing sources of truth. They run in order
and have different consumers:

1. `ConsumeSendPipeAdapter<T>` applies the general rules for every send initiated while consuming a
   message. `TransferConsumeContextHeaders` adds the source consume context as a payload, derives
   source/conversation/initiator metadata and copies only non-ViciOne headers. This is causal
   inheritance for ordinary send, publish, response and fault flows; it does not promise exact
   forwarding identity.
2. `ForwardMessagePipe<T>` applies the stronger forwarding contract. It projects the original
   identifiers, addresses, every header including ViciOne-internal headers and remaining lifetime
   into the outgoing `SendContext<T>`, then lets the optional caller pipe deliberately override that
   state. Transports, observers and middleware consume this context before or independently of body
   serialization.
3. The serializer context clones the received envelope to preserve the concrete payload and original
   supported-message-type set when the consumed generic type is only an interface. When the body is
   requested, the cloned envelope overlays the final send-context values so endpoint and caller-pipe
   changes win on the wire.

Removing stage 1 breaks causal metadata for the general consume-to-send path. Removing stage 2
leaves transport-visible metadata and caller-pipe input incomplete, including internal forwarding
headers deliberately excluded by stage 1. Removing stage 3 loses concrete-only body members and
original type identity. Their agreement in the ordinary JSON case can hide a defect in any stage,
so the replacement test observes the send context before serialization and the received forwarded
envelope independently, with separate ordinary and ViciOne-internal headers.

The raw `CopyBodySerializer` fallback is not counted as a second normal preservation route. The
current non-null `ConsumeContext.SerializerContext` contract makes it unreachable for internal
contexts, but the public serializer remains a useful explicit opaque-body capability. It is not
removed as incidental cleanup.

## Forwarding expiration

Generic forwarding runs the optional caller pipe before it makes the final expiration decision.
Only a positive final `TimeToLive` deliberately revives an inherited message that has already
expired. Clearing the inherited value, leaving it non-positive, or forwarding a replacement message
without a positive override does not revive it.

The forwarding pipe marks such a send without serializing it. The marker is consumed at every actual
dispatch or durable-storage boundary: the normal transport, the persistent outbox, and the mediator.
The message is logged once as structured `FORWARD-EXPIRED` information and is then discarded before
send observers, serialization, transport dispatch, persistent-outbox storage, or mediator dispatch.
It therefore creates no retry, fault, dead-letter, or successful-send signal. Deferred in-memory and
transactional outboxes use the same final transport boundary and do not duplicate this policy.

This rule applies only to generic forwarding. Responses and faults keep their separate one-second
minimum lifetime so that an expired request can still receive its terminal outcome.

## Larger independent normalization

`JsonMessageEnvelope.Update` and `MessagePackEnvelope.Update` currently duplicate the same metadata
projection, and both hard-code wall-clock reads for expiration. Collapsing the three stages above
would be wrong; sharing the serializer-independent implementation inside stage 3 would be right.
The root `TODO.md` records that dedicated product slice together with the required transport matrix,
deterministic time, delayed-redelivery coverage, and metadata-projection normalization. The
forwarding-expiration policy above is already resolved and must not be reopened or reimplemented in
that later normalization.

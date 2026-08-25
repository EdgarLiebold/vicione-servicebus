# Message identity, conversation, time, and header product-path analysis

## Scope and ownership

The five inherited fixtures are migration evidence, not the specification. Their complete contents,
the semantic ledger, the connected product source, and the relevant source history were read before
the replacement was designed. The permanent owners are:

- `Topology/Configuration/CorrelationIdConventionTests.cs` for correlation conventions;
- `Contexts/ConversationContextTests.cs` and `SentMessageMetadataTests.cs` for propagated and
  generated send metadata;
- `Serialization/HeaderRoundTripTests.cs`, `DictionarySendHeadersTests.cs`, and the strengthened
  `SystemTextJsonExtensionsTests.cs` for header storage and object materialization.

## Correlation contract

Message topology selects one correlation strategy per message type. The stable priority is explicit
message topology, `CorrelatedBy<Guid>`, `CorrelationId`, `EventId`, then `CommandId`. A selected empty
value does not change the message type's identity kind per instance. The caller's send-context pipe
runs after topology and may deliberately override the convention. Tests cover both send and publish,
plain and nullable properties, an implemented interface, the complete built-in priority, explicit
topology, empty values, and the caller override.

## Conversation and source contract

A root send or publish receives a non-empty conversation id at the final send-endpoint boundary.
Messages produced from a consume context inherit its conversation id and use its correlation id as
their initiator. Their source is the consuming endpoint, not the bus address. `StartNewConversation`
replaces the inherited value and records the previous value in `VSB-InitiatingConversationId`;
the overload used at a root preserves the exact supplied value and emits no initiating header.

Source address is intentionally protected at two layers. Consume-context transfer assigns the exact
receive endpoint, while `SendEndpoint` supplies its contextual source only when no earlier pipe did.
The layers are not competing truths: the first preserves causation, and the second is a null fallback
for every send. An attempted mutation of only the transfer layer therefore retained the correct
observable result; it was not counted as a killed behavior mutation. The accepted mutation clears the
final metadata and proves the externally visible contract instead.

## Time and headers

`MessageSendContext<T>` creates one `NewId` and derives both `MessageId` and `SentTime` from it. The
native test verifies the exact UTC timestamp rather than merely checking `DateTimeKind`.

`DictionarySendHeaders` has two deliberately different construction modes: an explicitly shared
dictionary and an independent case-insensitive snapshot. The inherited source accidentally copied
the snapshot and then added every entry again, so every non-empty independent copy threw a duplicate-
key exception. The product correction removes the impossible second insertion while preserving the
explicit aliasing mode. Set operations now have executable overwrite, replacement, removal, casing,
and null-input boundaries.

Object headers are verified through a real serialized in-memory delivery. Every interface property,
including the string array omitted by the inherited assertions, must round-trip without retaining
the sender object or array. `JsonElement.GetObject<T>` also supports a public interface when the
caller supplies ordinary `JsonSerializerOptions` without the global ViciOne converter; this distinct
fallback is tested directly because the transport path's global converter otherwise masks its loss.

## Gaps closed

The inherited suite did not bind convention priority, explicit selector or caller override,
empty-value behavior, independent root conversations, initiator metadata, exact previous-conversation
headers, exact source identity, exact sent-time derivation, header copy/alias semantics, overwrite and
removal, required inputs, the array member of an interface header, or converter-independent interface
materialization. The native cohort adds all of these without sleeps, wall-clock tolerances, shared
harnesses, global conventions, or inherited fixtures.

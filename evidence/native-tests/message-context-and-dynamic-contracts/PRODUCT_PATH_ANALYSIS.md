# Message contexts and dynamic contracts product-path analysis

## Scope and ownership

The four inherited fixtures are migration evidence, not the specification. Their complete contents,
all 30 ledger obligations, the connected request/client, send, response, initializer, reflection,
System.Text.Json, raw JSON, and MessagePack paths, and relevant source history were read before the
replacement was designed. Permanent owners are:

- `Contexts/MessageContextFlowTests.cs` for send, request, response, timeout, cancellation, and
  multi-response causation;
- `Initializers/DynamicContractIntegrationTests.cs` for anonymous-object materialization and real
  transport/serializer flows;
- `Internals/Reflection/DynamicImplementationBuilderTests.cs` for emitted contract structure;
- `ViciOne.ServiceBus.MessagePack.Tests/Serialization/InterfaceMessagePackFormatterTests.cs` for
  retained MessagePack interface serialization.

## Request and response context

An addressed send carries its generated message and conversation identities, message correlation,
source and destination, caller header, and the absence of request-only addresses. An addressed
request additionally carries a unique request id, the caller's conversation id, response address,
and the exact accepted response URNs. Its response preserves request, correlation, and conversation
causation and swaps source/destination as dictated by the responding endpoint. A separate bus
subscriber receives that same response; successful completion of only the request task is therefore
not accepted as proof of the inherited publication path.

A publish-addressed request reaches the consumer and the exact response endpoint without inventing
an addressed destination. With multiple accepted types, only the produced branch completes; the
other branch is canceled and cannot be revived by a late message. An unanswered request throws the
exact `RequestTimeoutException` containing its request id. Caller cancellation is a distinct terminal
state and preserves the caller's exact cancellation token.

A request deadline is owned by `ClientFactoryContext.TimeProvider`; every built-in context supports
an injected provider and external contexts retain a system-time default. `ClientRequestHandle` uses
that provider's `ITimer`. The timeout test therefore advances virtual time and proves the exact
exception immediately, rather than waiting for a real timer.

## Dynamic message contracts

Dynamic messages are property-only data contracts. The emitter accepts closed interfaces, includes
their inherited properties, produces one public sealed collectible implementation per builder and
contract, preserves `init` metadata and complete custom attributes, and deterministically merges
compatible duplicate declarations. It rejects concrete and open generic types, methods, events,
indexers, setter-only properties, default implementations, static properties, conflicting property
types, and case-insensitive wire-name collisions before emitting an invalid type.

Constructor, named-property, and named-field attribute arrays are converted to their declared CLR
array types. The inherited product path handled constructor arrays but passed named arrays as
`ReadOnlyCollection<CustomAttributeTypedArgument>`, causing `InvalidCastException`; the common typed
conversion fixes the product rather than weakening the test.

The emitted proxy intentionally carries no CLI serializable bit. A positive serializable control
guards the numeric CLI-bit assertion. The retained serializers are System.Text.Json envelope/raw and
MessagePack; formatter-based legacy serializers are absent. The real flows preserve scalar, URI,
nested interface, generic wrapper, credentials, correlation, conversation, and endpoint metadata.

## Gaps closed beyond the inherited suite

The inherited tests did not cover exact generated identities, initiator/request/fault fields, accepted
response URNs, response addresses and causation, independent subscriber delivery, late-response
isolation, timeout identity, caller-token preservation, nested anonymous contracts, custom-attribute
fidelity, inherited duplicate contracts, concurrency, collectibility, or any unsupported dynamic
contract shape. Those gaps are now executable contracts derived from the product source. No sleeps,
blocking waits, shared fixtures, wall-clock tolerances, or assertion-free delivery checks remain.

# API surface and preferred paths

ViciOne ServiceBus exposes five deliberately different API layers. The layers are a
compatibility promise as well as a discoverability rule: application examples and
IntelliSense must lead with the application layer, while provider and framework
authors retain the lower-level contracts they need.

## Application API

This is the default surface for application code. Configure one bus through
`AddViciOneServiceBus(...)`, register consumers with `AddConsumer<T>(...)`, select
one transport with `Using...(...)`, and inject application contracts such as
`ISendEndpointProvider`, `IPublishEndpoint`, `IRequestClient<T>`,
`IMessageScheduler`, or `IDurableSender<TBus>`.

Consumer retry and concurrency belong directly in the `AddConsumer<T>` callback.
Durable Sender belongs inside the owning bus block through
`UseReliableMessaging(...)`; a supported transport registers its dispatcher adapter
automatically. Application code sends typed messages and never constructs a
serialized durable envelope.

## Advanced SPI

Advanced contracts exist for framework extension and custom-provider work. They
include durable stores and dispatchers, serialized retained envelopes, concurrency
gates, payload evaluators, diagnostic inspectors, `ConsumerDefinition<T>`,
`Bind<...>`, and manual scheduler factories. Public advanced contracts are marked
`EditorBrowsable(Never)` so binary/source extensibility remains available without
making them the ordinary IntelliSense path.

An application should use an advanced contract only when it intentionally owns a
custom framework extension. Such use is outside the default application surface.

## Provider API

Each provider package owns its SDK dependencies, its `Using...(...)` registration
entry point, provider-specific settings, and provider operations. Core and
Abstractions remain provider-SDK-free. The authoritative support statement is
[`provider-capabilities.json`](provider-capabilities.json); unsupported Durable
Sender dispatch fails during startup instead of silently falling back.

## Operations API

Operational actions are explicit contracts, separate from producer messaging.
Use `IReliableMessagingOperations<TBus>` for durable quarantine/replay operations and
provider-owned contracts such as `IRabbitMqQueueOperations` for broker operations.
Commands return typed outcomes rather than ambiguous booleans.

## Testing API

Test harnesses and deterministic test helpers live in dedicated `.Testing`
packages. They are present in the Engineering graph and excluded from the Shipping
graph, so production packages never acquire a test-runtime dependency.

## Preferred examples

The 14 files under `samples/DeveloperJourneys` are compile-tested from freshly
packed NuGet packages. They are the normative learning paths. Architecture tests
reject advanced SPI names in those examples, ensuring that an old but functional
route cannot become the documented happy path again.

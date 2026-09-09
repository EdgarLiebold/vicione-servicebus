# API surface and preferred paths

ViciOne.ServiceBus separates application code, extension contracts, provider integration,
operations, and testing. The namespace and package identify the intended audience without hiding
public symbols from reflection or IntelliSense.

## Application API

Application contracts live in `ViciOne.ServiceBus`. Configure a bus with
`AddViciOneServiceBus(...)`, declare `MessageLimits`, register consumers, and select one transport.
Inject `ISendEndpointProvider`, `IPublishEndpoint`, `IRequestClient<T>`, `IMessageScheduler`, or
`IDurableSender<TBus>` for application messaging.

All public asynchronous operations end in `Async`, accept a final optional `CancellationToken`
unless the token is already carried by a callback context, and use typed option records for optional
send, publish, request, schedule, or durable-send behavior.

Consumer configuration belongs in `AddConsumer<T>(...)`. Reliable messaging and the message journal
belong inside the owning bus block. Startup validation rejects missing limits, transport ambiguity,
incomplete capability configuration, and unsupported durable transport combinations.

## Advanced SPI

Framework extensions use `ViciOne.ServiceBus.Advanced` and its focused child namespaces:
`Middleware`, `Serialization`, `Topology`, `Observers`, `Registration`, and `Initializers`.
This layer contains pipe-based overloads, supervised middleware lifecycles, serialized envelopes,
consumer definitions, topology contracts, observers, and the `IConsumerKind` extension point.

Application code should use this layer only when it intentionally implements a framework extension.

## Provider API

Provider contracts live in `ViciOne.ServiceBus.Providers`, with transport and persistence contracts
under `Providers.Transports` and `Providers.Persistence`. Each provider package owns its SDK,
configuration adapter, address model, and provider-specific operations. Core and Abstractions do not
depend on provider SDKs.

The machine-readable support matrix is [provider-capabilities.json](provider-capabilities.json).
Unsupported reliable-messaging combinations fail during startup rather than selecting a weaker
delivery boundary.

## Operations API

Operational contracts live in `ViciOne.ServiceBus.Operations`. Use
`IReliableMessagingOperations<TBus>` to inspect bounded outbox or inbox quarantine pages and to
requeue, discard, or explicitly abandon retained records. Provider packages expose their own typed
operations contracts where necessary. Commands return typed dispositions.

## Testing API

Harnesses and deterministic test helpers live in `ViciOne.ServiceBus.Testing` and provider-specific
`.Testing` packages. Shipping packages do not acquire test-runner dependencies.

## Capability packages

| Capability | Package | Dependencies |
|---|---|---|
| Core messaging and in-memory transport | `ViciOne.ServiceBus` | `ViciOne.ServiceBus.Abstractions` |
| Sagas and state machines | `ViciOne.ServiceBus.Sagas` | Core |
| Routing activities | `ViciOne.ServiceBus.Courier` | Core |
| Futures | `ViciOne.ServiceBus.Futures` | Core, Sagas, Courier |
| Job consumers | `ViciOne.ServiceBus.JobService` | Core, Sagas |
| In-process mediator | `ViciOne.ServiceBus.Mediator` | Core |
| Object initializers | `ViciOne.ServiceBus.Initializers` | Core |
| EF Core reliable messaging and journal | `ViciOne.ServiceBus.EntityFrameworkCore` | Core |
| EF Core saga, future, and job persistence | `ViciOne.ServiceBus.EntityFrameworkCore.Sagas` | EF Core, Sagas, Futures, JobService |

Transport, persistence, scheduling, serialization, visualization, SignalR, analyzer, and testing
packages follow the same `ViciOne.ServiceBus.<Capability>` naming convention.

## Preferred examples

The eighteen files under `samples/DeveloperJourneys` are compiled exclusively against freshly
packed NuGet packages. Journeys 15–18 cover the message journal, explicit message limits,
duplicate-safe inbox processing, and the minimal Suite package composition. The executable
`samples/SuiteComposition` host validates limits, SQLite reliable messaging, a consumer, a request,
a stored schedule, and the intended assembly closure.

The package gate also restores every runtime delivery package into an isolated cache and reflects
its complete public and protected surface. `docs/api/packed-public-api.txt` is the versioned
contract for those package assemblies: additions, removals, visibility changes, signatures,
parameter defaults, and inheritance changes fail the gate unless the contract is deliberately
updated.

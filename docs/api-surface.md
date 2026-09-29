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

## Saga request timeouts and scheduler cancellation

A Saga request with a positive `Timeout` schedules an expiry message and cancels
that schedule when any accepted response or request fault arrives. Select a
scheduler that accepts a caller-specified scheduling token and cancels by that
token, such as the Quartz or SQL integration. The scheduling capability is
exposed through `IScheduleCancellationCapability.CancellationMode` and forwarded
through the bus scheduler, consume scope and in-memory outbox. Endpoint and
publish scheduling declare `CallerSpecifiedToken`. Azure Service Bus native
scheduling declares `ProviderAssignedToken`: its broker token is available only
after dispatch, whereas the Saga stores the request ID. Transport-delayed
scheduling declares `Unsupported` because an accepted delayed message cannot
be recalled. Unknown custom schedulers must declare the capability to support
positive Saga timeouts. The Saga rejects these three incompatible modes before
creating a request ID, resolving an endpoint or dispatching the request.
Requests with no positive timeout remain usable with any scheduler mode.
The request requires an available scheduler before it is dispatched. A timeout
that would exceed the supported date range is rejected before dispatch. For an
accepted request, the timeout interval starts when the send completes.
If the clock moves to the end of the supported date range during a send,
the expiry is scheduled at the last representable instant.

Without a separate request-ID property, a Saga request uses the Saga's correlation
ID as its outgoing `RequestId`. Default response and fault correlation reads that
header, not an ID inside the response body. The three-response overload accepts
all three declared response types. Timeout correlation reads the timeout
message's `RequestId`. Explicit correlation callbacks may override these defaults.

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

ActiveMQ extension contexts identify a temporary destination by both its logical
name and its queue/topic type. `ConnectionContext.TryGetTemporaryEntity`,
`ConnectionContext.TryRemoveTemporaryEntity` and
`SessionContext.GetTemporaryDestination` require a `DestinationType` argument.
Queue and TemporaryQueue select the queue registration; Topic and TemporaryTopic
select the topic registration. Equal names may therefore coexist across the two
types, and deleting one does not remove the other. Response addressing explicitly
selects the temporary queue registration. Custom implementations of these transport
contexts must forward the destination type; application request APIs are unaffected.

For Azure Service Bus publishing, configure topic properties through
`IServiceBusMessagePublishTopologyConfigurator` before a subscription or broker topology evaluates
them. `IServiceBusMessagePublishTopology<T>.CreateTopicOptions` returns a separate SDK options
snapshot; editing that result does not change the publisher or its broker declaration. Once topic
options have been evaluated, changing a configured property throws. The high-level publish
configurator does not expose topic `Status` or `AuthorizationRules`; provider extension authors can
declare custom topics through `IBrokerTopologyBuilder.CreateTopic(CreateTopicOptions)` where those
SDK properties are needed.

## Operations API

Operational contracts live in `ViciOne.ServiceBus.Operations`. Use
`IReliableMessagingOperations<TBus>` to inspect bounded outbox or inbox quarantine pages and to
requeue, discard, or explicitly abandon retained records. Provider packages expose their own typed
operations contracts where necessary. Commands return typed dispositions.

## Testing API

Harnesses and deterministic test helpers live in `ViciOne.ServiceBus.Testing` and provider-specific
`.Testing` packages. Shipping packages do not acquire test-runner dependencies.

## Capability packages

The Core project has exactly one first-party project dependency:
`ViciOne.ServiceBus.Abstractions`. Every other direct `ViciOne.ServiceBus.*` sibling below `src`
is selected by a consumer, a higher-level capability, or the engineering toolchain. A project being
part of the repository or a solution does not make it a runtime dependency of Core.

| Direct sibling project | Kind and use | Direct first-party dependencies | When it is required | Why it remains separate |
|---|---|---|---|---|
| `ViciOne.ServiceBus.Abstractions` | Mandatory foundation containing application contracts, extension SPI, transport-neutral contexts, pipeline primitives, message metadata, and shared value implementations | None | Always; Core references it directly | Keeps contracts and provider SPI below Core and prevents a Core-to-provider cycle |
| `ViciOne.ServiceBus.Analyzers` | Roslyn compiler diagnostics for ServiceBus API conventions | None | Only while compiling a project that enables the analyzers | Runs in the compiler host on `netstandard2.0`; it is not runtime code |
| `ViciOne.ServiceBus.Analyzers.CodeFixes` | IDE code fixes for analyzer diagnostics | Analyzers | Only in an IDE or other workspace-based Roslyn host | Keeps workspace dependencies out of the compiler-only analyzer assembly |
| `ViciOne.ServiceBus.Analyzers.Package` | Packaging project that places analyzer and code-fix assemblies in one development NuGet package | Analyzers, Analyzer CodeFixes | Only when producing the analyzer package | It has no consumer library assembly; its only responsibility is package layout |
| `ViciOne.ServiceBus.Courier` | Routing slips, activities, compensation, and activity orchestration | Core | When an application uses routing-slip workflows; also pulled by Futures | This is a substantial optional workflow model with its own contracts and runtime |
| `ViciOne.ServiceBus.Futures` | Durable, state-machine-backed future requests and result routing | Core, Sagas, Courier | When future orchestration is configured | Its Saga and Courier dependencies must stay visible instead of becoming Core dependencies |
| `ViciOne.ServiceBus.Initializers` | Anonymous-value and convention-based message construction extensions | Core | When callers use initializer convenience overloads | Keeps dynamic object conversion helpers outside the application API; an isolated fresh-package consumer verifies that SignalR does not depend on it |
| `ViciOne.ServiceBus.JobService` | Distributed job submission, execution, retry, scheduling, and coordination | Core, Sagas | When the ServiceBus job runtime is configured | Jobs have their own lifecycle, persistence needs, and Saga coordination |
| `ViciOne.ServiceBus.Mediator` | In-process request, send, and publish through ServiceBus pipelines | Core | When broker-free in-process mediation is selected | It is a distinct execution and dependency-injection model |
| `ViciOne.ServiceBus.MessagePack` | MessagePack envelope serializer | Abstractions, Core | When MessagePack is selected instead of a built-in serializer | Keeps the external MessagePack dependency and serializer versioning optional |
| `ViciOne.ServiceBus.Sagas` | Saga repositories, correlation, state machines, and Saga-specific failures | Core | When an application or selected provider uses Sagas; also pulled by Futures and JobService | Saga execution is a large optional domain and must not enter Core |
| `ViciOne.ServiceBus.StateMachineVisualizer` | Graphviz DOT and Mermaid diagrams for Saga state machines | Abstractions, Sagas | Only for design, documentation, or diagnostics | Visualization must not add Saga or diagram APIs to the messaging runtime |
| `ViciOne.ServiceBus.Testing` | Transport-independent harnesses, observations, and deterministic test helpers | Core, Courier, Futures, Mediator, Sagas | Only in test projects and provider-specific testing packages | Prevents test APIs and the complete optional capability closure from entering shipping applications |

The table describes thirteen project boundaries. `ViciOne.ServiceBus.Analyzers.Package` is deliberately
packaging-only; the other projects produce their named assemblies. Transport, persistence, scheduling,
SignalR, and provider-specific testing projects follow the same `ViciOne.ServiceBus.<Capability>` naming
rule in responsibility folders below `src`.

The main runtime dependency direction is:

```text
ViciOne.ServiceBus.Abstractions
└── ViciOne.ServiceBus (Core)
    ├── Courier
    ├── Initializers
    ├── Mediator
    ├── Sagas
    │   └── JobService
    └── Sagas + Courier
        └── Futures
```

`ViciOne.ServiceBus.Testing` intentionally sits above most of this graph. It is an engineering
consumer of the capabilities and never a dependency of them.

### Abstractions ownership audit

`ViciOne.ServiceBus.Abstractions` is the mandatory foundation assembly, not an interface-only
assembly. A concrete type may remain there when it is a dependency-free implementation of a
transport-neutral contract and is needed by Core, providers, or extension authors. Examples are
message-body values, context proxies, pipe composition, observer fan-out, topology formatters,
`NewId`, and host/message metadata projection.

The ownership test applies these rules:

1. Application contracts and provider-neutral SPI belong in Abstractions.
2. A neutral concrete primitive may remain when moving it would force a Core dependency into the
   lower layer or duplicate it across providers.
3. Runtime composition, dependency injection, I/O providers, and feature-specific behavior belong
   to Core or the owning capability.
4. A type used only by one optional capability belongs to that capability, even when its namespace
   is the shared `ViciOne.ServiceBus` application namespace.

The current audit moved the remaining concrete ownership violations to their capability assemblies:

- Courier now owns `CourierException`, the activity execution and compensation exceptions, and
  `InvalidCompensationAddressException`.
- Futures now owns `FutureNotFoundException` and `FutureEndpointDefinition<TFuture>`.
- Sagas now owns `SagaException`, `ConcurrencyException`, all state-machine exceptions, and the
  unknown or unhandled state/event exceptions.

The neutral technical-retry classifier no longer names Saga exception types. Capability exceptions
publish their retry classification through `IRetryFailureClassification`, preserving the dependency
direction while keeping concurrency transient and invalid state-machine definitions terminal.
Architecture tests verify the declaring assembly of these types and reject a capability reference
from either foundation assembly.

The Saga and activity context vocabulary that remains in Abstractions is intentional. Core middleware
uses those transport-neutral context shapes for retry, timeout, outbox, scoping, observation, and the
configuration-observer seam through which optional packages attach their behavior. Moving them would
either create a reverse dependency from Core to an optional capability or require a duplicate contract.

### Provider-specific Saga integrations

The base Azure Service Bus and Event Hubs packages currently have direct Saga dependencies for real
features:

- Azure Service Bus contains the message-session-backed Saga repository and Saga dispatch entry points.
- Event Hubs contains activities that produce events from a Saga state machine.

This means selecting either provider currently brings `ViciOne.ServiceBus.Sagas` transitively, even
when the application does not configure those features. The long-term package target is to move these
features into provider adapters such as `ViciOne.ServiceBus.AzureServiceBus.Sagas` and
`ViciOne.ServiceBus.EventHubs.Sagas`. The base providers would then depend only on Core, while the
adapter packages would depend on both the base provider and Sagas. This is a public-package migration
and requires its own API, package-consumer, and provider integration validation; folding Sagas into
Core is not the migration path.

## Source ownership and navigation

`src/ViciOne.ServiceBus` owns the Core assembly; it is not a container for every ServiceBus
package. Independently compiled capability and contract projects are sibling directories under
`src`. Persistence, scheduling, and transport integration projects are grouped by responsibility
under `Persistence`, `Scheduling`, and `Transports`. These families include provider implementations
and adapters, not merely interchangeable implementations of one common adapter contract.

Within each project, files follow their type and namespace, with focused folders for related
functionality. Moving another project beneath the Core project would obscure assembly ownership
and require exclusions from the SDK's recursive source inclusion. The project boundaries preserve
optional dependencies and independently selectable package capabilities.

## Pipeline payload contexts

`BasePipeContext` and `ScopePipeContext` validate required cache, runtime-type and
factory arguments before payload fast paths. A required factory cannot be null even
when a compatible context or existing payload means it will not be invoked. Optional
initial payload arrays can still be null or empty. Token-only Base construction keeps
the empty cache lazily initialized; both cache-taking constructors require a cache.

Scope lookup prefers the scope itself, then local payloads, then the parent. Adds and
replacements stay in scope-local storage. Parent payloads are reused by reference,
not cloned; storage isolation does not prevent a callback from mutating a shared object.

## Test activity completion

Telemetry helpers await the action and then a continuous idle period for its related
trace. Unrelated traces do not restart that period; a newly active related span
invalidates it. The observation timeout remains anchored to the monitor's start,
even when spans stop close to that deadline. Queued timer callbacks recheck current
activity and deadlines before completing the wait.

The cancellation token is checked before invoking the action and cancels the later
activity wait. It is not injected into the callback: callers must pass cancellation
to their own messaging operation. An outer `Task.WaitAsync` timeout does not cancel
the underlying operation; deterministic tests cancel and drain that operation before
tearing down their harness or process-wide activity listeners.

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

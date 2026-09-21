# Changelog

Product and release history. This file is not the Apache-2.0 section 4(b) record: that is the
generated [CHANGELIST.md](CHANGELIST.md), which lists every file changed against the upstream
baseline.

## Unreleased

ViciOne.ServiceBus has not been released. The repository is in a private development state, and the
entry below records what the current work changed for anyone reading the source.

### Fixed during the source review since 2026-09-06

- RabbitMQ durable-send acceptance now requires a broker-confirmed, persistent, mandatory publish
  to an existing durable quorum queue (`6154ec2b4`). Queue proof no longer changes broker routing,
  synchronous and asynchronous publish failures both invalidate cached topology, and acceptance now
  follows matching pre-publish and post-confirm queue checks. Concurrent failed sends retain their
  original broker causes while a newer shared topology generation is being rebuilt.
- Saga removal and nested request outcome forwarding were corrected (`6a43c31ed`); in-memory saga
  indexes and queries now preserve registered identities and consistent snapshots (`fd11887df`).
- Semantic asynchronous API names and processor-lease handling were corrected (`7e5095b5a`).
- Reliable inbox operations and their evidence became deterministic (`f38685b51`), and SQL transport
  delivery invariants were enforced (`cbeb76206`).
- Mediator receive contexts no longer share mutable MIME state (`d071332b7`); resource caches release
  constructor-owned state when initialization fails (`020c146f8`).
- MessagePack formatter caches no longer retain the wrong lifetime (`e0d5fc1b0`), and typed inline
  object message data is preserved on round-trip (`bac2c88f9`).
- Payload-admission checks now cover transport and durable replay boundaries (`194271bbd`).
- Container saga test harnesses now resolve when a persistence provider supplies load but no query
  capability; unsupported operations still report their capability error (`468ba2369`).
- EventHubs raw-message tests now assert the actual receive address and default content type, and
  isolate observer events by message identity (`a2e9995a3`).
- The PostgreSQL transactional-outbox test now observes an uncommitted transaction across a polling
  interval and checks delivery after commit (`0578d6830`).
- The EF Future PostgreSQL fixture now retries complete serializable transactions when concurrent
  branches encounter a transient serialization conflict (`7a933284d`).
- RabbitMQ host and receive addresses now reflect TLS changes made before build, then retain the
  built runtime addresses and reject later host-address changes (`17f5dc4bd`).
- Amazon S3 message data now keeps caller streams open, uploads from their current position,
  applies its lifecycle rule only to explicit-TTL objects, rejects legacy untagged rules and
  versioned buckets, and revalidates startup state (`becc51c51`).
- Azure Blob message data now keeps SAS credentials out of new claim-check addresses and logs,
  reads older signed addresses using current credentials, isolates compressed upload block IDs,
  and treats `TimeSpan.MaxValue` as unbounded (`04d9708a2`). Azure TTL enforcement remains open.
- Azure Service Bus topology declarations now snapshot subscription options and rules, reject
  conflicting broker identities across relationship kinds and case variants, bind forwarding to
  the declared destination, propagate partitioning through earlier relationships, and reject
  session-enabled autoforwarding before deployment.
- EF JSON change tracking now compares and snapshots the value actually persisted. Selective
  `IEquatable<T>` implementations and shallow `ICloneable` snapshots can no longer silently drop
  changes to serialized fields; SQLite regression tests cover both cases.
- EF saga repositories now isolate EF model-cache entries by repository configuration and rebuild
  the model after a later saga-map registration. Repository probes return contexts through the
  configured factory release path instead of disposing factory-owned contexts directly.
- Raw Entity Framework outbox replay now restores persisted correlation, conversation, and request
  identities together with user headers.
- Direct Amazon SQS factories now require and enforce explicit body and transport-envelope limits.
  The configured JSON depth remains mandatory after later serializer-option callbacks, child
  endpoint overrides, and serialization resets.

### Removed

- The inherited message-audit contracts, observers, configuration and provider implementations.
  Their useful diagnostic capture capability is superseded by the intentionally incompatible,
  policy-controlled `MessageJournal`; no audit compatibility alias remains.
- The Python policy validator, its policy modules, and its validator self-test suite. They were a
  discarded Team 1 detour rather than imported behavior. Independently valid safeguards move to their
  effective MSBuild or native xUnit/MTP boundary; the validator must not be rebuilt.
- The foreign licence check and the usage telemetry that reported host, bus, rider and endpoint data
  to a hard wired third party address on every bus start, together with their dependency injection
  and public API surface.
- The inert `TypeAttributes.Serializable` flag on the dynamically emitted message proxy and its
  `SYSLIB0050` suppression. The modern serializers are unaffected.

### Changed

- The thirteen direct `ViciOne.ServiceBus.*` sibling projects retain their assembly boundaries, with
  Abstractions documented as the mandatory foundation. Courier, Future, and Saga implementations
  that were owned only by those optional capabilities have moved out of Abstractions into their
  respective assemblies without changing their namespaces or retry behavior. Core now consumes the
  neutral retry-classification contract instead of naming Saga exceptions, and SignalR no longer
  references the optional Initializers project or package; an isolated package consumer and NuGet
  metadata gate enforce that boundary. The API guide records every sibling project's use,
  dependency direction, and selection point, plus the planned provider-specific Saga adapters for
  Azure Service Bus and Event Hubs.
- MessagePack serialization now has symmetric bus and receive-endpoint configuration, isolated
  forwarding snapshots, payload-admission-safe byte handling, normalized byte, Base64 and object
  payload overlays, and System.Text.Json-equivalent case-insensitive recursive overlay semantics.
  Its internal runtime, forwarding serializer, formatter invokers, serializer context, files,
  comments, and source-owned xUnit/MTP contract suite were aligned around their actual
  responsibilities without adding a compatibility shim or test-only product instrumentation.
- Repository builds now follow the current stable .NET 10 patch channel instead of pinning one SDK or
  runtime patch. All direct dependencies and lock files were reassessed and refreshed; Quartz 4 is
  adopted through its `ValueTask` job lifecycle and builder-owned job factory. The former
  `QuartzSchedulerOptions.CreateJobFactory` hook is removed: standalone schedulers now use the
  default ViciOne job factory, while container hosts register scheduling through
  `AddQuartzConsumers`.
- RabbitMQ now owns a Durable Sender dispatcher whose acceptance boundary requires persistent,
  mandatory publishing and a publisher confirmation from a real broker. Unroutable and canceled
  attempts never report acceptance. All other external transports remain explicitly unsupported
  until they can prove an equally strong provider-owned boundary.
- Host lifecycle options validate at startup, and the public surface is documented and enforced as
  Application, Advanced SPI, Provider, Operations, and Testing APIs. Advanced definition, binder,
  manual scheduler, and persistence/dispatcher shapes remain available only where they carry an
  active extension capability and are hidden from default IntelliSense.
- The ambiguous inherited `ITransactionalBus` surface is replaced by two explicit Greenfield
  capabilities. `IAmbientTransactionBus` follows `Transaction.Current` and has no manual flush,
  while `IBufferedBus` exposes an explicit FIFO `FlushAsync` boundary. Both implementations are
  internal, retain every publish/send overload, preserve exact cancellation and failure identity,
  and reject composition with the durable Entity Framework bus outbox because all three own the
  same scoped publish/send boundary. A recursive flush from an action in the same logical drain is
  rejected immediately instead of self-deadlocking; unrelated concurrent callers remain serialized.
  Typed multi-bus registration preserves the original configuration failure across its reflection
  boundary. The former transactional-bus API has no compatibility shim.
- Quartz scheduled-message execution now propagates a causally requested job cancellation instead
  of converting it into up to five immediate refires. A dependency-thrown cancellation remains a
  retryable job failure when the Quartz execution token was not requested.
- Core pipeline behavior now has source-owned native xUnit/MTP coverage for dynamic consumer and
  handler connections, observer composition, context filtering, cancellation causality both before
  and inside an active retry attempt, consumer,
  send and publish configuration layering, partition conventions, and transaction ownership. The
  thirteen inherited NUnit pipeline files are removed after one-to-one disposition of all 34
  inherited obligations. Public handler, context-filter and transaction boundaries fail fast;
  transaction scopes enable asynchronous flow by default, externally supplied transactions retain
  ownership, and every retry receives a fresh owned transaction context. The concrete
  `SystemTransactionContext` adapter is now internal; the public capability remains the neutral
  `TransactionContext` contract.
- RabbitMQ host and endpoint addresses are immutable value objects with strict option parsing,
  scheme-owned TLS semantics, canonical port and URI rendering, defensive binding ownership and
  UTF-8 byte-accurate entity limits. Credentials retain password suffixes after the first colon;
  TLS lets the operating system negotiate enabled protocols and validates certificate chains and
  names by default. Query values and encoded short names round-trip without truncation, queue TTL is
  emitted as a numeric AMQP argument, and destination topology uses the final configured broker
  rather than its constructor default. The complete inherited address fixture is replaced by a
  source-mirrored native xUnit/MTP cohort with a one-to-one disposition of all 46 inherited
  obligations.
- `MessageJournal` is an optional, default-off diagnostic capability for terminal send, publish and
  consume outcomes. A mandatory caller policy selects and sanitizes the serialized envelope before
  an immutable entry reaches EF Core or Azure Table. Both stores enforce finite size, count and age
  on each append; failures are deadline-bounded and isolated from message flow. The feature has no
  query API, background queue, retry carrier, second outbox, log ownership or Suite-audit role.
- The circuit breaker now has one validated greenfield options boundary, an immutable runtime
  snapshot and a timer-free state machine. The snapshot is isolated from retained configuration
  builders and caller-owned arrays and is produced by the shared exception-filter semantics used by
  retry, rescue, redelivery and kill-switch paths. Exactly one caller owns each half-open recovery probe;
  competitors fail immediately with `CircuitBreakerOpenException`. Inclusive throughput/ratio
  boundaries, bounded backoff, causal cancellation classification and no-throw, low-cardinality
  OpenTelemetry signals replace public runtime states, router events and timer ownership.
- Two roots, and a run owns its own child of each. Compilation output under `artifacts/sdk`, packages
  under `artifacts/packages`; the raw TRX, the endpoint projection, the control files and the broker
  logs of one run under `artifacts/run-output/<run>/`; and the durable category record under the
  caller's own evidence parent, in its own `<run>` child. Saying that every file a run writes lives
  below the run-output root was false: the record is the one file meant to outlive the run, which is
  why it is written where the caller asked for it. Two runs on one machine still share no file.
- `.slnx` is the canonical solution format. Product and engineering have named targets; native test
  profiles are additional named targets and are materialized only when they contain an executable
  cohort. The current Unit profile uses xUnit 4 on Microsoft Testing Platform 2. Empty profile
  solutions are forbidden.
- Test support code is framework-neutral under `ViciOne.ServiceBus.Tests.Infrastructure`; test-only
  package versions do not participate in product evaluation, and the inherited NUnit/VSTest/Python
  stack is transition evidence rather than the target test architecture.
- Product Release builds keep embedded symbols while native MTP test applications use portable PDBs,
  as required for xUnit/MTP discovery. Applying the product symbol policy to the test executable had
  produced a successful build followed by a zero-test MTP run.
- Native xUnit executables set `UseMicrosoftTestingPlatformRunner=true`; the hybrid in-process entry
  point is not supported. The MTP-only `testconfig.json` replaces `xunit.runner.json`, fails skips and
  warnings, and CI rejects discovery below the current native profile floor.
- `Directory.Build.targets` carries the late half of the build contract: eleven errors that refuse a
  project which drops its lock file or locked mode, packs without its licence or notice, targets a
  framework this product does not support, or reaches for `netstandard2.0` while being neither a
  Roslyn component nor the analyzer package project whose framework group decides which consumers may
  reference it. The same contract prevents projects outside `tests2/` from claiming its package
  boundary or referencing its native xUnit/MTP entry package.
- The inherited verification inventory was consolidated during takeover. Its remaining runners are
  migration evidence only and are replaced cohort by cohort by the native xUnit/MTP test estate.
- The ActiveMQ publish topology is deployed to the broker. Resolving a destination name is a client
  side act and left the broker without the topic; `SessionContext.EnsureTopicExists` makes the broker
  hold it.
- Cron expressions tolerate repeated spaces and tabs between fields without shifting subsequent
  values into the wrong fields.
- Endpoint-name formatter behavior now has native source-owner xUnit/MTP coverage for snake-case
  boundaries, namespaces, prefixes, generic consumers, instance identifiers, and reserved names;
  the fully replaced inherited NUnit fixture was removed.
- Runtime `MessageUrn` overloads now share one fail-closed input validation path for null and open
  generic types. Native source-owner tests replace the complete inherited URN fixture and add the
  previously missing deconstruction contract without preserving static-cache exception wrappers.
- Request-rate behavior now has deterministic source-owner tests for processing, grouped execution,
  adaptive concurrency, limits, empty results, and invalid options. The replacement removes the
  inherited assertion-free, random, and delay-based fixture.
- Analyzers and code fixes are separate assemblies, so the analyzer no longer references
  `Microsoft.CodeAnalysis.Workspaces`, which a command line compilation does not provide. They still
  ship as the one package `ViciOne.ServiceBus.Analyzers`.
- Every project builds at the SDK warning level and on C# 14.
- The Apache-2.0 licence text moved from `LICENSE` to `LICENSE.txt` unchanged.

### Migration from MassTransit-style APIs

ViciOne.ServiceBus intentionally exposes one greenfield call form per application operation. There
are no compatibility shims; update call sites directly.

#### Changed call forms

| Old form | New form | Reason |
|---|---|---|
| `endpoint.Send(message, ...)` | `endpoint.SendAsync(message, cancellationToken)` or `SendAsync(message, SendOptions, cancellationToken)` | Makes asynchronous behavior explicit and replaces callback/pipe ambiguity with one typed options record. |
| `provider.GetSendEndpoint(address)` | `provider.GetSendEndpointAsync(address, cancellationToken)` | Uses the standard asynchronous suffix and a final cancellation token. |
| `publishEndpoint.Publish(message, ...)` | `publishEndpoint.PublishAsync(message, cancellationToken)` or `PublishAsync(message, PublishOptions, cancellationToken)` | Provides one typed publish shape and causal cancellation. |
| `requestClient.GetResponse<T>(request, ...)` | `requestClient.GetResponseAsync<T>(request, cancellationToken)` or `GetResponseAsync<T>(request, RequestOptions, cancellationToken)` | Separates deadline and cancellation and removes overload-specific timeout types. |
| `scheduler.ScheduleSend(...)` | `scheduler.ScheduleSendAsync(destination, dueAt, message, cancellationToken)` or the `ScheduleOptions` overload | Uses `DateTimeOffset`, names the due instant, and provides one application shape. |
| `scheduler.SchedulePublish(...)` | `scheduler.SchedulePublishAsync(dueAt, message, cancellationToken)` | Uses the asynchronous convention and an unambiguous due instant. |
| `scheduler.CancelScheduledSend(...)` | `scheduler.CancelScheduledSendAsync(scheduled, cancellationToken)` | Makes I/O and cancellation visible in the name and signature. |
| `consumer.Consume(context)` | `consumer.ConsumeAsync(context)` | Identifies the callback as asynchronous; its context already carries the cancellation token. |
| `filter.Send(context, next)` | `filter.SendAsync(context, next)` | Applies the same callback convention while retaining context-owned cancellation. |
| Pipe, callback, `object`, and anonymous-value send/publish overloads | Typed application overloads; specialized forms under `ViciOne.ServiceBus.Advanced` or `.Advanced.Initializers` | Keeps the application surface small without removing extension capabilities. |
| `context.Send(...)` / `context.Publish(...)` from a consumer | `context.Outgoing.SendAsync(...)` / `context.Outgoing.PublishAsync(...)` | Makes participation in the configured reliable outbox explicit. |
| Direct response buffering helpers | `context.DeferResponse(response)` | Defers the response until successful consumer completion. |
| `UseInMemoryOutbox`, `AddEntityFrameworkOutbox`, or `UseBusOutbox` | `bus.UseReliableMessaging(reliable => ...)` with one selected store | Unifies outbox, inbox, scheduling, retry, and quarantine ownership. |
| Separate durable-sender registration | `bus.UseReliableMessaging(...)` plus `IDurableSender<TBus>.SendAsync(...)` | Uses the same outbox store and delivery service as transactional outgoing messages. |
| Standalone scheduler registration | `UseReliableMessaging(...)` with the stored scheduler, `UseQuartzScheduler()`, or `UseTransportScheduler()` | Requires an explicit scheduling owner and prevents silent fallback. |
| `ConnectMessageJournal(...)` during bus creation | `bus.UseMessageJournal(journal => ...)` | Gives store, sanitization policy, and finite runtime options one validated owner. |
| Separate payload-admission registration | `bus.Limits(MessageLimits.Conservative)` or explicit `MessageLimits` | Makes send and receive size boundaries mandatory for every bus. |
| `ITransactionalBus` | `IAmbientTransactionBus` or `IBufferedBus` | Separates ambient transaction ownership from explicit FIFO buffering. |
| `new Vertex(type, targetType, title, isComposite)` | `StateMachineGraphNode.CreateState(name)`, `CreateEvent(name, messageType, isCompositeEvent)`, or `CreateException(exceptionType)` | Replaces runtime-type sentinels with a valid semantic state/event/exception model. |
| `new Edge(from, to, title)` and `graph.Vertices` | `new StateMachineGraphEdge(source, target, kind)` and `graph.Nodes` | Uses domain-specific names and explicit relationship kinds, removes the inaccessible redundant edge title, and exposes an immutable graph snapshot. |
| Direct use of `GraphStateMachineVisitor<TSaga>` | `stateMachine.GetGraph()` | Keeps traversal state internal while preserving graph inspection as the supported operation. |

#### Moved namespaces and provider names

| Old location | New location | Reason |
|---|---|---|
| Mixed application and infrastructure types in the root namespace | Application contracts only in `ViciOne.ServiceBus` | Keeps the default import focused on sending, consuming, requesting, scheduling, and durable send. |
| Service-registration extensions in product namespaces | `Microsoft.Extensions.DependencyInjection` | Follows the standard .NET discovery location for `services.AddViciOne...` methods. |
| Bus builders and option types mixed with runtime contracts | `ViciOne.ServiceBus.Configuration` | Groups configuration-time APIs separately from runtime messaging contracts. |
| Pipes, filters, definitions, binders, serializers, topology, observers, and initializers in mixed namespaces | `ViciOne.ServiceBus.Advanced` with `.Middleware`, `.Serialization`, `.Topology`, `.Observers`, `.Registration`, and `.Initializers` | Gives extension authors an explicit SPI without crowding the application API. |
| Transport implementation contracts in core namespaces | `ViciOne.ServiceBus.Providers.Transports` | Isolates provider-facing transport SPI. |
| Store and dispatcher contracts in feature namespaces | `ViciOne.ServiceBus.Providers.Persistence` | Isolates persistence SPI and retained-record contracts. |
| Quarantine, snapshots, health, and operator actions in feature namespaces | `ViciOne.ServiceBus.Operations` | Separates operational control from producer messaging. |
| Harness contracts mixed with product runtime types | `ViciOne.ServiceBus.Testing` and provider `.Testing` namespaces | Prevents test APIs and dependencies from entering shipping application packages. |
| `ViciOne.ServiceBus.RabbitMqTransport` / package spelling `RabbitMQ` | `ViciOne.ServiceBus.RabbitMq` | Aligns project, package, assembly, and namespace spelling. |
| `ViciOne.ServiceBus.ActiveMqTransport` / package spelling `ActiveMQ` | `ViciOne.ServiceBus.ActiveMq` | Aligns project, package, assembly, and namespace spelling. |
| `ViciOne.ServiceBus.AmazonSqsTransport` / package spelling `AmazonSQS` | `ViciOne.ServiceBus.AmazonSqs` | Aligns project, package, assembly, and namespace spelling. |
| `ViciOne.ServiceBus.Azure.ServiceBus.Core` | `ViciOne.ServiceBus.AzureServiceBus` | Uses one provider identity across project, package, assembly, and namespace. |
| `ViciOne.ServiceBus.EventHubIntegration` / package spelling `EventHub` | `ViciOne.ServiceBus.EventHubs` | Uses the provider's plural product name consistently. |
| SQL transport provider-specific mixed spellings | `ViciOne.ServiceBus.SqlTransport.PostgreSql` and `.SqlServer` | Makes the database provider explicit and consistent. |
| `ViciOne.ServiceBus.EntityFrameworkCoreIntegration` | `ViciOne.ServiceBus.EntityFrameworkCore` | Matches the package and namespace to the provider capability. |
| `ViciOne.ServiceBus.DynamoDbIntegration` | `ViciOne.ServiceBus.DynamoDb` | Removes the redundant integration suffix. |
| `ViciOne.ServiceBus.QuartzIntegration` | `ViciOne.ServiceBus.Quartz` | Aligns project, package, assembly, and namespace. |
| Analyzer implementation and package under one assembly name | `ViciOne.ServiceBus.Analyzers` and `ViciOne.ServiceBus.Analyzers.CodeFixes`, shipped by `ViciOne.ServiceBus.Analyzers` | Keeps compiler-only analyzer dependencies separate from workspace-based code fixes. |
| `ViciOne.ServiceBus.Visualizer` | `ViciOne.ServiceBus.StateMachineVisualizer` | Aligns the public namespace with the package and assembly identity. |

#### New capability packages

| New package | Contains | Reason |
|---|---|---|
| `ViciOne.ServiceBus.Sagas` | Saga contracts, repositories, state machines, and correlation | Makes saga capability opt-in. |
| `ViciOne.ServiceBus.Courier` | Routing slips and activities | Keeps activity orchestration out of core messaging. |
| `ViciOne.ServiceBus.Futures` | Future orchestration | Declares its Saga and Courier dependencies explicitly. |
| `ViciOne.ServiceBus.JobService` | Job consumers and job coordination | Declares its Saga dependency explicitly. |
| `ViciOne.ServiceBus.Mediator` | In-process mediation | Makes the broker-free mediator independently selectable. |
| `ViciOne.ServiceBus.Initializers` | Anonymous-value and object initialization extensions | Keeps initializer convenience overloads outside the application surface. |
| `ViciOne.ServiceBus.EntityFrameworkCore.Sagas` | EF Core saga, future, and job persistence | Lets reliable messaging and the journal use EF Core without loading saga capabilities. |

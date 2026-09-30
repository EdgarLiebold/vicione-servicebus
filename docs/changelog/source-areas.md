## Source history since the MassTransit 8.5.10 import

The starting point is the complete upstream source imported in commit
`9be1da2046218be2503c77c529b68d96fe113008`. The current product is an
intentional fork with different package boundaries and application contracts.
This section explains the net source changes by responsibility. The subsequent
Unreleased entries preserve the finer chronological record of defects, tests
and corrections. The [live repository diff](../../license/repository_diff.py) gives compact Git
counts; the exact patch and file history are available from the baseline
commit in Git.

### Identity, assemblies and application API

- Renamed the repository, assemblies, NuGet packages, namespaces, diagnostics,
  headers and transport identity from MassTransit to ViciOne.ServiceBus. The
  upstream attribution, Apache-2.0 license text and provenance remain.
- Kept `ViciOne.ServiceBus.Abstractions` as the mandatory contract base and
  `ViciOne.ServiceBus` as the core implementation. Application, configuration,
  advanced extension, provider, operations and testing APIs now have distinct
  namespaces. Optional capabilities remain separate assemblies rather than
  becoming hidden dependencies of the core.
- Replaced ambiguous send, publish, request and schedule overloads with typed
  options, consistently named asynchronous operations and causal cancellation
  tokens. Consumer outgoing work has an explicit reliable-messaging owner.
  Removed unsupported compatibility aliases and obsolete entry points after
  migrating their callers. The [API guide](../api-surface.md) documents the
  current call forms and package boundaries.
- Added startup validation for exactly one transport, explicit message limits,
  capability ownership, journal configuration and durable-delivery support.
  Invalid combinations fail before the bus begins delivering messages.

### Inherited modules removed from the product graph

The following imported projects no longer ship as ViciOne.ServiceBus modules.
Removal of a project does not imply removal of every general messaging concept
it once used: retained capabilities are identified separately below.

| Imported module | Disposition |
|---|---|
| `MassTransit.Azure.Cosmos` | Cosmos-specific persistence integration removed; Azure Table remains a separate provider. |
| `MassTransit.DapperIntegration` | Independent Dapper persistence package removed. |
| `MassTransit.EntityFrameworkIntegration` | Older Entity Framework integration removed; EF Core remains. |
| `MassTransit.MartenIntegration` | Marten persistence package removed. |
| `MassTransit.MongoDbIntegration` | MongoDB persistence package removed. |
| `MassTransit.NHibernateIntegration` | NHibernate persistence package removed. |
| `MassTransit.RedisIntegration` | Redis persistence package removed. |
| `MassTransit.HangfireIntegration` | Hangfire scheduling package removed; Quartz and other explicitly selected scheduling paths remain. |
| `MassTransit.KafkaIntegration` | Kafka transport package removed. |
| `MassTransit.WebJobs.EventHubsIntegration` | Azure WebJobs-specific Event Hubs adapter removed; the Event Hubs transport remains. |
| `MassTransit.WebJobs.ServiceBusIntegration` | Azure WebJobs-specific Service Bus adapter removed; the Azure Service Bus transport remains. |
| `MassTransit.Interop.NServiceBus` | NServiceBus interop package removed. |
| `MassTransit.Newtonsoft` | Inherited Newtonsoft wire-serializer package removed; JSON uses the retained System.Text.Json path. |

The inherited foreign-license check and its default-on usage telemetry were
also removed from core registration and runtime behavior. The inherited
message-audit contracts and their provider implementations were replaced by
the incompatible, explicitly configured `MessageJournal`. The unused package
logo, obsolete target-framework paths and retired test harnesses were removed
with their build and package references. No compatibility package restores
these removed modules.

### Core message flow, serialization and security

- Reworked send, publish, request, consume and response boundaries around
  typed contexts and a single serialized-envelope contract. Body size, JSON
  depth, content type, supported message types, addresses and identities are
  validated before transport admission. JSON and MessagePack retain their
  distinct payload encodings while projecting the same message metadata.
- Replaced legacy encryption paths with versioned authenticated AES-GCM
  protection. Configuration, key selection and malformed or unauthenticated
  payload handling fail closed.
- Fixed serializer and observer callbacks that could change message identity,
  correlation, expiry, route, content type or supported contracts after bytes
  were captured. The shared materialization guard rejects inconsistent sends,
  restores context state and prevents contradictory journal records. RabbitMQ
  and Event Hubs additionally bind their native route and delivery metadata;
  legitimate provider-assigned values and forwarding transitions remain
  permitted where explicitly validated.
- Corrected cancellation and fault classification across nested exceptions,
  observer callbacks, receive settlement and transport delivery. Original
  operation failures remain observable when logging, cleanup or fault
  notification also fails.

### Reliable messaging, scheduling and persistence ownership

- Introduced one application-owned reliable-messaging model for durable send,
  transactional outgoing messages, duplicate-safe inboxes, stored schedules,
  bounded retry, quarantine and operator actions. Store commit, carrier
  acceptance and consumer application are separate outcomes. Unsupported
  transport and store combinations fail during startup.
- Fixed message loss, duplicate dispatch, lease races, retry-state drift,
  cancellation races and identity changes at outbox and inbox admission. EF
  Core and in-memory implementations preserve the same public contract; the
  EF Core saga package is separate from EF Core reliable messaging and journal
  storage.
- Kept Quartz in its own scheduling assembly and added explicit scheduler
  capability contracts. Saga request timeouts, replacement and cancellation
  now distinguish caller-owned tokens from broker-assigned tokens and
  non-cancellable delayed delivery. Invalid replacements are rejected before
  dispatch.
- Retained Amazon S3 message data, Azure Blob Storage, Azure Table, DynamoDB
  and EF Core as optional persistence integrations. Their startup readiness,
  TTL, partition/key selection, journal retention, conditional writes and
  provider acceptance paths were hardened with source-owner tests.

### Transport integrations

- **RabbitMQ:** hardened publisher confirmation, queue-existence proof,
  mandatory routing, topology reuse and durable acceptance. Route and
  delivery settings cannot change after body binding. A distinct publish
  payload can still require mandatory routing, including after an earlier
  body read.
- **Azure Service Bus:** hardened entity declaration comparison, defensive
  snapshots, partition propagation, scheduling tokens, sessions and receive
  settlement. Conflicting topology declarations are rejected rather than
  silently reused. Broker-assigned schedule identifiers remain valid after
  confirmed scheduling.
- **Event Hubs:** hardened producer batch size, partition route, confirmation
  and partial-failure outcomes. Cached or lazily serialized bodies cannot be
  batched through a changed partition route.
- **ActiveMQ:** corrected broker-side topic deployment, producer ownership,
  temporary destination lifetime and native reply routing. The reply
  destination selected for a serialized send remains stable.
- **Amazon SQS/SNS:** corrected topic and queue declarations, policy and
  subscription reconciliation, FIFO ordering, receive polling, connection
  ownership, bounded attributes and move behavior. Failed declarations can be
  retried without duplicate cleanup registration.
- **SQL Server and PostgreSQL transports:** retained as separate database
  carriers behind a common SQL transport assembly. Delivery settlement,
  scheduling, outbox and cancellation paths were aligned with the shared
  reliable-messaging contracts.
- **SignalR:** retained as a separate transport assembly. Group routing,
  request ownership and representable timer boundaries are validated before
  runtime dispatch.

### Workflow capabilities and diagnostics

- Extracted Sagas, Courier, Futures, JobService, Mediator and Initializers
  into independently selectable packages. Their contracts, registration,
  scoped lifetime, request correlation, timeout scheduling, fault propagation
  and state transitions were reviewed through source-area tests.
- Rebuilt circuit-breaker state around validated options and an exclusive
  half-open probe, without a second timer-driven state owner. The
  StateMachineVisualizer retains diagram export with corrected escaping and
  explicit node and edge semantics.
- Added the opt-in `MessageJournal` with policy-sanitized immutable entries,
  bounded retention and a finite write deadline. Journal failures do not
  change send, publish or consume outcomes. OpenTelemetry and health
  projections expose the supported operational state without a second
  delivery or persistence path.

### Build, test and review structure

- Moved the build to .NET 10 and canonical `.slnx` graphs with locked package
  resolution, centralized outputs, warnings as errors and explicit provider
  fixtures. Split analyzers and code fixes into separate assemblies while
  retaining their package contract.
- Replaced inherited shallow or obsolete tests with source-owner behavior
  tests for success, failure, boundary, cancellation, restart and recovery
  paths. Architecture tests bind package, API, requirement and identity rules.
  The chronological entries below record red-first defects and focused
  adversarial reviews. The [current review status](../quality-status.md)
  names the latest product-wide Coverage/CRAP measurement, its source commit
  and the still-open SQL Server reliability finding.
- The Roslyn [API and XML-comment inventory](../api/roslyn-all-repos-api-2026-09-30.md)
  records public and protected symbols across the ViciOne repositories. It is
  a triage inventory, not a certificate that every comment is semantically
  correct or that every repository passes compilation.

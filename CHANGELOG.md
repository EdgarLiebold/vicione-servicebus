# Changelog

Product and release history. This file is not the Apache-2.0 section 4(b) record: that is the
generated [CHANGELIST.md](CHANGELIST.md), which lists every file changed against the upstream
baseline.

## Unreleased

ViciOne.ServiceBus has not been released. The repository is in a private development state, and the
entry below records what the current work changed for anyone reading the source.

### Fixed during the source review since 2026-09-06

- Azure Service Bus host configuration now treats an endpoint-only connection
  string assigned through the public setter the same way as the string
  constructor. It remains compatible with a separate named-key, SAS, or token
  credential in either assignment order; a credential-bearing string still
  rejects a conflicting authentication mode. Both entry points reject partial
  shared-key pairs and mixed shared-key/SAS values before connection creation.
  The setter rejects another namespace or port without changing the existing
  host or credential. Both entry points also reject duplicate or missing
  namespace endpoints and entity-bound connection strings, which cannot back
  a bus host with independent endpoints. Scoped host and entity addresses
  retain a custom port, including schema-free local emulator endpoints accepted
  by the Azure SDK. Explicit credential setters reject null instead of silently
  clearing authentication and falling back to ambient Azure credentials.
  Credentialless emulator and custom-port connection strings now fail before
  their transport settings can be lost; URI-configured custom ports also require
  a credential-bearing emulator connection string or both supplied SDK clients.
  Credential-bearing custom-port strings without effective emulator mode are
  rejected rather than silently connecting to the default endpoint. Factory
  contexts now preserve the configured namespace port in their reported endpoint
  and derived entity input addresses. Caller-supplied SDK clients may use a
  different physical emulator host or port while the configured logical host
  remains the address advertised to endpoints.
- The outbound HTTP boundary test now serializes its process-wide diagnostic
  listener with other global listener tests. Its synthetic positive control
  observes only its own request, while the default bus lifecycle test still
  detects any unexpected outbound HTTP request. This removes a parallel-test
  false failure seen in the complete Unit/Architecture gate.
- Invalid message types now yield no message-contract metadata, even when an
  infrastructure context implements an otherwise eligible base interface.
  `SendContext`, `ConsumeContext`, and `ReceiveContext` can no longer leak
  `PipeContext` into the contract list after their own validation fails. The
  special `JsonObject` contract exception now requires the actual framework
  type; a foreign type with the same namespace and name stays invalid.
- Saga instance wrappers now apply the same exact-runtime-type rule through
  typed and object equality. A derived wrapper can no longer compare equal to
  a base wrapper as a dictionary key while comparing unequal through
  `object.Equals`; distinct wrappers around equal saga states remain equal.
- Amazon SNS topic addresses now project to relative `topic:` URIs that resolve
  to the same entity under a scoped host. A topic name that cannot be represented
  relative to that scope fails explicitly instead of producing a misleading URI.
- Keyed `Bind<TKey, TValue>` values now apply the same exact-runtime-type
  equality rule through both typed and object comparisons. A derived binding
  can no longer compare equal to a base binding in a dictionary while
  comparing unequal through `object.Equals`; same-owner bindings with equal
  values retain their equality and hash behavior.
- Amazon SNS subscription setup now rejects a failed attribute-read response
  for an existing subscription. A broker HTTP error can no longer skip filter
  reconciliation while the stale subscription is recorded as configured and
  its SQS queue policy is updated. Existing subscriptions also reconcile every
  explicitly configured SNS attribute, including redrive policy, rather than
  silently ignoring settings outside the three filter/raw-delivery keys.
- The public Azure Service Bus connection-string endpoint parser now returns
  no endpoint for empty input and rejects null or blank keys with the intended
  argument or format error instead of leaking indexing exceptions or accepting
  a malformed key. It validates trailing connection-string segments before
  returning the first endpoint.
- The Azure Service Bus emulator dead-letter capability test now waits for the
  broker to confirm dead-letter settlement before receiving from the dead-letter
  subqueue. This removes an overlapping emulator cursor race while retaining
  strict completion checks and adding an exact payload assertion.
- RabbitMQ dependency-injection options now preserve normal, empty, mixed, and
  whitespace-containing credentials exactly through host and client-factory
  projection. Explicit empty values therefore remain anonymous instead of
  falling back to `guest/guest`; unspecified values retain the client defaults.
  Registration tests also enforce TLS enablement, strict and relaxed
  certificate policy, client-certificate identity, connection naming, and the
  complete certificate settings projection.
- RabbitMQ connection creation now preserves supervisor cancellation, owner
  registration, primary failures, and cleanup across settings refresh, both
  real client-adapter routes, shutdown subscription, publication, and disposal.
  Closed or concurrently closing connections cannot be published, subscription
  failures cannot leak an unpublished context, and diagnostic failures cannot
  block lifetime completion. Parallel RabbitMQ, SignalR, and Saga tests now
  synchronize on their actual disposal, consumption, and repository-removal
  boundaries instead of racing those asynchronous product transitions.
- Circuit-breaker runtime settings now reject decreasing recovery-delay sequences even when an
  internal caller bypasses the public options API. Runtime validation tests cover complete error
  aggregation, exact scalar boundaries, nonfinite ratios, missing and nonpositive durations,
  equal durations, and inner sequence inversions.
- Timeout activity and consumer fault handling now use the active delivery context as the
  cancellation authority, reclassify only cancellation from the elapsed configured timeout,
  validate required inputs before honoring caller cancellation, and fully own fault generation and
  receive notification. Consumer timeout wrappers publish through their original delivery context
  while preserving the passed context for receive notification; foreign contexts remain
  authoritative for both cancellation and publication. This prevents unrelated cancellation from
  being reported as a timeout, prevents an owner token from suppressing or canceling the wrong
  fault, and preserves exact asynchronous failures and cancellation tokens.
- RabbitMQ sends now round positive sub-millisecond message lifetimes and delayed-delivery
  intervals up to the next wire millisecond. This prevents a positive lifetime or delay from
  becoming zero and avoids shortening fractional intervals. Send tests also cover direct-reply
  routing, telemetry tags, durable destination validation, cancellation, and mandatory routing
  supplied by a distinct publish payload.
- Reliable-messaging configuration now rejects nonfinite retry jitter before the typed sender
  starts. The invalid-policy tests use a valid baseline and verify the rejected property's name;
  inclusive jitter limits and the frozen delivery policy have their own regression tests.
  Durable retry timing now preserves both outcomes even in a two-tick jitter window, avoids
  numeric wraparound at the largest `TimeSpan`, and retains a failed delivery with a saturated
  due date when adding its calculated delay to the current time would overflow.
- SQL topology subscriptions now compare their nested queue and topic declarations by logical
  broker identity instead of object reference, with matching hash codes. SQL topology diagnostics
  now expose the queue delivery limit, and the public publish-topology registration extensions
  reject missing configurators, missing message-type collections, and null collection entries at
  their API boundary before registering any preceding type.
- SQL host addresses now retain complete password suffixes and reject invalid mutable address
  components before startup. PostgreSQL host lists validate every segment atomically, preserve IPv6
  and inline-port semantics, reject unrepresentable Unix sockets, and rebuild data sources from the
  current configurator values without discarding unrelated security options. Explicit inline
  default ports override competing global ports, and an explicit single-host override clears prior
  multi-host state.
- SQL receivers now retain ownership of fetched delivery locks through shutdown, release late
  batches even when an earlier unlock fails, and wake promptly when a delivery completes during
  queue maintenance. Empty polling is bounded by auto-delete keepalive. Both SQL providers round
  fractional idle lifetimes up to database seconds, so sub-second settings cannot become immediate
  deletion, and address and endpoint validation reject values beyond the SQL seconds range.
- ActiveMQ header projection now preserves both Boolean values, limits native values to the shared
  OpenWire/AMQP message-property set, omits OpenWire-incompatible byte arrays, formats other
  `IFormattable` values with invariant culture, and keeps every `DateTime` kind on the same instant.
  Direct topic-consumer diagnostics no longer emit an empty destination field. Source-owned tests
  cover header precedence, actual OpenWire marshalling and broker round-trips, foreign null-valued
  header implementations, scheduled-delay consumption, transport-property round-trips, topology
  lifecycle identity and runtime message destinations.
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
- Azure Service Bus receive headers now take the broker enqueue instant for `TransportSentTime`
  even when an application property uses that name with different casing. Header enumeration
  excludes those forged time entries; other application names remain exact, and raw identity
  headers retain their canonical GUID format. Regression tests also verify persisted routing
  metadata, UTF-8 values, blank values, and session/partition consistency.
- Azure Service Bus receive transport properties now retain the broker's `ReplyTo` destination
  alongside session, partition, reply-session, and label values. Scheduling or replay that
  persists these properties no longer drops a reply destination; a forged application property
  cannot replace the broker value.
- Payload admission now rejects a send-context proxy before attaching an operation marker to its
  underlying transport context, so a rejected proxy cannot contaminate a later send. Event Hubs
  rechecks admission after send observers run, for both single messages and every batch member;
  observer changes to the serializer's content type are rejected before provider submission.
- Event Hubs receive headers now reject a missing SDK event at construction and treat blank
  message and correlation IDs consistently in enumeration and lookup. A lookup for a blank ID
  returns no value; non-null application properties retain their exact names and values.
- Azure Service Bus host retries and reliable-send classification now inspect complete exception
  trees, including every aggregate sibling, and give permanent causes priority. Both paths share
  broker-reason decisions: recoverable timeouts and explicitly transient general errors retry;
  non-transient SDK failures outside entity recovery and permanent HTTP statuses stop. HTTP status 0 remains retryable,
  missing broker entities retain send-side recovery, and the Azure classifier leaves generic
  connection failures to the transport that raised them.
- In applications that register Azure Service Bus before ActiveMQ, Azure's reliable-send classifier
  now delegates failures marked by a foreign transport connection type. This prevents a nested
  timeout or HTTP status from turning an ActiveMQ configuration failure into a durable retry.
  Azure's own retry-stop connection wrapper remains classifiable; tests exercise the actual retry
  wrapper and verify immediate InMemory outbox quarantine with both transports registered.
- Azure Service Bus subscription setup now propagates a missing configured rule on an existing
  subscription instead of reporting success with a broad `$Default` rule. A generated filter on an
  existing subscription requires one identifiable generated rule; ambiguous or externally named
  rule sets fail closed. When another creator wins a subscription race, the winner's delivery and
  forwarding settings and configured rule are reconciled before setup succeeds. Real emulator
  regressions check the persisted rule set, and a controlled SDK race checks both updates.
- Azure Service Bus publish-topology validation now reports invalid composed topic paths and idle
  lifetimes while ignoring excluded topics. Topic settings freeze once evaluated for a subscription
  or broker declaration, so later changes cannot make sender and broker options disagree. The public
  `CreateTopicOptions` getter now returns a separate snapshot: mutating it no longer configures the
  published topic. Use the publish configurator before evaluation; custom broker declarations can
  supply SDK options directly through the provider topology builder. The Azure emulator test
  project now includes its missing Microsoft CodeCoverage extension, so instrumented provider runs
  execute the tests instead of reporting zero discovered tests.
- SignalR's source and test NuGet lockfiles now reflect the earlier removal of their obsolete
  Initializers project dependency. A locked restore succeeds with the current project graph.
- Assembly directory scans now resolve candidates from their selected files instead of binding
  first by the file's simple name, which could substitute an unrelated already loaded assembly.
  Invalid images remain skippable; other load failures report the error from the selected path.
  Tests cover renamed assemblies, filename collisions, recursive filters, executable inclusion,
  disappearing files, and calling-assembly discovery.
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
- Amazon SQS/SNS message names now reject every open generic shape and distinguish contracts whose
  namespace or type identifiers would otherwise collapse onto the same topic name. Type-based
  destinations use the actual publish topology, repeated scoped host settings do not stack prefixes,
  and the factory rejects a scoped host configured after message or publish topology was created.
  Empty or duplicate naming separators are rejected, and long canonical names use a distinct,
  stable digest form within the SNS topic-name limit. Scoped topics remain within that limit too;
  opaque custom message-topology configurators reject scoped host settings because their cached
  names cannot be checked for consistency.
- Amazon SQS/SNS topology now rejects two declarations of the same queue or topic when their
  broker attributes, subscription attributes, or tags differ. Previously, the second declaration
  could silently reuse the first handle and lose its settings, including an explicit SNS raw-delivery
  choice. AWS names are unique even when an extension supplies a queue or topic entity subclass;
  conflicting subtype declarations can no longer produce duplicate broker names. Diagnostic
  descriptions and both equivalent and conflicting metadata declarations have source-owned tests.
- Amazon SQS/SNS subscription declarations now compare the complete topic and queue definitions
  rather than their object references. Extension collections reuse independently created equivalent
  declarations, reject conflicting definitions for the same broker pair, and prevent duplicate
  pairs supplied through a subscription subclass. The normal builder still reuses a repeated
  topic-to-queue subscription handle.
- Amazon SQS endpoint validation now rejects directly mutated visibility timeouts outside the AWS
  range, maximum visibility durations outside the positive 12-hour range, and renewal intervals
  below the effective 60-second floor. Diagnostics identify the setting that is invalid. The
  existing concurrency, polling, purge, redrive, and SNS raw-delivery checks retain their order;
  regression tests also protect the raw-delivery attribute's string-only boundary.

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

- Retry execution now keeps preparation of a scheduled attempt in a separate
  method. Contract tests verify that caller cancellation during the operation
  and independent caller or policy cancellation during pre-retry stop further
  attempts and report the originating token; concurrent cancellation retains
  caller precedence.
- Serialization validation now checks serializer and deserializer registrations
  in separate methods while preserving the existing failure order and member
  names. Contract tests cover an empty registration, ambiguous multi-format
  selections, and successful collection creation after explicit selection.
- Amazon SQS topology diagnostics now have a contract test that verifies all
  declared topics and queues, their lifetime flags, and every SNS-to-SQS
  subscription pair in the public probe result.
- Azure Service Bus queue and subscription fault-notification tests now verify
  that processor callbacks return while supervised shutdown is pending,
  overlapping and simultaneous fault reports share one stop attempt, and an already canceled
  notification leaves the supervisor running until a later active fault. A
  failed stop is logged and permits a new attempt on a subsequent fault.
- Two RabbitMQ shutdown lifecycle tests now wait until their connection fake
  has actually removed the shutdown handler before asserting that none remains.
  The previous signal fired before removal and could fail under parallel test load.
- NewId value tests now verify all four identity words through public equality
  operators, typed and boxed equality, dictionary lookup, hash consistency,
  and boxed comparison boundaries. The implementation is unchanged.
- JobService correlation and SQL partition-key registration now use smaller
  domain-grouped methods without changing the 30 correlation identities, 31
  partition formatters, or registration order. Source-owned tests verify every
  correlation identity, empty and null boundaries, and repeated registration
  after the global topology is frozen.
- `JobSagaDefinition` now groups its 18 receive partition registrations by
  admission, attempt, lifecycle, and progress/timer messages. A registration
  test verifies the exact 18 message types, one shared partition coordinator,
  and the endpoint concurrency limit. The separate direct-endpoint test verifies
  same-job serialization across message types while another partition continues.
- Azure Service Bus connection creation now separates custom-port validation,
  SDK option setup, and the four explicit credential routes from the namespace
  context. Custom-port rejection checks now include named-key and SAS settings.
  Emulator regressions verify both mixed-client directions with real queue
  administration and exact message delivery: the factory fills in the missing
  client while continuing to use the caller-supplied client.
- Azure Service Bus connection-string endpoint parsing now separates segment
  validation from URI normalization. Direct public-parser tests reject a second
  endpoint even when its key uses different casing, reject malformed segments
  before or after a valid endpoint, and preserve a schemaless scoped endpoint
  through leading, repeated, and trailing delimiters.
- Recurring publish scheduling now has a source-owned regression for the
  declared runtime message contract: the published command retains the
  destination, schedule, payload and contract identity, while the chosen pipe
  and cancellation token reach the publish endpoint. A failed publication
  propagates to the caller instead of returning a scheduling handle.
- The source-owned consume-transform pipeline test now checks the public
  delegate-based property transformation with both present and null source
  values. It verifies that the delegate sees the original message and
  property value even when another property is changed in the same transform.
- Existing Amazon SNS subscription tests now verify the exact broker update
  request and failure propagation for changed or missing filter policy,
  changed filter scope, raw-delivery settings, and redrive policy. Matching
  existing settings avoid redundant SNS updates while queue permission is still
  configured. A failed update cannot be reported as a completed queue subscription.
- RabbitMQ endpoint query parsing now groups lifetime, exchange, Boolean, and entity-name
  options in focused parsing steps. The public address behavior remains the same; source-owned
  regressions check combined host and endpoint options, unsupported schemes, conflicting or
  duplicated options, and stable deduplication of exchange bindings. An unreachable virtual-host
  fallback was removed after confirming the path parser always assigns a value.
- Generated Amazon SNS topic names containing separator characters in CLR identifiers now use a
  reserved canonical encoding, so their durable topic names differ from earlier development builds.
  Existing topics are not renamed automatically: migrate subscriptions and coordinate publisher and
  consumer rollout before using those earlier builds with this version. Configure a scoped Amazon
  SQS host before any message or publish topology to apply its prefix.
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

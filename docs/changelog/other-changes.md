### Changed

- Prepared the repository for external review: archived historical raw proof and agent work
  records in Git tag `archive/servicebus-pre-review-cleanup-20260930`, retained the obligation
  maps used by architecture tests, split this changelog into topic documents, and replaced the
  static source-only change list with a live whole-repository Git comparison under `license/`.
  CI now checks current source identity without rescanning archived binary evidence. The obsolete
  historical `scan` and `evidence` CLI modes and their path policy are retired with that archive.

- Message-limit tests now exercise inclusive body and envelope boundaries,
  independently optional warning and offload thresholds, and exact failure
  fields and reasons for invalid settings. A targeted `>` to `>=` mutation of
  the optional upper bound failed the intended two valid-boundary cases; the
  product implementation was restored and the normal suite passed.
- Mermaid label encoding now keeps the syntax-entity table separate from the
  Unicode and control-character loop. Exact syntax, control, and surrogate
  output remains covered by the visualizer behavior tests.
- Job-attempt state-machine setup now separates event correlation registration
  from schedule and state behavior registration while preserving their order.
  The existing transition tests still verify startup, liveness escalation,
  cancellation, fault handling, and finalization.
- EF-Core reliable inbox delivery now keeps lease acquisition separate from
  the existing commit and failure-handling path. A quarantined delivery is
  explicitly tested against duplicate dispatch: its consumer is not invoked
  and its attempt count, failure details, and lease state remain unchanged.
- Typed Durable Sender now resolves and validates its send context separately
  from constructing the serialized intent. The redundant transport-context
  check after the canonical `MessageSendContext` check is removed. New tests
  verify that endpoints without transport capability and noncanonical send
  contexts fail before durable admission, while preserving destination and
  cancellation-token propagation.
- Durable-send intent validation now keeps destination and media-type checks
  together in a focused operation. Validation order remains unchanged. Existing
  boundary tests cover the exception parameters, exact length limits, and
  acceptance of empty serialized bodies.
- Mermaid state-machine label encoding now handles Unicode scalars and control
  characters in a focused helper. The exact generated documents and syntax
  escaping remain unchanged; the encoding tests still cover reserved Mermaid
  characters, control characters, and paired or unpaired surrogates.
- Removed the unused internal Azure Service Bus `ReadAsBytes` stream helper.
  It had no source or test call site and was not an externally accessible
  transport API; keeping its inconsistent stream-position and generic-error
  behavior would create a false maintenance contract.
- Saga-index registration now keeps captured-key publication and rollback in
  one focused operation. The admission lock, exception order, and cleanup
  behavior are preserved while the public `Add` path is simpler to review.
  Transaction tests assert reverse rollback, continued cleanup after failures,
  and the exact primary and cleanup exceptions.
- Output-pipe and consume-output fault notification now run in focused methods
  so dispatch control flow and secondary observer failures are independently
  reviewable. New consume-output tests verify asynchronous callback order,
  pending pipeline state, exact context and exception identity, and failures
  from either observer group and the diagnostic logger.
- Filter-observer tests now hold each typed and untyped pre-send, post-send,
  and fault callback asynchronously. They verify that downstream work waits
  for the active observer, callbacks stay ordered, and a failed dispatch
  retains its original exception even if either fault observer fails.
- Azure Service Bus session-batching tests now exercise the public consumer
  extension through real batch options and the endpoint callback. They verify
  that broker session identities group messages, batch limits reach the
  queue and subscription endpoints, existing prefetch settings follow the
  configured boundary, and invalid options or endpoint kinds fail before
  endpoint mutation. The invalid-endpoint diagnostic now names the actual
  shared Azure Service Bus endpoint contract.
- Consume-observer converter tests now verify that each lifecycle stage
  forwards the exact typed context and fault, preserves the observer's
  asynchronous result and failure even when the caller cancels after
  notification, and rejects missing, wrong-type, or pre-canceled inputs
  before notifying an observer.
- Recurring scheduler tests now verify that both endpoint-backed and
  publish-backed schedulers initialize a scheduled message from supplied
  values and preserve its fields, destination, schedule, cancellation token,
  and returned handle. They also execute the typed pipe adapter and verify
  that the supplied pipe sees the initialized payload and its correlation
  write reaches the outgoing schedule command context.
- Endpoint QoS validation now resolves each endpoint in a focused helper while
  preserving declaration order, canonical values, and aggregated diagnostics.
  A new regression test verifies that endpoint-owned and consumer-owned QoS
  for one dedicated endpoint must agree.
- Recurring-publish scheduler tests now verify that both endpoint-backed and
  publish-backed schedulers resolve the runtime message type to the correct
  destination, preserve the schedule and payload in the command, forward the
  cancellation token, and return a matching handle. They also verify that a
  declared message contract selects its own publish address and that the
  caller's send pipe reaches the command endpoint unchanged.
- Amazon SQS host tests now construct the standard SQS and SNS SDK clients
  without sending requests. They verify that both use the configured host
  region when explicit credentials are supplied and that distinct
  caller-supplied SDK service endpoints and signing regions reach the correct
  client.
- The EF Core reliable-inbox regression suite now verifies cancellation after
  business data and the consumed fence have been flushed inside a transaction.
  The cancellation must roll back both records, clear the scoped change
  tracker, and permit the same delivery identity to commit on its first
  subsequent attempt.
- The typed Durable Sender regression suite now verifies that a relative
  destination is rejected without reserving its idempotency key or writing an
  outbox record. A retry with the same key and an absolute destination must
  persist exactly one intent.
- Request-rate construction now calculates the rounded-up request limit
  without overflowing and caps the default concurrent result capacity at the
  largest supported integer. Waiting for result capacity now responds to
  caller cancellation and disposal, releases the abandoned request lease,
  and returns an unused rate permit only within its original rate window.
  Boundary tests verify maximum prefetch size, cancellation, disposal, and
  rate-window rollover without issuing large result batches.
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
- NewId format tests now compare standard and sequential B/D/N/P output,
  case variants, defaults, and invalid format boundaries against independent
  Guid values. The formatting implementation is unchanged.
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

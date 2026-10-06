### Fixed during the source review since 2026-09-06

- Default typed transport headers now return `false` with an empty result from
  both wire-value conversion methods. Previously a default string header was
  accepted as text, while default numeric and Boolean headers could throw for
  their missing key. Valid typed strings retain their exact key and value.
- Amazon SQS auto-delete cleanup now retains one agent registration across a
  failed topology declaration and its retry, even when each attempt has a new
  scoped client context. Previously each retry could add another agent and
  attempt to delete the same queue or topic more than once. Cleanup uses the
  longer-lived client context after an attempt scope ends; a restarted
  endpoint registers a fresh agent for its new lifecycle.
- Amazon SQS moves now reject a request with more than ten message attributes
  before provider submission, naming the destination and actual count. A
  dead-letter move can add a reason to nine custom attributes; ten existing
  custom attributes plus that reason exceed the provider limit and fail with
  a local diagnostic instead of sending an invalid request.
- Amazon SQS queue-policy reconciliation now checks the Allow statement's
  effect, action, resource, SNS service principal and SourceArn condition
  together. Unrelated statements no longer masquerade as send permission or
  contribute extra conditions to a new grant. A matching explicit Deny,
  including wildcard principals and actions, is reported before a policy
  write. Dedicated ArnLike and ArnEquals grants safely collect multiple
  topics in one statement; existing unrestricted grants are left intact.
- Amazon SNS publish-topology discovery and explicit registration now reject
  a missing configurator or type list with named argument errors. An explicit
  list containing a null or invalid message type, and a namespace scan whose
  filter fails, are evaluated before any type is registered. These invalid
  inputs cannot leave a partly configured publish topology.
- Amazon SQS queue metadata now coordinates resolution, durable ownership
  transitions and removal per queue name. A durable queue can no longer start
  a second provider lookup while an evictable lookup for the same name is
  still being created, and a name lookup cannot create an evictable copy while
  durable creation is pending. Caller cancellation releases only that caller's
  wait; it does not release the queue-name gate before the cache-owned
  operation ends. Independent queue names continue to resolve concurrently.
  Evicted queue metadata now rejects late send and delete requests before a
  lazy batch worker can be created. Disposal waits for policy updates already
  admitted, preserving successful provider writes and their local result.
  An SQS client send or delete that resolves an already evicted queue now
  resolves the name once more before batch admission. If eviction closes a
  full batch channel while the entry still waits for admission, that entry
  also receives one safe retry. Provider failures after admission are never
  replayed, and repeated eviction ends after two lookups.
- Pending delivery-work waits now keep their captured tasks when the caller
  cancels the wait. A later `CompletedAsync` call still observes unfinished
  receive, mediator, queue, or job work instead of reporting completion early.
  Successfully awaited or faulted snapshots are removed by their original
  task identities, so work added during the wait is still drained.
- Generic transport-header adapters now reject a null converter at construction
  instead of failing later during a send. Their policy is covered for typed and
  untyped headers, including host and fault-detail filtering. An Amazon SQS
  queue-send regression test also checks the actual provider request: ordinary
  headers, fault input address and fault message survive, while host and fault
  detail headers are omitted.
- Diagnostic duration formatting now preserves the sign and natural units of
  negative values, including `TimeSpan.MinValue`. A negative millisecond no
  longer appears as a large nanosecond count in chart or transport diagnostics.
  The formatter was split into bounded calendar, clock, and submillisecond
  steps so each method remains below the CRAP risk threshold.
- NewId array-batch generation now validates null arrays, negative indices and
  counts, end bounds, and integer-overflow-sized ranges before reading the
  clock or entering the spin lock. Invalid requests leave the target array and
  identifier sequence untouched; an empty segment at the array end remains
  valid. Previously a negative index could fault while the lock was held.
- Typed exception predicates now inspect nested aggregate failures when the
  aggregate itself has the requested type but does not satisfy the predicate.
  They test both each direct inner failure and its root cause; previously a
  direct match with its own inner exception was skipped. This applies equally
  to include and exclude rules, so a matching inner failure can be handled or
  vetoed as configured.
- Transport bus creation now keeps its configuration and fault-notification
  boundary explicit. A DI regression test proves that a bus-instance
  specification failure remains the reported cause even when a creation-fault
  observer also throws; both failures reach the intended paths exactly once.
- Typed and outer consume-output fault observers can no longer replace the
  original consume-dispatch failure with their own exception. Both groups are
  notified even when the typed observer fails, and diagnostic logger failures
  remain secondary to the dispatch failure.
- Output-pipe fault observers can no longer replace the original dispatch
  failure with their own exception. A failed typed observer also no longer
  prevents the outer observer from receiving the dispatch failure. A failing
  diagnostic logger cannot override either guarantee.
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
- Azure Service Bus now applies the configured minimum retry backoff to both created SDK
  clients. Values outside the messaging SDK's inclusive 1 ms to 5 minute range fail before
  replacing the configured value. Caller-supplied client retry policies remain their owner's.
- Azure Service Bus factory contexts now close only messaging clients created by the factory.
  A caller-supplied client remains usable after endpoint shutdown and context replacement.
  The direct connection-context constructor retains its existing ownership transfer.
- Azure Service Bus session metadata now reads the current session expiry, including SDK-owned
  renewals, and returns it in UTC. It no longer reads the original individual message expiry.
- Event Hubs checkpoint diagnostics now remove query strings, fragments and URI user information
  from the Blob endpoint. The diagnostic container name is parsed from that sanitized endpoint
  because an SDK name can also contain the original fragment. The configured SDK URI is retained.
- Amazon S3 message-data options now reject the AWS-reserved `amzn-s3-demo-` prefix.
  Valid names containing that text elsewhere and the shorter `amzn-s3-demo` name remain accepted.
- Encryption-key identifiers now reject malformed UTF-16 before an envelope can be stored.
  Valid Unicode, the inclusive 65535-byte UTF-8 limit, supported AES sizes and defensive key copies are retained.
- Stable message-contract names now reject unpaired UTF-16 surrogates at the common identity boundary.
  Constructor, parsing, attributes and catalog registration share that validation; valid Unicode and existing length/version bounds are retained.
- Azure Service Bus session-id conventions now pass the configured default formatter to new
  message conventions. Explicit message formatters retain precedence; a null formatter result
  preserves routing metadata already supplied by the sender.
- Azure Service Bus consuming subscriptions now reject a complete rule combined with a separate
  default-rule filter before bus construction. Named and typed subscriptions retain this validation;
  configuring either option alone remains supported.
- Azure Service Bus shared administration leases now retain caller and lease cancellation links
  until queue/topic creation and subscription creation/deletion finish. Successful results and
  original faults are preserved; completed requests detach their cancellation registrations.

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

- F-SB-API-005: Document the empty MessageData address exception and optional repository address of populated handles.
- F-SB-API-010: Document the untrusted pre-authentication encryption-key selector and bounded provider lookup.
- F-SB-API-020: Document the enforced one-bus-per-DbContext-type boundary for typed transactional stores.

- F-SB-API-013: Always await internal EventHub partition close after application faults/cancellation, preserving original single causes and ordered dual causes.
- F-SB-API-014: Validate positive EventHub checkpoint/admission/delivery settings before construction, preserving inherited limits and native whole-millisecond timer boundaries.
- F-SB-API-016: Snapshot EventHub receive policy and the selected SDK options callback for the completed Build generation.
- F-SB-API-017: Honor individual caller cancellation while waiting for a shared deferred EventHub producer without cancelling the shared resolution.

- Mediator batch benchmarks now declare conservative message limits for both
  normal sends and request/response setup, allowing all declared batch sizes
  to exercise the intended workload.
- JSON deserialization benchmarks supply the existing empty headers object
  when the serialized benchmark envelope has no headers. This exercises
  deserialization instead of failing the header constructor.
- Latency benchmark capture records only the first consume callback for each
  message identity. Repeated and simultaneous duplicate callbacks cannot
  complete the unique-message total early or replace its first timestamp.

- EF Core saga send and query operations reuse an outer transaction only when
  their actual DbContext has the same active transaction identity as the
  consume-context marker. A distinct saga context retains its configured
  transaction strategy and explicit transaction opt-out. Separate databases
  do not gain distributed atomicity from this ownership check.

- F-SB-API-042: Inline inbox/outbox delivery now awaits the underlying transport
  when the captured consume endpoint contains one or more enclosing volatile
  outboxes. Previously, capture by that outer buffer could advance delivery
  progress and remove an outgoing message before its transport send failed.
  The delivery path unwraps only the contiguous known volatile endpoint layers
  and preserves the consume endpoint's context, request ID, deadline inheritance
  and outgoing task ownership. A failed or cancelled send does not advance that
  message's delivery checkpoint. EF inbox/outbox replay retains its stored row;
  the classic in-memory inbox/outbox retains work within its owning repository
  lifetime. Ordinary volatile consumer sends still buffer until success and
  discard on failure. This change does not add cross-process durability to an
  in-memory store or an exactly-once guarantee for transport delivery.

- Consumer, saga and state-machine callbacks supplied while configuring one
  endpoint now apply only to that invocation. They run after definitions and
  registration-wide actions, before endpoint attachment and consumer options.
  Callback failures and nested calls do not retain configuration for later
  endpoints or other providers built from the same service collection.
  Local callbacks require the built-in registration pipeline, including subclasses
  that inherit it unchanged. Custom SPI implementations or subclasses that
  reimplement `IConsumerRegistration.Configure` or `ISagaRegistration.Configure`
  reject a nonempty local callback before pipeline changes. Use registration-wide
  `AddConfigureAction` for intentionally shared custom configuration and invoke
  endpoint configuration without a local callback. Callbackless custom dispatch
  continues to use its own implementation.
- Manually configuring an activity no longer changes the registration-wide
  automatic-endpoint policy. Its registration context still skips a duplicate
  automatic endpoint; other providers retain their own configured exclusion policy.

- F-SB-API-043: SignalR sends to multiple connections, groups or users retain
  their scoped publish services until every publication they already started
  has completed. Previously, a later synchronous publication failure or null
  task could release that scope while earlier publications were still pending.
  The send stops admitting further recipients after that failure, joins the
  actual started tasks and preserves admission, publication and cleanup causes.
  This does not impose an input-order guarantee on asynchronous task faults.

## API review corrections, 5 October 2026

- Setter-only configuration properties now describe assignment rather than nonexistent getters.
- Saga interface and definition documentation now describes actual saga configuration,
  provider-specific undo/pre-insertion handling, returned configurators, final-state entry
  and activity data requirements. Outbox factory/filter methods describe incoming
  consumption and provider policy instead of destination sends.
- Saga request activities pass the consume-context cancellation token to message-factory
  admission and endpoint resolution, then check it again before starting a send.
  Message generation already admitted before cancellation is not rolled back.
- Typed and untyped state-machine raise paths retain the original activity failure when
  the fault observer also fails. Distinct failures are reported in order; the same
  exception instance is rethrown once. Actual caller cancellation retains its existing path.
- MessageIdHeaders now returns the supplied fallback for an incompatible requested
  reference or value type. An omitted nullable value fallback no longer becomes zero.
- BodyConsumeContext marks its four endpoint address overrides nullable and documents
  absent envelope addresses. The CLR property type and runtime projection are unchanged.

The review remains in progress. Targeted repair evidence and current completion status
are recorded in the repository review evidence and the Suite review report; these
entries do not declare completion of the entire API review.

- Normal saga response activities now pass the consume-context cancellation token to message-factory admission while preserving the existing null-context exception; pre-canceled execution leaves the factory, response transport, and continuation untouched. The repair passed 21 focused and 921 neighboring saga cases, with independently verified fresh Core and Sagas packages.

- Correct the four published summaries on typed recurring scheduling commands: their Schedule, PayloadType, Destination and Payload properties have getters. Independently verified actual Core/Sagas NuGet XML; non-generic mutable properties are unchanged.

- Await schedule-cancellation tasks already started by the in-memory outbox when a later provider invocation fails synchronously; warning logger failures no longer change the contained scheduler outcome. All84 targeted and377 adjacent Outbox/DI cases pass, with independently verified current Core/Sagas DLL/XML packages.

- Consume-Scope-Filter erhalten gleichzeitig auftretende Verarbeitungs- und Bereinigungsfehler in ihrer Reihenfolge; die Konvertierungs-Middleware wartet auf die echte Folgeaufgabe und übernimmt deren Fehler oder Abbruch. Unabhängiger Review und aktuelle Artefaktprüfung: 33 gezielte Tests und 915 angrenzende Middleware-/DI-Tests bestanden; zwei frisch erzeugte Pakete geprüft. Nachweis: `evidence/WP-SB-API-REVIEW-REPAIR-20261002/PACKAGE161_F138_F139_SCOPE_OUTPUT33_NEIGHBORS915_FRESH2_BOUNDED_ROOT_REPAIR_ACCEPTANCE_R1.json`.
- Dictionary-Header liefern bei inkompatiblen JSON-Darstellungen den vorgesehenen Ersatzwert und erhalten echte Konverterfehler. Die Serializer-Auswahl ist korrekt dokumentiert. Aktuelle unabhängige Nachprüfung: 25 Headerfälle, vier echte Scheduler-Adapter-Kontrollen, eine Zuordnungsprüfung und 2.304 angrenzende Tests bestanden; frische Core-/Sagas-Pakete mit Quellen und Debugdaten abgeglichen. Nachweis: `evidence/WP-SB-API-REVIEW-REPAIR-20261002/PACKAGE162_F134_F140_HEADER25_DEFAULT4_PROJECTION1_NEIGHBORS2304_FRESH2_BOUNDED_ROOT_REPAIR_ACCEPTANCE_R1.json`.

- Outbox-Verarbeitung und Scope-Bereinigung erhalten beide Fehler; wiederholtes Abmelden alter Request-Handles lässt Nachfolger verbunden. Forks warten bereits gestartete Aufgaben auch bei synchronen Folgefehlern oder ungültigen Task-Rückgaben ab. Fehlerhafte Scheduler-Debugdiagnosen verändern die Zurückweisung veralteter Nachrichten nicht; setter-only Delay ist korrekt beschrieben. Unabhängige aktuelle Nachprüfung: 53 gezielte Fälle, 30 Kontrollen und 2.304 angrenzende Tests bestanden; frische Core-/Sagas-Pakete geprüft. Nachweis: `evidence/WP-SB-API-REVIEW-REPAIR-20261002/PACKAGE163_F141_F145_FIVE_REPAIRS_CURRENT_NATIVE_FRESH2_BOUNDED_ROOT_REPAIR_ACCEPTANCE_R1.json`.

- Optionale Saga-Korrelationscallbacks deklarieren ihre gültigen leeren und zurückgesetzten Werte auch in den öffentlichen Nullable-Metadaten. Korrigiert42 Middleware-Verwendungsbeschreibungen und die XML-Angaben zur positiv konfigurierten Nachrichtenlebensdauer sowie zur bedingten Request-ID-Bereinigung. Alle96 gezielten Kontrollen und2290 angrenzenden Tests bestanden; Core-/Sagas-Paketinhalte unabhängig geprüft. Nachweis: `evidence/WP-SB-API-REVIEW-REPAIR-20261002/PACKAGE164_F146_F149_FOUR_REPAIRS_CURRENT_NATIVE_XML44_FRESH2_ROOT_REPAIR_ACCEPTANCE_R1.json`.

- Consume-Fanout: Ein später synchron fehlgeschlagener Konvertierungsschritt wartet alle zuvor gestarteten Ausgaben ab und erhält deren Fehler beziehungsweise Abbruch zusammen mit dem ursprünglichen Fehler. Die ursprüngliche Regression, 15 bestehende Kontrollen und zwei zusätzliche Mehrfachausgabe-Kontrollen bestehen; aktuelle Core-/Sagas-Pakete unabhängig geprüft (Paket165).

- Innere Outbox-Scope-Wiederherstellung erhält einen bereits aufgetretenen Verarbeitungsfehler zusammen mit einem zusätzlichen Wiederherstellungsfehler. Einzelne Ursachen behalten ihre ursprüngliche Ausnahmeinstanz; der geliehene äußere Scope wird nicht übernommen oder freigegeben. Vier gezielte Gegenproben, der gesamte 100er-Prüfblock, 18 Consume-Kontrollen und 2.268 angrenzende Tests bestehen; aktuelle Core-/Sagas-Pakete unabhängig geprüft (Paket166).

- Gezielte Saga- und XML-Korrekturen aus den Paketen168–172: Übergänge sowie InMemory-Load/Add/Insert geben das ausdrücklich gewählte Abbruchsignal an die tatsächlichen kooperativen Folgeoperationen weiter. Outbox-Debugdiagnosen verändern erfolgreiche Arbeit nicht. Bei fehlgeschlagener Saga-Konfiguration werden bereits erworbene Observer-Registrierungen vollständig abgemeldet; Verarbeitungs- und Bereinigungsursachen bleiben erhalten. Receive-Observer- und Reset-XML beschreiben jetzt den tatsächlichen Pipeline- beziehungsweise Wartevertrag. Die benannten Kontrollen und frischen Paketnachweise sind unabhängig geprüft; das gesamte API-Review läuft weiter. Nachweise: `PACKAGE168_F153_REPAIR_F152_PARTIAL_AND_TWO_NEW_CAUSES_ROOT_ACCEPTANCE_R1.json`, `PACKAGE169_F154_F155_REPAIR_AND_F152_DEFAULT_EXTENSION_ROOT_ACCEPTANCE_R1.json`, `PACKAGE171_F156_FIXED_F152_ADDED_EXTENSION_AND_F157_ORIGINAL_ROOT_ACCEPTANCE_R1.json`, `PACKAGE172_F157_XML_TWO_FIXED_TRANSPORT_THREE_ORIGINAL_F152_AZURE_EXTENSION_ROOT_ACCEPTANCE_R1.json` im Review-Evidence-Verzeichnis.

- Transport-Stopp versucht verbundene Consume-, Send- und eigene Kontextphasen auch nach einem früheren Fehler; mehrere echte Stoppfehler bleiben in ihrer Reihenfolge erhalten. Optionale Send-Stopp-Diagnosen verhindern die eigene Agentenfreigabe nicht. Dispatcher-Freigabe leert den Cache auch bei Diagnosefehlern. Azure-Saga-Insert erhält den tatsächlichen Speicher- beziehungsweise Konfliktausgang trotz fehlerhaftem Debuglogger. Unabhängiger aktueller Review: 160 Core- und 39 Azure-Kontrollen sowie 2.380 angrenzende Tests bestanden; fünf frische Pakete quellgebunden geprüft. Die separat reproduzierten DynamoDB-/EF-Core-Diagnosefehler werden im Folgepaket korrigiert. Nachweis: `PACKAGE173_TRANSPORT_THREE_FIXED_F152_AZURE_FIXED_PROVIDER_FIVE_ORIGINAL_ROOT_ACCEPTANCE_R1.json` im Review-Evidence-Verzeichnis.

- DynamoDB-/EF-Core-Saga-Insert bewahrt Speicher-, Konflikt- und Primärfehlerausgänge trotz fehlerhafter optionaler Diagnose. Beide Saga-Anmeldewege sowie Host-/Consumer-Stopp erreichen ihre eigentliche Arbeit trotz Fehlern im initialen Debuglogger. Fünf Initializer-/Mediator-Abschlussbeschreibungen berücksichtigen lokale Outbox-/Buffer-Erfassung vor Dispatch. Aktuelle unabhängige Nachprüfung: alle 2.677 ausgewählten Tests bestanden, vier strikte Builds ohne Warnungen oder Fehler und sieben frisch erzeugte Pakete quellgebunden geprüft. Nachweise: `PACKAGE174_PROVIDER_FIXED_F152_CONFIG_ORIGINAL_HOST_CONSUMER_TWO_REGISTERED_F001_XML5_EXTENSION_ROOT_ACCEPTANCE_R1.json` und `PACKAGE175_F001_XML5_F152_ALL14_HOST_CONSUMER_CURRENT2677_FRESH7_ROOT_REPAIR_ACCEPTANCE_R1.json`. Das gesamte API-Review und die abschließenden Gesamtprüfungen laufen weiter.

## API review completion, 6 October 2026

The source API review and repair work is complete within the individually recorded
contract boundaries. All 234 confirmed audit findings have been accepted with
their specific limits; no confirmed repair remains pending. The earlier
in-progress statements describe their dated review checkpoints. The following
entries summarize additional corrections and the final validation scope.

### Additional corrections

- Task-returning helpers now follow the existing `Async` naming convention. The
  packed API baseline records the nullable `JoinSeparator` contract already
  implemented by the endpoint formatter; the baseline update adds no runtime
  behavior (F-SB-API-176).
- Amazon SQS queue send topology retains configured queue attributes, tags and
  subscription attributes. Configured endpoint connection middleware is invoked,
  and endpoint validation includes client and connection pipe specifications.
  RabbitMQ receive endpoints also invoke their configured connection middleware
  before channel creation and validate channel and connection pipe specifications
  (F-SB-API-177, F-SB-API-178, F-SB-API-182, F-SB-API-192, F-SB-API-202).
- In the selected Product-owned diagnostic paths, optional logging and its
  diagnostic-only argument evaluation no longer interrupt the actual operation
  or owned cleanup. This includes durable-resource cancellation, startup purge,
  queue subscription/deletion and connection/session retirement. Business work,
  SDK calls and genuine provider failures remain outside diagnostic containment
  (F-SB-API-152, F-SB-API-191, F-SB-API-205, F-SB-API-208 through F-SB-API-212).
- Supervisors and host/endpoint shutdown attempt the captured owned stop stages
  and await already-admitted work within the public cancellation/budget policy,
  even after another stage fails synchronously. Reset attempts release of each
  distinct retained provider after an earlier release failure. A failed endpoint
  transport stop retains its handle instead of forcibly resetting a live
  transport (F-SB-API-204, F-SB-API-226, F-SB-API-228, F-SB-API-229).
- Failed consumer, endpoint-dependency, EF outbox and typed-observer
  admission now retires the registrations actually returned before rejection.
  An earlier cleanup failure does not skip later acquired registrations; the
  primary admission cause and additional cleanup causes are retained. This does
  not compensate unknown collaborator effects before a handle was returned.
  Activity-observer retirement also attempts later acquired child registrations
  after an earlier retirement failure
  (F-SB-API-217 through F-SB-API-221, F-SB-API-223, F-SB-API-225, F-SB-API-230).
- Request timer release still attempts owned cancellation-source and request
  capacity release when timer disposal fails. Test-harness disposal also attempts
  the remaining owned inactivity, observer and base-resource retirement after
  the selected cancellation or observer failure (F-SB-API-215, F-SB-API-222,
  F-SB-API-224).
- Courier execute and compensate redelivery honor the enabled `ReplaceMessageId`
  option while retaining the original identity when replacement is disabled
  (F-SB-API-216).
- `JsonTransportHeaders.Get` returns the documented caller default for valid but
  incompatible header representations instead of throwing for the selected
  built-in conversion cases (F-SB-API-203).
- The cancellation-token code fix selects the actual added parameter index and
  escapes keyword context identifiers in the corrected code-action routes
  (F-SB-API-206, F-SB-API-207).
- ActiveMQ topic cleanup retains both close and disposal causes. A RabbitMQ
  channel already acquired during construction is retired if the supported
  timeout setter rejects configuration (F-SB-API-213, F-SB-API-214).
- SQS topic discovery no longer readmits SDK work by replacing a cancelled loader
  after its owner lifetime ends. Settlement retains an active cancellation
  callback failure and independently joins the already-owned visibility-renewal
  task before disposing its cancellation source. Cancellation failure still
  prevents delete/redelivery. The latter is an explicitly adopted source
  ownership policy: the earlier four characterization tests did not reproduce an
  early-drain failure (F-SB-API-227, F-SB-API-232).
- Event Hubs producer disposal attempts adopted agent shutdown after observer
  disconnection fails. Rider construction retires a returned send-observer
  registration after later endpoint admission rejection. Receiver shutdown
  observes the real SDK stop outcome and independently attempts locally owned
  lease, pending-work and cancellation-source retirement. Additional
  `ReleaseClient`/pending-disposal multi-fault ordering is source-qualified,
  rather than a reproduced native failure (F-SB-API-231, F-SB-API-233,
  F-SB-API-234).
- Published XML now describes actual topology sharing, mutable retained views,
  concurrency limits, timeout units, observer callbacks, pipe behavior and
  configuration return values. RabbitMQ recreation documentation describes
  accepted management requests rather than guaranteeing restored delivery.
  These documentation corrections do not expand runtime guarantees
  (F-SB-API-147, F-SB-API-179 through F-SB-API-190,
  F-SB-API-193 through F-SB-API-201).

### Final verification

- Independent adversarial reviews cover the recorded code, test and tool
  changes, the integrated repair review and the final register/report updates.
- The current Roslyn run covers 33 source projects and 18,600 exposed declaration
  IDs without reported workspace, load or compiler errors. DLL/PDB/XML evidence
  binds the delivered assemblies to the reviewed source. The contract register's
  833 accessor representations are separate from the declaration denominator.
- Strict unit and Event Hubs builds completed without warnings or errors.
  The final unit run passed 13,460 tests in 23 modules, with no failures or skips.
  The separate Event Hubs run passed 166 tests from 22 classes, with no failures
  or skips; the six focused controls are included in that total. Seven
  emulator-dependent classes were excluded.
- The package/API/consumer gate verified 31 delivery packages and compiled all
  18 developer journeys. It executed Journey13 configuration and four isolated
  DI/assembly consumer smokes; it did not execute 18 end-to-end workflows.
- Forty relevant Microsoft/.NET skill records retain their fixed official
  source revisions: 31 were applied to their documented steps and nine were
  recorded as not applicable. Byte-retention recovery adds no new semantic
  review or execution credit.

### Remaining validation

Real cloud/broker/emulator workflows, Entra/RBAC/token rotation, and ARM64
operation with 512 MiB without swap under sustained load remain separate
evidence requirements. Dated coverage and CRAP measurements are not a fresh
global measurement of the final 23-module test run. Source and metadata
acceptance does not establish universal ABI, concurrency, arbitrary SPI or
per-member runtime guarantees. External validation remains partial, and no
production release or universal A+ grade is declared.

The repository evidence records the [final bounded acceptance](../../evidence/WP-SB-API-REVIEW-REPAIR-20261002/PACKAGE551_FINAL234_CURRENT18600_LOCAL_GATES_ROOT_ACCEPTANCE_R1.json)
and the [final administrative completion](../../evidence/WP-SB-API-REVIEW-REPAIR-20261002/PACKAGE551_FINAL_PRE_REVIEWED_ADMIN_COMPLETION_AFTER_RECEIPT_R1.json).
The Suite's `SERVICEBUS_API_REVIEW_AND_REPAIR` directory contains the full
finding catalog, PO report and remaining-evidence worklist. Frozen review
snapshots remain unchanged; this changelog update is subsequent documentation
maintenance and does not constitute a new code or test run.

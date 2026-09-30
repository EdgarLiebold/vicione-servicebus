## Unreleased

ViciOne.ServiceBus has not been released. The repository is in a private development state, and the
entry below records what the current work changed for anyone reading the source.

### Verification added during the source review

- Synchronized the Amazon SQS FIFO shutdown test with the receiver's actual
  `Stopped` signal. `Completed` can precede that signal, so the former immediate
  assertion intermittently failed despite a successful shutdown.

- Separated send-body metadata preflight, contract binding, and failure
  restoration into explicit operations; journal capture now separates its
  preflight, body read, and header snapshot. The admission proof and identity
  checks and the scalar identity comparisons have likewise been split at
  their behavior boundaries. This reduces complexity without relaxing
  rejection or restoration. New tests require cached bodies to reject later
  correlation, native-route, and contract changes, preserve the original
  serializer failure after multiple mutations, and reject stale journal
  metadata before capture.

- Fixed physical payload admission accepting a serializer getter that changed
  the send destination before body creation. The shared admission boundary now
  binds expected metadata before serializer callbacks, checks it through body
  length and proof binding, and restores rejected changes. A red-first boundary
  test verifies that the changed destination cannot cross admission.

- Fixed reliable in-memory inbox staging when a serializer changed the send
  destination after it had been selected, or a header getter changed correlation
  metadata after body materialization. Admission, body, proof, and replay
  metadata now share one expected metadata snapshot before a send is staged.
  A red-first destination drift test and a delayed header drift test assert
  that neither inconsistent message can be committed to the inbox outbox.

- Fixed durable sends and both EF outbox paths whose serializer or header
  callbacks could change identity, destination, or other send metadata during
  payload admission and later metadata capture. The expected metadata is bound
  before admission and checked through body, proof, and persisted record
  creation. Red-first regressions change `CorrelationId` from a header getter
  and verify restoration with neither durable admission nor EF staging; a
  serializer getter regression covers destination drift.

- Fixed sends whose serialized JSON or MessagePack envelope retained one
  application header value while the mutable send context or journal exposed
  another. The send boundary now binds application headers through the active
  wire serializer, rejects changes during body creation and byte reads, and
  restores replaced values and prevents rejected messages from reaching a
  transport. A write-only converter still fails closed if an existing value
  mutates in place. Forwarding binds inherited and newly supplied headers together;
  the journal captures that projected set. Diagnostic propagation and broker
  scheduling headers can still change at their defined transport boundary.
  Red-first tests cover replacement and in-place header mutations, byte-read
  callbacks, forwarding in both wire formats, and custom JSON header
  converters, including a write-only converter. The final product-wide coverage and provider replay for this
  change remain open.

- Fixed RabbitMQ and Event Hubs sends that could publish a body through a
  different native route or with different delivery guarantees after a body
  callback changed provider metadata. The shared body and journal guards now
  bind and restore native metadata. RabbitMQ applies a distinct publish
  payload's mandatory-routing requirement before binding, so a valid send
  remains readable and journalable. Event Hubs initializes its conversation
  identity before the first body read. Red-first provider tests cover route
  drift, cached bodies, durable acceptance, throwing callbacks and the valid
  mandatory-routing path.

- Fixed send metadata drifting away from its serialized body or native route.
  Identity, response and fault addresses, expiration, content type, and contract
  types are now checked when the body is created and materialized; rejected
  mutations restore the context before transport delivery or journal capture.
  ActiveMQ also checks its native reply destination. Azure Service Bus can still
  record the scheduling token returned after broker acceptance, and JSON and
  MessagePack forwarding can still restore the original contract types during
  their first body creation. Red-first bus and provider regressions cover the
  inconsistent deliveries and those legitimate forwarding and scheduling paths.

- Fixed outgoing journal captures that could retain changed `MessageId` or
  `ContentType` after a second body read. Successful delivery remains intact,
  while the inconsistent capture is discarded and the send context is
  restored. The same guard now applies to faulted sends, including a body
  callback that throws after changing metadata. Red-first real-bus tests
  distinguish delivered content type, body bytes, journal writes and the
  original send fault. A fault capture with metadata changed during body
  materialization is now conservatively discarded because a generic body
  cannot prove whether the change preceded or followed byte creation. The
  complete Core project passes 7,411/7,411; final read-only Red Team verdict:
  PASS, no further concrete P1/P2 in this change.

- Fixed the typed Durable Sender accepting a serializer that changed
  `ContentType` while creating or reading the message body. It now restores
  the original content type and rejects the send before durable admission;
  when the serializer throws, the original exception remains intact.
  Red-first tests cover both mutation phases, and separate fault tests check
  exception identity, metadata restoration and no admission. The adversarial
  review found no further concrete P1/P2 in this change. The complete Core
  project passes 7,405/7,405.

- Fixed send-time body callbacks that could change `MessageId` or `ContentType`
  after the serialized payload was read but before transport metadata was
  written. In-memory, RabbitMQ, Azure Service Bus, ActiveMQ, Event Hubs,
  Amazon SQS, SQL Server and PostgreSQL now use a shared metadata guard. A
  rejected or throwing callback restores the original metadata; the outgoing
  journal cannot record a contradictory fault entry. Red-first transport,
  SQL-storage and bus integration tests cover successful and throwing
  mutations, plus an untrusted exception that tries to suppress journal
  capture. The read-only adversarial review found no further concrete P1/P2
  in this change. The full Unit solution passed 12,268/12,269; its sole Quartz
  timeout passed alone and in the complete isolated Quartz project (318/318).

- Fixed four message-identity mismatches at outgoing serialization boundaries.
  The in-memory transport now rejects an identity change while materializing
  the body. Payload admission rejects a bounded serializer that changes the
  identity and binds the first admitted identity across the later `PreSend`
  observer pass. A successful send journal capture rejects a second body read
  that changes the already delivered identity, restores the send context, and
  avoids writing a contradictory journal entry. Four red-first bus integration
  tests exercise the distinct timing windows. The complete Core suite passes
  7,398/7,398; adversarial review found no further concrete P1/P2 in this diff.

- Fixed message-journal capture of a faulted outgoing send with a lazy
  serializer. Materializing the body before metadata exposed changes made by
  the serializer; the later journal consistency guard now discards such a
  capture when the identity or content type changes during materialization.
  The original send fault remains intact. The complete Core suite passed
  7,394/7,394 at this stage.

- Fixed the classic in-memory receive outbox accepting a serializer-modified
  outgoing `MessageId`, including `Guid.Empty`. It now snapshots a nonempty ID
  before serialization and rejects changes after transport text is fully
  materialized, before appending any message. Four red-first tests cover both
  immediate and deferred serializer callbacks and prove that a following valid
  send still appends exactly one record. The complete Core suite passes
  7,393/7,393; adversarial review found no concrete P1/P2 in the fix.

- Closed another serializer-driven message identity gap in the classic EF
  transactional and receive outboxes and the in-memory reliable inbox. Both EF
  callers now retain the original ID across payload admission; the EF message
  factory also checks deferred body materialization. The in-memory inbox checks
  before buffering. Red-first tests cover a replacement GUID and `Guid.Empty`
  through both productive EF callers and the registered in-memory inbox
  pipeline. The complete EF and Core suites pass 409/409 and 7,389/7,389.
  An adversarial review identified and then verified closure of the initial
  EF caller gap.

- Fixed two durable-send identity races with mutable serializer callbacks.
  The EF transactional outbox now rejects a `MessageId` changed during
  serialization before staging or reserving capacity. The typed durable sender
  preserves the ID set after schedule options and rejects a later serializer
  change before admission. This retains the supported case where a scheduled
  message ID differs from its idempotency key. Red-first tests cover a different
  GUID and `Guid.Empty`; the complete EF and Core suites pass 403/403 and
  7,387/7,387 respectively. The adversarial review found no remaining P1/P2
  issue in this change.

- The corrected EF message-ID path passed a new exact-commit, 33-profile
  product-wide measurement: 14,139 passing executions, 92.40183% Line,
  85.14605% conservative Branch, and no method above CRAP 30. All twelve
  local provider fixtures ended without findings.

- Fixed the EF transactional outbox accepting `Guid.Empty` as a message ID.
  It now rejects the invalid identity before serialization, staging or capacity
  reservation, matching the existing EF outbox contract. A red-first SQLite
  test checks that no record or capacity state is staged and that a following
  valid send commits normally.

- A fresh exact-commit product-wide checkpoint verifies 33 profiles,
  14,138 passing test executions, all 32 product assemblies and no method
  above CRAP 30. Line and conservative Branch coverage are 92.39550% and
  85.12921%. One Core collectible-type test failed under parallel load but
  passed in a complete isolated rerun; its load sensitivity remains open.

- SignalR backplane registration now rejects remote group timeouts that the
  .NET system timer cannot schedule. The final supported fractional-millisecond
  value remains valid; invalid values fail before DI registration. The complete
  SignalR project passes 101/101.

- Both public Azure Table Job Service repository overloads now have a real
  storage test for their three distinct saga key assignments. Deliberately
  swapped or ignored formatters fail; the complete Azurite integration project
  passes 51/51.

- Fixed Azure Table journal rejection of an already overfull partition when
  its capacity lease is absent: the store now checks the bounded partition
  before creating the lease, then reads the authoritative entry snapshot after
  the lease ETag. Azurite tests cover both lease states and a delayed competing
  append, and also verify that service-client journal composition uses the
  named table. The complete integration project passes 49/49.

- The Azure Table runtime saga provider now has an Azurite-backed public-path
  test. It verifies the configured partition and row keys in physical storage
  and reloads the saga through the public repository factory with that same
  formatter. The complete integration project passes 45/45.

- The DynamoDB runtime registration test now mixes versioned and unversioned
  sagas. It proves that the optional provider configures only compatible saga
  types; removing the version guard fails the test at the generic proxy bound.

- The S3 LocalStack suite now exercises the public repository selector through
  a real bus start. It proves that bucket and tagged lifecycle reconciliation
  finish during PreStart, while a legacy all-object rule fails startup before
  PostStart. An adversarial PostStart-only implementation fails this test.

- Receive transport startup now reports a terminal fault if retry-policy
  context creation fails or returns null. Policy decision and terminal callback
  failures retain their cause; retry delays and callbacks stop with the transport
  while policy-owned cancellation retains its original token. Throwing Info or
  Error loggers cannot consume a retry or hide the terminal transport fault.

- Property metadata caches now skip indexers before selecting scalar members.
  Message type metadata also selects case-distinct hidden properties using
  ordinal case-insensitive names, so runtime read and write caches can use the
  derived member without duplicate-key failures.

- Completed the test harness's Activity ownership path. Timeline diagnostics
  now survive hostile start and stop observers without leaking their listener;
  the idle tracker keeps its root trace current after a reentrant start
  callback and releases its timer and listener even if stop observation fails.

- Isolated Activity listener and sampler failures from durable-send,
  message-journal, circuit-breaker and message-flow operations. Activity
  completion restores the actual prior ambient scope even for a new trace
  root; receive `New` and `Link` modes now start independently of an
  unrelated ambient trace.

- Added a rerunnable Roslyn inventory of the externally visible C# API and
  source XML comments across the ten sibling repositories. The snapshot records
  project diagnostics and comment candidates without claiming that the full
  cross-repository API review is complete.

- The Roslyn public-documentation gate now classifies enum members through
  the visibility of their containing enum. Private receiver-state values no
  longer appear as missing public API documentation.

- Concurrency-limit management now keeps a committed adjustment and response
  successful if debug logging fails. Failure logging also cannot replace a
  stale-command failure or another original command error.

- Filter observers now receive the original failure when a typed or untyped
  pre-send observer fails, whether it throws synchronously or returns a failed
  task. Dispatch and post-send notification remain skipped in that case.

- Keyed routing handles now remove their registration only once. Disconnecting
  an old handle again cannot remove a replacement route registered under the
  same key.

- Dynamic dispatch now owns all started output work when a converter throws
  synchronously. Remaining registered routes are still visited, outstanding
  routes finish before the original failure escapes, and the continuation is
  skipped after any route fails. Single and multiple outputs reject a null
  task consistently. Diagnostic probing uses a stable registration snapshot,
  so a pipe can register another route during its probe callback.
  The running-job cancellation regression now observes the persisted Running
  state before canceling, closing a test race between consumer entry and the
  job saga's state transition.

- Preserved both causes when public `RetryAsync` processing or policy admission
  fails and policy disposal also fails. The execution cause precedes cleanup
  in an aggregate for both task and result overloads; successful processing
  still exposes a cleanup failure directly.

- Corrected half-open circuit-breaker cancellation ownership for nested
  aggregates. A pure caller cancellation now releases the exclusive recovery
  probe; a mixed business failure, a distinct dependency token, or a business
  failure inside cancellation still reopens the circuit.

- Retained faulted outbox measurements when a custom exception's base lookup
  throws. The metric records the original exception type and still completes
  once; a null base result also falls back to the original type.

- Preserved cleanup and cancellation diagnostics in utility and MessagePack
  boundaries. Both public asynchronous dispose callback forms still invoke
  cleanup when an exception's base lookup throws or returns null, then rethrow
  the original failure. If cleanup also fails, both causes remain observable
  in order. MessagePack distinguishes pure nested cancellation from mixed
  cancellation and business failures, including cyclic exception graphs and
  business failures nested inside a cancellation.

- Corrected Saga and Courier cancellation classification for nested pure and
  mixed failures. Unsafe exception base lookups no longer hide Saga fault
  notifications or turn Courier cancellation into a routing-slip failure.
  Mixed business failures keep their original route and exception identity.
  Courier fault-observer failures now preserve the activity failure before the
  observer failure. The request-client timer regression test waits for actual
  disposal rather than racing the timer's creation.

- Preserved fault settlement when a custom exception's base lookup throws or
  returns null. Azure Service Bus fault headers retain the original failure,
  and SQL still unlocks the delivery with its original type and message.

- Hardened exception selection and fault diagnostics. Retry and Rescue can
  still inspect the structural inner failure when a custom base-exception
  lookup throws. Activity exception events fall back to the original failure
  for throwing or null base lookups; a throwing stack-trace getter no longer
  prevents consumer fault notification. InMemory transport fault logging keeps
  the original failure when diagnostic base lookup fails.

- Hardened failure handling during duplicate receive-lock settlement, supervised
  pipe-context shutdown and consumer ingress cancellation. An exception with an
  unsafe base-exception lookup no longer skips a valid settlement fallback,
  replaces the final settlement cause, prevents agent shutdown or hides a
  structurally wrapped consumer cancellation. Agent shutdown also survives an
  unsafe exception-message getter and disposes its owned context once.

- Corrected receive-fault cancellation and ownership. A canceled delivery now
  stops fault publication and error-queue movement, and its token reaches
  endpoint resolution, fault send, notification and error transport. The
  rethrow mode reports the original receive fault once through the dispatcher,
  which also records fault state; observer and diagnostic-logger failures no
  longer prevent observer notification or lock settlement. Rescue fault headers fall back to the
  original exception when a custom `GetBaseException()` throws or returns null.

- Fixed observer and outbox failure ownership. An observer callback or consumed
  notification failure now retains its original cause even when fault
  notification or the observer error callback also fails; each reporting path
  is attempted independently. A later pipeline failure remains downstream and
  does not report a second observer fault. If an outbox `SetConsumedAsync`
  failure is followed by a fault-notification failure, both causes remain
  observable in their original order.

- Fixed unexpected consumer cancellation diagnostics in factory, handler and
  instance ingress. `ConsumerCanceledException.InnerException` now retains the
  exact failure already reported to the consume context, including a single
  cancellation wrapped by `AggregateException`. Mixed cancellation and business
  failures retain their original aggregate type and identity. Nested aggregates
  made entirely of cancellations are classified as consumer cancellation,
  while an already classified cancellation keeps its identity, even inside an
  aggregate. Ordinary wrappers inspect every aggregate branch, so a business
  failure cannot be hidden by an earlier cancellation. If fault notification
  also fails, both the operation and notification failure remain observable in
  a two-cause aggregate; Activity, metrics and the Handler fault counter still
  record the operation failure.

- Fixed Rescue selection for an `AggregateException` with one inner failure.
  Rescue now evaluates the original exception, so `Handle<AggregateException>`
  can select it and `Ignore<AggregateException>` can veto a broader Handle.
  The rescue context still receives the original exception. Regression tests
  also cover nested aggregate leaves and exclusion behavior.

- Fixed exception selection for nested `AggregateException` trees. Handle and
  Ignore now inspect every distinct inner exception in a stable order, so
  retry, rescue, circuit-breaker and kill-switch policies see a configured
  leaf even when multiple aggregate levels wrap it. Strong contract tests
  cover type matches, exclusion vetoes, typed predicates, outer wrappers and
  shared exception instances.

- Fixed Saga schedule replacement on delayed and native Azure scheduling.
  Replacement now rejects unsupported cancellation or missing delivery identity
  before constructing or dispatching a new message; the previous Saga token
  remains intact. Normal and faulted, typed and untyped paths share the guard.
  Initial schedules, delayed own-delivery schedules and custom schedulers with
  an undeclared mode retain their supported behavior. Unschedule now honors an
  already canceled context and rejects cancellation for explicitly unsupported
  schedulers before either direct or deferred outbox cancellation can clear the
  Saga token. A separate capability mode now records that the built-in delayed
  provider carries a caller-selected delivery token without supporting physical
  cancellation. The internal Job Service clears its token only for that mode,
  allowing its stale-event filter to discard the eventual delivery. Azure's
  provider-assigned cancellation continues to use the stored broker token.
  Focused regression tests cover the provider-mode matrix, header ownership and
  deferred-cancel success facade.

- Fixed Saga timeout cancellation identity. Expiry scheduling now sets its
  token to the request ID used by response and fault cancellation. Positive
  timeouts reject Azure Service Bus native scheduling, transport-delayed
  scheduling and undeclared custom schedulers before request dispatch because
  those providers cannot cancel with that caller-selected token. Endpoint,
  publish and SQL scheduling declare their supported mode; the bus scheduler,
  consume scope and in-memory outbox preserve it. Behavior tests verify the
  schedule/cancel sequence, early rejection, real provider classifications
  and the original four modes through both wrappers. Core 7,140/7,140 passed before
  the final test-strengthening edit; the changed focused tests and all three
  requirement projections passed afterward. Red Team final review is PASS.

- Fixed saga requests with a positive timeout dispatching and changing their
  request ID before discovering that no scheduler is available. Timeouts that
  exceed the supported date range now fail before dispatch as configuration
  errors. Normal and faulted, typed and untyped request tests protect the
  admission boundary; a blocked-send test confirms that an accepted request's
  timeout still begins when its send completes. If the clock reaches the end
  of the date range during the send, the expiry uses the last representable
  instant so the accepted request still receives a schedule.

- Fixed saga state re-declaration after a property receives a same-name state
  from another machine. All four direct/nested state and substate declarations
  restore the registered state and its transition-event identity. Reparenting
  now compares the registered parent instance, and replacing a substate
  removes it from the former parent's hierarchy before registering it with
  the new parent. Replacing a parent moves its registered children to the
  new parent while preserving their identities and transition events. Ten
  focused behavior cases cover foreign ownership, direct/nested reparenting
  and the named-substate overload. Direct, nested and named attempts to move
  a state beneath its own descendant now fail atomically before a hierarchy
  cycle can form. The global A+ checkpoint remains open.

- Fixed cron evaluation at the representable time limits. A search after
  `DateTimeOffset.MaxValue` now returns no next occurrence instead of
  overflowing, and `IsSatisfiedBy` returns false at the earliest UTC instant
  instead of subtracting outside the date range. Red-first tests caught both
  errors. UTC-10 and UTC-14 cases prove that the last permitted local second
  of 2199 is still found in UTC year 2200 before the schedule is exhausted;
  237 focused Cron test executions pass. The global A+ checkpoint remains
  open.

- Added twelve behavior cases for recurring message initialization failures
  across endpoint and publish command transports, explicit and topology
  destinations, and all three pipe forms. A failed input property read now
  has a direct test for exact exception identity and zero command, endpoint
  resolution and pipe effects; the same input must then succeed with the
  correct payload, destination, schedule and cancellation token. A deliberate
  early endpoint-resolution counterchange failed the no-pipe test. Product
  source was unchanged; global A+ awaits the next full checkpoint.

- Fixed EF reliable outbox admission after change-tracker callback failure:
  rejected sends are detached before they can be saved without a session,
  capacity is restored, and earlier accepted sends remain intact when
  selective cleanup succeeds. The cleanup also handles post-change,
  pre-change and DetectChanges callback failures. If a callback persistently
  forbids detach, the tracker and staged session are cleared to prevent an
  orphan, with explicit notice that all pending DbContext changes must be
  replayed. Five strong behavior tests cover six variants, including four
  red-first failures. Five related outbox/inbox methods were split along
  ownership and processing boundaries; the four changed source files now
  have no method above CRAP 30 in direct affected-project coverage. Complete
  Core 7,099/7,099 and EF 400/400 pass on `78f0f17b7`; global A+ remains
  open until the next strict 33-profile checkpoint.

- Ran a fresh strict product-wide Line/Branch/CRAP checkpoint on `ee2253fa3`:
  33 exact-commit profiles, all 32 product assemblies and 13,783 passed test
  executions. Line coverage is 87,071/94,328 (92.30663%) and conservative
  branch coverage is 31,583/37,199 (84.90282%). Five methods exceed CRAP 30,
  concentrated in EF outbox and reliable inbox code. The checkpoint records
  this regression from T97's zero and keeps global A+ open.

- Fixed caller cancellation during shared one-time setup. Canceling the
  initiating caller or a follower now ends only that caller's wait; the
  shared callback continues for other callers and retains its cache or
  failure/retry semantics. Four red-first cases cover leader/follower and
  success/fault outcomes. Focused 10/10, neighboring OneTime 15/15 and
  complete Abstractions 966/966 pass on `aaa6aa9b4`; adversarial review is
  PASS. Product-wide Line/Branch/CRAP A+ remains open.

- Fixed recurring schedule and control commands that could reach a transport
  after caller cancellation. Publish scheduling now checks cancellation
  before topology lookup; endpoint scheduling and controls recheck after
  endpoint resolution. Red-first regressions assert zero external calls,
  exact token identity, command payload and healthy recovery. Focused tests
  pass 24/24, neighboring Recurring tests 275/275 and complete Core
  7,099/7,099 on `9d403c961`; adversarial re-review is PASS. The next
  product-wide Line/Branch/CRAP profile remains on the grouped cadence.

- Fixed cancellation handoff in send, consume, execute and compensate
  transformation filters. Each path now passes its context token to message
  initialization. Four pre-canceled regressions prevent downstream delivery
  and preserve the exact cancellation token; the existing success, pending
  and fault matrices also verify token forwarding. Transformation tests pass
  62/62 and complete Core passes 7,075/7,075 on `c2a204265` with no skips;
  adversarial review is PASS. The next product-wide Line/Branch/CRAP profile
  remains on the larger-packet cadence.

- Fixed transformation property handling when the source has no input, when
  providers or nested initializers return a null task, and when nested
  conversion starts with a canceled operation or null context. The source
  envelope, its owner token, and the separate operation token are checked by
  behavioral regressions. The Transformation suite passes 58/58 and complete
  Core passes 7,071/7,071 on `f34880737`; adversarial re-review is PASS.
  Product-wide Line/Branch/CRAP A+ remains open; its next measurement follows
  the larger-packet cadence.

- Fixed adaptive request parallelism after a canceled downward adjustment.
  The published request count now follows a completed permit drain; partial
  drains are refunded and overlapping adjustments are serialized. Four
  regressions cover cancellation, concurrent empty completions, disposal and
  a pre-canceled completion whose limit stays unchanged. Two cases failed
  red-first; focused tests pass 39/39 and complete Abstractions 962/962 on
  `8142f0b29`, with no skips. Final adversarial review is PASS. T97 remains
  the product-wide Line/Branch/CRAP checkpoint; global A+ remains open.

- Fixed request lease settlement so duplicate completion, completion after
  disposal, and concurrent completion/disposal cannot release the owner's
  request permit or pending capacity twice. The `ActiveRequest` constructor is
  now internal: external package callers must obtain an owned request through
  `RequestRateAlgorithm.BeginRequestAsync`. This intentionally removes the
  former public constructor from the unreleased package API; friend assemblies
  retain internal access. Four red-first regressions verify accounting,
  blocked requests, healthy successors and the external API boundary. Focused
  tests pass 4/4; complete Abstractions passes 958/958 without skips. T97
  remains the product-wide profile; global Line and Branch A+ remain open.
  Fresh-package API validation and final adversarial re-review pass.

- Fixed receive-side outbox cancellation ownership across classic InMemory
  and EF and outgoing admission across reliable InMemory and EF. Distinct
  delivery and operation tokens now cancel state transitions, serialization,
  blocked writes and final EF commit with the original source token. SQLite
  rollback and healthy retry tests guard the consumed fence; durable and
  external outgoing state remain empty after cancellation. Complete Core
  tests pass 7,060/7,060 and EF tests 394/394. Global Line and Branch A+
  remain open. Final adversarial re-review is PASS on `b200c1148`.

- Fixed distinct delivery and operation cancellation in the InMemory and EF
  reliable inboxes. Both sources now cancel acquisition and processing,
  preserve the original exception token, and prevent a canceled consumed
  fence or outgoing intent from committing. EF rolls back business state
  even when cancellation arrives between `SetConsumedAsync` and transaction
  commit. Focused Core tests pass 3/3, focused EF 5/5, complete Core
  7,046/7,046 and complete EF 376/376 on `2882773e6`; final adversarial
  re-review is PASS. Global Line and Branch A+ remain open.

- Fixed RabbitMQ quorum reconfiguration retaining an old group size and
  broker topology emitting auto-delete or exclusive quorum queues. Endpoint
  and direct declaration tests verify final queue and exchange behavior,
  including expiration and invalid-repeat recovery. Focused tests pass 32/32
  and complete RabbitMQ unit tests 525/525 on `fbb25b940`; final adversarial
  re-review is PASS. Global Line and Branch A+ remain open.

- Fixed an in-memory outbox race that could acknowledge a deferred send or
  scheduled cancellation after the final release drain without executing it.
  Admission now checks release and cancellation under the queue lock, while
  immediate scheduler callbacks run outside internal locks. Five deterministic
  regressions cover the lost-send boundary, exact one-time delivery, empty
  queues, cancellation during lock wait, and callback lock state. The original
  race tests failed red-first; focused tests pass 5/5 and complete Core passes
  7,043/7,043 on `d954933e6`.
  Independent adversarial re-review is PASS. The global Line and Branch A+
  targets remain open.

- Added startup tests for two simultaneous transport/limits owners on one bus
  beside a healthy neighbor and for duplicate catalog, outbox, inbox,
  scheduler, and dispatcher ownership. They require exact diagnostics before
  the invalid bus or owner factories are constructed. Two controlled
  counterchanges failed; focused tests pass 14/14 and complete Core passes
  7,038/7,038 on `a34fe1f07`. Adversarial review is PASS after its
  bus-materialization test gap was closed. Product code did not change; global
  Line and Branch A+ remain open.

- Fixed EF outbox delivery and cleanup when persisted state contradicts
  unsent messages. The worker now rejects invalid cursor state before external
  sends, guards completion against races, and retains messages if cleanup
  encounters any remainder. Red-first SQLite tests caught silent loss and an
  intermediate duplicate-send risk; focused tests pass 11/11, complete EF
  unit tests 371/371 on `bb89661c2`, and canonical PostgreSQL integration
  8/8. Independent adversarial review is PASS. Global Line and Branch A+
  remain open.

- Fixed the classic EF transactional outbox falsely treating a tracked state
  change as a database commit. It now validates exact State and Message
  ownership, rejects unsafe delivery-state mutations before saving, and
  signals only after a complete EF save. Abort preserves foreign entries;
  failed factory or EF tracking operations leave no orphan and allow retry.
  External `SaveChanges(false)` completes only the owned batch. Seven new
  requirement variants and one corrected existing variant pass 22/22 focused
  tests and 368/368 EF tests on `2957da1d2`; independent adversarial review
  is PASS. Global Line and Branch A+ remain open.

- Fixed EF transactional outbox sessions that could claim a commit after
  staged records were detached, reserve capacity twice for a duplicate ID,
  or persist a message without matching capacity. Exact staged-entry and
  capacity checks now guard SaveChanges; abort restores the prior ledger.
  Successful `SaveChanges(false)` accepts only outbox-owned entries, so later
  commits and batches do not reinsert them. Twelve new requirement variants
  pass 28/28 focused tests and 359/359 EF tests on `ea3ce51ee`; independent
  adversarial review is PASS. Caller-owned outer transactions and EF
  SaveChanges-interceptor assumptions are documented. The global Line/Branch
  A+ checkpoint remains open.

- Fixed persistent outbox send capture to carry the caller's cancellation
  token through durable admission. Cancellation after transport context
  creation can no longer admit the message. A seven-shape test matrix proves
  exact token, context, message and pipe behavior, plus waiting for delayed
  storage admission and propagating its exact error. Red-first and controlled
  counterprobe failures verified the regression tests; focused tests pass
  21/21 and complete Core passes 7,036/7,036 on `f221b6af1`. Independent
  adversarial review is PASS. The next global coverage/CRAP checkpoint remains
  on the agreed larger packet interval.

- Completed the next strict 33-profile product-wide checkpoint on commit
  `3cb94a285`: 13,604/13,604 test executions, 32 assemblies,
  86,639/93,963 covered physical lines (92.20544%), 31,312/36,927
  conservatively covered branches (84.79432%), and zero methods with CRAP
  above 30. Line and Branch A+ remain open. The full profile follows twelve
  coherent packets since T85; future packets use targeted tests during work,
  full affected-project tests at closure, and less frequent full profiles.

- Added message-handler adapter tests across all eight context/message and
  zero-to-three dependency signatures. They prove exact pending task identity,
  ordered inputs, one invocation, asynchronous success/failure, and exact
  synchronous exceptions. Substituting a completed task or a null message
  failed controlled counterprobes. The focused class passes 24/24 and complete
  Core passes 7,015/7,015 on exact test commit `190a35eb4`; adversarial
  review is PASS. Product code did not change.

- Added request-handler consumer tests for pending handler and response
  operations across all eight message/context and zero-to-three dependency
  signatures. All four consumer arities now prove that null responses are
  not sent and that handler and response faults retain the exact cause and
  number of send attempts. Removing a response await or sending a null
  response failed controlled counterprobes. The focused class passes 20/20
  and complete Core passes 6,991/6,991 on exact test commit `0d6e59d4a`;
  adversarial review is PASS. Product code did not change.

- Fixed saga request-start and request-fault activities that could publish a
  lifecycle event before discovering a missing pipeline continuation. Both
  now validate context and continuation before publication. New tests verify
  complete started and structured-fault metadata, asynchronous publication
  order, failure propagation, invalid messages, and all four null boundaries.
  The old behavior failed the regression test; a fault-payload counterchange
  also failed. Focused tests pass 6/6 and complete Core passes 6,971/6,971
  on exact commit `775efa3e2`; adversarial re-review is PASS.

- Added ActiveMQ cached-producer contract tests for every native factory and
  send overload, close/dispose lifecycle, and all eight public property pairs.
  Pending native tasks prove exact async task forwarding; usage order and
  property behavior are checked. Two controlled forwarding errors failed the
  new tests. The focused class passes 33/33 and complete ActiveMQ passes
  266/266 on exact test commit `323dce211`; adversarial re-review is PASS.
  Product code did not change.

- Added resource-cache tests for usage-event faults during eviction and
  partial subscription compensation. They prove once-only disposal, index
  removal, successor admission under capacity pressure, and two warning
  records with their exact distinct exceptions. Bounded capacity waits keep
  a regression test finite. Both controlled counterchanges failed. The
  focused class passes 15/15 and Core passes 6,965/6,965 on the exact test
  commit; adversarial re-review is PASS. Product code did not change.

- Added durable-delivery tests for caller cancellation during dispatch and
  after transport acceptance. Both preserve the intent for lease replay with
  the same attempt number. A consumer completion that races timeout quarantine
  now proves successful retirement, no quarantine record, and the exact
  awaiting-to-delivered telemetry sequence. Two controlled counterchanges
  failed the tests. The focused class passes 20/20 and Core passes
  6,963/6,963 on the exact test commit; adversarial re-review is PASS.
  Product code did not change in this packet.

- Added assembly scanner integration tests using real assembly files. They
  distinguish DLL-only from `.exe`-extension discovery, prove scanning
  continues after an invalid DLL, and verify that the assembly predicate
  rejects a loaded decoy in both path overloads. A controlled early-exit
  counterchange fails the new test. The focused class passes 11/11 and Core
  passes 6,960/6,960 on the exact test commit; adversarial re-review is PASS.
  Product code did not change in this packet.

- Added an integrated raw-handler test for `UseMessageScope`. It proves a
  distinct scope for each success, failure and subsequent success; matching
  handler scope/provider identity; no disposal while asynchronous work is
  blocked; exactly one disposal after completion; and a fault tied to the
  failing message and exception. Removing `UseMessageScope` fails the test
  with a missing scope payload. The focused class passes 1/1 and complete
  Core passes 6,957/6,957 on the exact test commit; adversarial re-review is
  PASS. Product code did not change in this packet.

- Fixed nearest-weekday cron progression: a backward-adjusted date could
  repeat after firing, while a partial correction skipped an adjusted date
  in the next month. An exhausted month now restarts at day 1, and the day
  selector never returns a date before its cursor. Five boundary calendars
  and four three-occurrence journeys cover repeat, skip, short-month and leap
  cases. The focused suite passes 36/36 and complete Core passes 6,956/6,956
  on the exact fix commit; independent adversarial re-review is PASS.

- Strengthened the integrated Courier retry and outbox journey. The revised
  route's successor now proves its outgoing effect is buffered until release,
  then admitted and delivered once. Both object and enumerable completion
  variables update the result and remove a stale key; termination proves the
  successor never runs. A controlled removal of its outbox failed both revise
  cases. The focused class passes 7/7 and Core passes 6,947/6,947 on the exact
  test commit; adversarial re-review is PASS. Redundant concurrent-delivery
  rows were removed because that behavior was not distinguished here.

- Added an integrated catch-path saga request test with dynamic address and
  deferred factory. It verifies original failure context, three distinct
  correlation IDs, exact request and response routing, and no request or
  state advance if the factory fails. The focused theory passes 2/2 and Core
  passes 6,948/6,948 on the exact test commit; adversarial re-review is PASS.
  Product code did not change in this packet.

- Added integrated tests for a data-free saga signal sending a pending message
  task with a callback. They verify that no callback or delivery occurs before
  the task completes, exact destination and transport metadata on success, and
  preserved cause with no send or continuation on failure. The focused theory
  passes 2/2 and Core passes 6,946/6,946 on the exact test commit; independent
  adversarial review is PASS. Product code did not change in this packet.

- Completed a fresh 33-receipt product-wide Line/Branch/CRAP measurement on
  commit `fa4a1a910`: 13,500/13,500 test executions passed, all 32 product
  assemblies and required provider/portability profiles were present. Line
  coverage rose to 92.10344%, conservative branch coverage to 84.71286%,
  and no method exceeded CRAP 30. Line and branch A+ remain open.

- Added public typed receive-dispatcher lifecycle tests with real consumers.
  They verify two simultaneous serialized deliveries, separate completion,
  cumulative and peak metrics, awaited zero-activity notification, caller cancellation and
  a healthy successor. The dispatcher class passes 4/4 focused tests and
  the affected Core project passes 6,944/6,944 on the exact test commit;
  adversarial re-review was PASS. Product code did not change in this packet.

- Corrected shared EF Core saga registration to finish configuration and
  validation before admitting the saga map. A red-first regression test
  reproduced a retained map after a callback exception; the final test also
  verifies validation failure and a successful retry with the intended table
  and lock strategy. The affected EF Core test project passes 344/344, and
  the read-only adversarial review found no P1/P2 issue in this bounded fix.

- Added consume-scope send tests for all eight callback overloads in the
  Abstractions assembly. They verify the exact endpoint overload, message or
  initializer value, callback context mutation, single invocation and caller
  token. Each asynchronous form must remain pending until its callback
  completes; synchronous and asynchronous callback faults prevent transport
  acceptance. The independent Red Team found two surviving wrong-route and
  fire-and-forget mutations, which the final tests now distinguish. The
  Abstractions project passes 954/954 on the exact test commit; no product
  implementation changed in this packet.

- Added SQL scheduled-message cancellation tests for the active consume client
  and the host connection supervisor. They verify exact schedule and caller
  tokens, failed database deletion, one immediate retry with a fresh client,
  cancellation before and after entering the supervisor, stopping-host
  rejection, and the no-op result when a schedule is already absent. The
  adversarial review identified three missing counterexamples; all were
  added and the re-review found no concrete P1/P2 gap. The SQL unit project
  passes 242/242 on the exact test commit. Product code was unchanged.

- Corrected SQL scheduler registration to reject a missing default or typed
  bus configurator at the public boundary. New DI tests verify that a prior
  scheduler keeps precedence, a non-SQL host is rejected, and both default
  and typed schedulers retain their bus owner, configured clock and per-scope
  lifetime. The independent Red Team found and closed two typed registration
  gaps: scope reuse across owners and replacement of a pre-registered typed
  scheduler. The affected SQL test project passes 233/233. This packet did
  not run a new whole-repository Line/Branch/CRAP profile.

- Corrected the ActiveMQ cached producer so a missing destination or native
  producer fails at construction. A null producer returned by a factory can
  no longer poison the destination cache key; a healthy retry can send and is
  released exactly once on shutdown. Tests also prove exact explicit send
  settings, one usage signal per attempt and propagation of both a faulted
  native send task and a synchronous native async-call failure. Two red-first
  failures reproduced the defect. The affected class passes 9/9 and the
  complete ActiveMQ unit project passes 233/233 on the exact product/test
  commit; independent Red Team re-review found no remaining concrete P1/P2
  issue.

- Added Azure Table saga persistence tests for SDK-provided offset timestamps,
  strict failure on wrong required and nullable native storage types, and
  sparse nullable properties versus explicitly stored false, zero and empty
  values. The affected entity-converter class passes 29/29, and the entire
  Azure Table unit project passes 92/92 on the exact product/test commit;
  no product implementation changed in this packet. Independent Red Team
  re-review found no remaining concrete P1/P2 issue.

- Added public reliable-scheduler tests with an injected clock and real
  in-memory durable store. They verify exact relative send and publish due
  times, separate routing, payload and token identity, rejected admissions,
  one-tick claim boundaries, and preservation of the original fencing lease
  when cancellation of a claimed intent is rejected. An independent queued
  neighbor remains cancelable. The affected class passes 39/39; no product
  implementation changed in this packet.

- Corrected normal saga request declaration to reject missing request,
  message factory and service-address provider before the machine can run.
  Request completion now rejects a missing pipeline context or continuation
  before publishing an event. Red-first tests reproduced both defects.
  Request lifecycle tests verify exact send and completion ordering,
  request-ID ownership through failures and cancellation, and the original
  or generated completion payload and metadata. Focused request and
  completion classes pass 21/21 and 5/5; the exact-commit Core suite passes
  6,940/6,940 and independent Red Team re-review has no remaining concrete
  P1/P2 finding.

- Corrected saga composite declaration so invalid constituents fail before a
  property event is replaced or a named event is registered. Tests reproduce
  both former defects and verify null, empty, oversized and uninitialized
  inputs across property, named and existing-event APIs, followed by a valid
  declaration that raises once. A two-saga request journey also verifies that
  either accepted response follows the request-header ID even when its payload
  names a different saga. A faulting request affects only its own saga while a
  healthy neighbor succeeds; the fault keeps the exact request-header ID,
  decoy payload ID and cause. The exact-commit Core suite passes 6,930/6,930;
  independent Red Team re-review has no remaining concrete P1/P2 finding.

- Added four task-lifecycle tests for pending success and failure paths,
  exact outcome transfer, and virtual-clock timeout boundaries. The
  Abstractions suite passes 940/940; no product implementation changed.

- Added mediator dispatch tests for callback-configured direct send, runtime
  publish, consume-scope forwarding, and response. They check asynchronous
  configuration order, transport-visible headers, exact callback failure,
  non-delivery after failure, metadata isolation between messages, and exact
  forwarding of an explicit interface contract distinct from the runtime type.
  The affected Core suite passes 6,916/6,916; the independent Red Team
  re-review found no remaining blocker.

- Corrected adaptive receive scheduling when a provider enumerates one result
  and then fails, or an ordered group fails after one result. These failures
  now retain their exact cause instead of returning a successful partial
  count; earlier completed work and capacity for a later pass are preserved.
  An admitted callback also retains its result permit until it runs and
  releases it, even if the caller cancels before the ThreadPool schedules it.
  Red-first tests exposed the partial-error swallowing; Red Team source review
  found the cancellation race. Full Abstractions, Amazon
  SQS, and SQL Transport suites pass 936/936, 322/322, and 229/229. The full
  Line/Branch/CRAP measurement remains scheduled for the grouped milestone.

- Preserved the full integer prefetch value when endpoint definitions configure
  InMemory and other transports. Derived values now use wide arithmetic and
  stop at both integer limits. Amazon SQS computes FIFO partition capacity
  without integer overflow, preserving queue admission and backpressure under
  a large prefetch value. RabbitMQ reports prefetch above 65,535 as a
  named configuration failure before its 16-bit QoS setting can wrap to zero;
  direct settings projection also rejects out-of-range values. Red-first tests
  exposed the previous wrap and arithmetic overflow. Full Core passes
  6,911/6,911, Amazon SQS 322/322, and RabbitMQ 516/516 unit tests. The
  independent Red Team found the two additional overflow paths and passed
  the re-review after correction. Product-wide coverage and CRAP
  remain scheduled for the grouped milestone.

- Corrected RabbitMQ stream retention and offset configuration. Retention
  periods now use exact whole-second broker units instead of rounding 36 hours
  to two days or 90 minutes to two hours; invalid negative or fractional-second
  changes preserve the earlier limit. Negative numeric and pre-epoch timestamp
  offsets preserve the earlier consumer position. Maximum stream length is
  nonnegative, and segment size cannot exceed RabbitMQ 4.2's 3,000,000,000-byte
  limit. Public documentation now accurately describes timestamp and `last`
  chunk behavior. Red-first tests exposed the original defects; the full
  RabbitMQ unit project passes 513/513. A fresh real broker test passed 2/2,
  confirmed a declared `36h` queue and message delivery, and reported no
  fixture findings. The complete coverage/CRAP audit remains at the grouped
  milestone.

- Fixed RabbitMQ no-ack publishing through shared and scoped channel views:
  linked caller, owner and parent cancellation now remains active until the
  actual client publish finishes, even though the public no-ack call returns
  immediately. Scope disposal waits for all in-flight publishes before
  releasing the parent link. Five red-first cases exposed the original loss;
  expanded tests cover two concurrent publishes, successful cleanup and exact
  awaited failure. The full RabbitMQ unit project passes 505/505 and the
  independent Red Team re-review is PASS. The complete Line/Branch/CRAP audit
  remains scheduled for the larger packet milestone.

- Corrected RabbitMQ duration argument conversion for queue and exchange
  declarations. Whole-millisecond values retain the broker's 32-bit argument
  type where possible and use an exact 64-bit value beyond that range;
  fractional and negative values fail before changing existing arguments.
  Generic `x-expires` durations require a positive value. Queue expiration
  now rejects positive sub-millisecond values, round-trips through its getter,
  and reaches the broker topology without floating-point precision loss or a
  change from 32-bit to 64-bit argument type. Red-first tests reproduced the
  defects; the full RabbitMQ unit project passes 496/496. The complete
  coverage/CRAP measurement remains scheduled for the grouped milestone.

- Fixed RabbitMQ queue configuration boundaries: rejecting an invalid quorum
  replication factor now preserves the earlier classic queue, exclusivity and
  priority settings; fractional-millisecond acknowledgement timeouts no longer
  overwrite a valid broker timeout with a truncated value. Whole-millisecond
  timeouts now use exact integer conversion, fixing a large-value precision
  loss found by Red Team. Red-first endpoint tests reproduced all three errors.
  Focused boundary, stream and requirement tests pass 6/6; the next complete coverage/CRAP audit remains grouped with later
  packets.

- Rejected an empty token returned by a configured one-time scheduling token
  selector before command, delayed-transport or SQL endpoint resolution. Such
  a token could previously be reported as accepted and then rejected by the
  Quartz consumer or SQL cancellation path. Red-first Core tests reproduced
  the acceptance; final focused Core and SQL suites pass 24/24 and 4/4,
  including requirement projections and valid-token recovery.

- Corrected assembly scanning when the same assembly is registered through
  direct, named and type-based entry points. Repeated registration previously
  duplicated discovered types; a red-first public API test observed three
  entries for one assembly. The scanner now shares its existing deduplication
  path. Scanner, finder and cache tests pass 18/18, and the requirement
  projection passes 1/1; the full coverage profile
  is reserved for the next grouped measurement.

- Fixed Event Hubs partial-batch progress: after one provider batch succeeds
  and a later one fails, a retry sends only pending messages. Confirmed
  contexts receive `PostSend`; unresolved contexts receive `SendFault` with
  the original failure. Cleanup and observer failures cannot replay a
  confirmed message. A confirmed message's telemetry span no longer records
  a later message's failure. Red-first tests caught the replay and false span;
  focused sender/producer suites pass 15/15 and 12/12, with a separate 3/3
  real broker delivery control. The subsequent complete 33-profile audit
  passed 13,318 tests but exposed one CRAP>30 hotspot in the new
  batch send method. The method was then split into focused steps. Fresh
  focused coverage reports CRAP 6.04 for the send method and 14.27 for its
  largest helper; the complete frozen measurement predates this extraction.

- Corrected send outcome ownership across the common transport and Event Hubs
  single/batch producers. A failed post-send observer or logger after provider
  confirmation can no longer report a false send fault or make the confirmed
  send retryable. Fault-observer and logger failures no longer replace the
  original provider exception. Red-first tests reproduced both errors before
  the correction; focused Core and Event Hubs outcome tests pass, and the
  complete Core project passes 6,897/6,897. Event Hubs outcome tests use a
  controlled transport context. The existing broker suite passed 96/97 in one
  fresh fixture; the sole checkpoint observation timeout passed 1/1 in
  another. The fixture runner now waits for the Event Hubs emulator's own
  entity-ready signal. A later failed Event Hubs partial batch can still misclassify
  earlier confirmed messages and is tracked as the next batch-progress case.
  T59 remains the latest complete global coverage/CRAP measurement.

- Corrected ActiveMQ producer creation shared by concurrent sends: cancellation
  of the first sender no longer cancels another sender's pending producer.
  The session executor now receives the cache-owned creation token through an
  internal path; the public cache API remains unchanged. A red-first session
  test reproduced the failure, and new cache tests check canceled waiters and
  independent destinations. A three-flavor broker test checks exact native
  routing, identity, priority and durability across sequential sends. Full
  ActiveMQ Unit and LocalIntegration suites pass 229/229 and 106/106 with no
  skips; fresh broker fixture findings are empty. T59 remains the latest
  complete global coverage/CRAP measurement.

- Added six persistent JobService integration cases across Azure Table/Azurite
  and EF Core/PostgreSQL. They check genuine overlapping success and fault,
  per-job terminal state and events, and release of a held execution slot after
  completion, fault or cancellation. The affected local projects pass 44/44
  and 96/96 without skips; both provider fixtures have empty findings. A
  deliberate Completed-state slot-release defect fails the new Azure Table
  completion test. Observation timeouts now match the validated local-provider
  operation timeout. T59 is the latest complete 33-profile coverage/CRAP
  baseline; no new product-wide figure is claimed for this packet. The next
  full measurement will cover several larger connected packets.

- Added a larger multi-response Saga request verification packet: stored
  request-ID routing, three response types, service fault, real Quartz timeout,
  callback owner override, missing/wrong IDs, two active Saga owners and stale
  one-/two-/three-response generations. Thirty new cases and the complete
  Quartz project (318/318) pass. Two deliberate correlation defects were
  detected by the corresponding tests and restored byte-for-byte. The frozen
  33-profile measurement passes 13,287 tests across 32 product assemblies:
  86,006/93,754 physical lines (91.73582%), 31,084/36,845 conservative
  branches (84.36423%) and zero methods with CRAP>30. Independent receipt/XML
  audit and four accepted broker-fixture groups are clean. One initial Azure
  Table local test observation failed and passed in a fresh fixture; the failed
  attempt remains documented. No product source changed; global Line/Branch A+
  remains open.

- Added a connected registration-to-failure verification packet: filtered
  consumer discovery and its definition, scoped compensation with observed
  disposal, retry exhaustion before selected rescue, and message-journal
  ownership across two active buses. Seven new integration cases and the Core
  suite pass; a wrong-owner product mutation fails both journal variants.
  The frozen 33-profile measurement passes 13,257 tests across 32 product
  assemblies: 85,958/93,754 physical lines (91.68462%), 31,057/36,845
  conservative branches (84.29095%) and zero methods with CRAP>30. Four
  broker-fixture groups and independent receipt/XML audit are clean. No
  product source changed in this packet; global Line/Branch A+ remains open.

- Corrected Consumer, Handler and Instance middleware lifecycle accounting:
  a failure in `next` after successful consumption now propagates without
  recording a second fault for the already completed consumer. Its process
  activity and metrics also finish before downstream work. A red-first test
  reproduced the duplicate fault in all three forms. Twenty-eight new cases
  check exact context and exception identity, asynchronous notification order,
  caller versus dependency cancellation, per-delivery retry budgets, circuit
  recovery and process-span ownership. The affected Core suite passes
  6,887/6,887 without skips. The frozen 33-profile measurement passes 13,250
  tests across 32 product assemblies: 85,902/93,754 physical lines (91.62489%),
  31,048/36,845 conservative branches (84.26652%) and zero method CRAP>30.
  Relative to T56, 13 more lines and 12 more conservative branches are covered.
  Global A+ coverage remains open.

- Durable admission atomicity verification: the EF reliable inbox now has
  SQLite and real PostgreSQL regression cases that save a first business record
  and outgoing intent inside a transaction, reject a later oversized message,
  verify rollback and persisted retry, then commit only a distinct replacement
  with its exact serialized body. Direct EF scoped-outbox cases cover the
  caller's explicit Abort and Commit choices after a later rejection. Both
  requirement projections pass; an isolated cleanup counterchange and a
  commit-instead-of-rollback counterchange are detected by the tests. The
  PostgreSQL case exercises the registered EF provider and scoped factory;
  it does not claim broker dispatch. The corrected complete 33-profile run
  passes 13,222 tests with 85,889/93,754 lines (91.61102%),
  31,036/36,845 conservative branches (84.23395%) and zero CRAP>30. This
  packet adds four behavioral cases; the unchanged product source has ten
  fewer observed lines and one more observed branch than T55. Global A+
  remains open.

- Consumer-outbox recovery work in progress: reject a loaded message without a
  DestinationAddress before treating the delivery pass as complete, and pass the
  linked delivery timeout/cancellation token into endpoint resolution. Three
  focused cases reproduce the original failures and pass after correction;
  requirement projection also passes. Thirty-three PostgreSQL cases now verify
  retained corrupt intents, committed delivery windows, allowed at-least-once
  replay, Save/Commit/Cleanup failure recovery and exact neighbor preservation;
  pending-send deadlines and late pipeline failures retain recoverable intent;
  the combined local control passes34/34 including requirement projection. A test
  observer mismatch on typed consume faults was corrected without changing product
  error semantics. Two isolated counterprobes detect neighbor deletion and a missing
  final-batch delivery watermark; product files were byte-restored. The frozen
  33-profile/provider measurement passes13,218 tests without failures or skips;
  line coverage is85,899/93,754 (91.62169%), conservative branch coverage
  31,035/36,845 (84.23124%), with zero method CRAP>30. The remaining5,819
  method gap identities keep global A+ open. Independent read-only audit verifies
  all33 receipts,487 hashes and exact XML-derived aggregate counts.

- Corrected a race in the SQS Quartz integration test discovered by the T54 full
  measurement: consumer delivery can precede Quartz trigger removal. The test now
  observes matching finalization and actual bounded store removal, retains the
  absence assertion, and checks delivery count after bus stop. A blocked-finalization
  counterprobe fails as required; restored controls pass2/2. This corrects test
  synchronization and does not claim a product delivery defect.
- Added a combined transport verification packet for Event Hubs configuration
  ownership and repair, endpoint identity, deferred producer resolution/delivery,
  and native ActiveMQ group isolation across OpenWire, AMQP and Artemis. The
  22 new cases distinguish rejected configuration from successful continuation,
  exact inherited/overridden metadata, provider failure/cancellation and missing
  awaits. Read-only review strengthened cleanup and overload discrimination.
  Both isolated counterprobes are detected and restored; combined controls pass
  27/27 without skips. After the documented SQS test correction, the fresh complete
  measurement passes13,182 tests across33 profiles and four clean fixture groups:
  91.59814% lines,84.21581% conservative branches and zero CRAP>30. Independent
  integrity and numerical audits agree. Global A+ is open with5,826 gap identities;
  no product defect is claimed from this test-only packet.
- Added a connected Azure Table saga-persistence verification packet: 13 cases
  cover corrupted native and serialized rows through public loads and real
  consumption, schema evolution with absent versus explicit empty values, and
  exact UTF-16/binary/UTC storage limits. Rejected inserts and updates perform
  no HTTP writes; existing rows, ETags and neighbors remain intact. Repaired
  sagas continue through the same repository. Independent review strengthens
  the JSON error-category oracle; two isolated counterprobes are detected and
  restored. Final focused controls pass19/19 with zero build warnings/errors.
  No product defect is claimed from this test-only packet. One complete run passes
  13,160 tests across33 profiles and four clean fixture groups:91.55120% lines,
  84.11811% conservative branches and zero CRAP>30. Independent integrity and
  numerical audit agrees. Global A+ remains open with5,837 gap identities.
- Added a connected scheduling verification packet: 32 cases cover publish
  admission before initializer or endpoint effects, control-command resolution
  and delivery ownership, invalid Quartz replacements and provider failures.
  Existing target schedules and same-name neighbors in other groups remain intact
  on rejection; subsequent valid commands preserve exact payloads and headers.
  Read-only review strengthened asynchronous failure and header assertions.
  All three isolated counterprobes are detected and manually restored; final
  focused controls pass179/179 without skips. No product defect is claimed from
  this test-only packet. Its full-product measurement passes13,147 tests across
  all33 profiles and four clean fixture groups:91.54160% lines,84.10997%
  conservative branches, zero CRAP>30. Independent integrity and numerical audit
  agree. Global A+ remains open with5,843 line/branch gap identities.
- Added a connected receive/settlement verification packet across Core, Amazon
  SQS, SQL transport and Azure Service Bus: 39 cases cover exhausted duplicate
  fallbacks, canceled waiters, pending receive work, renewal failure and drain,
  rejected ownership, exact settlement metadata and awaited abandonment.
  Three isolated counterprobes are detected and manually restored; final focused
  controls pass72/72 without skips. This packet changes tests and the SQL test-only
  fake-time dependency; it does not claim a newly fixed product defect. The full
  product measurement passes13,115 tests in33 profiles with four clean fixture
  groups:91.53094% line coverage,84.08554% conservative branch coverage and zero
  methods with CRAP>30. Independent integrity/numerical audit agrees; global A+
  remains open with5,850 line/branch gap identities, including generated methods.
- Corrected raw JSON forwarding through a consumed interface losing the original
  concrete message contract URNs. The preserving serializer now receives the
  nonempty original declared contract set, so a concrete downstream consumer can still
  be selected. When raw header contracts are absent, separately restored send
  contracts are retained, including Quartz scheduled delivery. The combined
  JSON-boundary packet adds value-conversion failures,
  configured envelope admission and independent forwarding targets. Its raw
  regression fails against the original implementation while three forwarding
  controls pass. All three isolated counterprobes are detected and restored;
  56/56 combined corrected controls and 6/6 scheduling controls pass with zero
  build warnings/errors. The corrected product-wide measurement passes 13,076
  executions in all 33 profiles, including 284/284 Quartz cases. It records
  91.51067% line coverage, 84.07197% conservative branch coverage and zero methods
  with CRAP > 30. Global A+ acceptance remains open.

- Added twelve combined Courier journeys for execute retry with route replacement
  or termination, successful and exhausted compensation retries, and activity
  deadlines during execution or compensation. Real received effects distinguish
  committed attempts from discarded attempts; logs, variable removal and failure
  ownership are checked through the complete routing-slip pipeline. Controlled
  pending work and an independent healthy slip prove deadline and outbox
  isolation. Both sequential and concurrent delivery are exercised. Read-only
  review strengthened the negative event assertions. Missing compensation-outbox
  registration and incorrect timeout-token propagation are detected by isolated
  counterprobes. Product source remains unchanged; 69/69 restored controls pass.
  Commit `ad3ddbd40` passes one complete 33-profile measurement with 13,032
  successful executions. Lines reach 85,771/93,753; conservative branches reach
  30,948/36,845. Independent integrity and numerical audit confirms all counts,
  physical changes and gap transitions without discrepancy. Overall A+ remains
  open with 5,855 remaining line/branch-gap identities and zero CRAP > 30.

- Added seventeen combined MultiBus host and scheduler scenarios. Real Generic
  Host lifecycle tests distinguish each bus's health options, failure floor and
  continued delivery when the other bus stops; invalid options identify their
  owner without contaminating the companion. Scoped endpoint, publish and native
  delayed schedulers deliver through the correct bus with their own scope and
  clock. Received recurring/control commands retain exact identities. These tests
  prove command routing, not execution by an external scheduler. Product sources
  are unchanged. Both injected ownership faults are detected, and 50/50 restored
  new/existing controls pass. Commit `3422dc2fd` passes one complete 33-profile
  measurement with 13,020 successful executions. Lines rise to 85,703/93,753;
  conservative branches to 30,937/36,845. Independent integrity and numerical
  audits confirm the deltas and retained branch-only gaps. Global A+ remains open.

- Added ten Saga journey cases across Core and Quartz. Real transport verifies
  owner/event-dependent callback metadata, pending factories and sends, primary
  dispatch failures and failed compensation. Reusing a Saga for a second request
  proves that late responses, faults and timeout messages from the first request
  cannot change the second request or cancel its real Quartz trigger. Both trigger
  existence and removal are observed. Selected callback-pipe and timeout-owner
  counterchanges are detected; restored combined checks pass20/20 Core and22/22
  Quartz, with no product source change. Commit7d0e3d332 passes one complete
  33-profile measurement and13,003 executions. Line coverage remains85,587/93,753;
  conservative branches rise to30,906/36,845. Independent integrity and numerical
  audits find no packet blocker. Observation gains/losses and remaining method
  gaps are explicitly reconciled; global A+ remains open.

- Fixed ActiveMQ receive timestamps being shifted by the local UTC offset when
  the OpenWire SDK returns a local DateTime. The provider now converts the
  timestamp to UTC before checking epoch eligibility and exposing its instant.
  Regression cases use real OpenWire and AMQP message objects, preserve exact
  milliseconds and reject timestamps at or before the epoch. The combined
  transport-header packet also verifies native-map overwrite/removal, RabbitMQ
  reserved metadata and SNS/SQS filtering of normalized scalar values.
  All four selected deliberate faults are detected; restored isolated checks
  pass21/21 ActiveMQ,21/21 RabbitMQ and13/13 SNS/SQS including requirement
  projections. Commit01617f4ec passes all33 product-wide profiles and12,993
  executions without retries:85,587/93,753lines and conservative30,903/36,845
  branches. Independent behavioral and numerical reviews found no concrete
  blocker. Remaining enumeration-wrapper and branch gaps stay documented;
  no global A+ claim is made.

- Added six InMemory reliable-inbox behavior cases for ownership changes during
  consumer failure, delayed consumer commits and admission rejection followed by
  explicit operator recovery. Assertions prove discarded buffered messages,
  preserved current ownership, exact due-time eligibility and corrected content
  under the same outgoing identity. Main Core tests pass 6,737/6,737 and four
  correctly targeted deliberate faults are detected. A misdirected preliminary
  probe is documented separately. The isolated full control exposed a race in
  the new test's global quarantine expectation: nested recovery does not clear
  a failed send registered with the enclosing consumer. Assertions now check
  both consumer states at their completion boundaries. Corrected isolated
  verification passes 15/15 with no review blocker. Commit5c3e6c3a4 passes all33
  profiles and12,953executions:85,536/93,749lines and conservative30,862/36,843
  branches. No product defect is claimed; global A+ remains open.

- Added eighteen EF reliable-store regression cases for composite inbox
  pagination, stale or removed leases, failure atomicity, initialization recovery,
  capacity boundaries and explicit schedule due times. Real SQLite checks preserve
  retained records, neighboring stores and capacity ledgers across failures.
  All five isolated deliberate faults are detected; main and restored isolated
  EF suites pass 340/340. No product defect was reproduced. Commit a30933c1c
  passes all 33 profiles and 12,947 executions without retries. Product totals
  are 85,532/93,749 lines and conservative 30,858/36,843 branches; target store
  gains 37 covered lines. Global A+ remains open.

- Added six EF Core inbox regression cases for stale and removed consumer
  attempts across completion, retry and quarantine. Real SQLite assertions prove
  current-owner fencing, exact persisted state, no resurrection and unchanged
  same-message, same-consumer and other-store neighbors. All five deliberate
  faults are detected; main and restored isolated EF suites pass 322/322.
  Production behavior already passes these cases. Full measurement at a763757b4
  passes 33 profiles and 12,929 executions. The ownership/missing helper reaches
  9/9 lines, 4/4 branches and CRAP 4. Product totals: 85,485/93,749 lines and
  conservative 30,839/36,843 branches. Non-target gaps remain recorded; A+ is open.

- Fixed Azure Functions receiver cache collisions that dispatched messages to the
  previously selected consumer at the same queue or subscription path. Cache keys
  now distinguish transport, dispatch kind and handler type; each pipeline gets
  independent registration state while preserving bus ownership. Consumer-only
  all-handler dispatch no longer requires the optional Saga capability. Fifteen
  behavior cases cover consumers, saga state, activities, invalid subscriptions
  and typed-bus context. All five deliberate faults are detected; restored
  isolated tests pass 395/395. Full measurement at c28e9feb4 passes all 33
  profiles and 12,923 executions: 85,480/93,749 lines and conservative
  30,840/36,843 branches. Changed method identities and generated accessors
  remain separately accounted for; global A+ remains open.

- Added six Cron regression cases for invalid tokens, Unicode whitespace and
  calendar-union ordering across month boundaries, coincident dates and year
  exhaustion. Removed unreachable private parser guards and simplified calendar
  selection with preserved semantics. Three deliberate faults are detected;
  restored isolated Core tests pass 6731/6731 without skips. Full measurement at
  118cc5ded passes 33 profiles and 12,908 executions. Targets reach full line
  coverage and CRAP 20/18; four target branches remain. Product totals are
  85,401/93,751 lines and conservative 30,808/36,823 branches. Removed code and
  changed reachability are separately reconciled; global A+ remains open.

- Added eight RabbitMQ factory-option cases proving independent message/frame
  limits, certificate/provider/static-credential priority and actual cluster
  resolver selection with port/TLS projection. Four deliberate faults are detected;
  restored isolated controls pass 482/482. No broker-authentication or negotiated-
  limit claim. Full measurement at d286a336a confirms 33 profiles and 12,902
  executions; SQL fixture startup failed before tests and was separately retried.
  Factory reaches 46/46 lines, 25/28 branches and CRAP 28. Product totals:
  85,407/93,762 lines and conservative 30,814/36,841 branches. A+ remains open.

- Added 16 DynamoDB registration validation cases covering complete diagnostics,
  rejection before registration/context creation, valid TTL boundaries, immutable
  registered options and lazy context creation through actual DI. Three deliberate
  faults are detected; restored isolated controls pass 44/44 without skips.
  No product code change or cloud persistence claim. Complete measurement at
  49dbda6d9 passes 33 profiles and 12,894 executions; Validate reaches 14/14
  lines and branches, CRAP 14. Product totals: 85,402/93,762 lines and conservative
  30,813/36,841 branches. Non-target observation changes remain documented;
  global A+ is not established.

- Fixed an ActiveMQ Quartz integration-test race: trigger finalization precedes
  job-store removal. The test now observes removal within a bounded deadline while
  retaining exact delivery and broker-state assertions. Both protocols pass;
  deliberately blocking finalization causes both to fail at the deadline, and
  restored isolated controls pass. The first T38 full measurement remains recorded
  as failed; fresh T38b passes33profiles/12878executions at7c617f34c, including
  ActiveMQ100/100. Lines85396/93762, conservative branches30809/36841,
  no CRAP strictly above30; A+ remains unproven.

- Added 44 Azure Service Bus subscription processor cases for message/session
  callbacks, exact arguments/tokens, awaited completion, configuration guards,
  start/stop/close/dispose and documented warning behavior. Four deliberate faults
  are detected; restored controls pass44/44 without skips. No real broker or full
  SDK-processor disposal claim. T38b covers69/69 method lines and41/44 branches
  in SubscriptionClientContext; three branches remain open.

- Added Event Hubs processor lifecycle tests proving cancellation forwarding,
  isolation of equal offsets in distinct partitions, shutdown waiting for an
  in-flight checkpoint, and subsequent client re-leasing. Both deliberate faults
  are detected; restored controls pass 2/2 with no skips. Checkpoint callbacks
  are simulated. All 33 profiles now pass with 12,834 executions; line coverage
  is 91.0507%, conservative branch coverage 83.5699%. ProcessorLockContext.Canceled
  reaches 3/3 lines. There remain 4,466 line-gap identities and 1,508 additional
  branch-only candidates. Unrelated observation changes are not attributed to
  the new tests. A+ remains open.

- Added Event Hubs checkpoint tests for newest accepted offset, ordered fallback,
  healthy continuation after rejected updates, and cancellation of a waiting
  admission in a full queue. Exact callback order, stored offset and tokens are
  checked with real batching and confirmation objects. Three mutations are
  detected; restored controls pass 5/5. Provider storage remains simulated;
  all 33 profiles now pass with 12,832 executions. Line coverage remains
  91.0465%; conservative branch coverage is 83.5754%. BatchCheckpointer's
  measured coverage is unchanged; behavioral evidence is stronger. There are
  still 4,467 line-gap identities and 1,509 additional branch-only candidates.
  A+ remains open.

- Completed T35 across all 33 profiles: 12,827 passing executions, 91.0465%
  line and 83.5699% conservative branch coverage. Both recurring schedulers
  have stronger command/pipe/completion evidence. Coverage increased by 100
  physical lines and 39 conservative branches; method guards remain, and
  three line-gap identities reappeared elsewhere. A+ remains open.

- Added 132 recurring-scheduling cases across endpoint/publish schedulers,
  explicit/publish destinations, message and pipe forms, and delayed success,
  failure or cancellation. Assertions check exact contracts, payloads, schedule
  values, headers, caller/provider tokens and completion semantics. Three
  Publish-scheduler mutations are detected; restored controls pass 132/132.
  The complete product-wide measurement of this packet remains pending.

- Completed T34 across all 33 profiles: 12,695 passing executions, 90.9398%
  line and 83.4641% conservative branch coverage. EF Saga RollbackAsync is
  fully covered at 5/5 lines. Changed observations elsewhere leave net four
  fewer covered lines than T33; their causes remain open. There are 4,464
  line-gap identities and 1,505 additional branch-only candidates. A+ is open.

- Added real-SQLite Saga load/query checks that preserve the original operation
  exception when rollback also fails. Tests prove transaction identity, an
  uncancelable rollback attempt, no commit, owned-context disposal and healthy
  subsequent reads. Three deliberate faults are detected; restored controls
  pass 2/2. The full product-wide measurement of this packet remains pending.
- Completed the T33 measurement across all 33 profiles: 12,693 passing
  executions, 90.9441% line and 83.4641% conservative branch coverage. All 30
  outbox scheduling overload bodies now have full line coverage. Across the
  product, 37 line gaps closed and one reappeared; 4,465 line-gap identities
  and 1,506 branch-only candidates remain. Product-wide A+ is still open.
- Added outbox scheduling lifecycle checks across typed, runtime and initialized
  messages, pipe variants and explicit/input/publish destinations. Tests verify
  exact schedule metadata, real checkpoint rollback, deferred cancellation,
  repeated commit, provider errors and successful recovery without phantom
  cleanup. Focused 78/78 cases pass; three isolated mutations are detected.
  The complete all-profile measurement of this packet remains pending.
- Completed the T32 measurement across all 33 profiles: 12,615 passing
  executions, 90.7852% line and 83.4586% conservative branch coverage. SQS
  ApplyResponse and EF AwaitConsumerCompletionAsync now have complete line and
  observed branch coverage. Five line gaps closed and two reappeared elsewhere;
  4,501 line-gap identities and 1,505 branch-only candidates remain. A+ is open.
- Added SQS checks for absent opposite result collections in all-success and
  all-failure batch responses, and real-SQLite checks for consumer completion
  preceding dispatch transition and rejection of a reclaimed lease's old owner.
  Tests assert exact caller outcomes, persisted bytes, store isolation and
  capacity. Read-only review strengthened ordered byte assertions. Four isolated
  mutations were detected and restored; focused controls pass 6/6 and 15/15.
  The new packet's complete 33-profile measurement remains pending.
- Completed all 33 profiles for the SQS batch packet: 12,611 passing executions,
  90.7788% line and 83.4288% conservative branch coverage. ApplyResponse now
  has 20/20 lines and CRAP18, with two branches still open. Five line gaps
  closed and two newly appeared; no product-wide A+ acceptance is claimed.
- Added SQS batch identity checks for reordered mixed success/failure results
  and atomic rejection of duplicate or contradictory response IDs. Every
  caller's exact outcome, complete request membership and provider token are
  checked. Deliberate wrong-caller and missing-duplicate-guard changes are
  detected; product source is restored. Full packet measurement is pending.
- Completed all 33 profiles for the cancellation packet: 12,607 passing
  executions; all four targeted cancellation methods have full line/observed
  branch coverage. Overall coverage is 90.7628% lines and 83.4179% conservative
  branches. Six newly observed method gaps remain under investigation; covered
  lines decreased by six versus the previous run. A+ remains open.
- Added Event Hubs confirmation cancellation checks for partition/offset
  isolation, replacement admission, exact cancellation tokens, preserved
  terminal outcomes and rejection after processor shutdown. Added a request
  lifecycle check that repeated cancellation during failure cleanup cannot
  replace the original transport or response exception. Both packets detect
  deliberate incorrect terminal-state changes; product-wide measurement of
  this packet remains pending.
- Completed the 33-profile measurement for the five-gap test packet: 12,604
  passing executions, 90.7692% line coverage and 83.3989% conservative branch
  coverage. All five targeted line gaps are closed. Four newly observed
  cancellation-path gaps remain under review; product-wide A+ remains open.
- Added SQS shutdown checks for an in-flight poll that returns an empty result
  successfully after stop is signaled. No further provider poll or warning/error
  is allowed; the test detects a consume-loop fault hidden by successful agent
  completion. Existing cancellation-path controls remain covered.
- Added RabbitMQ channel-owner invalidation checks after an acquired operation
  fails, and KillSwitch checks that late successful completions cannot revive
  paused or terminated endpoints or contaminate the next recovery window.
- Added real-SQLite checks for repeated actions on removed schedules and inbox
  entries. Exact NotFound results, retained neighbor messages and capacity
  accounting are checked across store instance recreation and store identities.
- Added real Courier execution tracing checks for tracking number, processor,
  argument contract and caller/send/receive/process ancestry alongside successful
  routing-slip completion. These additions verify existing product behavior;
  they do not establish a new product-wide coverage result or A+ acceptance.
- Refreshed the complete 33-profile coverage measurement at one source/test
  commit, including every local provider profile and CPU fallback mode:12,597
  passing executions. Line coverage is90.7521%, conservative branch coverage
  is83.3799%;4,512 method identities retain line gaps. Five newly observed gaps
  remain under investigation. The measurement does not declare A+ completion.
- Added outgoing message-data retention checks for send-policy precedence,
  additional retention, unlimited storage and exact duration boundaries.
  Overflow must fail before repository access. Tests detect omitted retention
  addition while preserving fallback and existing provider behavior controls.
- Added real Quartz checks rejecting all three Saga response types and fault
  replies when RequestId is absent. A valid body identity must not silently
  select a Saga; rejection preserves both Sagas and the actual scheduled timeout.
  A controlled third-response fallback to body correlation is detected while
  the other thirteen response/correlation cases remain passing controls.
- Added real Quartz checks for all five Saga request correlation callbacks.
  Each override must select its configured Saga instead of the default owner,
  preserve the other Saga and its trigger, and address cancellation to the
  selected owner. Ignoring the third-response callback is detected independently
  of the unchanged default-correlation cases.
- Added real Quartz-backed Saga-ID request tests for all three response types,
  service faults and timeout dispatch. Misleading body IDs must not redirect
  responses away from the request-header owner. Response/fault cases verify
  removal of the actual scheduled trigger; a controlled wrong-correlation
  change is detected. Documented the cancellation requirement for positive
  Saga request timeouts and the limitation of transport-delayed scheduling.
- Added completed-initializer transform cases across Execute, Compensate,
  Consume and Send, checking preserved/replaced data, context identity and
  downstream success/failure without premature completion.
- Added asynchronous send-transform checks for message replacement, preserved
  identity/address/header metadata, downstream completion and initializer
  failure without forwarding. A controlled wrong-message change is detected
  by both replacement cases while preserve cases remain green.
- Extended asynchronous transform checks with successful downstream completion
  and Consume-context replacement/failure behavior, preserving the tested
  correlation ID and cancellation token. A controlled wrong-context change is
  detected by the replacement-message assertions.
- Added controlled asynchronous activity-transform tests for initialization
  ordering, context/data identity, downstream completion and exact failure
  propagation. They detect a prematurely completed compensation pipeline when
  its downstream task is not awaited.
- Added real Courier routing-slip tests for repository-backed arguments and
  compensation logs, including deterministic expiry before execution and before
  compensation. Assertions preserve exact content/reference/tracking identity
  and distinguish stage-specific data failures from successful effects.
- Added RabbitMQ nested consume-binding regression tests for the exact directed
  exchange tree, settings, sibling parent restoration and rejection before
  declaration when a parent binding is missing. These tests strengthen existing
  behavior without changing production code.
- Added five real-SQLite inbox cases for failure-state persistence/logger faults
  and terminal winners committed after rollback, two in-memory cancellation
  cases after prepared publication, and seven job-schedule admission/boundary
  cases. Exact exception/token, persistence, outgoing-message and schedule-field
  assertions distinguish controlled product counterchanges. These fourteen
  cases strengthen existing behavior; they introduce no production-code change.
- RabbitMQ fixture health checks run as the broker user. An early root diagnostic
  could create an unreadable root-owned Erlang cookie after the entrypoint's
  ownership setup, preventing broker startup. A forced diagnostic-before-start
  probe reproduces the ownership failure and verifies the corrected startup.
- Updated the background-work architecture guard for the observed ActiveMQ stop
  and monitor tasks, and corrected two asynchronous test/helper names together
  with their requirement projection.
- SQL Server provisioning validates existing principal kinds and SQL login identity
  before transferring schema ownership or granting transport permissions. A user
  occupying the role name, a role occupying the user name, or a same-name user
  mapped to another login is rejected. Regression tests reproduced all three
  unintended permission transfers. Real database tests also verify contained users
  without server logins, connection-string credentials, membership repair and
  quoted passwords. Existing user mappings are never silently rewritten.
- Dependency-graph validation now detects self-edges and rechecks all nodes on
  every validation, including after a prior failure or newly added edge. Four
  regressions distinguish real cycles from shared descendants and disconnected
  acyclic edges. Unused internal topological sorting and comparison methods were
  removed after a repository-wide caller check; live message-fabric validation
  remains intact.
- The SIMD Base32 formatter explicitly initializes its input padding. Existing
  independent reference strings detect poisoned padding at output position 24;
  no prior runtime failure from default stack initialization is claimed.
- Corrected the error-transport filter summary to describe sending and pipeline
  continuation; this filter does not itself generate a fault message.
- One-time setup now clears its running state before publishing success, failure
  or cancellation. Immediate eviction or retry can no longer observe a terminal
  result while the setup still rejects a new attempt. The caller retains the
  original attempt's task even when a completion continuation starts another
  attempt immediately. Six controlled reentrancy regressions preserve exception
  identity, cancellation tokens and exactly one healthy follow-up attempt.
- ActiveMQ now registers send-first temporary destinations in the same connection
  cache used by consumers. Previously a lazy reply endpoint could consume queue B
  after the request had advertised an uncached queue A, losing the response despite
  a successful native send. Queue/topic creation is serialized across sessions so
  competing factories cannot leave unowned native destinations. Six regressions
  cover both startup orders and concurrent creation; integration assertions bind
  the request ID and actual native reply destination across send and response.
- Temporary ActiveMQ registrations now separate equal queue and topic names.
  Lookup, deletion and failed-delete restoration retain the requested type, and
  response addressing explicitly selects a queue. The three low-level context
  lookup/deletion methods now require `DestinationType`; provider extension
  implementations must forward it. Four regressions exercise both creation orders
  and cleanup with and without a native deletion failure. Two native-send cases
  additionally verify the actual reply queue address beside a same-name topic;
  both reject a deliberately wrong topic lookup. The package verification script
  now restores its API inventory tool before building it in a clean checkout.
- The ActiveMQ Classic future-delivery test now waits for a separate scheduled
  probe before checking that the original scheduler job was removed. Classic
  6.2.0 dispatches before updating its scheduler index; client receive/stop was
  not a cleanup barrier. The probe enters a later scheduler iteration while the
  original exact delivery and queue-state assertions remain in place. Artemis
  retains its existing queue-executor observation.
- ActiveMQ connection exception listeners now run in an observed lifecycle task.
  Failed or partially successful registration retires the connection through its
  owning agent; removal failures after disposal are contained, including failures
  in diagnostic logging. A failed close keeps the listener available until a
  successful cleanup retry. Deterministic tests verify native operation order,
  resource ownership, failure identity, throwing loggers and null configuration.
  The previous unguarded registration callback was reproduced terminating the
  test process with an unhandled provider exception.
- ActiveMQ temporary-reply integration tests now distinguish handler entry,
  native reply-address inspection, response-send completion and client receipt.
  Handler failures are surfaced directly, with correlation and bounded queue
  diagnostics before teardown; cleanup cannot replace the primary failure.
  The product request deadline is unchanged and the outer watchdog has one
  shared budget. An injected post-send failure verified both protocol paths
  and was removed. The original intermittent request timeout remains unresolved.
- ActiveMQ connection creation now preserves the original cancellation or
  classified provider failure when disposing a failed connection also throws.
  Cleanup and warning-log failures are contained so diagnostic listeners cannot
  replace the retry-relevant cause. Twelve deterministic cases verify acquisition,
  startup, exactly-once cleanup and exception/token identity, including a throwing
  logger. The previous integration-only coverage of these failure paths varied
  between otherwise successful broker runs.
- Message-contract analysis now follows nested collection element contracts
  instead of comparing collection implementation properties such as `Count`.
  Existing nested implicit-array initializers are repaired at every leaf by the
  missing-property code fix, preserving values, element order and dimensions.
  Regressions cover missing, incompatible and complete leaf values, two and three
  collection levels, and scalar-to-collection conversion. Collection recognition
  is shared by analysis and the code fix; synthesizing a completely omitted
  nested collection remains outside this correction.
- EF outbox quarantine listing now preserves the corrupt attempt counter or empty
  message identifier retained by the producer for classified invariant failures.
  Previously those producer-created rows caused the entire listing to throw.
  Regressions exercise production failure handling, persistence and listing with
  a healthy neighboring row. Corruption cases still reject missing identifiers,
  zero counters and invalid classifications without changing stored evidence.
- The SQL Server parallel-publish test now captures missing message identifiers,
  duplicate counts, warning/error logs and a bounded queue snapshot on timeout
  before teardown. An injected timeout verified the diagnostics and was removed;
  the original intermittent timeout's cause remains unresolved.
- Copied-body stream tests now enforce exact declared lengths, complete short
  reads, stream disposal and isolation from source or returned-array mutation.
  Oversized declarations, including `long.MaxValue`, are rejected before the
  source stream opens. Removing the trailing-byte check failed the new
  truncation regression; all 40 admission contract tests pass after restoration.
  Transport-text selection is now a separate helper from copy admission, with
  the original size-check and strict-JSON validation order preserved.
- Durable payload replay now retains the offload evidence that was actually
  admitted when producing another persistence proof. Previously the current
  send context could replace the original proof's flag, either losing valid
  offload evidence or claiming an offload that never occurred. Both directions
  failed a two-generation replay regression before the correction; all 35
  bounded-serializer admission contract tests pass after it. The regression
  also checks the next replay's acceptance or rejection under a stricter
  message-data threshold, exact bytes, body length and envelope binding.
- EF outbox factory tests now reject absent, incomplete and byte-mismatched
  payload-admission evidence before persistence. A matching proof preserves
  the exact envelope and application headers. The proof check is a separate
  factory helper, keeping admission validation distinct from metadata assembly.
  Removing its envelope-match check failed the new mismatch case; restoring
  the check passes all 281 EF unit tests.
- SQL Server timestamp-projection tests now explicitly make their two inserted
  deliveries due before invoking the normal and partitioned fetch procedures.
  Exact timestamp values, UTC offsets and SQL result types remain asserted.
  Returning `datetime2` instead of `datetimeoffset` failed the projection test;
  the restored procedures pass all 69 SQL Server integration tests. This removes
  the test's immediate-readiness assumption, but does not establish the cause of
  the earlier intermittent empty fetch.
- The endpoint-name contention test now starts dedicated workers instead of
  exhausting thread-pool workers behind a synchronous start gate. It always
  releases and joins its workers after failed setup assertions. A counterprobe
  replacing the shared product lock with per-call locks failed with 16 formatter
  calls instead of one; the shared product lock was restored afterward.
- SQL Server schema provisioning now restores any missing transport-role
  database permission when rerun. Previously one remaining permission made the
  migrator skip all grants, leaving a separately provisioned transport account
  unable to create views after `CREATE VIEW` was revoked. A native integration
  test confirms the account is denied before reprovisioning and can create and
  read a view afterward. The check also recognizes permissions granted with
  grant option as already present.
- Azure Service Bus now registers error-queue and dead-letter move sender
  supervisors with the connection lifecycle. A faulted delivery
  previously reached both its error queue and correlated fault consumer, but
  bus shutdown hung because the error transport retained a connection lease.
  The existing local fault-flow test reproduced the timeout before the fix and
  passes afterward; all 30 Azure local integration tests and 336 unit tests
  pass with the corrected lifecycle.
- Durable copied-envelope replay tests now verify that an exact prior
  admission proof preserves the original body length without consulting a
  mutable locator. Changed body bytes, envelope suffix or content type
  invalidate the proof. A tighter current body limit still rejects a
  previously admitted envelope, while a current MessageData threshold uses
  the offload evidence bound to the proof rather than the new replay context.
  Replacing the proven offload flag with the replay context's flag made both
  positive and negative threshold tests fail; the restored code passes.
- Endpoint-scheduler provider tests now verify that an accepted command keeps
  the configured token consistent across the command, send context, scheduling
  header, correlation and returned handle. They reject a dispatch that skips
  its send pipe, a token change after command serialization, and replay of a
  pipe after acceptance. A deliberately corrupted scheduling header failed the
  accepted-command test; the restored implementation passes.
- Nine RabbitMQ test-double methods now carry the required `Async` suffix,
  matching their task-returning contracts and clearing the repository's
  bidirectional asynchronous naming gate.
- RabbitMQ channel leases now dispose their scoped cancellation links when a
  borrowed use ends. The active agent keeps the scope alive through an in-flight
  pipe operation even if the supervisor begins stopping concurrently. A stop
  budget can still cancel the wait and a later stop can finish after the use.
  A new regression test failed before the fix because a canceled owner still
  canceled a released lease. Channel-lease tests also verify that a borrower
  retains its owner, links caller and owner cancellation, rejects a closed broker channel
  with its exact close reason or an actionable fallback, cancels a pending
  acquisition without disposing the owner, and preserves channel-creation
  failures. A deliberately weakened closed-channel check failed both broker
  shutdown variants.
- RabbitMQ now uses one send-context configuration path for channel-bound and
  direct sends. Channel-bound reply tests verify the final broker frame,
  inherited priority and reply address, cancellation token, and rejection of
  a blank reply route before publish. Omitting the inherited AMQP properties
  failed both the new publish test and the existing direct-context test.
- RabbitMQ startup purge now serializes passive queue inspection and purge
  across concurrent channel starts for the same endpoint filter. A second
  channel cannot purge twice or enter the receive pipeline while an earlier
  purge remains in flight, even when its queue snapshot is empty. Failed
  purges remain retryable; canceled waiters leave the owner untouched. The
  pre-fix parallel test reproduced two purge calls for one startup.
- RabbitMQ receive-transport retries now respect the explicit `IsTransient`
  decision on connection failures. A nested stream error can no longer turn a
  permanent outer failure into a retry, and an exclusive queue conflict remains
  terminal even if an outer connection exception claims transience. Host-policy
  tests cover AMQP reply-code boundaries, nested 405 conflicts, authentication,
  configuration, stream drops, and selective pipelining retries. A deliberately
  changed AMQP 300 boundary failed its test.
- RabbitMQ consumer-delivery tests now exercise the broker callback through
  receive dispatch and acknowledgement. They check AMQP metadata and body,
  both acknowledgement modes, pre-canceled and late callbacks, two broker
  failure types, and continued consumption after an ordinary handler failure.
  Disabling the late-delivery guard makes its regression test fail.
- RabbitMQ cleanup tests verify that open channels and connections close before
  disposal, closed clients skip the handshake, and reply codes and cancellation
  tokens reach the client. Close and state-query failures still dispose the
  resource; channel disposal failures are suppressed while connection disposal
  failures retain their original exception. Removing connection disposal makes
  five of the eleven focused cases fail.
- RabbitMQ dead-letter moves now copy the incoming AMQP header table, retaining
  a dictionary's key comparer, before adding move and host headers. The
  received message remains unchanged, and case-insensitive keys cannot split
  into duplicate reason entries.
  Move-transport tests verify the copied body, properties, routing, mandatory
  publish, closed-channel causes, and topology retry after publish failure.
- RabbitMQ consumer-filter tests verify broker start parameters, readiness and
  completion order, startup cancellation, and reuse of a broker-assigned
  consumer tag after a channel restart.
- RabbitMQ exchange-address tests verify that configured delayed and alternate
  arguments reach the exchange declaration, while removed and incorrectly typed
  arguments cannot silently become endpoint routing options.
- RabbitMQ publish-topology tests now verify topic and fanout parent routing,
  exclusion without builder mutation, an alternate-exchange queue route, and
  direct implemented-contract hierarchy bindings using real broker-topology
  snapshots. The full RabbitMQ unit project passes 411 tests.
- Coverage receipts can limit MSBuild restore/build parallelism on constrained
  hosts and reject a restore that exits without the test project's assets file.
  The selected node limit is recorded in the receipt.
- RabbitMQ Durable Sender unit tests now reject ten unsafe destination forms
  before endpoint resolution, including explicit queue names, short addresses,
  and direct reply-to variants. A valid exchange address reaches resolution
  exactly once. Removing the alternate-exchange guard makes the intended test
  fail; the restored implementation passes all 406 RabbitMQ unit tests.
- All thirteen local integration test projects now reference the centrally
  versioned Microsoft CodeCoverage extension directly. Eleven previously
  lacked it, preventing their runs from producing the Cobertura reports
  required by the product-wide coverage receipt gate.
- Copied send-only JSON envelopes now have behavioral admission tests for
  exact whole-envelope body accounting, the one-byte-over-body-limit boundary,
  lossless text after source mutation, and withheld text for malformed JSON
  or invalid UTF-8. A deliberate invalid-text classification mutation failed.
- The native-test architecture inventory now includes the two retained
  coverage receipt tools, so the path-complete verification gate recognizes
  every tracked Python utility under `tools/ci` and `tools/identity`.
- The SQS publish-discovery fixtures now occupy their own source file and
  matching namespace folder, satisfying the native-test source layout gate.
- SQS lifecycle tests now name asynchronous test cases and provider stubs with
  the `Async` suffix required by the repository's bidirectional naming gate.
- Amazon SQS connection creation now has lifecycle tests for the configured
  provider connection, release on supervised handle stop, failure cause
  preservation, provider cancellation passthrough, and stopping before a new
  connection is opened.
- Amazon SQS send-transport creation now has contract tests for queue topology
  lookup based on the destination kind, ownership of the scoped client and
  transport agents, and cancellation before address validation or registration.
  Provider-boundary tests additionally verify that created queue transports
  invoke SQS sends and topic transports invoke SNS publishes with the expected
  destination and serialized body.
- Amazon SQS consumer connection now has a contract test for all three
  subscription gates: endpoint topology setting, connection option, and
  message-level topology setting. The default overload used by ordinary
  handlers and consumers must enable topology configuration. Every variant
  still connects the requested consumer pipe exactly once.
- Amazon SQS topology cleanup now has lifecycle tests for every combination
  of auto-delete topic and queue flags. They verify single registration after
  repeated configuration, exact cleanup selection, and propagation of the
  endpoint stop token to provider deletion calls.
- Amazon SQS move requests now have provider-boundary tests for the admitted
  body, custom string and binary attributes, FIFO identifiers only on FIFO
  destinations, removal of stale transport headers, and fresh move headers.
  A failed provider send must redeclare topology on retry; a successful retry
  must keep it cached.
- Amazon SQS FIFO receiving now has provider-facing tests for numeric sequence
  order, per-group ordering in an interleaved batch, and progress of a second
  group while the first group's dispatch is blocked. Missing or invalid FIFO
  ordering attributes must stop the receiver before dispatch with the
  corresponding diagnostic.
- Amazon SQS receiver startup now has provider-facing regression tests proving
  queue metadata is resolved before polling, valid visibility settings are
  adopted, malformed or missing settings leave the configured value intact,
  the first poll uses the exact request limit and wait time, and shutdown
  cancels the outstanding provider poll.
- Amazon SQS queue creation now has provider-facing regression tests for
  declared attributes and tags, the missing FIFO flag, unsuccessful creation,
  and an attribute-read failure after creation. The recovery test proves that
  a retry finds the existing queue without issuing a second create request.

# A+ remediation research

## T107 receive-side outbox cancellation ownership

The classic in-memory factory links delivery and explicit operation tokens
for lock acquisition, but its consume context checks only the token supplied
to each completion or send operation. The classic EF factory chooses the
explicit token when present, so a canceled delivery can still reach the
consumer and transaction commit. The reliable InMemory and EF context
`AddSendAsync` methods similarly choose the explicit send token and can
stage an outgoing intent after their delivery token has been canceled.
Existing T106 coverage establishes the consumed-fence boundary for reliable
providers, not these adjacent send and classic-outbox boundaries.

Target inventory: four receive-side outbox context implementations and the
two classic context factories. The Roslyn static pairing map found 4,290
source files, 1,578 test files, 1,723 unpaired sources; internal EF types
are not referenced by name from tests, which does not prove that runtime
coverage is absent. This package uses behavior and mutation risk to select
targets. Acceptance requires cancellation from either distinct source to
propagate with its original token, prevent consumed/delivered state and
outgoing intent from being committed, and leave a healthy subsequent attempt
possible. Tests must observe exact stored state, not only exceptions.

## T106 reliable inbox cancellation across in-memory and EF providers

The two reliable-inbox factories choose a cancellable operation token instead
of the consume-context token. If both are distinct and the delivery is
canceled during the consumer callback, the factory can classify that
`OperationCanceledException` as a business failure and schedule a retry or
quarantine. The in-memory context also chooses the explicit token for
`SetConsumedAsync`, so it can commit a canceled delivery. Existing cancellation
tests use the same token for both sources, leaving this combination unproved.

Acceptance: cancel either token after the inbox is acquired while the other
remains active. The original cancellation must propagate; no consumed fence
or outgoing intent may commit, and no retry or quarantine may be recorded.
The in-memory provider retains its original busy lease at attempt one, whereas
EF rolls back the uncommitted lease and accepts a fresh healthy delivery at
attempt one. Exercise both providers through their real factory paths and
inspect their state.

## T105 RabbitMQ queue reconfiguration and broker projection

RabbitMQ queue configuration is mutable until the receive endpoint topology
is built. `SetQuorumQueue(3)` writes `x-quorum-initial-group-size`; a later
`SetQuorumQueue()` selects quorum again but leaves the earlier group size,
although the later configuration omitted it. This can start a broker queue
with a stale replication request. `SingleActiveConsumer` and `Lazy` similarly
change broker arguments and need to be observed in the final topology rather
than only in the intermediate settings object. Existing boundary tests cover
invalid quorum factors, first-time quorum setup, expiration, and duration
projection, but not replacing an earlier quorum group size or toggling these
delivery settings before topology construction. The static Roslyn pairing
finds this source paired to existing RabbitMQ tests; pairing is not evidence
of branch or behavioral coverage.

Acceptance: later quorum configuration without an initial group size clears
the stale parameter while preserving quorum type, non-exclusive ownership
and removal of classic priority settings. Repeated delivery-setting toggles
must project the final single-active-consumer and queue-mode values into the
broker topology. Host startup must report each invalid batch limit together
and then accept corrected limits at their inclusive boundaries. A built host
must reject replacement of its address settings without changing either the
host or receive endpoint address. These are declarative startup contracts, so
no broker fixture is needed.

The first Red Team review found that invalid `SetQuorumQueue(0/-1)` after a
valid group size also needs a state-preservation oracle. A new regression
checks both settings and final broker topology after rejected calls. Its
durability concern was ruled out: `BrokerTopologyBuilder.QueueDeclare` already
forces a quorum queue's `Durable` flag true even if the source settings are
false; the exchange remains governed by source settings. The verified broker
defect is that an auto-delete request without `x-expires` still produced an
auto-delete quorum queue, and a direct queue declaration could preserve an
exclusive request. Quorum queues require durable, non-auto-delete,
non-exclusive broker flags. Tests inspect both endpoint and direct queue-declaration
paths with and without expiration.

## T104 in-memory outbox release admission race

`InMemoryOutboxConsumeContext.ExecutePendingActionsAsync` marks `ClearToSend`
complete immediately before draining deferred sends. The collection's
`AddAsync` checks that task outside `_pendingMethods` lock. A concurrent Add
can read incomplete, block at the lock, and enqueue after the drain has
removed its final batch, leaving an acknowledged send permanently pending.
The scheduler cancellation path has the same split: it reads `ClearToSend`
outside `_listLock`, then queues into a deferred collection with no release
task. A cancellation admitted at that boundary can also be stranded.

Acceptance: when either producer has passed its old release check and blocks
on its admission lock, then release and drain happen, the admitted operation
must execute exactly once and leave no pending entry. Both tests will hold
the existing synchronization object to force that sequence, observe that
the producer thread is waiting for the monitor, release the outbox and drain,
then assert exact send/cancellation effects and empty queue. A canceled
producer must not be admitted. No timing-based hope of hitting the race.

Red Team found two adjacent admission hazards in the first repair. An
immediate scheduler cancellation could invoke provider code while `_listLock`
was still held, and a token canceled while its producer waited for the
admission lock could still be accepted. The corrected contract checks the
token and release state at one collection lock, then invokes any immediate
callback after leaving both internal locks. The regression observes the
callback's lock state and cancels two producers while each is blocked on its
admission lock, requiring the exact token and zero side effects.

## T103 bus composition ownership across buses

The T97 core report has 63 uncovered physical lines in
`Configuration/BusFactoryConfigurator.cs`, but the next test decision is based
on startup ownership risk rather than that count. Roslyn static pairing labels
`BusCompositionValidation.cs` unpaired because the existing
`BusCompositionStartupValidationTests` use the public host configuration path
and find the internal validator by `IHostedService`; these tests do exercise
missing transport/limits, orphan and duplicate features, and missing durable
owners. The remaining contract gap is ambiguous *multiple* transport and
limits registrations for one bus alongside a healthy neighbor, plus multiple
owners for every durable component. A host must reject all contradictions
before constructing the invalid bus or store and must not misattribute the
fault to a different bus. Red Team found that counting only conflicting store
factories did not prove the bus itself was left unconstructed. Separate
throwing bus descriptors now make that preflight boundary observable.

Acceptance: real DI registrations trigger a combined diagnostic for duplicate
transport and limits only on their owning bus; a second configured bus starts
validation successfully. Reliable messaging with a valid contract catalog but
duplicate catalog, outbox, inbox, schedule and dispatcher descriptors reports
all five ownership faults without materializing those descriptors. Assertions
must identify the bus and each cause, not merely expect an exception.

## T102 classic EF outbox cursor and cleanup integrity

The delivery source pages messages with `SequenceNumber > LastSequenceNumber`.
When a persisted cursor is beyond a still-present message, the selected page
is empty and `TryCompleteOutbox` marks the state Delivered; a later source pass
deletes all messages without sending. A state already marked Delivered is
removed with every remaining message by `RemoveOutboxAsync`, even if those
messages were never delivered. These are real data-loss paths for persisted
inconsistent state, including legacy rows or caller-owned EF mutations. The
normal empty final window is valid only when no messages remain for that
OutboxId. Existing tests cover that valid case but not the contradictory rows.

Acceptance: both corrupted cursor and premature Delivered state must fail
closed with zero progress and retain the exact state/message rows in SQLite.
Healthy paged delivery and cleanup must still pass. Avoid an operational retry
storm for a permanent invariant breach; the worker can retry after repair.
Read-only Red Team review found that an end-of-page check is too late when a
stored cursor lies between two remaining messages: the later message is sent
outside the transaction before the earlier one is detected, and rollback can
cause repeated external sends. A real bus send observer now proves zero send
attempts across two delivery passes while both message identities remain.
The cursor must be checked before loading the first page, with the completion
check retained as a late race guard.

## T101 classic EF transactional outbox state provenance

`EntityFrameworkTransactionalScopedBusContext` infers persistence solely from
`_outboxState.State == Unchanged`. A caller can call `AcceptAllChanges`, set
the state to `Unchanged`, or detach all staged messages before SaveChanges.
`EnsureOutboxState` then signals delivery and starts a new batch without a
durable row; the existing `TrackerTransition` test even expects the false
signal. Abort searches messages by mutable OutboxId rather than exact staged
entity ownership. The neighboring T100 durable-send path already fixed the
same class of tracker-state confusion with exact staged entries, pre-save
validation and save-event evidence. The classic path has distinct state and
message records, so it needs its own behavioral regressions.

Acceptance: no false signal or successful commit after unsaved tracker state
changes; no partial state/message write; abort detaches only exact session
entities and preserves foreign business or outbox entries. A successful
external SaveChanges, including `SaveChanges(false)`, must complete only the
owned batch once and allow a subsequent batch. Use SQLite and fresh contexts.
Read-only Red Team review found that a caller could also set `Status=Delivered`
or `LastSequenceNumber=long.MaxValue` before save, causing the delivery worker
to discard or skip unsent messages. It also found that factory rejection after
state attach left an empty active session and blocked a healthy retry. The
accepted fix validates every initial delivery field before save and creates
the message before attaching a new state, with compensation if attach fails.
The second review found a narrower EF tracking failure: `ChangeTracker.Tracked`
can throw after EF attaches an entity but before `DbContext.Add` returns its
entry. A test that only rejects an invalid message ID does not exercise this
path. A two-case real-EF test now throws once for State or Message tracking,
requires both Local sets to be empty, and then commits a healthy retry.
Cleanup must use the newly constructed entity reference even when no entry was
returned to the caller.

## T100 EF transactional outbox tracker loss

Manual review of `EntityFrameworkScopedBusContext` found that `WasCommitted()`
uses `All` over tracked staged records. If `DbContext.ChangeTracker.Clear()`
or detachment removes every staged record before `SaveChanges`, the empty
enumeration returns true. `CommitAsync` then clears session ownership and
returns successfully without storing the message; `AbortAsync` likewise calls
the branch reserved for an already persisted session. Existing EF outbox tests
prove normal commit, external save, abort and disposal, but do not detach a
staged durable record before commit. The same class accounts for a partially
detached record through a tracked capacity row; detachment may leave that row
inflated if abort only counts still-tracked records.

Acceptance: a lost staged record cannot be reported as committed; the caller
must receive a clear failure, abort must release ownership, and a surviving
capacity row must return to its previous count and byte values. Preserve
unrelated business entities and previously committed records. Use real SQLite
and a fresh DbContext to distinguish tracked state from persisted state.

The same admission method reserves capacity before attaching the durable
record. Reusing a message ID within one session can therefore fail during EF
tracking after incrementing capacity. Acceptance also requires rejecting that
duplicate before capacity changes while retaining the first valid intent.

Adversarial review extended the packet to EF `AcceptAllChanges`, foreign-store
IDs, suppressed saves, externally saved `SaveChanges(false)` sessions, missing
capacity writes, and mutated staged size. The final design validates exact
tracked records and the capacity reservation before EF writes, then completes
the session from a successful `SavedChanges` event. For `SaveChanges(false)` it
accepts only the outbox-owned entries, leaving caller business entries in
their original state. Database readback after save was rejected because an
independent outbox worker can legitimately change records and counters before
the read. Caller-owned outer transactions retain their normal commit/rollback
ownership. Custom SaveChanges interceptors must report actual persisted writes.

## T98 persistent outbox cancellation handoff

The exact T97 33-profile aggregate leaves 33/109 physical lines in the
persistent `OutboxSendEndpoint` unobserved. The coverage count alone does not
justify tests. Manual source review found a real cancellation handoff gap:
all six capture paths pass the caller token into transport context creation,
but their common `AddSendAsync` helper calls `OutboxSendContext.AddSendAsync`
without that token. The in-memory outbox implementation checks the token and
the EF scoped bus uses it in its write coordinator. A cancellation after
context creation can therefore still admit a persistent send. Existing
`OutboxSendEndpointBoundaryTests` cover null inputs and volatile cancellation,
but no persistent cancellation or exact token forwarding. The T98 Roslyn
pairing artifact at `artifacts/t98-static-pairing.json` is static routing
evidence, not behavioral or coverage evidence.

Acceptance: all six capture callsites plus runtime-typed dispatch must pass
the exact caller token to persistent admission; cancellation after context
creation must reject admission without sending or recording. A user pipe must
still configure the context. Verify exact message/context identity, one
admission attempt, and no accepted record on cancellation. A pending
admission must keep the send pending, and a delayed storage failure must
surface unchanged. The seven-shape matrix distinguishes all six capture
callsites and the explicit runtime-type dispatch path.

## T79 ActiveMQ cached producer admission and send ownership

The frozen T74 profile leaves 63/79 lines in `CachedMessageProducer`
uncovered, but most are direct interface forwarding. Existing cache tests
already prove single-flight creation, independent destinations, canceled
waiter isolation, retry after a faulted factory, and a basic cached producer
usage signal. The product constructor currently accepts a null destination
or producer. A factory that completes with a null producer therefore creates
and caches a wrapper that fails later on first use, poisoning its destination
key. This is a concrete product failure, not an uncovered overload count.

Acceptance: missing constructor dependencies fail with exact parameter names;
a null-producing factory cannot install a cached resource and a healthy
subsequent factory can recover the same key and release only its own producer;
explicit destination/delivery settings are forwarded unchanged by both sync
and async sends, with one usage signal per attempted operation, including a
failed async send. The frozen profile is only a locator, not a current
coverage measurement. The T75 Roslyn pairing artifact is reused.

## T78 Azure Table saga native-property restoration

The frozen T74 profile leaves 55/196 lines in
`AzureTablePropertyTypeConverter` uncovered. Existing entity-converter tests
prove one fully populated round trip, reserved-name isolation, malformed
serialized values and storage limits. They do not prove the Azure Tables SDK
projection from a non-UTC `DateTimeOffset` into CLR `DateTime`, reject wrong
native types in both required and nullable fields that could silently coerce
a persisted identity or count, or
distinguish absent optional properties from explicitly persisted false/zero
and empty values. This is a connected saga persistence boundary. The T75
Roslyn pairing artifact and frozen T74 profile were reused for selection;
neither is a current coverage measurement.

Acceptance: materialization of SDK table values normalizes `DateTime` to the
exact UTC instant while keeping `DateTimeOffset` instants and offsets;
corrupted native fields fail with the named property and target type;
omitted nullable properties remain absent while explicit false, zero and
empty values survive the table projection and restoration.

## T77 reliable scheduler timing and cancellation ownership

The frozen T74 profile leaves 28/71 physical lines in
`ReliableMessageScheduler` uncovered. Existing Core integration proves an
absolute due time, metadata persistence, one successful cancellation and
multi-bus registration. It does not prove relative delay from the injected
clock, relative publish route ownership, rejection without admission after
negative delay or pre-cancellation, or cancellation behavior after a store
lease has already been claimed. These behaviors are meaningful for durable
delivery even if method coverage did not change. The relevant recurring
scheduler tests were inspected and already strongly cover control commands;
no duplicate recurring tests are planned. The T75 Roslyn pairing artifact
is reused for source selection; the T74 coverage snapshot is stale for the
current tree.

Acceptance: with a fake clock, relative send and publish handles and stored
intents agree on exact due times, destinations and payloads; rejected inputs
leave the store empty and a later valid schedule succeeds. Once a due intent
has been leased, scheduler cancellation must fail with the precise invalid
state and leave the leased intent and an independent neighbor intact.

## T76 saga request lifecycle and completion publication

The frozen T74 profile observes 74/89 uncovered lines in the request
extension overloads, 56/60 in send callback overloads, 19/35 in request
completion publication and 15/46 in normal request activity. The large
extension counts mostly represent forwarding overloads and alone are no
reason to add tests. The static Roslyn pairing artifact from T75 is reused.
The existing faulted-request suite exercises address precedence, async send
ordering, persisted request identity, timeout scheduling and failure, while
normal `RequestActivity` has no direct activity test. The two
`RequestCompletedActivity` variants have no direct runtime test.

Source review found a concrete configuration defect: the normal request
activity constructors accept null request, message factory and explicit
service-address provider. The faulted variant rejects those values at
declaration. A null normal dependency can thus enter the state-machine graph
and fail only when a message arrives. Acceptance requires fail-fast named
argument errors at declaration, real send order/identity and failure
isolation on the normal path, and completion publication with exact payload,
metadata, async ordering and failure non-continuation. Request and send
extension overloads will be selected only when they expose distinct runtime
semantics, not to touch their uncovered lines.

## T75 saga declaration atomicity and response routing

The T74 exact-commit profile observed 109 uncovered physical lines in
`ViciOneServiceBusStateMachine`, including the complete two-response request
declaration using saga-ID correlation and multiple composite declaration
validation branches. The required Microsoft Roslyn static pairing run at
`artifacts/t75-source-test-pairing.json` classifies the machine as paired;
request and callback extension classes are unpaired by their declaring class
names, a known limitation for extension-method calls. This pairing is not
coverage evidence. The Core integration suite already proves a three-response
request with explicit request-ID storage, and several composite runtime
behaviors. It does not currently prove atomicity after an invalid composite
declaration or the two-response saga-ID routing path.

Source review found that property and named composite overloads call
`CreateEvent()` before the shared constituent validation. Null, empty,
oversized or uninitialized constituent arrays therefore mutate the machine
even though declaration throws. Implicit registration means a property event
already exists: the failed call replaces its identity. A named declaration
instead leaves a new ghost event in `Events` and `IStateMachine.GetEvent`.
The first bounded acceptance slice requires invalid property declarations to
retain their original event identity and named declarations to leave no new
event, then a valid declaration with the same identity to work and dispatch
exactly once. The next slice will inspect the two-response
request path for real response/fault correlation and cleanup oracles; it will
not add tests solely for the overload count.

## T73 asynchronous task outcome ownership

The frozen T63 union leaves 36/126 physical lines in `TaskExtensions`
uncovered. They cluster in pending `OrCanceledAsync<T>` source outcomes,
generic timeout and numerical timeout overloads, and success/failure transfer
in both `TrySetFromTask` overloads. `Agent` uses the non-generic transfer for
readiness/completion; `OneTimeSetupMethod` uses the typed transfer for its
terminal value. Existing tests cover cancellation identity, active timeout
with a fake clock, stopped-agent retry, and one-time reentrancy. The new
bounded acceptance cases are: pending source completion/fault must preserve
its exact outcome while a cancelable caller is waiting; a generic task must
time out exactly at the virtual boundary with caller location; successful and
faulted terminal task transfer must preserve the exact value or exception and never overwrite an already
completed target. These outcomes matter to real Agent and one-time setup
callers, independently of any numeric coverage gain.

## T72 callback-configured mediator dispatch

The frozen T63 aggregate has related uncovered lines in four Abstractions
entry points: consume-scope send (48/74), response (30/34), direct send
(26/38), and publish (22/38). The static pairing inventory was already run
for this campaign. Existing Abstractions tests prove consume-scope overload
validation and forwarding, while Core mediator tests cover ordinary dispatch,
headers, and requests. The missing behavioral question is whether synchronous
and asynchronous callback pipes actually configure messages before dispatch
and whether an asynchronous callback delays completion and propagates failure.
The bounded T72 inventory is those four entry points and the in-process
mediator tests. Acceptance requires transport-visible metadata, exact payload
and explicit contract forwarding (a declared interface distinct from its concrete type), callback ordering, and a negative callback outcome; a
counterpart without a callback must not provide the configured metadata.
The T63 report remains a targeting snapshot, not a current coverage claim.

## T71 request-rate partial-failure ownership across Abstractions and SQS

The frozen T63 profile leaves 31/321 lines in `RequestRateAlgorithm` uncovered,
including the `count > 0` exception paths in the ungrouped result enumerator
and grouped ordered result dispatcher. Both catches return a partial count
after a later failure. Amazon SQS FIFO uses the grouped overload in its
receive loop, but its executor separately owns failures after queue admission;
the algorithm owns only admission failures. SQL and SQS standard receive paths use
the ungrouped overload. Existing algorithm tests establish adaptive limits,
rate permits and cancellation, while SQS FIFO tests establish ordering and
partition admission. Acceptance is behavioral: after earlier successful work,
the exact later provider enumeration or group callback error must propagate;
completed work must not be replayed or reclassified, and request/result permits
must remain usable. A proposed SQS post-admission dispatch-failure test was
rejected because it crossed the executor's ownership boundary. The source/test pairing and frozen coverage inventory
are reused. No global profile is due until the grouped milestone.
Red Team additionally identified cancellation after result-permit acquisition:
`Task.Run` with the caller token can produce a canceled task without executing
the callback's sole permit-release `finally`. Scheduling must be unconditional
once the permit is owned; the callback still observes the caller token.
The exact post-acquire/pre-schedule race has no deterministic public hook, so
its closure is established by source review and a broader admitted-cancellation
recovery test, not a claim that the race itself was reproduced in a test.

## T70 endpoint-definition prefetch width across Core and RabbitMQ

Target inventory: `BaseHostConfiguration.ApplyEndpointDefinition` projects the
provider-neutral `IEndpointDefinition.PrefetchCount`/`ConcurrentMessageLimit`
(`int?`) into an `IReceiveEndpointConfigurator.PrefetchCount` (`int`). It
currently casts both explicit and derived prefetch counts to `ushort` before
transport validation. Thus 65,536 becomes zero, and an inferred count from
60,000 concurrent messages becomes 6,464 rather than 72,000. The shared
implementation affects InMemory and broker transports. RabbitMQ's AMQP QoS
count is genuinely `ushort`, but its receive-settings getter also casts an
`int` unchecked; the Rabbit-specific endpoint must report out-of-range
configuration and prevent a bypassed context build from using a wrapped
value. The existing Roslyn pairing and T63 aggregate are reused; the frozen
Core profile has 58/69 lines in `BaseHostConfiguration`. Acceptance: preserve
full-width values through the neutral layer, avoid arithmetic overflow when
deriving 120% of concurrency, accept RabbitMQ's exact 65,535 boundary, reject
65,536 as a named validation failure before constructing a runtime channel,
and retain InMemory's valid 65,536/72,000 values without wrapping.

## T69 RabbitMQ native stream retention and offset configuration

Target inventory: `RabbitMqReceiveEndpointConfiguration.Stream(...)` and
`RabbitMqStreamConfigurator` project stream queue and consume arguments from
the public RabbitMQ receive-endpoint callback. The latest frozen full T63
profile has 15/29 lines in the stream configurator and 87/126 in the endpoint
configuration; later stream tests exist but are not included in those figures.
The existing Roslyn pairing report and `RabbitMqStreamConfigurationTests` are
reused. RabbitMQ's stream guide defines `x-max-age` units (D/h/m/s), maximum
length and segment-size arguments, and offset forms (first, last, nonnegative
numeric position, AMQP timestamp). Current `MaxAge` uses `F0` formatting on
floating-point totals, so 36 hours can become `2D` and 90 seconds can become
`2m`. A negative duration silently removes an existing age limit. Acceptance:
represent whole-second retention exactly using the largest exact broker unit;
reject negative and unrepresentable positive fractional seconds before
changing a valid limit; preserve the documented positive subsecond removal
behavior. RabbitMQ's server offset parser expects a nonnegative numeric offset
or timestamp; the generic timestamp helper otherwise emits an ISO string for
pre-epoch instants, which the stream parser interprets as an interval and
rejects. Stream configuration therefore rejects negative offsets and pre-epoch
instants before replacing a prior consumer position. Public endpoint tests
inspect exact queue/consume arguments and state after rejection, not merely
setter execution.
RabbitMQ 4.2's server limit for stream segment size is exactly 3,000,000,000
bytes, not 3 GiB; `x-max-length-bytes` is documented as nonnegative. Both
limits were previously accepted without local validation. The server's segment
checker enforces the upper limit only, so no lower-limit rule is inferred.
The public implementation's `FromLast` comment also described the behavior of
`next` although it writes `last`; it now matches the latest-chunk semantics.

## T68 RabbitMQ no-ack publish cancellation lifetime

The last complete T63 coverage has 63/95 lines in `RabbitMqChannelContext`,
44/52 in `ScopeChannelContext`, and 25/48 in `SharedChannelContext`; the
`awaitAck=false` owner path is not exercised. `RabbitMqChannelContext` starts
the SDK publish under a `TransportLifetime` lease but returns completed before
the SDK task may settle. Both wrappers await that completed public task and
dispose their per-call linked cancellation source immediately. A later caller,
shared owner or scope owner cancellation can no longer reach the token already
given to the SDK. `ScopeChannelContext.Dispose` can also dispose its parent
link while the hidden publish is pending. Read-only Red Team triage confirmed
the path and the missing tests. Acceptance: no-ack return remains prompt,
actual in-flight publish retains caller/owner cancellation until completion,
scope disposal defers token-link cleanup, and awaited publish retains its
existing error propagation. Controlled pending operations will serve as the
observable oracle; the source/test pairing inventory is reused.

## T67 RabbitMQ duration argument projection

Inventory: `RabbitMqQueueConfigurator.SetQueueArgument(TimeSpan)` and
`RabbitMqExchangeConfigurator.SetExchangeArgument(TimeSpan)` cast through
`TotalMilliseconds` to `int`; positive values beyond 24.85 days wrap/truncate,
and fractional milliseconds silently shorten. The public endpoint and bus
configurators forward into these two implementations. `QueueExpiration` writes
`x-expires` as `long` but can write zero for a positive sub-ms value. Its getter
only recognizes `long`, while the generic duration overload writes `int` for
ordinary `x-expires` values. `RabbitMqReceiveEndpointBuilder` copies the
setting into the actual queue topology using floating-point conversion again.
RabbitMQ documents a nonnegative integer millisecond TTL, permits AMQP
long-long-int, and requires positive integer milliseconds for `x-expires`:
https://www.rabbitmq.com/docs/ttl . Existing endpoint configuration tests
inspect settings before provider start; topology tests inspect `BrokerTopology`
without broker I/O. Acceptance: exact supported integer types and values,
fractional and negative value rejection without mutation, expiration
getter/argument/topology consistency including broker argument types, and
removal behavior. Existing source/test pairing inventory reused.

## T66 RabbitMQ queue configuration boundaries

The public receive-endpoint API forwards quorum selection and acknowledgement
timeouts into `RabbitMqReceiveSettings` before provider startup. A rejected
quorum replication factor currently changes queue type, exclusivity and priority
first. A positive timeout with fractional milliseconds currently truncates to
zero or an earlier deadline. Red Team also found that double conversion of
a large whole-millisecond timeout loses one millisecond. Existing stream tests show how to inspect the
projected broker arguments without starting RabbitMQ. Acceptance: failed
configuration preserves prior settings, and valid follow-up quorum selection
projects all compatible queue arguments. The existing source/test pairing
inventory is reused.

## T65 scheduling token admission across providers

`ScheduleTokenIdCache<T>.GetTokenId` accepts `Guid.Empty` from a configured
selector. Its three production callers are the base command scheduler, the
transport-delay provider and the SQL scheduling provider. Quartz rejects an
empty one-time command token, and SQL cancellation rejects an empty token.
Thus a producer can report scheduling accepted for an unusable identity.
Existing scheduler/SQL tests prove generated and valid selected tokens, but
none rejects an empty selected token before endpoint resolution. The existing
Roslyn source/test pairing inventory is reused. Acceptance: empty selection
fails before remote or SQL dispatch, while the same configured selector accepts
a subsequent non-empty token with matching handle, wire/context identity and
header. Use distinct test contract types to avoid process-wide selector state
coupling.


## Current T63 — Event Hubs partial-batch confirmation and retry

The T62 Red Team found a separate provider outcome defect in the connected
Event Hubs batch path. `EventHubProducerBatchSender` can finish one route/size
batch and later fail another. `BatchSendPipe` currently receives only one
aggregate exception, sends `SendFault` for every input context, and the host
retry policy reruns the same pipe from the beginning. The earlier confirmed
batch is therefore mislabeled and can be submitted again. The existing batch
sender tests verify route/size splitting and failure disposal, but only an
initial provider failure. The T62 producer outcome tests verify all-or-none
controlled provider results. Neither proves partial success across retries.

The affected source owner is one Event Hubs integration package: batch sender,
message send context, and producer batch pipe. Acceptance requires explicit
progress from a confirmed provider sub-batch, PostSend only for that confirmed
subset, SendFault only for unresolved messages, and retry of only unresolved
messages. A provider send that itself throws remains uncertain and is not
claimed confirmed. Tests must force both route and size splits, inspect exact
provider call order and context identities, challenge disposal, and use a
controlled retrying transport context to prove no replay. The existing real
broker delivery suite is the adjacent regression control. T63 completes the
four-packet interval T60–T63 before the next full 33-profile measurement.

## Current T62 — send observer outcome ownership

The next connected packet follows one message from provider submission through
the common `SendTransport` and the Event Hubs single and batch producer paths.
The `ISendObserver` contract calls `PostSend` after confirmed submission and
`SendFault` for a failed send. Before T62, all three paths included `PostSend` inside
the catch that classifies a send failure. Event Hubs also awaits `SendFault`
without shielding the original send exception. A post observer failure can
therefore report a false send fault after delivery, and a fault observer can
replace the real provider failure. Existing Core tests prove the latter only
for the common transport. Event Hubs admission tests prove presend rejection,
not these outcome boundaries. Fresh Microsoft Roslyn pairing identifies
three static test references for the Core source and none for the Event Hubs
producer; this is a search heuristic, not runtime coverage evidence.

Read-only Red Team additionally identified throwing send loggers as a way to
misclassify confirmed sends or replace provider failures. Event Hubs batches
can also partially succeed before a later provider batch fails; the current
single catch then sends `SendFault` for every context. These are part of this
packet's outcome ownership review. The Core post-observer test failed on old
product bytes with the exact observer exception after provider submission.

The corrected source passed focused 6/6 Core and 8/8 Event Hubs tests; full
Core passed 6,897/6,897. A separate Event Hubs broker startup race was found
while validating the existing suite. The runner now waits for the emulator's
entity-ready log marker. The ready full run passed 96/97; its sole existing
checkpoint observation timeout passed 1/1 in a fresh fixture. Both fixture
reports have empty findings. The partial provider-batch outcome remains the
next behavior packet. No global A+ measurement is inferred from these runs.

Acceptance: after confirmed provider submission, a failing post observer
cannot turn success into a retryable failure or emit `SendFault`; a true send
failure remains the thrown exception if its fault observer also fails. Check
single and batch paths and assert provider submission, message identity and
observer events. Use narrow red-first tests, the complete affected suites,
and an adversarial read-only review. The red-first failures are the causal
counterprobes for the product correction. T59 remains the
last complete global coverage/CRAP baseline under the agreed cadence.

## Current T61 — ActiveMQ cached producer and native send ownership

The connected owner is the ActiveMQ producer cache through the native session
send path and broker delivery in OpenWire, classic AMQP and Artemis AMQP. The
bounded source inventory is `MessageProducerCache`, `CachedMessageProducer`,
`ActiveMqSessionContext.SendAsync`, `ActiveMqSendTransportContext` and
`ActiveMqReceiveContext`. Existing controls already check same-key single
flight, creation failure retry, selected wrapper usage, transport timing,
broker restart and native group/header round trips. The T55 gap inventory
still lists wrapper and send-path gaps; most wrapper overloads are simple
delegation and are not by themselves reasons to add tests. The source tree has
not changed since the T58 Roslyn source/test pairing, so that static pairing
is reused as a search aid, not test evidence.

Acceptance checklist: cancel one waiting caller without losing another's
shared producer creation; create two destinations concurrently and release
both independently; prove that a reused send endpoint and another destination
deliver their own payloads and native priority/durability over each broker
flavor. Tests must assert exact creation/disposal counts, routing and native
delivery fields, with no skip. A deliberate product counterchange and read-only
Red Team review must challenge the oracles. The review found a real first-sender
cancellation defect; a session-level red-first test reproduced it. The fix uses
the cache-owned creation token through an internal path, keeping the public
API stable. Full Unit and LocalIntegration projects pass 229/229 and 106/106;
the fresh Classic/Artemis fixture has no findings. The [T61 acceptance record](coverage-a-plus-20260921/t61-activemq-producer-ownership.md)
captures exact oracles, the failed first broker setup and the counterprobe.
T59 remains the global coverage/CRAP baseline until several connected packets
are ready for the next 33-profile run.

## Current T60 — persistent JobService terminal and slot behavior

The bounded target spans Azure Table and EF Core/PostgreSQL local JobService
integration suites, their requirement projections, and their harness fixtures.
Both providers already had one-job completed, faulted, canceled and clock
controls. The new acceptance checklist covers genuine two-job overlap, terminal
ownership, exact consumer attempts, persisted Saga state, and release of a
single slot after completion, fault or cancellation. T59's short Azure Table
observation failure motivated provider-sized harness waits. The unchanged
source tree permits reuse of the prior Microsoft Roslyn pairing report, a
static reference heuristic rather than execution evidence. The full
[T60 map](coverage-a-plus-20260921/t60-persistent-job-terminal-slots.md)
records selected contracts and limits.

## Current T59 — larger saga request lifecycle packet

T58 is complete and pushed. The [T59 contract map](coverage-a-plus-20260921/t59-saga-request-lifecycle.md)
groups property-stored request IDs, multiple response types, fault and real Quartz
timeout ownership, custom correlation, missing/stale IDs and request generations.
Existing Saga-ID, one-response and callback integration tests were screened by
read-only Red Team to avoid duplicate coverage. The prior full-repo Microsoft
Roslyn pairing and T58 measured gap list are reused for selection. Thirty new
cases pass across two Quartz files; the complete affected project passes
318/318 without skips. Two-response and three-response owner, fault, timeout
and stale-generation families are complete. Read-only Red Team final review
and two causal counterprobes are complete. The frozen 33-profile measurement
and independent audit pass: 13,287 tests, 91.73582% lines, 84.36423%
conservative branches and zero CRAP>30. One initial Azure Table local failure
passed 40/40 in a fresh fixture; the failed run remains diagnostic evidence.
Global Line/Branch A+ is open.

## Current T58 — registration, scope and failure journeys

T57 is complete and remote-verified at `25ef0c773`. The next larger packet
follows consumer registration through DI filter scopes, retry/rescue
and two-bus journal isolation. Its eight-source candidate inventory, existing deep-test
controls, one Microsoft Roslyn pairing and behavior acceptance map are recorded
in [T58 research and plan](coverage-a-plus-20260921/t58-registration-scope-failure-journeys.md).
The read-only selection review corrected three assumptions in the map. Four
behavior families are now implemented without a product-source change. The
owner-filter inversion is killed by both journal variants and restored at
the original SHA. The complete Core suite passes 6,894/6,894. The frozen
33-profile measurement and independent audit pass: 13,257 tests,
91.68462% lines, 84.29095% conservative branches and zero CRAP>30. See the
[T58 complete report](coverage-a-plus-20260921/product-wide-profile-5e9509367.md).

## T56 — durable admission across reliable inbox and scoped outbox

Baseline is pushed T55 `45e250bf0`, with33 valid profiles and5,819 remaining
method-gap identities. This larger packet studies one connected contract: if
durable payload admission rejects a later send, previously buffered effects must
follow their owning transaction/session semantics. In-memory reliable inbox
already proves rejection after one staged send, quarantine, no durable effect and
operator recovery (`ConsumerAdmission_RejectionDiscardsBufferedSendsAndOperatorRetryCommitsAsync`).
Use that as a control; do not duplicate it. Existing EF reliable-inbox pipeline
covers generic consumer exceptions, rollback, retry, terminal winner and
cancellation, but no later payload-admission rejection after valid staged intent.
Direct EF scoped-outbox tests cover commit/abort/dispose and capacity accounting,
but no failure of a later AddSend followed by explicit owner action.

One bounded Microsoft Roslyn pairing run uses26 byte-identical inputs:
`artifacts/t56-pairing-inputs.json`, `t56-pairing.json`, `t56-pairing.log`.
It reports18 source files, four tests, three paired and15 unpaired. The heuristic
misses internal types reached through public integration tests; it does not
replace runtime coverage or imply no test.

`EntityFrameworkReliableInboxContext.AddSendAsync` delegates to scoped outbox.
`EntityFrameworkReliableInboxContextFactory.SendWithLeaseAsync` rolls back,
aborts staged outbox and persists retry/quarantine after non-cancellation errors.
`EntityFrameworkScopedBusContext.AddSendAsync` rejects payloads before capacity
reservation. A direct caller catching a later rejection owns the Abort/Commit
decision; there is no implicit batch-rollback promise for that API. The existing
in-memory implementation commits only after all sends are accepted.

Selection review confirmed the three paths. SQLite and PostgreSQL now save the
first business record and outgoing intent inside the open transaction before
the second send fails admission. This detail was forced by an adversarial
counterchange: committing instead of rolling back survived when the first
intent existed only in the EF tracker. With the saved partial attempt, the same
fault fails on a persisted leaked business row. The final tests also inspect
the exact replacement payload and outer retry signal, after read-only review
found those initial oracle gaps. PostgreSQL uses the public EF provider
registration and the scoped factory against a real database. The delivery
host is intentionally not started: concurrent polling caused a PostgreSQL
serializable write conflict during the deliberate partial SaveChanges. This
test proves persistence, not broker dispatch.

## Current T55 — consumer-outbox retention and recovery

T54 is complete and remotely verified at `5acd82701`. The
[T55 research and acceptance map](coverage-a-plus-20260921/t55-consumer-outbox-recovery.md)
records the connected recovery families, existing proof limits and source-level
missing-destination risk. One bounded Microsoft Roslyn pairing is complete:
118 byte-identical inputs,78 sources,35 tests,37 paired/41 unpaired. Static pairing
is not runtime coverage. Read-only adversarial selection review is complete;
no product correction or new test is claimed yet. The map records additional
resolver-token and committed-window recovery oracles before implementation.

## Current T54 — transport ownership and isolation

T53 is complete and remotely verified at `f492b3ed6`. The [combined T54 map](coverage-a-plus-20260921/t54-transport-ownership-and-isolation.md)
plans six connected contract families across Event Hubs and ActiveMQ. One bounded
Roslyn pass is complete:244 sources,78 tests,five projects;93 paired/151 unpaired.
Static pairing is not runtime absence. Existing measured T53 worklists remain the
selection baseline; no new measurement was run. ActiveMQ read-only selection
review identifies native message grouping and isolation as a missing product proof.

## Current T53 — persisted saga integrity

T52 is complete and pushed at `322dcc16d`. Reuse its measured worklist; no new
coverage run is needed for selection. The bounded Azure Table pairing contains
22 sources, seven tests and three projects: 15 paired and seven unpaired sources.
Static pairing is not proof of missing runtime coverage. Existing full native/
serialized roundtrip and individual converter guards are retained. Four connected
families cover corrupted native values, schema evolution, exact storage limits
and corrupted serialized values through real persistence and consume paths.
See [T53 acceptance map](coverage-a-plus-20260921/t53-saga-persistence-integrity.md).
Implementation, independent review, two detected/restored counterprobes and19/19
final focused controls are complete. The single full33 measurement passes13,160
tests; independent integrity/numerical audit agrees. The current measured worklist
contains5,837 gap identities. See the [final report](coverage-a-plus-20260921/product-wide-profile-f41b145f6.md).

## Current T52 — scheduling admission and failure ownership

T51 is complete/pushed at `e0e9d3c38`. The next connected packet uses its existing
gap inventory, not a new measurement. One bounded Roslyn pairing finds60 paired
and14 unpaired sources (74source/59tests/five projects). Existing recurring
completion coverage already has132 cases; it is retained. New work focuses on
rejection before collaborator effects, endpoint/topology resolution and control
failure ownership. See [acceptance map](coverage-a-plus-20260921/t52-scheduling-admission-journeys.md).
Read-only scope review confirms four families and rejects a duplicate nullparameter
matrix. All four families are now implemented in three test files: twelve control
ownership cases, sixteen publish-admission cases and four Quartz replacement/provider
cases. Quartz uses a real bus and real scheduler; only the provider failure seam is
delegated. Requirements are bound. Final restored controls pass179/179; the frozen
full33 passes13,147 tests with91.54160% lines,84.10997% conservative branches and
zero CRAP>30. Independent integrity and numerical audits agree. Remaining
union5,843 is the current worklist, including generated identities; global A+
remains open.

## Current T51 — receive lifecycle across transport contracts

T50 is completed and remotely verified at `0f7f19e30`. T51 combines Core receive
ordering/duplicate fallback with SQS visibility, SQL terminal lock transitions
and Azure expiry/abandonment. The single bounded Roslyn pairing and acceptance
checklist are in [T51](coverage-a-plus-20260921/t51-receive-settlement-journeys.md).
Existing measured T50b gaps guide selection; no new coverage run is needed.
Implementation now contains ten methods and 39 requirement-bound cases. Focused
Core/SQS/SQL/Azure controls and three counterprobes pass their intended checks.
The complete measurement at `b42dcf790` passes13,115 tests in33 profiles, with
91.53094% lines,84.08554% conservative branches and zero CRAP>30. Independent
audit agrees. T51 worklists now guide further selection; union5,850 remains open.
The T51 report retains initial authoring failures and review limits.

## Current T50 — JSON boundaries across conversion, admission and forwarding

The corrected full33 measurement at `a74627818` is complete: 13,076 passing
executions, 91.51067% lines, 84.07197% conservative branches, zero CRAP > 30,
5,851 remaining gap identities. The first full33 exposed a Quartz raw replay
regression; corrected targeted controls pass 6/6 plus 56/56 and full Quartz
passes 284/284. Independent final audit passes without discrepancies; publication
follows the documentation commit. See
[measured evidence](coverage-a-plus-20260921/product-wide-profile-a74627818.md).
The following preparation notes are historical.

T49 is complete and pushed at `fd3c32976`. The larger connected scope, one
bounded Roslyn pairing (60 sources/50 tests), existing evidence and acceptance
map are in [T50](coverage-a-plus-20260921/t50-json-boundary-journeys.md).
The combined families are implemented; a raw-forwarding contract-loss defect is
reproduced and corrected. Three counterprobes are detected and SHA-restored;
56/56 final controls and verify-only formatting pass. Freeze before the sole full
measurement. Earlier current/publication labels below are historical.

## Current T49 — combined Courier outbox journeys

T48 is completed and remotely verified at `704163364`. The bounded inventory,
single Roslyn pairing, existing-test comparison and acceptance checklist are in
[T49](coverage-a-plus-20260921/t49-courier-outbox-journeys.md). Execute
retry/revision, compensation retry/log preservation, and activity timeout with
outbox effects are implemented as one packet; reviewed counterprobes are detected
and 69/69 restored controls pass. The sole complete measurement of `ad3ddbd40`
and independent final audit are complete without discrepancy; see
[final evidence](coverage-a-plus-20260921/product-wide-profile-ad3ddbd40.md).
It records 13,032 executions, 91.48614% lines, 83.99511% conservative branches,
zero CRAP > 30 and 5,855 remaining gap identities. Global A+ remains open.
The older current/publication labels below are historical. Select larger
connected areas with several behavior families for subsequent packets; reuse
this inventory and avoid repeated overall measurements during implementation.

## Current T48 — MultiBus host ownership

[T48 research and acceptance map](coverage-a-plus-20260921/t48-multibus-host-ownership.md)
owns current work. T47 is completed and pushed at `50bd0a527`; its pending
publication wording below is historical. One bounded Roslyn pairing pass is
complete. Test planning follows the mandatory Microsoft code-testing-agent and
test-gap-analysis skills. No new global inventory or full measurement is needed
before implementing the combined health, hosting and scheduler journeys.
The completed T48 implementation at `3422dc2fd` now has one successful full33
measurement and independent numerical/integrity audit; see
[final evidence](coverage-a-plus-20260921/product-wide-profile-3422dc2fd.md).
Publish the packet before selecting the next larger behavior scope.

## Current T47 — combined Saga journeys

The current packet research, single Roslyn pairing result, existing behavior
evidence and acceptance map are in
[t47-saga-journeys.md](coverage-a-plus-20260921/t47-saga-journeys.md).
The first source reading remains complete; the iteration histories below do not
reopen it. T46 is completed and pushed at `1011ef1dd`. T47 implementation
`7d0e3d332` passes the complete measurement and independent audit; see
[final evidence](coverage-a-plus-20260921/product-wide-profile-7d0e3d332.md).
Global A+ remains open. Publish the completion record before the next larger packet.

The bounded source scope is request declaration/correlation, request activities,
timeout cancellation, SendCallbackExtensions, SendActivity/FaultedSendActivity and
their behavior chains. New real-transport tests combine two Saga owners, pending
factory/send stages, callback metadata and compensation failures. A Quartz journey
reuses one Saga for two request generations and injects an old response, fault or
timeout while the second request and its real trigger are active. This closes a
generation-isolation gap absent from the existing single-request Quartz journeys.
The stale timeout is an injected late message, not a second firing of a deleted job.

Requirements: preserve larger coherent packets; use mandatory Microsoft test
skills; add only product-contract discriminators; adversarial read-only review and
isolated counterprobes; one final full33 with exact-commit coverage/CRAP; document
limits and publish only after validation. No new global source inventory is needed.

## Iteration 132 connected ownership and liveness remediation

The original goal and secured input 3e4eae03435f3b7343bb63a1eac66eeca2269139
remain unchanged. Main personally completes all 43 shared support inputs/4,311
physical lines and the effective Core build/package/execution graph. The exact
sorted 621-path Git/read set and bytes reconcile; XML/JSON parsers and the strict
Core Release compiler succeed. Evaluated Compile matches all 553 tracked Core
C# inputs; seven repository-owned imports are fully read. External SDK/package/
generated imports are hash-bound separately, not claimed as personally read.

Focused targets: CS01 failed cache-constructor allocation ownership, MD01 mutable
Mediator MIME exports, H01 synchronous filter liveness and H02 readiness-clock
causality. Required discriminators are exact primary fault identity, independent
CTS/semaphore disposal with linked/unlinked lifetime tokens, literal timer policy,
one terminal timer release, fresh MIME identities and unshared media/parameters
across reads/deliveries/contract types, producer completion within the filter, and
actual configured-clock timer allocation plus fault/underlying-ready/stop state.
New waits/cleanup are bounded; requirements gain three manually authored bindings.

Final restored compiler succeeds with zero warnings/errors. Fresh focused native
25/25 and unfiltered Core 4,011/4,011 pass strict zero-tests/fail-skips/fail-warns
policy. Five separately compiled CTS/gate/MIME/clock/monitor mutants fail exactly
the intended assertions and their sources are SHA-restored. Internal Lead review
finds zero concrete current defects, not external product-team acceptance. Initial
fixture mistakes and the accidentally launched stale-DLL diagnostic are rejected.
Current nine observed productive modules measure 50,608/64,568 lines (78.3794%)
and 17,445/24,298 branches (71.7960%); neither number is whole-product coverage.

MD02/PA01, other cache/test findings, whole-product coverage/CRAP, real cloud
acceptance and the original whole-src manual architecture/comment obligations
remain open. No source/comment/test/report generator or shared-helper runtime edit.

## Iteration 131 Core directory EOF and connected source contracts

All remaining150 owner inputs/32,604 lines are fully personally read, including
every helper/comment. The JSON duplicate-field negative control/schema/nonempty
strings/input bytes and exact ordered reconstruction pass; the main reads every
five-field association through EOF:2,922 records,527 types,5,143 lossless view lines.
This data presentation generates no source, comment, assertion or report.
Cumulative Core directory557/557/143,127 physical input lines; final sorted Git/read
path union and binary reconciliation exit0. Full effective graph/language-parser/
GitReadSet admission remains required before new Core test design/edit or acceptance.

Nine complete productive reads/1,510 lines expose global mutable Mediator ContentType
(MD01), notification validation/invalid observer task inconsistency (MD02), and encoded
payload versus hard writer reservation semantics (PA01). Only the inaccurate buffer
constructor comment is manually corrected; exact two-line derivative and input/final
hashes verify. All three source contracts stay OPEN. Five High/two Medium/two Low
existing-test groups cover exact forwarding/order, failure cleanup/ambient state,
terminal ledgers, timing/overlap, independent security rejection, state/boundaries,
producer-derived or selected-surface expectations and naming/file/style. Strong real
held tasks, literal wire values, defensive MIME exports, post-stop counts, original
failures and independent state/content controls retain their actual stronger status.

EV01 neighboring raw-body data attribution and EV02 exact KillSwitch-name ambiguity
are resolved by exact current/Git/structured historical rows, not new product fixes.
The separate empty Mediator body-type set is genuinely asserted. Failed binary/text
encoding and shell-quoting probes are corrected diagnostics, never successful receipts;
truncated output gets no credit. Five extra shared build/project reads total443 lines
but do not constitute the entire effective execution/dependency closure.

Report and prefixes are handwritten; old tails unchanged. Normal five-path Git/tag/
atomic remote security precedes coherent admission/remediation/mutation work. No new
native/coverage/CRAP/global Async/provider/external review or final A+ acceptance;
historical128 4,007 passes and all earlier open findings remain explicit.
[Detailed checkpoint](../evidence/WP-F2-SERVICEBUS-SOURCE-ARCHITECTURE-A-PLUS-01/ITERATION-131/CORE_OWNER_READING_COMPLETION_AND_SOURCE_CONTRACT_REVIEW.md).

## Iteration 130 complete connected execution/provider/harness reads

The main fully personally reads all 93 selected files / 22,693 lines, including
25 Courier, 16 Futures, 8 Scheduling, 13 RetryPolicies, 29 Testing and two bootstrap
inputs. Names/declarations 523 reconcile with 597 historical passed cases and exact
input Git bytes; prior 314 / 87,830 lines remain exactly bound. Cumulative Core
owner 407/557 / 110,523 lines, 150 / 32,604 lines remain. Full effective owner,
language parser and GitReadSet admission still precede new Core test design/edits.

Concrete test issues: unbounded snapshot-filter Thread.Join can hang on the target
lock regression; two Task.Yield calls do not prove endpoint timeout completion.
Async cleanup inspected without await and early-failure lifetime ownership are
also recorded. Qualified gaps cover await/token causality, drained multiplicity,
paired metadata, retained unrelated/mutated state, configured provider execution,
generic extra-slot membership, variants/semantics and naming/style. Fourteen groups,
0 Critical / 9 High / 3 Medium / 2 Low, all open; no productive defect inferred
from selected weak assertions and no actual mutant execution claimed.

Strong actual held retry callbacks, one-tick trace deadlines/constructor cleanup,
scope/registration identity, full schedule options, compensation exhausted budget,
all-nine supplemental variables, terminal factory stored values, saga retention
and acquired-context registration removal are retained as adjacent counterexamples.
No productive source/test/project/dependency/comment changes or generators occur.
Seven authority/current-input/history bindings verify before checkpoint edits.
No new tests/build/coverage/CRAP/cloud/provider/independent acceptance; historical
128 native 4,007/4,007 passes and all prior findings retain actual disposition.

The report and all prefixes are handwritten, preserving exact prior history bytes.
After normal four-path security, finish remaining owner inputs and effective graph/
parser admission, then original whole-src quality remediation without feature loss.
[Detailed checkpoint](../evidence/WP-F2-SERVICEBUS-SOURCE-ARCHITECTURE-A-PLUS-01/ITERATION-130/COURIER_FUTURES_SCHEDULING_RETRY_AND_HARNESS_READING_CHECKPOINT.md).

## Iteration 129 full connected existing-contract review

The main personally reads all 59 remaining files / 14,429 physical lines through
EOF, including every fixture/member/comment. All 338 declarations reconcile with
398 historical passed cases and unchanged Git input. Prior 255 / 73,401 lines
also bind exactly. Connected packet 102/102 / 23,480 lines / 508 declarations /
665 historical cases; cumulative Core owner 314/557 / 87,830 lines, 243 remaining.
No complete language parser/effective graph/GitReadSet or whole-owner admission.
Agreement §4.3 still gates new Core test design/editing and full owner acceptance.

Positive actual scope identity, retained NamedEntity order, terminal activity
delivery, gated active deltas, fixed gauges/hash vectors, and attempted secondary
logger/factory failures are retained. Narrow deferred gaps concern causal/terminal
observation, exact ownership/scalars/configured execution, rejected retained state,
collectible-owner lifetime, cleanup bounds, pending typed forwarding, global
transition, variants/canonical schema and fixture naming/style. Grouped scoped
review: 0 Critical / 9 High / 3 Medium / 2 Low, all open, not confirmed productive
defects or executed mutation kills; overlapping prior findings are not additive.

Input 020c146f8067d8f5bf38ef51aa35344e7dd7709e is actually remote-secured.
Authority/input diagnostic 0 confirms seven bindings and exact old DECISIONS
bytes after removing only Licensing PO-2026-09-15-03 row/section. ServiceBus
authority is unchanged; failed diagnostic probes are not successful receipts.
No productive source/test/project/dependency/comment changes, no generators,
fresh build/native/coverage/CRAP/global Async/provider or counter-review here.
Historical 128 Core 4,007/4,007 passes are not a fresh 129 run or CS01 proof.

Four owned report/history files are manually authored and normally secured after
strict manifest/tail validation. All old tails and original goal/findings remain.
Finish the remaining 243 owner inputs and parser/effective graph admission,
then effective regressions/mutations and whole productive-src quality work.
[Detailed checkpoint](../evidence/WP-F2-SERVICEBUS-SOURCE-ARCHITECTURE-A-PLUS-01/ITERATION-129/CORE_COMPOSITION_TELEMETRY_AND_TOPOLOGY_READING_CHECKPOINT.md).

## Iteration 128 actual constructor correction and full connected reads

Input 6c0916b20eb5b8f71ca924c84b87b3bca054f50e is actually remote branch/tag/peeled
verified before edits. Complete authority hashes and Core tree are unchanged.
ResourceCache constructor now disposes acquired linked cancellation source and
observer semaphore on linking/timer initialization failure, preserving the original
exception. Successful timer/token/API behavior is retained. One productive file
is manually edited; 17 other Cache files are exactly input-byte equal.
CS01 remains open pending independent fault regression, effective counter-mutants
and a verified portable interval contract; existing passes do not close it.

43 new files / 9,051 physical lines are fully personally read, including every
member/fixture/arrangement/assertion/comment. All 170 declarations reconcile to
267 historical and 267 fresh passed cases. Cumulative owner 255/557 / 73,401 lines,
302 remaining. Connected packet 43/102 complete; DI contract lines 1–235 remain
partial and uncredited. No new Core test design, edits or owner acceptance.
Eleven settled scoped review findings: 0 Critical / 8 High / 2 Medium / 1 Low,
all open, with independent deferred criteria and strong adjacent proofs retained.

Fresh strict Core build: zero warnings/errors, 62.65s. Fresh native Core exits 0,
4,007/4,007 passed, no failed/skipped/pending/other records, including all 105 Cache
cases. Source/read/native diagnostics exit 0. No new coverage/CRAP, cloud/provider,
mutation kill or fresh Architecture execution claim. Test-review/run-tests skills
guide actual inspection and native MTP command detection, never generated code.

The attempted internal Sol counter-review is unadmitted: mandatory authority output
truncates, zero source reads/hashes/judgments. It initially enumerates protected
legacy AGENTS names, with no protected contents read or changes; that violation
is documented and receives no clean-scope or independent-acceptance credit.
Main resumption erroneously repeats an already answered informational question
despite its summary marking it closed. Continue the concrete unfinished file,
not that discussion; preserve the original goal and all prior work.
[Detailed checkpoint](../evidence/WP-F2-SERVICEBUS-SOURCE-ARCHITECTURE-A-PLUS-01/ITERATION-128/CACHE_INITIALIZATION_REPAIR_AND_CORE_READING_CHECKPOINT.md).

## Iteration 127 intermediate Cache source-structure and reading checkpoint

Input cabe618cae88992da2409781a5ed7e8eba001675 and iteration-126 annotated tag are
actually normally pushed and independently branch/tag/peeled verified. Complete
authority bindings remain unchanged; the agreement is completely reread.

Main fully personally reads all seventeen productive Cache input files / 1,701
physical lines and all six Cache test files / 2,974 lines, including every member,
fixture, helper, arrangement, assertion and comment. Four productive files receive
manually authored functional XML corrections; the exact existing internal index
base moves into its matching file. Final productive scope: 18 files / 1,707 lines.
Exact non-XML/body/signature equivalence proves no executable or API change.
No generator authors comments, tests, code, dispositions or the handwritten report.

All 104 Cache methods map to 105 historical and 105 fresh native passed cases.
Prior 206 / 61,376 plus six / 2,974 gives 212/557 / 64,350 lines; 345 remain.
The connected 108-file reading selection is still in progress, 102 unread. No
Core test design/edit, complete-owner admission or whole-source/A+ certification.

Three High productive risks concern timer-construction ownership, creation lifetime
commit boundaries and multi-index comparer failure/reentry. Initial caller-only
cancellation may be intentional and must be distinguished from shared lifetime.
One Medium concerns the minimum-age parameter's lack of independent ordinary
expiration effect. Three High test weaknesses concern winning-token observation,
failure-sensitive bounds/release and coverage labels for unarranged/retired
mechanisms. All seven remain open with qualified independent acceptance criteria.
The apparent expired-resource rejection leak is rejected after complete path
reading: index-version invalidation releases expiration before reprojection.
Real single-flight, independent identity/state, disposal, generation, clock,
observer-fault isolation and outside-lock probes are retained, not blanket flagged.

Fresh strict Release builds show zero warnings/errors. Native MTP reports prove
Core 4,007/4,007, Architecture 439/439, no failed/skipped/pending/other records and
bidirectional Async naming passed. Lost truncated wrapper outcomes are not invented
as exit 0; process/log/native report completion avoids duplicate execution. The
run-tests skill/detection reference guides native .NET-10 invocation. The attempted
internal Sol advisor supplies no code review, 0/18 source reads, no credited advice
or independent acceptance because required authority reading was not closed.

The initial inventory diagnostic selects no owner paths through an unqualified
exclude and terminates 1. All read-byte/native checks succeed. Explicit owner-
qualified glob/exclude restores exact 557-file membership; corrected diagnostic 0.
Preserve the failed receipt; no missing work, blind clean or SDK reinstall follows.

Keep separate SDK projects as src siblings: ViciOne.ServiceBus owns only the core,
not an umbrella. Persistence/Scheduling/Transports remain integration families.
Actual root src has shared Directory.Build.props only, no direct C# files.
[Detailed checkpoint](../evidence/WP-F2-SERVICEBUS-SOURCE-ARCHITECTURE-A-PLUS-01/ITERATION-127/CACHE_SOURCE_STRUCTURE_AND_READING_CHECKPOINT.md).
Normal checkpoint security requires actual commit/tag/push/independent keyed refs.
Continue the original complete-source/manual-comment, greenfield API/architecture,
owner admission, causal test/mutation, provider, coverage/CRAP and feature goals.

## Iteration 126 complete consume, request and native transport reading

Actually secured input e6ad845133aa67550f52696c63709c5924e58f78 and iteration-125
annotated tag remain the baseline; complete authority hashes and Core tree
e3be6b831183b3b65636c3b5e167c165696037d2 are unchanged.

The main completely personally reads 60 new files / 18,004 lines: eleven Clients,
twenty-one Consumers, twelve Context and sixteen InMemoryTransport files, including
all fields, test methods, fixtures, helpers, data arrangements and comments.
All 312 existing methods are manually reviewed and independently reconciled to
459 historical native passed cases. No advisor substitutes for the main reading;
no test/code/comment/report generator or partial-owner test design is used.

Five High weaknesses concern effects before rejected work fails, independently
sorted fields losing their pairing, incomplete argument/overload and pending-task
forwarding observation, failure-sensitive wait/cleanup bounds, and primary/fallback
ordering inputs that select the same order. Medium findings concern unrelated
failure reasons passing a no-pump regression and factory/settings proofs that stop
at type/non-null observation. One reflection helper is artificially async (Low).
All eight remain open with exact independent correction criteria after admission.

Strong native proofs include actual durable consumer-completion ownership,
retained failed/unmatched local outbox records, immutable headers, terminal request
ordering, losers canceled before disposal, complete drained send/publish matrices,
500 exact faults, 1,000 identities with a separate late-duplicate recorder, rejected
batch admission with zero retained/delivered work, and real scoped context isolation.
Grouped results still need key-to-membership multisets, not disconnected sets.
Reflection, meaningful identity assertions, legitimate no-op fixtures and configured
batch time windows are not blanket anti-patterns. Local durable acceptance is not
real persistent/provider/cloud/recovery acceptance.

The first prior-reading diagnostic terminates 1 solely on newline-byte counting:
packages.lock.json has 445 newline bytes but 446 physical lines. All prior Git
byte comparisons succeed. The corrected physical-line diagnostic terminates 0;
prior 146 / 43,372 plus new 60 / 18,004 gives 206/557 / 61,376, 351 remaining.
The failed receipt is preserved. There is no lost file, changed lock file, test
failure or need to repeat compilation to diagnose this accounting issue.

The test-anti-patterns skill and fully read .NET extension guide causal assertions,
isolation and async-failure review without generating code, comments or dispositions.
One hand-authored packet binds sorted per-file hashes/lines/methods/cases. Reading
does not grant full owner admission or whole-source/A+ acceptance; lexical native
membership reconciliation is not a complete language-parser admission gate.
No productive/test/project/gate change or fresh full build/test/mutation/provider
run occurs. Iteration-123 4,007/4,007 Core and 439/439 Architecture stay historical.

The actual src-root tracked tree contains only shared Directory.Build.props as a
root file, no direct C#. Separate assemblies/projects are deliberate siblings:
the core ViciOne.ServiceBus folder is not a repository umbrella. Integration
families stay Persistence/Scheduling/Transports. Each productive type must belong
to its owning project; namespace sharing does not require physical project nesting.
No superficial move, recursive compile-glob workaround or feature removal.
[Detailed packet](../evidence/WP-F2-SERVICEBUS-SOURCE-ARCHITECTURE-A-PLUS-01/ITERATION-126/CONSUME_REQUEST_TRANSPORT_READING_PACKET.md).
Checkpoint security follows actual normal commit/tag/push/keyed remote verification.

## Iteration 125 complete middleware, transaction and in-memory saga reading

Input da77c920b0df07bf867b3858176f58b545a0ddc3 and iteration-124 annotated tag
are actually normally pushed and independently branch/tag/peeled verified.
Main-completely-read authority remains hash-identical. Core owner tree remains
e3be6b831183b3b65636c3b5e167c165696037d2; productive/test scopes are unchanged.

The main completely personally reads all 54 selected files / 17,659 lines:
forty Middleware, six Transactions and eight Saga, including all nested fixtures,
fields, helpers, data arrangements and comments. Together with the exact revalidated
prior 92 files / 25,713 lines and no overlap, owner progress is 146/557 files /
43,372 lines, 411 remaining. No partial-owner test design/change or Lead acceptance.
All 392 existing methods are manually reviewed and exactly reconciled to 637
historical passed cases; counts and native green do not replace actual reading.

Four High weaknesses concern eight unasserted forwarded argument positions,
foreign-checkpoint retained actions not arranged, failure-sensitive wait/cleanup
bounds and virtual-time negatives based only on asynchronous task completion.
Medium findings qualify the unrelated inner one-second request timeout and
transaction-filter flow without its own independent commit observation. Compact
compound outbox fixture readability is Low. All seven remain open; candidate
regressions are not relabeled actually executed/killed mutations.

Strong sibling proofs include retained rollback tails, a genuinely queued failed
response, exact lease ownership and staged index admission, real commit/rollback,
complete delayed-redelivery sequences after drain, exclusive 33-contender recovery,
observed flow-control rollback and exact nested retry failure ownership. The review
avoids blanket broad-exception, private-reflection or no-op fixture findings.
The test-anti-patterns skill and .NET extension inform uniform causal assertions,
isolation and async-failure review; they do not generate comments or reports.

One compact manually authored packet preserves 54 sorted hash/line/method/case
rows, exact findings and scoped positives without hundreds of duplicate method
dispositions. Actual binary input/prior-read and historical native diagnostics
terminate 0. UTF8 textual parsing with binary .b Git equality reuses the resolved
diagnostic lessons; no repeated build is needed to resolve an inventory issue.
There are no productive/test/project/gate changes or fresh native runs here.
Iteration-123 actual Core 4,007/4,007 and Architecture 439/439 remain historical;
genuine provider/cloud acceptance and current global coverage are not inferred.

The src-root tree confirms only shared Directory.Build.props directly at root.
Core and independent optional SDK projects remain siblings; Persistence/Scheduling/
Transports remain integration families. No superficial move or feature deletion.
[Detailed packet](../evidence/WP-F2-SERVICEBUS-SOURCE-ARCHITECTURE-A-PLUS-01/ITERATION-125/MIDDLEWARE_TRANSACTION_SAGA_READING_PACKET.md).
Security requires actual normal commit/tag/push and keyed remote verification.

## Iteration 124 complete job and reliable-messaging reading

Actually secured input 10d1198187cf7ab1050351cfc0cac97489111b79 and iteration-123
annotated tag are independently branch/tag/peeled verified after normal atomic
push. Complete main authority bindings remain unchanged. Core tree remains
e3be6b831183b3b65636c3b5e167c165696037d2.

The main fully reads 45 new owning files / 11,805 lines, including forty test
files and five standalone fixtures. Together with the revalidated prior 47
reads, progress is 92/557 files / 25,713 lines, 465 remaining. All 51 selected
job/reliability folder files are personally read, not the complete Core owner.
Every existing method/helper is reviewed; 283 method declarations independently
equal 283 historical native methods / 514 passed cases at the unchanged input.
No partial-owner test design, modification or Lead acceptance is introduced.

Four High weaknesses concern exact lifecycle serializer input, an unexecuted
dispatcher configuration callback, failure-sensitive wait/cleanup/traversal
bounds and zero-work duplicate admission. A disconnected default-off journal
store is a Medium oracle weakness; compact fixture formatting is Low. All six
remain open. Actual strong sibling assertions prevent inflated global-gap claims:
independent serialized input, executed configuration callbacks, UUID golden vector,
drained exact reliable event sets, deterministic custom DST and complete snapshots.
Intentional unsupported/no-op test fixtures are not productive dummy features.

This packet writes no productive source, tests, projects or gates. Agreement
§4.3 does not require unchanged expensive full execution for reading-only progress.
Iteration-123 actual Core 4,007/4,007 and Architecture 439/439 remain historical
input-bound execution, not fresh runs or mutation/provider/global coverage proof.
Binary read bindings, compatible Ruby enumeration, report membership and scoped
diff whitespace are the proportionate current checks. All corrections/reviews
are manually authored with apply_patch; static diagnostics never generate them.
One initial report checker exits 1 on a UTF8 em-dash regex versus an ASCII-8BIT
string. Explicit UTF8 report reads, while keeping Git byte comparisons binary,
resolve the cause: final checker 0, all 45 manifest and 283 method rows match.
Preserve its failed receipt; do not treat it as a test failure or rerun builds.

The [manual manifest and scoped review](../evidence/WP-F2-SERVICEBUS-SOURCE-ARCHITECTURE-A-PLUS-01/ITERATION-124/CORE_OWNER_READ_PROGRESS.md)
preserve exact counts, hashes, findings and remaining whole-goal obligations.
Checkpoint security requires actual normal commit/tag/push/keyed remote checks.

## Iteration 123 complete saga Core reading and outbox source

Input 3bb808bc999141638b1620c4adeb5bc51ceacc86 is actually committed,
annotated with iteration-122, normally atomically pushed and independently
reference-keyed verified (actual exits 0, three matching references). All six
normative hashes and the selected ServiceBus slice are unchanged from complete
main readings; no new product authority or protected-tree work is introduced.

The main personally completes twenty additional Core-owner files / 6,804 lines,
including every fixture/helper/field, 93 methods / 137 declared cases. Cumulative
owner reading is 47/557 files / 13,908 lines; 510 files remain. All forty files in
the selected saga/job state-machine folders are read, not the whole owner. No new
Core test design, change or full-owner acceptance before complete admission.
The handwritten review records 0 Critical / 4 High / 0 Medium / 1 Low: unarranged
outbox failed response, actual-derived correlation IDs, aliased/incomplete stale
snapshot and unbounded removed-owner waiter; Legacy label is cosmetic, not a
reason to remove native saga features. Exact correction targets remain deferred.

Nine productive source files are completely personally read / 1,099 input lines.
Five files receive manually authored comment-only repairs for actual rebinding,
deferred delivery, cancellation requests, conditional warning logging, exception
ordering and repeated disposal. Final lines 1,105; binary non-comment comparison
is actual 0 with no signature/executable change. No source/comment generator.
The internal Sol advisor completely reads eight exact sources, yields three
manually applied qualifications, fully rereads their final versions and finds no
further mandatory comment fix. It initially enumerates two protected review README
filenames, without content reads/writes; this reported deviation is explicitly
preserved rather than claiming perfect exclusion or independent external acceptance.

Structured diagnostic lessons: binary .b equality, not Ruby encoding-sensitive
String equality, correctly reconciles the unchanged Å partitioner fixture.
Initial checker 1 is preserved; final exact 47-file read-binding check is 0.
System Ruby lacks filter_map; initial diagnostic 1/empty raw file is preserved,
then read-only map/compact succeeds. Neither issue is a product/test failure.
Known build/host Sandbox IPC is handled by targeted approved execution, strict
no-restore/no-build-server/single-node/nonshared-compilation flags, not blind reruns.

Final strict Core/Architecture builds are 0, zero warnings/errors (12.60s/4.94s).
Fresh native unfiltered Core is 0, 4,007/4,007 (22.745s); Architecture is 0,
439/439 (4m03.398s), zero failures/skips. Actual bidirectional Async case passed
(204718ms). Independently parsed reports exactly match 93 reviewed methods /
137 passed cases and the actual unique Async record. Exact five-source format
is 0 with no output/writes. Focused incremental builds are not global warning
inventory. No fresh mutation/package/cloud/global coverage claim is made.

The actual source-root tree contains sibling project owners and only the shared
Directory.Build.props file directly at root. ViciOne.ServiceBus is Core, not an
umbrella project. Retain sibling capability assemblies and provider/integration
families; do not nest other SDK projects under Core's recursive source inclusion.
All runtime/API/metadata/provider/full personal reading/global proof work remains
in the original active goal. [The bounded packet](../evidence/WP-F2-SERVICEBUS-SOURCE-ARCHITECTURE-A-PLUS-01/ITERATION-123/SAGA_CORE_READING_PACKET.md)
preserves exact readings, findings, actual final receipts and scope qualifications.

## Iteration 122 state-accessor source and Core-owner reading

Input e280df694ab9e6c50e3877aebf97882dfeeacb5e is actually committed,
annotated and atomically normally pushed with the iteration-121 checkpoint tag.
Independent reference-keyed verification matches the branch, tag object and
peeled tag. The original whole-product A+ goal remains active.

Six normative bindings and the ServiceBus slice remain hash-identical.
DECISIONS now hashes to 43d9d6a2969e16284706e4b644de73573930cd9fe408fd8db856340849599cd4.
The main completely reads the changed Licensing decision and surrounding current
entries. A read-only reconstruction excluding only that new decision and its
counter/index entries exactly matches the previously completely read
07147cdb45bdd87950c3cd91705bd636fe7bcd08a13714ec7a874edad284627d binding.
No selected ServiceBus rule or source authority changes.

The main personally completely reads eight related state-accessor source files:
507 input lines, 517 final lines. Four files receive manually authored XML-only
comment repairs after understanding their complete code. An exact eight-file
comparison excluding only XML-comment lines proves unchanged executable and
signature bytes. Other comments are inspected, not mechanically rewritten.
Two context files were already read in iteration 120; this is not eight new
unique files in the whole-src census. No generator writes source or comments.

Core owner e3be6b831183b3b65636c3b5e167c165696037d2 contains 557 tracked files.
The main completes 27 personally read files / 7,104 lines, including 24 C# files,
the project, lock file and Protobuf test input. Shared effective policies are
read/reconciled separately. This is partial admission, not a full Core-owner
review. No new Core test design or modification occurs before complete admission.
Existing declaration/runtime/storage/composite/cancellation/recovery tests guide
the connected source trace without certifying their unseen neighbors.

Focused strict Core/Architecture builds terminate 0 with zero warnings/errors
(64.64s / 23.66s); these are not a clean global warning inventory. Fresh native
unfiltered owners terminate 0: Core 4,007/4,007 (20.439s), Architecture 439/439
(3m33.469s), zero failures/skips. The actual bidirectional Async guard passes
(174206ms). Exact four-source whitespace verification is 0 without output/writes.
Both native reports independently contain their exact passed-case counts and
no other status. Persisted source-equivalence and Core-read bindings are actual 0.
No fresh runtime mutation, package/cloud execution or global coverage is claimed.

User obligations remain verbatim: "features dürfen nicht verloren gehen";
"ich möchte nicht, dass du einen generator nutzt"; "das gilt für den gesamten
Quelltext unter /src !!!". The original runtime, full source-reading, coverage,
API-metadata and provider-acceptance obligations remain open.

## Iteration 121 recursive member nullability contracts

Input 197a150e35c542060e8d416a124fc93f49a3ebd9 is actually remotely secured
with the iteration-120 annotated tag and independently checked references.
All seven normative bindings remain unchanged. Complete Architecture-owner
admission reuses the personally read c0ea82bf915b1f0c744cced8f80215bc6911f81b
tree plus the personally authored/read 282-line member-modifier test and exact
20-row catalogue delta. Current owner tree is 69583711d5397f6dd8cf0aa5a0da2b52310c41f4;
the exact diff from the admitted input has only those two files.

The sole tested source target is the completely personally read 449-line actual
PublicApiBaseline.cs, SHA256 a4178ebc8861ef1a029fd28f2107527c88e3978408e1a2b1a2a01d57a305a564.
Its prior safely scoped pairing result is reused, not rediscovered or mislabeled
as absence of the external Architecture tests. Existing typed inventory tests
and effective native test/MSBuild policies are read before authoring.

Current signatures omit member nullability: nullable reference roots, nested
generic/array positions and distinct read/write promises can collide. Official
NullabilityInfoContext APIs expose parameter/property/field/event trees; the
runtime property implementation handles getter and setter attributes separately.
Roslyn metadata distinguishes oblivious, nonnullable and nullable references.
NullabilityInfoContext is not thread safe and caches reflection members; each
enumeration must own its own context, not a static cache retaining collectible
package assemblies. Unknown metadata must never be silently called NotNull.

User obligations remain verbatim: "features dürfen nicht verloren gehen";
"ich möchte nicht, dass du einen generator nutzt"; "das gilt für den gesamten
Quelltext unter /src !!!". Tests, tooling and comments are handwritten with
apply_patch. A narrowly scoped nullable-disabled test fixture intentionally
exposes oblivious compiler metadata; it is not a product warning bypass.
Production signatures/behavior remain unchanged. Conditional flow attributes,
tuple/dynamic/function-pointer metadata and annotated generic-constraint binding
remain connected obligations, not accepted omissions or whole-API A+ claims.

## Iteration 120 state-machine declaration and composite source

Input 3a7386ca27250bffaf482b828833be643c554e23 is actually locally committed,
annotated, normally atomically pushed and independently verified remotely.
The main personally completely reads the 2,266-line state-machine root and
five complete neighbors (481 lines), then manually corrects relevant comments
in the root and CompositeEventActivity. All six input/current hashes and counts
are bound in the new packet; no internal advisor reading substitutes for this
personal source obligation. The other four files' comments are inspected and
require no change. No generator writes source, tests or comments.

Concrete declaration/cache identity, mutation/snapshot boundary, caught-failure
partial configuration, required input/optional callback, separate cancellation
owner, scheduled-token, composite repetition/recovery, hierarchy progression and
configured/probe-identity concerns remain connected behavioral obligations.
The internal Sol counterreview completely reads the exact six current files
(2,708 lines), finds no mandatory comment correction and qualifies four medium
static priorities without executing tests or claiming external acceptance.
Core test-owner execution is not full personal admission for new runtime tests.
This documentation-only pass changes no test, tuple, directive, dependency,
signature or executable statement and closes no runtime defect by comments.
The original whole-product A+ goal and all remaining proof gates stay active.

## Iteration 119 member modifiers and optional parameter contracts

The remote-secured input is 9990fe12490d2330964c4e53945097c0688fbff6.
The main personally reopens the complete 391-line PublicApiBaseline.cs and
all three neighboring typed inventory test files. Architecture owner reading
reuses the verified complete tree c0ea82bf915b1f0c744cced8f80215bc6911f81b;
subsequent manually authored files are personally read. Core test execution is
not mistaken for complete personal Core test-owner admission.

Bounded implementation checklist: typed struct defaults versus real null;
invariant and escaped literals; optional metadata without a constant; params
versus ordinary arrays; init versus set; readonly versus writable reference
returns; volatile field custom modifiers; ordinary versus sealed overrides;
static/virtual/abstract accessor contracts. Typed fixtures and exact independent
expected records exercise the actual formatter, not a duplicate implementation.
Tests and comments are written manually with apply_patch. The testing pipeline
is executed inline under the user's explicit no-generator requirement.

User obligations remain verbatim: "features dürfen nicht verloren gehen";
"ich möchte nicht, dass du einen generator nutzt"; "das gilt für den gesamten
Quelltext unter /src !!!". This bounded tooling repair does not certify full
feature equivalence or the original whole-product A+ goal. Member nullability,
conditional flow, function pointers, tuple/dynamic annotations and bound generic
constraint nullability remain connected inventory obligations, not exclusions.

Nine complete state-machine partial files / 1,164 starting lines are personally
read. Their comments are corrected only after reading and understanding the
complete corresponding file. Missing correlation is a deliberate validation
failure, and request-ID storage can deliberately fall back to the saga ID.
Late event initialization, input guards, observer error preservation and state
hierarchy consistency require behavioral disposition in the real owning tests.

## Iteration 119 nested API source reconciliation

The secured e282 input preserves the failed API-baseline comparison. Completely
reading 24 selected source files (1,872 starting lines) shows that correct nested
names alone do not confer A+ contracts. Manual comments now distinguish converted
context key selection, saga versus outer-message middleware, zero-output/multi-output
continuations, partial existing correlation retention and actual policy behavior.
Only XML documentation changes; no source generator/script writes source/tests/comments.

Connected follow-up observations are NST-01–NST-10 in
[the packet](../evidence/WP-F2-SERVICEBUS-SOURCE-ARCHITECTURE-A-PLUS-01/ITERATION-119/NESTED_API_SOURCE_RECONCILIATION_PACKET.md):
rollback disposal/fault-observer primary error preservation, nullable callbacks
and missing-pipeline chains, required SPI inputs/results, dynamic probe concurrency,
correlation naming, public generic container/repeated builder semantics, redundant
keyed allocation and custom retry extension equivalence. These are static source
observations, not fabricated runtime failures or automatically inferred missing tests.

The internal Sol advisor reads 50 implementation files / 3,531 lines and 19
retained public source files / 1,381 lines. All 57 implementation types remain
internal. The main detects/corrects checksum-manifest transcription order and
exact generic identity errors through read-only comparison against real inputs;
final 57 recorded identities equal the actual selected old-only key set. An
unchecked manually transcribed name list is not accepted as API proof either.
The advisor's reads do not substitute for personal main source reading or
external/runtime/package/cloud acceptance. Protected review/result trees stay
outside current exact scope; the older root-discovery deviations remain recorded.

## Iteration 119 governed traversal and generic API contracts

The intervening folder explanation is navigation, not implementation progress.
The real fresh package gate subsequently exits 134 when the Console inventory
host reflects SignalR without Microsoft.AspNetCore.App. The first two new
runtime tests have arrangement failures, not causal red evidence: the helper
does not request FrameworkReference items, and its default Debug TargetPath
does not match the executing Release assembly. Correct both arrangements before
observing the unchanged host fail on its actual missing shared framework.
Use an explicit versionless FrameworkReference, not Web SDK, a NuGet substitute,
warning suppression or a machine-specific shared-framework path.

One navigation rg --files command incorrectly started at the product root with
filename filters: it could walk protected names even though no protected match
or content was returned. This procedural deviation is recorded, not disguised
as safe scoping; subsequent file discovery stays at explicit governed owners.

Corrected runtime host bounded tests pass 48/48; the additional projection filter
was misnamed and contributes no test. Fresh package execution now reaches final
comparison and exits 1 with thirty reflected assemblies / 20,045 lines. Output
diagnostics reveal 25 colliding old type identities. A dictionary that overwrites
such keys cannot prove full pairing: use collision-preserving reconciliation
and manual actual-contract review before baseline disposition.

The previous implementation checkpoint is authoritative progress: 18 owned
files committed, tagged and remote-verified at
`1e86575a712c5fe590f1b808eb592943d102f7ea`. The starting tracked tree was clean;
normative agreement/glossary/decisions/current order/findings/slice
hashes match the personally read bindings. Licensing exceptions do not apply.

The complete personally read Architecture inventory now binds 42 tracked files
at tree `f2c09ba1c51b57d51e9bd404eccfd0911bbb2086`, 39 C# files / 9,013 lines:
the prior complete read plus manually authored/reviewed checkpoint deltas.
Relevant traversal consumers, effective test/build/package detection and the
new direct formatter tests are reopened. No whole-product reading is claimed.

Root-recursive project/build discovery must not enter protected review/result/
artifact trees. Preserve the five governed roots and top-level build policies,
derive all traversal from one scoped helper and verify exact owned membership,
recursion, extensions, ordering and exclusions using an isolated temp fixture.
Do not run the existing unsafe real-root methods until correction is verified.

The prescribed Roslyn pairing analyzer is executed once only at the tool root;
it cannot include the external Architecture test project in that safe scope.
Treat its JSON strictly as scope-limited static pairing, not absence of real
tests, coverage or mutation evidence. Current direct compiled tests are stronger
behavioral evidence. No analyzer may broadly enumerate protected review paths.

Next connected inventory repair captures type/method generic variance and
constraints, using explicit independent reflection fixtures and exact strings.
No generator, automatic comment rewrite, automatic baseline copy or new test
platform is authorized. Final package contract disposition requires actual
fresh output review; wider API metadata remains subject to its own proof.

The intervening navigation-answer turn made no implementation change. This
continuation revalidates the unfinished owned changes, verifies the earlier
build is no longer live, then obtains a definitive strict build and real
functional results. A completed raw log alone is not an observed process exit.

The Lead personally reads all five PipeConfigurator partial files (417 lines),
ISpecificationPipeBuilder, both complete send/publish specification implementations
and the consume/message-data consumers: ten source files / 888 starting lines.
Comments are manually corrected against filter composition and application
markers; no source signatures or executable statements change. The empty
continuation is intentional pipeline behavior, not a missing implementation.
The public builders' missing required-input guards and the consume validation
scope are connected follow-up contract questions, not asserted A+ completion.

## Iteration 119 packed API type identity

The secured `fd11887df54fbf5731a50e4626ef4224b5f43c6e` checkpoint retains a
failing package-baseline comparison. Its reflection formatter truncates generic
metadata names at the first backtick, losing nested names and assigning inherited
arguments to the wrong declaring type. Rank-one non-vector arrays also collide
with vectors. Both are inventory defects, not reasons to weaken the package gate.

Before test edits, the Lead personally read the complete tracked Architecture
project: 38 C# files (8,900 lines), its project, 287-line requirement catalogue and
617-line lock graph. Effective root/test build policy, central package policy,
native test configuration/workflow, Unit/Engineering solutions and the complete
318-line formatter and 226-line package-gate script were read. Truncated outputs
were reread in smaller ranges. This is not a claim of complete product reading.

Use an ordinary nonpackable console project with no additional NuGet dependency,
retaining the existing manually authored CLI. An internal friend seam allows
direct exact-string xUnit oracles without copying its implementation into tests.
No source/comment generator or automatic baseline replacement is permitted.

The complete test-project read also found root-recursive build-policy discovery
in RepositoryGraphTests and VerificationCapabilityDispositionTests. Those paths
can enter protected review inputs. Do not execute those methods or claim a full
Architecture run until governed-root discovery is corrected and proved.

## Iteration 119 saga query/index integrity

Complete personal source reads and secured baseline establish predicate-null,
mutable key/hash, partial getter publication and deferred-enumeration diagnostics.
[The saga index packet](iteration119-saga-index-packet.md) distinguishes static
advice, deliberate Greenfield semantics and the still-required causal tests.

## Iteration 119 in-memory saga ownership follow-up

The unchanged remote checkpoint, full-read target inventory, static-only pairing,
concrete ownership/input findings, process cleanup and acceptance checklist are in
[the saga ownership packet](iteration119-saga-ownership-packet.md). No source or
comment generator is used; query-index integrity and cross-provider read-only/
preinsert semantics remain explicit, separate open work.

## Baseline

- Product commit: `c26f7cafcba4cc6828ad46ffa0985828c01b8074`
- Product tree: `ede39ccab9fc8dfed674362accb408180555ac22`
- Protected remote tag: `servicebus-a-plus-api-program-complete-2026-09-05`
- Architecture commit: `94da260cbe66ea49abe61459218cb576cb47f27e`
- Protected architecture tag: `architecture-servicebus-a-plus-program-baseline-2026-09-05`
- Review input: immutable files below `review/**`; they are never changed or staged.

## Test platform and repository conventions

- Target framework: .NET 10.
- Test runner: Microsoft Testing Platform v2.
- Test framework: xUnit 4.
- Existing tests use `Fact`, `Theory`, `Assert`, and `RequirementCoverage` mappings.
- Focused project builds and tests precede a clean full Release validation.
- Any behavior change requires a causal test that fails under a one-cause mutation.

## Existing test architecture

- Core behavior: `tests/ViciOne.ServiceBus.Tests`.
- Public abstractions: `tests/ViciOne.ServiceBus.Abstractions.Tests`.
- EF reliable storage: `tests/Persistence/ViciOne.ServiceBus.EntityFrameworkCore.Tests`.
- Azure Service Bus: `tests/Transports/ViciOne.ServiceBus.AzureServiceBus.Tests` and its local-integration project.
- Architecture/API rules: `tests/Architecture/ViciOne.ServiceBus.Architecture.Tests`.
- Requirement mappings are stored in each owner project's `Requirements/*.json` files.

## Static source-to-test pairing scan

The mandatory Roslyn-based repository scan inspected 4,190 source files and 859 test files. It classified 1,030 source files as name-paired and 3,160 as not name-paired. This is a discovery heuristic only: generic dispatch, integration tests, architecture tests, and differently named behavior owners make a direct filename pair neither necessary nor sufficient.

High-value findings from the scan and the semantic reviews:

- `OutgoingOptionsPipe.cs` has no name-paired behavior test; all public options branches need direct tests.
- `ConsumeTopology.cs` has no name-paired behavior test; its shortened hash needs deterministic collision-resistance tests.
- Several composition and configuration types lack direct tests even where setters currently discard caller intent.
- The broad count cannot be used as evidence that 3,160 files are behaviorally untested.

## Confirmed iteration-1 defects

1. EF reliable inbox abandonment discards the supplied `abandonedAt` and does not persist `CompletedAt`, unlike the in-memory provider.
2. `ReceiverConfiguration` silently discards six operations even though the wrapped `IReceiveEndpointConfiguration` implements their semantics.
3. `InMemoryBusFactoryConfigurator.AutoStart` silently discards the configured value.
4. `EndpointRegistration<T>.IncludeInConfigureEndpoints` silently discards the configured value.
5. Both composite filter implementations expose replaceable-looking setters that discard assigned values; the public variant also contains the typo `DoesNotMatcheAny`.
6. The Azure Service Bus message-session saga repository exposes query correlation but throws `NotImplementedException` only when a message is consumed. The supported capability boundary must be explicit and fail before processing.

## Related risks queued for later iterations

- `NewId` has mutable process-global providers with ambiguous post-initialization semantics.
- Job notifications may discard caller cancellation.
- Public options and outgoing-message application APIs lack complete parameter-level behavior coverage.
- Public API layering, member-level baselines, XML documentation, filenames, folders, namespaces, directives, and comments need repository-wide remediation.
- Topology hash suffixes provide only 30 bits of disambiguation.
- SQL URI handlers expose incorrect nullability.
- Test polling and fixed-delay negative assertions should become deterministic.

## Confirmed iteration-10 defects

1. The fresh-package API gate writes an inventory but never compares it with a committed baseline,
   so an accidental public API change remains green.
2. The gate packs only the nineteen packages needed by the developer journeys although the product
   publishes thirty packages. Eleven delivery packages and twelve runtime assemblies are therefore
   outside its package and API verification boundary.
3. No package-only consumer restores the complete runtime package catalog. The API inventory can
   consequently inspect only the subset that earlier samples happened to restore.

The greenfield reference is the current .NET task-based asynchronous pattern, the .NET runtime
framework-design-guideline digest, and NuGet package-authoring guidance. The resulting repository
rule is stricter than compatibility-oriented library evolution: every intended API change must be
an explicit reviewed baseline update, while an unreviewed addition, removal, rename, visibility
change, default-value change, or signature change fails CI.

## Reference API direction

The greenfield reference model is a small application surface, explicit capability packages, validated immutable options, provider contracts outside application IntelliSense, standard .NET naming and cancellation conventions, and no historical compatibility aliases. Feature preservation means retaining behavior through the correct layer; it does not require retaining accidental public exposure or silently ignored members.

## Confirmed iteration-11 defects

1. Both generic and non-generic `Task.OrTimeoutAsync` convert caller cancellation into a
   `TimeoutException`, whether the token is canceled before entry or while the operation is
   waiting. The caller's exact token and cancellation outcome are therefore lost.
2. `ConsumerJobHandle.CancelAsync` checks the caller token only before canceling the job. It does
   not pass that token to the subsequent job-completion wait, and its broad cancellation catch
   would also hide caller cancellation if the token were forwarded.
3. ActiveMQ session topic and queue deletion check cancellation only before enqueueing and then
   submit the operation with `CancellationToken.None`, so cancellation cannot release a caller
   waiting for bounded executor capacity.
4. The state-machine test harness polling loop substitutes `CancellationToken.None` for its public
   operation token, so an aborted observation can remain asleep until the polling interval ends.

The iteration uses deterministic cancellation sources and virtual time. It distinguishes caller
cancellation from a genuine timeout and from the job-owned cancellation that represents normal job
shutdown.

## Confirmed iteration-12 defects

1. Two synchronous SignalR group-membership methods carry an `Async` marker even though they return
   `void`; four synchronous test infrastructure helpers carry the same misleading marker.
2. Request-handle factory contracts place `CancellationToken` before `RequestTimeout` across the
   abstractions, client implementations, dependency-injection adapters, and mediator. This conflicts
   with the standard .NET cancellation shape and makes positional calls inconsistent.
3. The existing cancellation architecture check verifies only that a token identifier appears in a
   public method body. It does not enforce the API parameter shape, and asynchronous naming has no
   executable repository-wide contract.

The bidirectional gate inspects every evaluated product and test source method, including local
functions. An `Async` name marker must correspond to `async`, `Task`, `ValueTask`,
`IAsyncEnumerable`, or `IAsyncEnumerator`, and every such asynchronous contract must expose the
marker. The public product API separately requires `CancellationToken` to be its final parameter.

## Confirmed iteration-13 defects

1. Product sources contained 471 redundant nullable directives, an always-enabled conditional
   compilation branch, three region pairs, IDE suppressions, and maintenance markers.
2. A 7,377-line embedded expression compiler duplicated an independently maintained dependency and
   carried 277 additional compiler directives plus stale maintenance narrative.
3. Transport-neutral API documentation falsely promised broker acknowledgement for 253 send and
   publish operations. Relative schedule delays were described as absolute times in 96 parameter
   contracts, while 40 genuinely absolute `dueAt` parameters shared the same ambiguous wording.
4. Receive-start methods documented task returns although they return lifetime handles, RabbitMQ's
   publish contract ignored the `awaitAck` partition, and Amazon SQS documentation attributed the
   library's 60-second renewal floor to an AWS API minimum.
5. A line-oriented comment check was insufficient because trailing comments and structured XML
   trivia can evade it. Source hygiene requires syntax-aware Roslyn traversal.

The current dependency reference is `FastExpressionCompiler` 5.4.1, centrally versioned and directly
owned only by core, sagas, and MessagePack. Remaining generic and historical documentation findings
are intentionally carried into iteration 14 rather than being hidden by the bounded iteration-13
verdict.

## Confirmed iteration-49 Azure Table findings

The complete 2,164-line Azure Table product project and both owning test projects were read
file-by-file before implementation. The unchanged baseline passes 55 unit tests, 25 real Azurite
tests, and warning-level Roslyn format verification.

1. The package identity is `ViciOne.ServiceBus.Azure.Table`, while its public types are split across
   `ViciOne.ServiceBus.Azure.Table`, `ViciOne.ServiceBus.AzureTable`, configuration, saga, and
   message-journal namespaces. An extra physical `AzureTable/` directory repeats the capability
   name below the project root.
2. Repository contexts, converter machinery, storage records, ETag payloads, and DI implementation
   types are public even though callers need only repository factories, key formatters, immutable
   journal settings and stores, and composition extensions.
3. Composition verbs use three incompatible legacy shapes: `AzureTableRepository`,
   `SetAzureTableSagaRepositoryProvider`, and `UseAzureTableSagaRepository`. Configuration members
   named `TableClientFactory` and `KeyFormatter` do not express an action.
4. Saga repository methods omit several null and cancellation boundaries, contain compressed
   multi-statement lines, and expose an `async` query-rejection method that never awaits. The owning
   operation token must be explicit: the method token is checked before work, while provider I/O
   uses the surrounding consume/load context token required by the repository lifetime.
5. The property converters do not validate their inputs and contain unnecessarily indirect boxed
   conversions. The implementation remains necessary because Azure Table has a narrow native value
   set, while other saga properties require the existing stable serializer.
6. The bounded message journal has strong transactional behavior, but its persistence record is an
   accidental public API and its direct conversion boundary lacks complete argument validation.
7. The product project contains historical package narrative and a JetBrains namespace suppression;
   both exist only to accommodate the current inconsistent layout.
8. Requirement-projection tests are not themselves represented in their requirement manifests, and
   there is no exact exported-surface guard for this delivery package.

The completed 2,582-line source review keeps only eleven intentional public types in the package
namespace. Public composition APIs now use `UseAzureTable`, provider implementation types live in
matching `Configuration`, `Infrastructure`, `MessageJournal`, and `Saga` namespaces and folders,
and saga properties use an isolated `Saga_` storage prefix. Constructor, key, table-name, converter,
message-journal, cancellation, and concurrency boundaries fail before unintended provider work.

The first post-remediation Azurite run exposed a real composition defect that unit-only review did
not: the three Job Service saga types shared one non-generic formatter registration, so the first
formatter controlled all three repositories. A type-specific internal formatter provider now owns
each `TSaga` registration. The unit contract resolves two saga types from one container and proves
their provider and formatter identities remain distinct; the complete real-provider profile then
passed all 27 cases.

Azure Table concurrency is now a public typed contract. Duplicate inserts (409), stale ETags (412),
and operations on a disappeared entity (404 with `ResourceNotFound`) map to
`AzureTableSagaConcurrencyException`; a missing table or unrelated provider failure remains a
general saga failure. Both SDK-boundary tests and a real Azurite test preserve the original
`RequestFailedException` for retry and diagnosis.

## Confirmed iteration-50 MessagePack findings

The complete 1,244-line MessagePack product project and all seventeen owning test sources were read
before implementation. The unchanged baseline passed 60 tests.

1. Eight implementation-oriented types were exported from the transport-neutral serialization
   namespace even though callers need only registration extensions and an advanced factory.
2. Product and test global-using facades hid each file's dependency ownership, while a JetBrains
   suppression preserved a physical folder/namespace mismatch.
3. The serialization package directly referenced optional Courier and Job Service packages solely
   to enumerate their concrete internal message implementations. Generic interface serialization
   already supplies the same behavior without reversing those capability boundaries.
4. The public static media-type instance was mutable process-global state. A caller could change it
   and invalidate subsequent bus composition in the same process.
5. Formatter creation used a check-then-create concurrent dictionary pattern, so simultaneous cold
   access could return multiple formatter instances even though only one was eventually retained.
6. Several serializer, envelope, body, probe, and configuration boundaries deferred null failures
   into unrelated implementation calls instead of assigning them to the owning parameter.
7. The requirement projection test was absent from its own manifest, and no exact export or optional
   dependency contract prevented the old surface from returning.
8. The benchmark assembly bypassed the public factory through a product `InternalsVisibleTo`
   declaration, coupling shipped code metadata to a development-only consumer.

The completed module exports exactly two sealed/static package-root types. Implementation types now
live under matching `Serialization` and `Serialization.Formatters` folders and namespaces. Courier
and Job Service references are absent from the product assembly, while real nested routing-slip and
job contracts round-trip through the generic interface path. Media-type descriptors are independent,
lazy formatter creation returns one instance under concurrent cold access, and benchmarks exercise
the same public factory contract as application consumers.

## Confirmed iteration-77 cache and request-client findings

All 34 production files in `ViciOne.ServiceBus/Caching` and `ViciOne.ServiceBus/Clients` were read
manually before implementation, together with the public request contracts, scoped and mediator
factory consumers, transport cache owners, and all 13 directly owning test files. No source-comment
generator is permitted. The unchanged focused native-MTP baseline passed all 145 cache and client
tests. A `dotnet test --project` invocation discovered zero tests in this repository's current MTP
layout; direct execution of the built MTP test host is the reproducible runner, while compilation
remains a separate serial `dotnet build` with build-server and shared-compilation isolation.

1. Synchronous `ResourceCache.AddIndex` projects live resources without entering the cache's active
   operation lifetime. Disposal can therefore dispose a resource while a new index selector is
   still using it.
2. Absolute-expiration caches subscribe to `IResourceUsageSource.Used` even though absolute mode
   deliberately ignores usage. The subscription is an observable, failure-capable side effect that
   contradicts the public sliding-expiration contract.
3. The public correlated request-factory overloads accept nullable consume contexts even though
   explicit no-context overloads exist. Several request boundaries resolve endpoints or reach a
   wrapped factory before validating required context, address, message, or initializer values.
4. `IClientFactory` can own a temporary response endpoint but does not expose the concrete
   factory's asynchronous disposal contract. The concrete disposal path is not idempotent and does
   not prevent new request creation after disposal begins.
5. An absolute `RequestOptions.Deadline` is converted to a relative timeout before asynchronous
   endpoint acquisition and pipeline work. Those delays incorrectly extend both response timeout
   and the implicit transport time-to-live beyond the caller's absolute deadline.
6. Request-handle cleanup captures the ambient `SynchronizationContext` through a task scheduler.
   A non-pumping UI context can strand cancellation and fault completion inside infrastructure code.
7. Two endpoint adapters and four request callbacks create async state machines only to await and
   return one task. These are implementation inefficiencies, not user-visible asynchronous APIs.
8. Cache folder ownership is already coherent. The client folder mixes public factories, factory
   contexts, endpoint adapters, and request-handle mechanics in one flat namespace; these internals
   have distinct owners and should move to matching `Contexts`, `Endpoints`, and `Requests`
   directories and namespaces. The core project root itself already contains only project
   infrastructure (`GlobalUsings.cs`, the project file, and the lock file).
9. Existing focused assertions are behaviorally strong, but they do not cover the disposal/index
   race, absolute-mode subscription side effect, fail-before-dependency boundaries, factory
   lifetime ownership, delayed absolute deadline, or hostile synchronization context.

The completed bounded implementation addresses all nine findings. The focused profile grew from
145 to 190 passing tests, and seventeen isolated mutations were killed before final validation.
Client internals now have explicit `Contexts`, `Endpoints`, and `Requests` owners; factory disposal
is a public async contract; deadlines remain absolute across endpoint acquisition; cache index
projection participates in the active lifetime; and absolute-expiration caches do not attach a
sliding-usage observer.

A related repository-level finding was discovered during API verification: the packed-public-API
extractor did not encode direct interface relationships. `IClientFactory : IAsyncDisposable` could
therefore have changed without changing the old baseline. The extractor and exact architecture
guard now encode and mutation-protect direct externally visible interfaces. An update run and an
independent comparison run both produced the same 19,961-line contract with SHA-256
`7a63fd620a3dedc925a4a3409d419905171388458a0ca78ef482e2579466fb7a`.

The global lexical follow-up inventory contains two names outside the iteration whose owning code
must still be adjudicated manually: `LegacyAzureDiagnosticId` in diagnostics and `legacyCanonical`
in QoS validation. The single `placeholder` wording names the real lazy-deserialization sentinel;
it is not a dummy implementation.

## Confirmed iteration-78 consumer, event, metadata, and message-data findings

The complete 70-file production scope in `Consumer`, `Events`, `Metadata`, and `MessageData` was
read manually before implementation, together with the public contracts, configuration entry
points, serializer consumers, dynamic implementation owner, direct call sites, and owning tests.
No source-comment generator is permitted. The unchanged MessageData baseline passes all 53 tests.
The unchanged Consumer/Event/Metadata profile exposes a non-deterministic 108-of-109 run: the
consumer metadata identity test passes in isolation but can overlap tests that mutate the global
consumer convention. That process-global mutation requires one explicit serialized test owner.

1. The singular `Consumer` folder and flat namespace combine public consumer factories, internal
   convention caches, and context implementations. Contexts already have a physical subfolder but
   deliberately remain in the flat namespace. The public factories are useful extension points;
   the caches are accidental exports. Their folders, namespaces, filenames, and visibility should
   express those different responsibilities.
2. Consumer-factory tests do not prove the exact ownership matrix. Owned consumers must be disposed
   on successful and failed consumption, asynchronous disposal must take precedence, and an
   externally supplied instance must never be disposed by the factory.
3. `Events` combines fault snapshots, bus/host readiness, and receive endpoint/transport lifecycle
   projections. Five lifecycle event implementations have no direct semantic test. Fault snapshots
   copy their type arrays but currently admit null elements into a non-null contract.
4. The top-level `Metadata` folder has no cohesive responsibility. Registration metadata belongs to
   consumers; the message-data converter seam belongs to MessageData; and `TypeMetadataCache`
   duplicates the public `MessageTypeCache` while also exposing internal dynamic-implementation
   machinery. The retained implementation cache should own only implementation-type construction.
5. MessageData exports configuration specifications, conventions, converters, property providers,
   lazy value implementations, identifiers, and references that applications do not compose
   directly. The intentional greenfield surface is the public data contract, repository contract
   and implementations, policy, application extensions, and repository selection/composition API.
6. The file-system repository defers null failures, ignores cancellation at some boundaries, and
   derives a path from address segments without proving that the result remains below the configured
   root. The in-memory repository similarly omits low-level cancellation/null ownership and ignores
   the supplied retention period. Both repositories therefore implement a weaker contract than the
   public `IMessageDataRepository` promises.
7. The lazy reader infers stream ownership from one concrete converter type. That is not a stable
   capability contract and fails for any semantically equivalent converter. Converter and value
   constructors also defer several null and snapshot boundaries.
8. Both get and put property providers use `Task.IsCompleted` followed by `.Result`. A task that is
   already faulted consequently produces a different exception shape from an asynchronously
   faulting task. The get provider also treats `HasValue == false` differently depending only on task
   completion timing. Public behavior must be independent of scheduling.
9. MessageData composition owns several missing null/result guards, and the repository selector can
   defer a null repository or invalid path into unrelated infrastructure. All fail-fast boundaries
   must identify the caller-owned parameter before registration or provider work.
10. Many comments in the bounded scope describe construction history, use generic filler wording,
    or no longer state the exact lifetime, ownership, conversion, or storage behavior. Every comment
    must be rewritten manually from the implementation it documents, including internal code after
    visibility reduction.

The mandatory Roslyn source-to-test pairing scan inspected 4,256 source files and 1,087 test files.
It classified 1,596 as name-paired and 2,660 as not name-paired. In the initial Consumer/Event/
Metadata boundary it highlighted the five receive lifecycle projections plus
`IMessageDataConverter`; semantic tests rather than filenames remain the acceptance evidence.

The completed iteration preserves every supported behavior while assigning each implementation to
an explicit owner. Consumer factories now have deterministic sync/async disposal rules; event
projections snapshot mutable inputs; metadata is divided between consumer registration,
message-data conversion, and internal message implementation; repositories enforce retention,
cancellation, snapshot, and path-containment contracts; and property providers behave identically
for synchronously and asynchronously completed tasks. The reviewed capability measures 88.62%
line and 80.59% branch coverage with no method above CRAP 30.

The bidirectional asynchronous naming guard was separately attacked until its final, unchanged
SHA-256 `9014a87363875e58dc12937cd4b61e6692707c0edafc27a7c2b37ff7467fe318` resolved canonical
metadata symbols, evaluated Release compile/using items and symbols, and accepted Quartz names only
for an actual interface-member implementation. Its focused profile passes 30 tests. The complete
architecture profile passes 289 tests, and the final sequential Unit solution passes 5,323 tests
with no failures or skips. A transient SQL test failure was traced to a non-atomic counter in its
concurrent recording spy, corrected with interlocked access, and then passed 20 repetitions, the
126-test SQL module, and the complete solution.

## Confirmed iteration-79 abstractions-root findings

The fifteen non-generated C# files directly in `src/ViciOne.ServiceBus.Abstractions` and the owning
project file were read manually before implementation. They comprise the intended application
contracts (`IBus`, `IBusControl`, `IConsumer`, `IOutgoingMessages`, send/publish endpoints and
provider, `ConsumeContext`, message headers and limits, and the four application option records)
plus `GlobalUsings.cs`. This is a project root, not a duplicate `ViciOne.ServiceBus` source folder.
Keeping these application-layer contracts at that root agrees with the reviewed five-layer API
model and gives callers one stable root namespace.

1. `IOutgoingMessages.cs` violates that ownership boundary by combining the public application
   contract with the internal `ConsumeContextOutgoingMessages` runtime implementation. The
   implementation belongs in the existing `Context` capability and `ViciOne.ServiceBus.Context`
   namespace; the contract file should declare only its interface.
2. The general source-file naming gate only requires one matching primary type. It therefore
   accepts a correctly named contract file containing an unrelated secondary implementation. An
   exact abstractions-root inventory and type-ownership assertion is required to prevent recurrence.
3. The public `IOutgoingMessages` contract has five operations. Existing direct evidence exercises
   only explicit send and configured publish in one in-memory journey. Routed send, default publish,
   scheduled send, exact cancellation forwarding, dependency-call counts, missing route/scheduler,
   and all null argument boundaries lack direct ownership tests.
4. `MessageLimits.Conservative` is a public named policy used throughout the repository, but no test
   pins all five values or proves that the published singleton is stable. Configuration tests cover
   all eight invalid invariant classes and binding/duplicate-owner behavior, not the named policy's
   exact contract.
5. The other root contracts, type groupings, names, namespaces, and comments match their current
   behavior. `ConsumeContext` and `IConsumer` are cohesive generic/non-generic interface families;
   `MessageHeaders` is a deliberate constant catalog whose exact values are already in the packed API
   baseline; and the option records are covered by snapshot and forwarding tests.

The unchanged baseline passes the existing application consume-outgoing test (1/1) and every
existing source-file navigation test (15/15). That green baseline is evidence of the detection gap,
not evidence that the embedded implementation is correctly placed.

The completed remediation moved the internal implementation into the context capability and added
an exact root/type inventory, seven direct outgoing tests, one exact `MessageLimits.Conservative`
test, and stronger real InMemory evidence. Seven isolated mutations were killed and restored. The
final Release build reports no warning or error, the sequential Unit solution passes 5,332 tests,
the bidirectional Async guard passes 30 tests, and the fresh-package gate preserves the exact
19,773-line API hash. Core instrumentation reports 85.71% line and 85.00% branch coverage for the
two executable iteration files. The separate Abstractions project passes 537 tests but cannot emit
numeric coverage with its current MTP dependencies, so no percentage is inferred. A related read
identified generic/stale wording in `Middleware/BasePipeContext.cs`; it remains explicitly queued
for the future manual Middleware/Context owner pass rather than being changed outside this scope.

## Confirmed iteration-80 abstractions-context findings

All ten files and 1,485 physical lines in `src/ViciOne.ServiceBus.Abstractions/Context` were read
manually in full, followed through their interfaces, production callers, project references,
friend-assembly boundaries, and three owning test files before any production edit.

1. The directory combines unrelated layers. `SendContextProxy`, `PublishContextProxy`, and
   `SendContextScope` are public extension infrastructure over contracts already owned by
   `Advanced/Contexts`. The endpoint converter caches and option adapters are internal mechanics.
   `ConsumeContextOutgoingMessages`, `MissingConsumeContext`, and `PendingFaultCollection` are used
   only by the Core runtime (plus the Mediator sentinel registration). Keeping them together under
   an undocumented public-looking `Context` owner obscures the five-layer API.
2. `SendEndpointConverterCache`, `PublishEndpointConverterCache`, and
   `ResponseEndpointConverterCache` are exported even though callers already use endpoint APIs and
   the types expose caching as an implementation detail. All first-party cross-assembly consumers
   are named friends of Abstractions, so the dispatchers can be internal without a compatibility
   wrapper or feature loss. Their functional names should describe runtime dispatch rather than
   the cache used to implement it.
3. `MissingConsumeContext` and `PendingFaultCollection` are exported runtime implementation types,
   not application or Advanced SPI. Both can move to their Core owners and become internal.
   `ConsumeContextOutgoingMessages` likewise belongs beside `BaseConsumeContext`, its sole
   production owner, rather than in the contract assembly.
4. `SendContextProxy.CreateProxy` delegates directly to the wrapped context. A derived proxy such
   as `SendContextScope` consequently loses its local payload layer when creating a typed view.
   `PublishContextProxy` correctly creates the replacement view over `this`; send must preserve the
   same current-view semantics.
5. `OutgoingOptionsPipe.cs` declares five independent top-level implementation types. Splitting
   the send, publish, schedule, application, and snapshot responsibilities into matching files
   restores deterministic type-to-file navigation without changing option behavior.
6. Existing runtime-dispatch tests cover only the three simplest overloads. Pipe forwarding,
   initializer dispatch, returned-task identity, null pipes/values, and all invalid runtime type
   classes are not directly protected. Proxy tests cover constructors and one publish conversion,
   not their complete forwarded state. Scope tests cover only one local payload and four null
   boundaries, not precedence, update ownership, cancellation, typed message, or proxy retention.
   The unavailable sentinel test exercises one property and one publish overload, leaving most of
   its contract unverified.
7. `PendingFaultCollection.NotifyAsync` enumerates notification calls directly into `Task.WhenAll`.
   If one context throws synchronously while producing its task, enumeration stops and later
   collected faults are never notified despite the method's every-fault contract. Synchronous
   throws must be captured as faulted tasks so all entries are attempted before aggregate
   completion.
8. The comments in the ten files were reviewed against current behavior. Most are accurate. The
   proxy base summaries overstate that they forward operations when they actually forward context
   state and payload behavior; sentinel and dispatcher comments must follow their new ownership and
   functional names. No scripted comment rewrite is permitted or needed.

The completed remediation replaces the mixed directory with three explicit ownership layers.
Public context proxy and payload-scope SPI now lives in `Advanced/Contexts`; reflection-backed
runtime dispatch and immutable outgoing-option adapters live below `Internals`; and the consume
outgoing facade, unavailable sentinel, and pending-fault collector live beside their Core owners.
The old Abstractions `Context` directory is absent. No behavior was replaced by a compatibility
wrapper.

Eight isolated mutations were killed and restored. A final manual reread then found that the
temporary proxy-property mutation had initially been restored against the wrong matching getter;
the swapped correlation/conversation getters were corrected before final compilation. The final
Unit solution passes 5,356 tests with no failures or skips, the complete architecture profile
passes 292 tests, and the 30-test bidirectional Async profile passes against its unchanged semantic
guard hash. Fresh-package validation passes 18 journeys, 31 packages, three isolated provider
consumers, and 30 runtime API assemblies. The deliberate Greenfield API contract contains 19,701
lines with SHA-256 `eeef563f54d8dc551467fa19bda58c69caa2991e4c9e0e6ca0688dcb1f489866`.

Core coverage after direct concrete-sentinel binding is 100% line (136/136) and 100% branch
(20/20) for the three executable Core files in this iteration. The complete product code loaded by
that module measures 70.11% line and 62.69% branch coverage. Abstractions has no coverage-provider
reference, so its 541 passing tests and six isolated behavior/API mutation kills are reported
without inventing a numeric percentage.

## Confirmed iteration-81 Advanced context and source-topology findings

The 31 non-generated files and 2,111 physical lines in
`src/ViciOne.ServiceBus.Abstractions/Advanced/Contexts` were read manually in full. Their owning
tests, direct production callers, all concrete `MessageBody` implementations, and the repository's
project-root layout were then traced before production edits. The Roslyn pairing analyzer inspected
4,262 source files and 1,101 test files; it reports 1,617 name-paired and 2,645 unpaired files. Its
22 nominally unpaired files in this directory include extension methods invoked through instance
syntax, so the classification is a worklist rather than coverage evidence.

1. The apparently duplicated `src/ViciOne.ServiceBus` name is not a second source tree. It is the
   directory of the Core assembly and contains that project's capability folders. Likewise,
   `src/ViciOne.ServiceBus.Abstractions` owns the application and Advanced contract assembly.
   `Persistence`, `Scheduling`, and `Transports` are repository-level implementation-provider
   groups; direct project roots contain first-party product capabilities or tooling. Moving these
   paths solely to remove visual asymmetry would erase a useful architectural distinction and force
   unrelated project-reference churn. The final topology decision remains evidence-led as every
   project is read.
2. `Advanced/Contexts` is cohesive at namespace level but contains four roles: context contracts,
   transport capability payloads, context metadata values, and context extension operations. Its
   flat physical layout currently preserves one public `ViciOne.ServiceBus.Advanced` namespace and
   deterministic type-to-file navigation. Introducing physical subfolders without corresponding
   public namespaces would create a misleading path/namespace mismatch; fragmenting the namespace
   would make the API harder to discover. No cosmetic folder split is justified.
3. The getter extensions named `PartitionKey()` and `RoutingKey()` use noun-shaped method names.
   Greenfield .NET method naming requires verb phrases; `GetPartitionKey()` and `GetRoutingKey()`
   communicate that these are method calls and align with the surrounding `Get*` context API.
4. `SendConsumeContextExtensions` validates message/values/pipe before the extension receiver and
   destination. When several caller inputs are invalid, the exception therefore identifies a later
   parameter rather than the first owned boundary. All ten overloads must validate in declaration
   order and must preserve the exact effective cancellation token through endpoint resolution and
   send.
5. Direct tests do not currently prove all ten consume-scoped send overloads, linked-token
   cancellation from either source, first-invalid-parameter ownership, or zero dependency calls on
   rejected input. Partition/routing capability lookup, set/try-set behavior, and absence behavior
   likewise have no focused contract owner.
6. `ArrayMessageBody(default)` returns empty bytes but throws from its stream and text views because
   the default segment has no backing array. One logical empty body consequently changes meaning by
   accessor. `Base64MessageBody`, `StringMessageBody`, `ActiveMqMessageBody`, and
   `ServiceBusMessageBody` defer or misidentify required-input failures instead of owning their
   constructor parameters. `SqsMessageBody` dereferences a missing native message before it can
   report the public parameter.
7. The public `MessageBody` summary promises repeatable serialized reads, but the mediator's
   length-only implementation deliberately throws from every read operation. This is a real
   contract/capability mismatch, not a documentation problem to conceal. Its final disposition is
   held for the owning Mediator/source-boundary pass because choosing between lazy materialization
   and an explicit readable-body capability affects memory, mutation snapshots, message limits,
   journaling, and public nullability across multiple projects.
8. Several comments are filler, historical repair narratives, or stronger than current behavior:
   the body extensions say they copy data that many bodies return from a retained array; the body
   contract overstates readability; generic context summaries say only "used by the member"; and
   the first consume-send token omits its linkage rule. Every comment in the bounded files must be
   rewritten manually from the code, with the unresolved mediator capability mismatch documented
   honestly rather than papered over.
9. The repository-wide convention of public context interfaces without the .NET `I` prefix is a
   cross-cutting API decision spanning `PipeContext`, send/publish/consume/receive contexts,
   transport handles, provider packages, tests, and documentation. Renaming only this directory
   would make the API less consistent. The full bidirectional type-and-caller inventory remains a
   later dedicated Greenfield migration after all participating owners have been read.
10. `SendContextExtensions.TransferConsumeContextHeaders` uses `GetOrAddPayload` for the originating
    consume context. Reusing a send context can therefore copy current identifiers and headers while
    retaining an older consume-context payload. Replacement is required so metadata and supplemental
    scope always identify the same operation.
11. `ReceiveContextExtensions.GetTimestamp` returns a directly stored `DateTimeOffset` without UTC
    normalization while its string and `DateTime` paths return UTC values. Equal instants therefore
    have accessor-dependent offsets. Every accepted representation must produce a UTC timestamp.
12. Retry metadata readers dereference a null receiver, and the active attempt documentation calls
    a one-based value zero-based. `EmptyHeaders` is a public stateless implementation with a mutable
    field-shaped singleton, permits derivation despite having no extensibility contract, and accepts
    invalid keys unlike every concrete header collection. These are small but observable API
    consistency defects.
13. Following `IObjectDeserializer` into its fully read extension surface found that
    `SerializerContextExtensions` contains generic placeholder documentation and names an
    `IHeaderProvider` parameter `dictionary`. The comments must state the exact lookup, conversion,
    default, and typed-header behavior; the public parameter must be named `headers` in both generic
    overloads and protected by direct boundary evidence.

The completed remediation keeps Core, sibling feature packages, and provider category roots as
distinct ownership boundaries. It renames the two transport-key readers to verb phrases; validates
all consume-send arguments in declaration order; preserves exact single/shared/linked cancellation;
routes runtime messages through the Advanced endpoint; replaces stale consume payloads; normalizes
all receive timestamps to UTC; and makes retry and empty-header semantics explicit. Default and
required-input behavior is now consistent across the reviewed message bodies. All comments in the
31-file owner plus every fully read dependent file were manually checked against the implementation;
no generated comment rewrite was used.

Ten isolated counterchanges were killed and restored: stale consume payload retention, bypass of
the Advanced runtime send, missing UTC normalization, divergent default-array access, missing
empty-header key validation, consume-send validation reordering, swapped partition capability,
missing string-body null validation, missing retry receiver validation, and reversal of the renamed
header-provider parameter. The last counterchange was rejected at compile time by the XML contract
gate before the boundary test could run.

The final Release Unit-solution build has zero warnings and errors, and the complete sequential Unit
solution passes 5,392 tests with no failures or skips, including all 292 architecture tests.
Whitespace and warn-level style verification both return success. Fresh-package validation passes
18 developer journeys, 31 packages, three isolated provider-testing consumers, and 30 runtime API
assemblies. The reviewed 19,701-line packed API contract has SHA-256
`982dc572231657c53b09f70a396f7cdec26ac93fe07401f06eb681da1931a6a1` and reproduces exactly on a
second unchanged package run.

Core-module instrumentation reports 43,449 of 61,986 lines (70.09%) and 14,990 of 23,907 branches
(62.70%) across all product assemblies loaded by that module. This is not mislabeled as whole-suite
coverage: the direct Abstractions project does not currently reference the MTP coverage provider, so
its 574 direct tests are behavior and mutation evidence without a numeric percentage. Whole-suite
merged instrumentation remains a separate repository-wide coverage owner.

The global `src` scan finds no C# preprocessor directives, no empty source directories, and no
dummy, stub, TODO, FIXME, or compatibility-shim markers. Lexical matches for `temporary` describe
real endpoint/entity lifetime semantics, and the sole `NotImplementedException` match is an input
case handled by the technical-failure classifier. The mediator's non-readable measured body and
mutable-array ownership differences remain explicit owning-pass decisions rather than hidden
documentation changes.

The separate internal bidirectional Async Red Team inspected all 4,112 physical production C#
files on a frozen iteration source hash. The 30-test semantic guard and two independent
MSBuildWorkspace scanners report no naming mismatch in either direction, no `async void`, no
conditional-compilation blind spot, and only the deliberately bound `Quartz.IJob.Execute`
third-party interface exception. Its comment axis found a different global debt: after the eight
occurrences in this iteration's fully read files were corrected manually, 696 generic "task that
represents the asynchronous operation" return descriptions and two equivalent "notification
operation" descriptions remain across later, not-yet-read owners. They are not proven
semantically false, but they do not explain the operation-specific completion contract and
therefore are not A+ documentation. They must be removed only during the mandated manual owner-file
reads; automated rewriting is forbidden.

## Confirmed iteration-82 Abstractions serialization findings

All 22 non-generated source files and 1,256 physical lines under
`src/ViciOne.ServiceBus.Abstractions/Serialization` have been read manually in full. The direct
tests, the complete `SerializerContextExtensions` call surface, all concrete `MessageBody`
implementations, and every production `GetBytes` consumer were traced. The Roslyn pairing analyzer
inspected 4,262 source files and 1,106 test files; its nominally unpaired Serialization entries are
not accepted as absence evidence because direct tests exist under contract-oriented names and
internal types are exercised from their owning implementation tests.

1. `CamelCaseDictionaryExtensions` is referenced only by `SerializerContextExtensions` inside the
   Abstractions assembly. Exporting this low-level lookup mechanic enlarges the public ServiceBus API
   without providing an application or Advanced extension point. It must be internal and its
   visibility must be held by a compile-bound test.
2. The helper indexes `key[0]` before validating the public key boundary, producing a null-reference
   or index failure for absent keys and silently accepting whitespace. It also used current-culture
   lowercasing and lowercased only one character. That fails under Turkish culture and does not match
   the JSON camel-case representation of acronym-prefixed property names such as `URLValue`.
3. The sixteen public `SerializerContextExtensions` overloads had boundary-only direct evidence in
   the Core test project. Exact/camel-case lookup, reference/value conversion, conversion failure,
   default preservation, send-versus-consume header semantics, dictionary roundtrip, and object
   projection were not owned by direct behavior tests.
4. `SerializeDictionary` accepts blank metadata keys that its own readers reject, making serialized
   entries unreachable through the public lookup API. `DeserializeDictionary` uses case-insensitive
   `Add`, so duplicate casing throws even though serialization deterministically retains the later
   value. Both directions need one valid-key and last-value rule.
5. The SerializerContext boundary test depends only on Abstractions contracts but lived in
   `ViciOne.ServiceBus.Tests`. Its path and requirement projection therefore named the wrong assembly
   owner. It belongs beside the implementation in `ViciOne.ServiceBus.Abstractions.Tests/Serialization`.
6. `EmptyHeaders` is not a path mismatch: both its declared namespace and its test namespace are
   Serialization. Its placement is retained after inspection rather than changed from a superficial
   semantic guess.
7. `IHeaderProvider`, followed and read in full because it participates in the public overloads,
   still contained generated filler descriptions. The contract is raw, read-only native transport
   header access; its comments must say that precisely.
8. The public `MessageBody.GetBytes()` ownership remains explicitly implementation-defined and the
   concrete types differ between caller-owned copies and retained mutable arrays. This affects
   immutability, repeated reads, allocation, transport integration, and the mediator's length-only
   implementation. A separate read-only Red Team is reviewing the full cross-owner design before a
   public capability decision is made.

The independent read-only `MessageBody` Red Team inspected the public interface, all fourteen
product implementations, direct body readers and producers, and their contract tests. Runtime
probes confirmed that caller mutation can make bytes, streams, and text disagree in
`BytesMessageBody`, `ArrayMessageBody`, `StringMessageBody`, and `MemoryMessageBody`; a
`BinaryData` body can likewise retain caller-owned memory. It also confirmed mixed permissive and
strict UTF-8 behavior, Base64-versus-text ambiguity, mutable first-access sources, and the
Mediator's non-readable length-only implementation. Its A+ disposition is a dedicated atomic
cross-owner iteration: one owned immutable snapshot exposed as `ReadOnlyMemory<byte>`, a newly
opened read-only stream, an explicit transport-text representation, and a bounded materialized
Mediator body unless an allocation/load measurement proves that a capability split is necessary.
Changing only the Abstractions bodies now would preserve the unsafe contract in Core, MessagePack,
Mediator, providers, persistence, scheduling, journal, durable-send, and forwarding paths.

The current remediation internalizes the camel-case helper, matches `JsonNamingPolicy.CamelCase`
under invariant culture and acronym prefixes, rejects missing lookup keys, ignores null-valued
serialization entries, rejects retained entries with missing keys, and uses case-insensitive
last-value semantics in both dictionary directions. Seven valid pre-fix RED cases captured the
old culture, key, acronym, and duplicate behavior. Four additional isolated counterchanges were
killed and restored for API visibility, direct textual consume headers, value-type `TryGetValue`,
and exactly-once object projection; a contradictory `NotNullWhen(true)` mutation was rejected by
the compiler before execution.

## Confirmed iteration-83 message-body findings

The public `MessageBody` contract, every concrete implementation, and all production consumers of
its former byte, stream, and text accessors were traced across Abstractions, Core, Mediator,
MessagePack, Persistence, Scheduling, ActiveMQ, Amazon SQS, Azure Service Bus, Event Hubs,
RabbitMQ, and SQL Transport. Every file changed from that trace was then read manually in full,
including its comments, namespace, type/file relationship, and physical project owner.

1. The former interface did not define byte ownership. Some implementations returned retained
   mutable arrays, some returned fresh arrays, some exposed native-provider storage, and Mediator
   could not return content at all. The same logical body could therefore change after creation or
   behave differently depending on accessor order. The Greenfield contract must own one immutable
   materialized byte snapshot and expose only defensive copies and newly opened read-only streams.
2. Binary payload bytes and text-only transport carriers are separate representations. Treating
   arbitrary binary as UTF-8 silently corrupts payload meaning, while always Base64-encoding a JSON
   text body changes interoperable wire contracts. `TryGetTransportText` now expresses an existing
   lossless text carrier, and `GetRequiredTransportText` defines the strict text-only boundary.
3. `ArrayMessageBody`, `BytesMessageBody`, and `MemoryMessageBody` represented the same binary
   concept with different ownership semantics. One sealed `BinaryMessageBody` removes the
   ambiguity without removing behavior: selected memory, empty bodies, exact bytes, independent
   streams, and non-text capability are directly tested.
4. Lazy JSON and MessagePack bodies retained mutable caller graphs and could serialize more than
   once under concurrent first access. They now materialize a stable serialized snapshot at the
   owning boundary. Tests vary accessor order, mutate the source after construction, access in
   parallel, and verify a single serialization.
5. The Mediator previously retained only a measured length and threw for every body read. This
   violated the public readable-body promise and prevented downstream observers and middleware from
   seeing what would be dispatched. It now performs one bounded canonical JSON materialization
   before dispatch, respects cancellation and configured admission limits, and exposes the same
   stable body contract as transports.
6. Native transport wrappers for NMS, Amazon SQS, and Azure Service Bus could otherwise retain
   mutable SDK objects or memory. They now snapshot native input on construction. SQS distinguishes
   direct payload text from a structurally valid SNS notification envelope; documentation does not
   claim cryptographic validation that the implementation does not perform.
7. SQL persistence must store genuine JSON transport text as text and opaque MessagePack as binary.
   PostgreSQL and SQL Server real-provider tests prove the database representation and exact typed
   roundtrip, including high-bit and null bytes. The common SQL receive body preserves the same
   distinction.
8. Text-backed schedule and outbox records require one canonical reversible carrier for opaque
   bytes. Quartz and classic Entity Framework outbox now persist MessagePack as canonical Base64,
   rehydrate it, and deliver/replay the exact typed payload. Their tests exercise the actual store
   and delivery pipes rather than testing only a helper.
9. ActiveMQ must use a native bytes message for MessagePack on both OpenWire and AMQP. Real Artemis
   acceptance now proves send, publish, and successful forwarding, exact identifiers and content
   type, and a binary native received body. Amazon SQS/SNS LocalStack acceptance proves the opposite
   necessary transformation: binary envelope to canonical text carrier and text carrier back to
   exact binary MessagePack.
10. A successful `ForwardMessagePipe` rebuilds transport metadata, so whole source and destination
    envelopes are not expected to be identical. The correct invariant is an exact typed payload,
    content type, identifiers, and byte ownership together with the expected new forwarding
    metadata. The new test also kills a copy-body counterchange because that invalid implementation
    incorrectly leaves the complete envelope unchanged.
11. `src/ViciOne.ServiceBus` is not a general container for all assemblies. It is the directory of
    the Core assembly and its internal capability folders. Sibling `src/ViciOne.ServiceBus.*`
    directories are independent first-party assemblies; `Persistence`, `Scheduling`, and
    `Transports` group independent provider projects. Moving those projects beneath Core would
    falsely imply Core ownership, complicate project-reference direction, and expose nested source
    files to default SDK globs. The topology is retained, while each genuine filename, namespace,
    and directory mismatch remains subject to the complete owner-file review.

Five deliberately narrow mutations were applied one at a time, compiled, executed against their
owning acceptance, and restored immediately. Copy-body forwarding, UTF-8 persistence of opaque
Quartz bytes, UTF-8 persistence of opaque Entity Framework outbox bytes, ActiveMQ text messages for
MessagePack, and extraction of an SNS wrapper rather than its payload all produced the expected
red result. Final real-provider runs pass ActiveMQ OpenWire/AMQP under `vicione-856b34496390`,
Amazon SQS/SNS under `vicione-1eee9ac4c69b`, PostgreSQL under `vicione-c6477f50991d`, and SQL Server
under `vicione-5a43c63de896`.

The final normal sequential Release Engineering build passes with zero warnings and errors, and
the complete sequential Unit solution passes 5,477 tests with no failures or skips. Both format
verification gates and `git diff --check` pass. Fresh-package validation passes all 18 journeys,
31 packages, three provider-testing consumers, and 30 runtime API assemblies; the reviewed public
API is 19,674 lines with SHA-256
`7841eea6a51d14b0dfbe8062838e5d1cacb10248b55da34ad5add0f6f0cc186d`.

Core-host instrumentation measures 43,447/62,001 lines (70.07%) and 14,967/23,883 branches
(62.67%) across product assemblies loaded by that test host. The separate Quartz coverage host
passes all 216 tests; within it, `ViciOne.ServiceBus.Quartz` measures 98.01% line and 85.36% branch
coverage. The Quartz host's aggregate 32.98% line and 27.09% branch figures are not a useful product
quality headline because it loads many unrelated assemblies without executing their owning tests.
Likewise, the two reports must not be summed or presented as repository-wide coverage. A truthful
whole-suite merged figure requires coverage instrumentation in every test host and deduplication of
overlapping modules, which remains a dedicated repository-wide owner.

## Confirmed iteration-84 MessagePack findings

All fourteen non-generated C# files and 1,345 physical lines in
`src/ViciOne.ServiceBus.MessagePack` were read manually in full. Their comments, primary types,
namespaces, filenames, directory owners, project dependencies, public exports, and directly owning
tests were adjudicated from the implementation. No comment generator or bulk comment rewrite was
used.

1. `MessagePackEnvelope.Message` was typed as `object?` even though every current producer writes
   encoded MessagePack bytes. That type admitted inherited dictionary and inner Base64 string forms
   which no Greenfield ViciOne wire path produces. The envelope now expresses the true `byte[]?`
   invariant and implements the transport-neutral `MessageEnvelope.Message` member explicitly.
2. The old normalization helper interpreted every remaining string as an inner Base64 MessagePack
   payload. Serializer-independent envelope metadata is textual JSON, as in the System.Text.Json
   serializer. Valid JSON object metadata therefore failed with a Base64 `FormatException`. String
   metadata now uses scalar conversion first and shared `ServiceBusMetadataJson` parsing second;
   binary payloads remain MessagePack.
3. Removing the inner Base64 compatibility path does not remove the outer transport carrier.
   `GetMessageBody(string)`, `Base64MessageBody`, and `TryGetTransportText` remain required because
   SQS, Quartz, and other text-backed boundaries must carry the entire opaque MessagePack envelope
   reversibly. The distinction is inner envelope payload versus outer transport representation.
4. Serialized payload constructors and clones previously retained input arrays in some paths.
   Every byte-bearing construction path now clones its input, and supported-message-type transfer
   also creates a new array. Direct adversarial tests mutate caller/source storage and verify both
   value identity and readable roundtrip.
5. `MessagePackSerializerContext.TryGetMessage` caught every exception, including an
   `OperationCanceledException` wrapped by MessagePack after a deserialization callback. That made
   a requested abort indistinguishable from an unsupported or malformed contract. It now unwraps
   and rethrows cancellation with preserved exception information while retaining the documented
   non-throwing result for ordinary decoding failures.
6. Formatter invokers accepted missing delegates, and their cache accepted null, interface,
   abstract, or unrelated types until reflection/expression compilation failed elsewhere. The
   owning constructors now reject each invalid input by exact parameter name before caching or
   compilation.
7. The four public configuration overloads were shape-tested but did not directly prove every
   endpoint/bus serializer/deserializer operation for both `isDefault` states. Dispatch-proxy
   recording now verifies call kind, ordering, flag forwarding, content type, and one shared factory
   for each bidirectional registration.
8. Message-data coverage now includes a malicious wire `nil` in addition to public null/empty
   handles, inline bytes, inline text, and external references. JSON `null`, blank text, null
   declared message types, and every serializer-context constructor parameter are also direct
   behavior boundaries.
9. Quartz already used the transport-neutral JSON metadata deserializer, so the suspected header
   regression was not a product defect. The strengthened real scheduling test nevertheless proves
   application-header preservation alongside canonical Base64 storage, exact binary body replay,
   identifiers, content type, and typed delivery.
10. The source topology is coherent. The two exported package-root types own composition and the
    advanced factory; encoding mechanics are internal under `Serialization`; concrete formatter
    mechanics are under `Serialization/Formatters`. Moving this independent project under the Core
    project directory would misstate assembly ownership and risk SDK default-glob collisions.

The focused native MTP host passes 113 tests. Direct package instrumentation reports a 99.36% line
rate and 97.75% branch rate for `ViciOne.ServiceBus.MessagePack`. Coverage includes one generated
MessagePack resolver class. In handwritten code the two uncovered sequence points follow
non-returning `ExceptionDispatchInfo.Throw` calls; executing them is impossible by contract. The
three partial handwritten conditions are defensive fallbacks around a guaranteed JSON-object
projection and a closed formatter mapping table. They are retained because deleting the guards to
inflate a percentage would reduce failure quality. The host-wide aggregate is deliberately not
reported as product coverage because it loads many dependency assemblies without their owning
test suites.

Six isolated counterchanges were compiled and run one at a time, and each was killed by its owning
test: retained caller payload bytes, removed JSON metadata parsing, omitted bidirectional endpoint
deserialization, an unrelated formatter-cache type, discarded overlay bytes under payload
admission, and removed cancellation propagation. Before remediation, the cancellation test also
produced the expected red result against the original catch-all behavior. Every counterchange was
restored manually before the final gates.

The final sequential Engineering Release build reports zero warnings and errors; the complete Unit
solution passes 5,485/5,485 tests with no skips; both format gates and `git diff --check` pass; and
the locked MessagePack dependency graph includes the coverage collector. Fresh package validation
passes all 18 journeys, 31 packages, three provider-testing consumers, and 30 runtime API
assemblies. The public API remains 19,674 lines with SHA-256
`7841eea6a51d14b0dfbe8062838e5d1cacb10248b55da34ad5add0f6f0cc186d`.

No C# preprocessor directive, empty source directory, dummy implementation, optional Courier/Job
Service product dependency, stale history comment, or file/type/namespace mismatch remains in the
MessagePack owner. Repository lexical matches for “placeholder” are the actual saga schedule
placeholder domain concept; `NotImplemented` is RabbitMQ reply code 540 and
`NotImplementedException` is intentionally classified as a non-retryable input exception. They are
not dummy production behavior.

## Confirmed iteration-92 StateMachineVisualizer findings

All four production C# files and 300 physical lines in
`src/ViciOne.ServiceBus.StateMachineVisualizer` were read manually in full together with every
comment, the project file, all directly owning tests, the requirement projection, and the immutable
state-machine graph contracts consumed from Sagas. No source-comment generator or bulk source
rewrite was used.

The focused baseline passes 25/25 tests. After adding the repository-standard Microsoft Testing
Platform coverage collector to the owning test host, fresh instrumentation measures the Visualizer
assembly at 100% line and 100% branch coverage. The 3.31% aggregate host line rate is not package
coverage because four large dependency assemblies are loaded without their owning tests.

The mandatory Roslyn pairing heuristic classified the two public generator files as paired and the
two internal helpers as unpaired. Manual call-chain review and the 100/100 instrumentation establish
that both helpers are exercised indirectly through both generators; direct filename pairing is not
behavioral evidence.

The package uses `QuikGraph` and `QuikGraph.Graphviz` only to copy an already immutable graph into a
second adjacency representation and serialize a small fixed DOT grammar. It uses none of the graph
algorithms that would justify the dependency. The latest NuGet releases are still 2.5.0 from 2022.
Owning the small deterministic serializers directly removes both packages, their transitive graph
model, event-based formatter wiring, and platform-selected output line endings without changing the
two-type public API or any supported diagram relationship.

Mermaid currently encodes the syntax-sensitive characters exercised by the suite but emits all
other control characters verbatim. Graphviz delegates the same boundary to the third-party
formatter. Both output paths need explicit total label handling, canonical LF documents, invariant
numeric identifiers, and exact tests for otherwise valid node names containing C0 controls.

## Resolved iteration-92 StateMachineVisualizer findings

The final source was manually reread after implementation. `StateMachineGraphProjection` now owns
the only required projection: reference-identity indexing, immutable node observation, and stable
edge grouping by source-node order. The Graphviz and Mermaid generators directly serialize their
small fixed grammars, so the general-purpose QuikGraph and Graphviz formatter dependencies no
longer add value or risk. All original shapes, relationships, typed labels, nested generic/array
names, fault unwrapping, disconnected nodes, and concurrent repeatability remain covered.

Graphviz escapes quotation marks and backslashes, normalizes CR/LF label breaks to DOT newlines,
and visibly escapes every other control or unpaired UTF-16 surrogate. Mermaid entity-encodes its
grammar delimiters, line controls, all remaining C0 controls, and unpaired surrogates while
preserving valid scalar pairs. Both documents use invariant node identifiers and LF only. Public API
shape remains exactly two sealed synchronous generators.

The focused suite passes 29/29 and the package measures 100% line, 100% branch, complexity 125.
Six non-equivalent isolated counterchanges were killed and restored. The pre-remediation dependency
test also failed exactly on QuikGraph/QuikGraph.Graphviz, then passed after removal. Full validation
passes at 5,955/5,955 tests, 292/292 Architecture tests, zero-warning Engineering Release build,
both format gates, three locked restores, two fresh-package consumer gates, and unchanged packed API
hash `34c7a90ef04451531e03134e0891e752a410996742627d4648941427f04aee27`.

The online dependency pass found stable direct updates and exposed a real partial-family downgrade
when only the initially reported top-level Microsoft packages were advanced. Aligning every
centrally pinned Microsoft 10.0 package to 10.0.12 resolved the graph. The fresh-package gate then
identified three isolated consumer projects with explicit 10.0.11 pins; these were aligned and
their locks regenerated. The final inventory contains no outdated direct, vulnerable direct or
transitive, or deprecated direct or transitive package.

## Iteration 95 transport-provider Testing research

The bounded owner is the provider-testing family under
`src/Transports/ViciOne.ServiceBus.AzureServiceBus.Testing`,
`src/Transports/ViciOne.ServiceBus.EventHubs.Testing`, and
`src/Transports/ViciOne.ServiceBus.RabbitMq.Testing`. All 14 original production C# files and all
1,278 physical lines were read manually in full, including every comment, together with all three
project files and their directly owning unit and local-integration tests. These are independent
provider assemblies and therefore remain grouped under `src/Transports`; they are not children of
the Core assembly directory `src/ViciOne.ServiceBus`.

The native MTP baselines pass 66/66 Azure Service Bus unit tests, 197/197 RabbitMQ unit tests, and
3/3 focused Event Hubs producer-resolution tests. The three owning test hosts lacked the repository
coverage extension. After adding the standard locked Microsoft code-coverage dependency, direct
instrumentation measures Azure Service Bus Testing at 64.97% line and 63.04% branch, RabbitMQ
Testing at 36.39% line and 40.00% branch, and Event Hubs Testing at 100% line and 100% branch.

The mandatory Roslyn source-pairing heuristic reports the public harness and registration files as
paired. It cannot credit internal types reached indirectly through the public harness or dependency
injection; instrumentation confirms those indirect paths but also identifies real hosted-service
and provider-configuration gaps. This static pairing result is not line- or branch-coverage proof.

The RabbitMQ direct harness currently permits destructive cleanup of the root virtual host without
the explicit opt-in required by its dependency-injection counterpart. It also constructs a bus
before startup cleanup, so a cleanup failure can leave a newly created bus outside the base
harness's failed-start rollback. The hosted path encodes management credentials as ASCII while the
direct path correctly uses UTF-8. These are production defects, not coverage-only concerns.

The Azure Service Bus cleanup cancellation comment mentions a nonexistent retry delay. Its direct
harness configuration and input-address lifecycle are not behavior-tested, and the hosted-service
cleanup branch has no isolated test seam. Both provider harnesses intentionally inherit the
template-method model of `BusTestHarness`; their provider events and protected extension points are
real customization capabilities rather than legacy aliases, so they remain unless a complete
replacement can preserve those capabilities.

## Iteration 96 analyzer-toolchain research

The bounded owner is `src/ViciOne.ServiceBus.Analyzers`,
`src/ViciOne.ServiceBus.Analyzers.CodeFixes`, and
`src/ViciOne.ServiceBus.Analyzers.Package`. All 17 original C# files and their comments were read
manually in full, as were the three project files, shipped/unshipped rule records, two old NuGet
scripts, owning tests, and requirement manifests. The physical tree was also checked as a whole.
`src/ViciOne.ServiceBus` is the Core project directory, not a product-wide container. Independent
capability assemblies therefore remain its siblings; external integrations remain grouped by
`Persistence`, `Scheduling`, and `Transports`. Nesting those projects under Core would misstate
ownership and expose their sources to the SDK project's recursive compile glob.

The unchanged baselines pass 126 analyzer tests and 32 code-fix tests. Initial focused coverage is
88.1356% line and 71.5360% branch for the analyzer assembly at complexity 953, and 92.8571% line
and 72.3881% branch for CodeFixes at complexity 140. Static source-to-test pairing identifies the
two internal conversion helpers as filename-unpaired, but runtime coverage proves their indirect
execution through public analyzers; filename pairing is not behavioral evidence.

Manual analysis found incomplete producer-family registration, magic generic-carrier indices,
unobserved `ConfigureAwait` and discard forms, nonterminating recursive structural paths, missing
concrete-interface conversion, overbroad property selection, an infinite loop for unsupported
value-type `MessageData<T>`, incomplete diagnostic descriptions, incomplete blocking primitive and
configuration-write handling, a leaked public helper surface, a mismatched CodeFix namespace, an
obsolete nullability polyfill, and legacy NuGet install/uninstall scripts. Coverage hotspots agreed
with these findings but did not discover them on their own.

After the first remediation and full gates, the final manual reread found two further exact defects:
a public property with a private getter was treated as a serialized contract member, and timed
`Monitor.TryEnter`/`SpinLock.TryEnter` calls were not classified as blocking. Tests were added first
and failed 2 of 164 cases with the exact unexpected missing-property diagnostic and a diagnostic
count of 9 instead of 11. Public-getter filtering and timeout-parameter recognition then made the
same 164 cases pass. A third adversarial test proved that inherited concrete consume contexts were
already correctly excluded from self-token recommendations, so no speculative source change was
made there.

Direct Roslyn dependencies are on the current coherent 5.9.0 family. Older transitive System and
Humanizer packages are owned by that compiler dependency graph and are not overridden without an
upstream-supported combination. Packaging outside the sandbox succeeds in seconds and the package
has exactly the modern Roslyn asset layout with no `tools`, `lib`, or compatibility scripts.

Sandbox behavior is causal rather than a source defect. An authoritative coverage attempt inside
the sandbox exited 134 with `SocketException (13): Permission denied` while Microsoft Testing
Platform created its named-pipe server. Earlier `pack` attempts could wait with no live MSBuild
child. Repeating the same commands outside the sandbox succeeds. This is the retained diagnostic
rule for future iterations: first confirm the active process and error; for MTP/Roslyn IPC, NuGet,
restore, pack, format, or full-repository gates, use the approved outside-sandbox execution rather
than changing source or tests to accommodate the environment.

## Iteration 97 Saga owner research

`ViciOne.ServiceBus.Sagas` is the next independent source owner without a complete manual A+
acceptance. It is correctly a sibling of the Core project under `src`: Core references neither the
Saga assembly nor its optional feature surface, while the Saga assembly references Core. Moving
the project below `src/ViciOne.ServiceBus` would invert that physical ownership and expose its files
to Core's recursive SDK compile glob. The internal directory vocabulary remains under review:
`Sagas`, `Saga`, and `SagaStateMachine` currently express overlapping concepts and cannot be
accepted merely because their assembly placement is correct.

The baseline contains 357 C# files, 32,433 lines, 9,008 comment lines, no preprocessor directives,
and 30 direct Saga/State-Machine test files with 101 declared test methods (147 executed cases).
The complete unchanged Core host passes 3,256 tests. Microsoft Testing Platform coverage must run
outside the sandbox because its named-pipe server is denied there; the same command outside the
sandbox passes and writes the requested Cobertura artifact.

Package instrumentation reports 60.8131% line coverage, 52.8113% branch coverage, complexity
2,947, and 2,525 methods. The leading genuine risks are the uncovered DI registration paths and
`SubState` API (CRAP 210 each), partially covered state-machine dispatch (CRAP 204.79), uncovered
classic saga registration and missing-instance redelivery (CRAP 156 each), and uncovered schedule
fault/execution paths (CRAP 72-110). Nine transition-classifier methods each score 42. Static
filename pairing reports 269 unpaired source files; that heuristic is an index, not proof, because
integration tests execute many internal collaborators through public behavior.

The first manually read cohorts already show why a full pass is required. Several public comments
are generic or grammatically stale, `RequestState` describes implementation fields imprecisely,
dependency-injection repositories expose implementation-oriented public types without clear null
boundaries, `MissingInstanceRedeliveryPipe.Probe` and both send-pipe probes emit no diagnostics,
and a repository fallback is literally named `NotImplementedSagaRepositoryContextFactory`.
These are candidate findings until their callers, tests, package surface, and replacement semantics
have been read; none will be removed from a marker scan alone.

### Iteration 97 final findings and disposition

All 357 baseline production C# files and their comments were read manually in full. The three new
types introduced by the remediation were then read with their complete callers and tests; no source
or comment generator was used. The final owner contains 359 C# files and 32,622 physical lines.
The external assembly placement is intentional: `src/ViciOne.ServiceBus` owns only Core, optional
first-party capabilities are sibling projects, and external integrations are grouped under
`Persistence`, `Scheduling`, and `Transports`. Within the Saga assembly, `Sagas` is the public
domain/configuration surface, `Saga` owns repository execution contexts, and `SagaStateMachine`
owns state-machine implementation. Those responsibilities and their dependency direction are
distinct despite the related names, so collapsing them would reduce navigation accuracy.

The repository API advertised capabilities it could not always execute. Its optional load/query
factories silently became throwing stand-ins, dependency injection registered dispatch services
that failed by design, and the default registration provider accepted a saga without selecting
persistence. This was compatibility behavior rather than a valid Greenfield contract. The public
`SagaRepository<TSaga>` is now a sealed dispatch-only repository. `CreateLoadable` and
`CreateQueryable` return explicit `ILoadableSagaRepository<TSaga>` and
`IQueryableSagaRepository<TSaga>` capability contracts, and Azure Table, DynamoDB, and Entity
Framework callers use the capability they actually provide. The temporary, unsupported, and no-op
repository implementations are gone. Registration without a persistence provider fails during
configuration with the saga identity; in-memory registration remains explicit for production and
is selected explicitly by the test harness.

Missing-instance redelivery was configuration-shaped but did not provide a complete observable
runtime contract. Its configurator and pipe are now internal sealed implementation types with null
and policy validation, retry-observer lifetime ownership, diagnostic probing, exact terminal-pipe
execution, and real scheduled redelivery carrying correlation, transport headers, serializer,
message identity, and the consume cancellation token. Faulted schedule activities now persist the
issued schedule token and pass the exact saga-operation token when canceling either supported
message form.

State-machine cancellation could be delayed, wrapped, observed as a fault, or replaced by
telemetry cleanup. Completion checks, event raising, nested scheduling, state finalization,
message filtering, transitions, behaviors, and exception traversal now preserve cooperative
cancellation and its exact token. Observer fault callbacks are bypassed for cancellation, while
fault telemetry still completes independently without replacing the primary outcome. Required
dependencies are checked at their public or internal construction boundary, and implementation
types without a consumer-facing construction purpose are internal and sealed. The full comment
reread removed generic or stale descriptions and aligned retained prose with current behavior.

Nineteen new requirement-mapped cases cover four missing-instance redelivery outcomes, nine
repository capability/configuration boundaries, four cancellation paths, and two faulted-schedule
cancellation paths. They contain exact state, identity, token, delay, header, scheduler, callback,
exception, and DI graph assertions. A bounded assertion and anti-pattern audit found no sleep,
wall-clock dependency, blocking wait, unawaited task, trivial assertion, swallowed exception,
skip, mutable shared fixture, or assertion-free behavioral case. The sole caught reflection wrapper
is rethrown through `ExceptionDispatchInfo`, preserving the original exception and stack.

Five isolated non-equivalent source counterchanges were each compiled and killed by their exact
test before manual restoration: replacing the selected missing-instance delay with zero, dropping
each of the two faulted-schedule cancellation tokens independently, bypassing the pre-canceled
completion check, and permitting saga registration without an explicit repository. Their observed
failures were respectively the exact delay, exact token identity, cancellation terminality, and
configuration exception.

Fresh accepted full-host instrumentation raises Saga package coverage from 60.8131% to 62.4669%
line and from 52.8113% to 54.4440% branch coverage. Complexity is 2,963 across 2,523 methods, and
methods above CRAP 30 fall from 18 to 15. The remaining scores are dominated by broad declarative
state-machine/DI composition entry points and generated branches; they remain visible in the
source-wide completion audit rather than being misreported as absent. The coverage artifact is
`/private/tmp/vsb-iteration97-sagas-final-fullhost.cobertura.xml` with SHA-256
`f8c75157e348ed859c2dda67460ccfc4a9bfc6045b1d747800a40e151d8450b3`.

Fresh package/API validation builds 31 packages, executes 18 developer journeys and three isolated
provider consumers, and validates all 30 runtime assemblies. The intentional API change removes
implementation types and unsupported capabilities while adding the two explicit composite
capability contracts. The 19,029-line packed contract has SHA-256
`6870002dc25251fe785d4e0bbd51a0f66c15ce533a3be92beb78224a2fa28486`.
The requirements JSON, repository-owner preprocessor and dummy-marker scans, empty-directory scan,
Git whitespace check, and full whitespace-format gate pass. A Saga-only optional info-level style
scan reports 660 suggestions but no warning or error: 323 namespace/folder suggestions conflict
with the intentional cross-assembly API namespace model, while 162 primary-constructor and 128
other expression/style suggestions are non-semantic preferences. The remaining 47 interface-name
findings expose a real Greenfield API decision inherited from the former DSL (`State`, `Event`,
`BehaviorContext`, and related contracts). They are explicitly carried into the next Saga API
naming iteration rather than bulk-renamed without consumer, comment, and package evidence. The
strict build and final complete Unit/Architecture results are recorded in the iteration completion
evidence.

## Iteration 98 Saga interface and documentation research

The remotely secured Iteration 97 state passes its complete gates, but the explicit info-level
style audit exposes 47 unprefixed interface declarations across the Saga owner. The red-first
`SagaInterfaces_UseTheDotNetInterfacePrefix` architecture test independently parses every Saga
source file and reports the same 47 identities, including three nested internal interfaces. It is
requirement-mapped and fails before remediation with the exact path, line, and type name.

Microsoft's current C# identifier guidance states that interface names start with a capital `I`,
and the Framework Design Guidelines use the stronger `DO` language. CA1715 identifies an
unprefixed externally visible interface as the precise cause and classifies renaming as a breaking
fix. This repository is a Greenfield fork with no old API compatibility obligation, so retaining
the names solely because the former state-machine and messaging DSL used them would contradict the
goal. Message contracts, fluent descriptors, contexts, settings, visitors, binders, repository
contexts, and nested strategy interfaces are all interfaces to a C# consumer and use the same rule.

A repository-wide syntax inventory finds 369 unprefixed interface declarations, confirming that
the Saga result is part of a broader inherited API pattern rather than an isolated formatter quirk.
Iteration 98 is deliberately bounded to all 47 Saga declarations and their complete cross-project
consumer closure. Subsequent owner iterations and the final source-wide audit must apply or
explicitly redesign the remaining contracts; a Saga-only fix is not represented as repository-wide
completion.

The Saga interfaces are spread across 33 identities and approximately 6,000 source/test/sample
references. The high-volume identities (`Event`, `State`, `SagaStateMachineInstance`,
`BehaviorContext`, and `CorrelatedBy`) cannot be safely changed by textual replacement because they
collide with ordinary vocabulary and framework concepts. A symbol-aware rename is acceptable only
as a reference-preserving refactoring after manual type review. It is not permitted to generate or
rewrite documentation: comments are reread and authored manually, and the complete diff is reviewed
before compilation.

The completed rename closes all 47 Saga violations without an unrelated public-contract delta.
The permanent architecture test passes and kills a deliberate `ICorrelatedBy` prefix mutation. All
final builds, 6,234 Unit/Architecture tests, 293 direct architecture cases, package journeys,
formatting, and hygiene checks pass. The packed API has 19,029 lines and SHA-256
`1a4fdef247c3b4ece1e5b8fed35dbe533f8409541a4aedf3be2dce9be8891be6`.

The user's source-tree question exposes a documentation concern but not a structural defect.
`src/ViciOne.ServiceBus` is the Core SDK project and recursively compiles its own subtree. Moving
independently packaged Sagas, Courier, Mediator, Futures, JobService, or Testing below it would
misstate ownership and risk compile-item overlap. Persistence, scheduling, and transport projects
are different: they are cohesive external provider/integration families and therefore benefit from
family directories. This is consistent with the project graph, package surface, architecture
requirements, and `PO-2026-09-08-01`'s cohesion-and-owner rule.

## Iteration 99 Initializers owner research

The complete Initializers owner contains eight C# files and 598 lines. Six files expose thin
advanced send, publish, request, and schedule overloads; their public APIs already validate required
inputs or delegate to validated runtime dispatchers. Existing tests assert every overload's exact
values, pipe, timeout, cancellation token, capability failure, and required-input behavior.

`IdVariable` and `TimestampVariable` each contain a private, one-implementation interface used only
as the payload-cache key for `InitializeContext.GetOrAddPayload`. The indirection adds no substitutable
behavior and leaves the only two unprefixed interfaces in the project. A sealed nested context type
provides the same distinct cache identity more directly. The current ID test proves multiple ID
variables share one context value, but the equivalent timestamp invariant is only implicit. One
direct test should cover both identities with deliberately different explicit values.

The Roslyn static pairing report classifies all eight source files as paired. Extension-method
pairing and reflection remain known heuristic limitations, so this result does not replace the
manual test review, runtime coverage, or mutation checks. The directory and namespaces express an
intentional split between public advanced facade extensions and internal initializer variable
types; no project relocation or public API expansion is indicated.

The completed review found no behavior or comment defect in the six advanced facade files. Their
forwarding, timeout, cancellation, pipe, required-input, and unsupported-capability contracts are
already covered by direct assertions. The two variable files contained needless private interfaces
used only for distinct payload-cache identities and an inaccurate copied local name. Sealed nested
context types preserve the cache identity without suggesting polymorphism, and a new combined test
proves that distinct explicit ID and timestamp variables share the first value within one
initialization context.

A deliberate default-timestamp mutation survived the pre-existing suite, exposing a real gap. The
default constructor now delegates to a public `TimeProvider` overload, enabling deterministic
verification of the selected clock while preserving the system-clock convenience. Null clock,
captured UTC value, UTC offset, and exact supplied-time behavior are asserted. The final manual
pseudo-mutation audit kills four of four substantive counterchanges: removing either cache reuse,
returning the default timestamp, and ignoring the supplied clock. The 20 directly reviewed tests
contain no assertion-free, trivial-only, self-referential, blocking, skipped, fixed-delay, or
mutable-shared-fixture behavior. The one intentional system-clock assertion uses a bounded interval;
exact time semantics use the deterministic provider.

Both new architecture rules were demonstrated red first. Interface naming reported exactly
`IdContext` and `TimestampContext`; namespace navigation reported both variable files plus the
evaluated root namespace. Both are green after the remediation. A final static scan finds no
unprefixed Initializers interface, preprocessor directive, dummy marker, TODO/HACK marker,
`NotImplementedException`, temporary marker, or empty owner directory.

Fresh full-host coverage passes 3,278/3,278 and reports 75.3322% line and 68.0491% branch overall.
`ViciOne.ServiceBus.Initializers` itself reports 100% line and 100% branch coverage with complexity
13. The artifact is `/private/tmp/vsb-iteration99-initializers-final.cobertura.xml`, SHA-256
`039bbdeeb0bed44ea4b49d9ea5310edb136b0e0352b57552b26b310aeed2dd8f`.

The complete serial Unit/Architecture solution passes 6,239/6,239 with no failures or skips,
including 295 architecture and 3,278 Core-host cases. The Engineering Release build passes all 77
projects without warning or error. Both complete format gates, requirements JSON parsing, Git
whitespace, source hygiene, and owner style audit pass. Fresh package validation passes 18 journeys,
31 packages, three isolated provider-testing consumers, and all 30 runtime API assemblies. The
intentional `TimeProvider` constructor is the only packed-contract addition; the 19,030-line
contract has SHA-256 `a31b98d00ab15941a47bef08aa05447a24db4e445aba00d0838a0686ca85aa0a`.

## Iteration 100 Futures interface research

The complete Futures source was manually read in Iteration 88 and remains at 55 files. A current
syntax inventory finds exactly one unprefixed interface: the public empty correlated message
contract `Get<TFuture>`. Its only product consumer is the public protected `ResultRequested` event
on `Future<TCommand, TState>`, so the contract is behaviorally small but part of the packed public
API. The name describes a command, not a method, yet it is still an interface exposed to C# callers;
the .NET interface rule therefore applies just as it did to the Saga event and message contracts.

No compatibility alias is appropriate in this permanent Greenfield fork. Renaming the declaration,
file, and event generic argument to `IGet<TFuture>` preserves generic arity, correlation inheritance,
constraint, state-machine behavior, and feature availability while making the contract immediately
recognizable as an interface.

The user's source-tree question prompted a second structural check rather than relocation beneath
Core. Project directories express assembly and package ownership; source subdirectories express
namespace and responsibility. `ViciOne.ServiceBus.Futures` is therefore correctly a sibling project,
but its former root-level `ViciOne.ServiceBus.Futures` declarations and nested folders did not mirror
their namespaces. Declaring `ViciOne.ServiceBus` as `RootNamespace` and moving those declarations
beneath `Futures/` resolves all 23 IDE0130 findings without changing namespaces or binaries.

Thirty additional optional expression findings were reviewed and applied manually. One IDE0290
suggestion remains intentionally informational on the public convenience future definition: an
explicit documented constructor is clearer at that framework boundary, and primary-constructor
syntax would be cosmetic. There are no style warnings or errors.

The first full test execution discovered that a harness test asserted observer counters after the
handler task, although post-operation observer callbacks may legally finish later. Awaitable
milestones for consume, publish, and send observations eliminate that scheduling race. The next
complete run then found an architecture test with a hard-coded path to the old Futures layout; its
current path was corrected. The final Unit/Architecture solution passes all 6,241 tests.

The isolated correlation-inheritance counterchange fails compilation in six required correlation
and topology call sites. Fresh coverage reports 75.3393% line and 68.0696% branch overall, and
90.4990% line and 85.4839% branch for Futures. The final package gate passes all 18 journeys, 31
packages, three isolated provider-testing consumers, and 30 runtime APIs. The 19,030-line contract
hash is `a96d93cc091d97174baceb59fe5230228734c2b98a676431521e70a329cce9fa`.

## Iteration 101 JobService API and navigation research

The pre-change JobService analyzer reports 210 informational findings: 122 namespace/path
mismatches, 45 unprefixed interface names, 14 primary-constructor suggestions, and 29 smaller
expression/style suggestions. There are no warning or error diagnostics. The missing explicit root
namespace causes Roslyn to infer `ViciOne.ServiceBus.JobService`, although the project intentionally
publishes contracts in nine namespace branches rooted at `ViciOne.ServiceBus`.

The 45 naming findings are 40 public message/execution interfaces and five internal runtime or
configuration interfaces. All 41 affected files, 971 physical lines, signatures, attributes,
inheritance relationships, and comments were reread before editing. They describe current behavior
accurately. Because each declaration is a C# interface, message-contract vocabulary does not justify
an exception to the .NET `I` prefix rule. The project is Greenfield and has no legacy compatibility
obligation, so aliases would only perpetuate the ambiguity.

The package remains correctly placed at `src/ViciOne.ServiceBus.JobService`: it is an independent
first-party capability assembly, whereas `src/ViciOne.ServiceBus` owns Core. Namespace-relative
folders inside the project are a separate concern and can be aligned without changing assembly,
package, public namespace, or feature ownership.

All 170 JobService production files now follow that ownership model. Their paths mirror nine
namespace branches relative to `ViciOne.ServiceBus`; the two JobService-specific exceptions belong
under `JobService/`, and the project root contains only `GlobalUsings.cs` plus project
infrastructure. Thirteen obsolete empty directories were removed. Static configuration and
background-work ownership evidence now names the current paths. Every renamed declaration and all
comments in the 41 affected interface files were reread manually; the source and comments were not
generated.

All 45 declarations and their consumers now use the `I` prefix without compatibility aliases.
Symbol-aware rename support was limited to references after manual classification and was disabled
for comments and string literals. Generic arity, variance, inheritance, attributes, correlation,
message initialization, state transitions, serialization, and consumer behavior remain intact.
Fourteen primary constructors and 29 expression-level cleanups were then assessed and applied
manually. The final JobService info-severity analyzer report is empty. Cancellation propagation was
also corrected for `_jobCompletions.CompletedAsync(cancellationToken)`.

Both new architecture rules were proved red first: one reported all 45 old interface declarations,
and the other reported the former namespace/path divergence. The full architecture host then found
two useful adjacent defects: the two root exceptions violated the stronger existing root policy,
and the documentation rule did not yet understand class and struct primary-constructor parameters.
Both were corrected and the final architecture host passes 299/299.

The two new tests contain two meaningful collection assertions. Neither is assertion-free,
trivial, self-referential, skipped, fixed-delay based, or dependent on mutable shared fixtures; a
single assertion category is appropriate for these repository-wide invariants. Four substantive
observed counterchanges were killed: the 45 old interface identities, the old namespace/path
layout, JobService exceptions at the project root, and undocumented primary-constructor parameters.
Equivalent expression and primary-constructor rewrites were not counted as mutations.

Fresh coverage passes 3,278/3,278 and records 75.3255% line and 68.0424% branch overall.
`ViciOne.ServiceBus.JobService` records 95.6189% line and 89.7257% branch with complexity 2,384. The
artifact is `/private/tmp/vsb-iteration101-jobservice-final.cobertura.xml`, SHA-256
`13c11b36fd86a504a27f5db3f25d6c1e5e91bfe9b50d47df7648e4e476d99f7e`.

The final serial Unit/Architecture solution passes 6,243/6,243 with no failure or skip. The
Engineering Release build passes all 77 projects with zero warnings and errors, and both complete
format gates pass. Fresh package verification passes 18 journeys, 31 packages, three isolated
provider-testing consumers, and 30 runtime APIs. The 19,030-line packed contract has SHA-256
`f12d21461b1d4403c5ebed180f1c00d43a9a24b672745e0d3739452600091423`.

Build diagnosis was kept reproducible. The first targeted parallel MSBuild invocation ran for more
than five minutes and returned exit code 1 despite reporting zero warnings and errors. Process
inspection showed active build nodes rather than a compile diagnostic. Using an isolated CLI home,
`MSBUILDDISABLENODEREUSE=1`, serial `-m:1`, and `--no-dependencies` for the target project produced
the real result in about four seconds. Complete solution builds remain serial but intentionally do
not use `--no-incremental`, as required by this repository. MTP tests and Roslyn format verification
run outside the filesystem sandbox because their named-pipe servers otherwise fail with
`SocketException (13): Permission denied`.

## Iteration 102 Courier interface and navigation research

The post-Iteration-101 source inventory identifies Courier as the next coherent owner. Its complete
135-file implementation and comments were manually read in Iteration 87; current inspection finds
16 unprefixed interfaces: the advanced `CourierContext` facade and 15 immutable routing-slip wire
contracts. Each is a real C# interface, so its message-contract role does not justify an exception
to the .NET naming convention. The project currently relies on an inferred
`ViciOne.ServiceBus.Courier` root namespace despite publishing types across 13 namespace branches.

Courier remains correctly located at `src/ViciOne.ServiceBus.Courier` as an independent capability
assembly. The Greenfield correction is internal to that project: declare the common
`ViciOne.ServiceBus` root namespace and make physical folders mirror the already established
namespaces. This changes navigation, filenames, and the 16 interface identities, but not package
ownership or routing-slip behavior. No compatibility alias is appropriate.

The current Courier inventory contains 137 production C# files. The project boundary is deliberate:
`src/ViciOne.ServiceBus.Courier` is an independently shipped capability assembly, not a child of
the Core project's physical folder. Within that project, source paths now mirror namespaces
relative to the explicit `ViciOne.ServiceBus` root. The project root contains only infrastructure
and the three exception types whose namespace is exactly `ViciOne.ServiceBus`; Courier-domain types
live below `Courier/` and the remaining namespaces use their matching top-level branches. Empty
legacy directories were removed.

All 16 unprefixed interface declarations, their consumers, filenames, serialization identities,
attributes, inheritance, generic constraints, and comments were inspected before the rename. A
symbol-aware rename changed references after that classification but did not generate source or
comments. Exact fully qualified scans find none of the old identities, and the packed API multiset
contains only the intended 16 removals and 16 additions.

The registration implementation contained unused internal generic and runtime overload chains;
removing them reduced dead surface without changing a public feature. Decomposing activity scanning
into compensatable and execute-only paths made definition ownership explicit. Manual inspection
also found that both `AddExecuteActivity<TActivity,TArguments>` and its runtime-type counterpart
accepted `IActivity<TArguments,TLog>` implementations, silently discarding their compensation
capability. Both APIs now reject that misuse and direct callers to `AddActivity`. Removing the two
guards in an isolated mutation caused exactly the generic and runtime rejection tests to fail; the
restored guards pass.

Coverage initially exposed two dead registration methods at CRAP 272 and two complex host state
machines above CRAP 30. Dead overload removal and separation of host lifecycle notification from
activity result evaluation reduced Courier complexity from 1,009 to 981. New requirement-mapped
tests verify constructors, `SendAsync` null boundaries, probe metadata, and optional compensation
addresses. Final coverage passes 3,283 tests and records 78.3472% line and 70.6344% branch across
the Core host's 36-project reachability closure. Courier records 88.6212% line, 75.3304% branch,
617 methods, and zero methods above CRAP 30. The accepted Cobertura artifact is
`/private/tmp/vsb-iteration102-courier-final6.cobertura.xml`, SHA-256
`5aed13e5db9cbe785a3edc46ad2456737299a65c32c88283fcdddf40ea9e7cdf`.

The first post-change full run exposed an unrelated deterministic test race: a reliable-inbox test
configured its inactivity interval to the same 30-second value as its outer wait. The two timers
could expire in either order. Retaining the 30-second assertion timeout while using the harness's
short inactivity default reduced the isolated case from about 31 seconds to about 3 seconds; three
consecutive isolated runs and every subsequent complete run pass. A later full run also caught
duplicate requirement-variant metadata on the newly added registration tests; unique variants and
the canonical projection entries now pass the projection gate.

The final serial Engineering build covers all 77 projects with zero warnings and errors. Both full
Roslyn format gates pass with zero changes. All 23 native hermetic test hosts pass 6,250/6,250 with
no failures or skips, including 301 architecture and 3,283 Core tests. Package verification passes
18 developer journeys, 31 fresh packages, three isolated provider-testing consumers, and 30
runtime API assemblies in both update and comparison mode. The 19,030-line packed contract SHA-256
is `b81db7838a57f4205d2c10687643a8ce8853f85c6de4a96b6a07f843631b7d51`.

Repository-wide C# preprocessor, exact old-identity, real `NotImplementedException` throw,
empty-directory, requirements JSON, and Git whitespace scans are clean. The sole textual
`NotImplementedException` occurrence is the intentional technical-failure classification that
marks such application exceptions non-retryable; it is executable policy, not a dummy. Roslyn
format requires running outside the filesystem sandbox because its build host opens a named pipe;
fresh package consumers require network access outside the sandbox for NuGet. The protected
`review/` and `TestResults/` trees were neither modified nor staged.

## Iteration 103 Mediator API and navigation research

Mediator is an independently shipped capability assembly and therefore remains a sibling of the
Core project directly below `src`; `src/ViciOne.ServiceBus` is the Core project boundary, not an
umbrella directory. The same rule explains the repository's provider and integration families:
cohesive persistence, scheduling, and transport assemblies are grouped beneath `Persistence/`,
`Scheduling/`, and `Transports/`, while standalone capability assemblies remain direct siblings.
Within every project, folders mirror the namespace below an explicit common root.

All 28 Mediator production files and 2,730 source lines were read manually. No source or comment
generator was used. The project now declares `ViciOne.ServiceBus` as its root namespace. Public
mediator contracts and implementation live below `Mediator/`; their contexts and runtime are below
`Mediator/Contexts` and `Mediator/Runtime`; configuration implementations live in `Configuration/`;
and the conventional Microsoft service-collection entry point lives in `DependencyInjection/`.
Two architecture tests fail red against the old tree and enforce the final interface and path rules.

Public API review found three coherent Greenfield defects. The explicit address overload accepted a
null address even though a separate default-address overload exists. Both dependency-injection
callbacks were optional even though the mandatory message limits can only be declared through that
callback, allowing registration of an unusable mediator. Finally, direct `Limits` returned void while
the registration form was fluent. The repaired overloads validate all required arguments before
changing the service collection, both callbacks are required, and both limit forms return their
input contract. The exact packed API diff contains only those three intentional signature changes.

Coverage exposed four tests whose static message types accidentally selected generic overloads
instead of the intended runtime-object-plus-pipe overloads. Explicit object dispatch now proves the
real paths. All consume-pipe option and request-pipe connector forms are exercised through the
scoped mediator, runtime mediator, and client-factory context. The focused suite passes 91 tests.
Fresh full-host instrumentation passes 3,291 tests and records 75.4858% line and 68.1517% branch
coverage across the Core host. Mediator records 90.7182% line and 77.5974% branch coverage,
complexity 322 across 268 methods, and no CRAP score above 30. Its remaining uncovered methods are
internal registration delegations and transport-context projections, not unexecuted public
Mediator entry-point implementations. The accepted artifact SHA-256 is
`61f6f26fee246a7dfacd7388f78e56a65f3889d3816458cd3facef63af084cc7`.

The first sandboxed test-project compile again stalled without a compiler diagnostic. Process
inspection showed an active build node; terminating that exact process and rerunning outside the
filesystem sandbox with disabled build servers produced the authoritative zero-warning build. A
solution-level `dotnet test` attempt forwarded the VSTest `--logger` argument to native MTP hosts and
reported zero selected tests. Direct execution of all 23 built native hosts is the correct path.
One subsequent Core run reported one transient failure before a retained log existed; five complete
3,291-test repetitions after it passed, including the final all-host matrix.

The final Engineering build passes 77 projects with zero warnings or errors, both full format gates
make no changes, and all 23 native hosts pass 6,260 tests without failure or skip. Package/API
validation passes in update and comparison mode with 18 journeys, 31 packages, three isolated
provider-testing consumers, and 30 runtime assemblies. The final 19,030-line contract SHA-256 is
`9f0d543184d729768ba0606420ca05d005c6e1bd1961bfeda472600d18985345`. Requirements,
directives, dummy markers, old identities, empty directories, and Git whitespace are clean; protected
review and test-result trees remain untouched.

## Iteration 107 Core Advanced API and ownership research

`src/ViciOne.ServiceBus` is the physical boundary of the Core assembly, not an umbrella directory.
Moving independent projects below it would misstate assembly ownership and make the Core SDK
project's recursive compile ownership unsafe. Standalone capability projects therefore remain
siblings directly below `src`, while the related external adapters are coherently grouped by
provider family below `Persistence/`, `Scheduling/`, and `Transports/`. Inside Core, the 80-file
Advanced owner already has the correct physical branches for `ViciOne.ServiceBus.Advanced`,
`Advanced.Middleware`, `Advanced.Registration`, and `Advanced.Serialization`; the partitioning
implementation folders intentionally belong to the flattened middleware namespace.

Every Advanced file, all 5,803 production lines, and every comment were read manually. No source or
comment generator was used. The remaining unprefixed public interface was `TransactionContext`.
It is now `ITransactionContext` throughout Core, Courier, Sagas, JobService, internal test access,
tests, its filename, and the packed API, without retaining a Greenfield compatibility alias. A
permanent architecture rule enforces the interface convention. Nullable-flow metadata now states
that successful consumer-kind dispatch always returns a dispatcher, and all implementations agree.

Behavior review corrected three cancellation and lifecycle details. Bus start and stop translate a
linked-token cancellation back to the caller's original token identity. Supervisor agent creation
now gives both its completion source and direct caller the same usable cancellation token, including
the tokenless-supervisor fallback. A focused deterministic test exposed that the original rethrow
could race and deliver a tokenless exception; the repaired behavior passed 20 isolated repetitions.
Log-context configuration now binds metrics to an already usable current context without replacing
its identity or logger. JSON conversion checks explicit mappings before its interface convention,
so deliberately mapped abstract base contracts materialize correctly.

Diagnostic redaction was decomposed into length bounding, unsafe-character detection, and
sanitization. Tests cover control characters, broken low and high surrogates, preserved Unicode
pairs, truncation boundaries, primitive values, reflected fields, and value types. Process-wide
consumer-convention comments now describe the actual lifetime. Broader direct contract tests close
all Request and endpoint-convention overloads, client-factory boundaries, redelivery payload and
fallback behavior, correlation selectors, message-data retention, retry factories and filters,
supervisor and pipe lifecycles, consumer connector forms, activity variables, dependency-injection
selectors, fault data, and message-catalog identity.

The assertion-quality and test-smell audits reviewed all 50 changed or added test methods. Every
test has a meaningful outcome or state assertion, and no assertion-free, trivial, self-referential,
swallowed-exception, skipped, random, sleeping, or timing-dependent test remains. Long matrix tests
represent cohesive overload-family contracts. Nine isolated counterchanges covered abstract JSON
mapping, caller cancellation identity, log-context preservation, null pipe contexts, fallback
cancellation identity, redelivery payload forwarding, message-data retention, retry filtering, and
Unicode surrogate validation. Every counterchange failed its owning test and was restored before
the next experiment.

Fresh full-host coverage passes 3,382 tests. Advanced records 98.7% line coverage (1,639/1,661),
91.1% branch coverage (574/630), and 373 methods with zero CRAP scores above 30. Remaining uncovered
branches are private defensive or compiler-generated paths and do not justify artificial tests.
The accepted Cobertura SHA-256 is
`0b1b7a780345c2727bcdabad6f2236e8008b008d7c6e9ef10b4cb64794a4ab69`.

The final Engineering Release build passes 77 projects with zero warnings or errors. Both complete
format gates pass. All 23 native hermetic test hosts pass 6,355/6,355 with no failures or skips.
Package validation passes in update and independent comparison modes with 18 journeys, 31 freshly
packed packages, three isolated provider-testing consumers, and all 30 runtime API assemblies. The
18,879-line API contract SHA-256 is
`ab7469f985f1e269c5cceb803c06cfdd27cfe19f9b4ca51eded8f6c97857f12f`; its only intentional change
is `TransactionContext` to `ITransactionContext`.

Repository-wide scans find no C# preprocessor directives, dummy markers, MassTransit identities,
SDK-version pinning, or empty source directories. The sole `NotImplementedException` text is an
intentional non-retryable failure-classification rule, not a placeholder. Requirements JSON and Git
whitespace pass. The protected `review/` and `TestResults/` trees were neither changed nor staged.

## Iteration 108 Core Batching runtime research

The complete owner contains eight files and 1,075 production lines. `Batching/Contexts` contains the
immutable delivered snapshot and its consume-context facade; `Batching/Runtime` contains collector,
per-batch state, lifetime, factory, settings, and the internal collector contract. All paths,
filenames, namespaces, type names, interface prefixes, and comments match their responsibilities.
Batching is correctly part of Core rather than a separate assembly or external-adapter family.

The Roslyn static pairing pass scanned 4,267 source and 1,211 test files. Seven Batch files have
direct named test references. It labels `BatchCollectorLifetime.cs` unpaired because callers own it
through `BatchCollectorBase`; instrumented coverage proves that indirect path is extensively
executed. The result is a static symbol-reference heuristic, not line or branch evidence. The fresh
focused baseline passes all 70 Batch test cases.

Existing tests cover public batch delivery, size/time/forced completion, grouped and ungrouped
streams, retry isolation, duplicate suppression, exact ordering, TimeProvider behavior, cancellation
identity, dispatcher failures, fault fan-out, outbox rollback, connection disposal, probes, settings,
and a 1,000-message concurrency scenario. Owner coverage from the accepted full Core run is 96.6%
line (394/408) and 89.5% branch (154/172) over 74 methods, with zero CRAP scores above 30. The highest
scores are `BatchConsumer.AddAsync` at 18.00 with full line coverage and
`StopTimerAndRegistrations` at 12.32.

The first semantic gap is not a percentage concern. `BatchConsumer.AddAsync` inserts and registers a
message before starting or restarting the timer. If `ITimer.Change` throws, the method returns no
consumer to the caller, yet the entry remains inside an active batch and can later be delivered.
That would allow one message admission to both fail and subsequently participate in a batch. A
red-first test must establish the exact terminal contract before remediation. Equal ordering keys,
all sent-time fallback sources, saturated cancellation callbacks, and direct lifetime drain ordering
remain bounded hypotheses until their tests or code proofs establish whether a change is required.

The final owner contains the same eight files and 1,150 production lines. Failed timer starts and
restarts now terminate the batch, clear retained entries, stop the timer and cancellation
registrations, and propagate one terminal failure to every owned pipeline. A `false` return from
`ITimer.Change` is treated as an explicit scheduling failure. When admission and cleanup both fail,
the primary failure remains first and every distinct cleanup cause is retained without duplicating
the same exception instance.

Both timer and cancellation callbacks previously used `EnqueueBlocking`. A callback invoked on the
collector worker could therefore wait synchronously for work queued behind itself when the bounded
executor queue was full. The callbacks now launch fully observed asynchronous operations through
`ExecuteAsync`; terminal races and executor disposal are handled explicitly. Equal primary ordering
keys now use a monotonic admission-order tie break, so cancellation and dictionary slot reuse cannot
reorder surviving messages. The context sent time, transport sent time, and configured-clock
fallback priority is now directly demonstrated. Collector lifetime tests prove admission closure,
drain-before-flush, exactly-once disposal, both-executor shutdown, and retained flush-failure
identity.

Nine new requirement-mapped tests cover those contracts. Every changed or added test was reread
against the owning source and has causal state, ordering, exception-identity, or resource-lifetime
assertions. No assertion-free, trivial, self-referential, skipped, random, sleeping, wall-clock, or
swallowed-exception case remains. Six isolated one-cause counterchanges were killed: duplicate
failure aggregation, context timestamp fallback, lifetime drain signaling, message-limit boundary,
retry-count direction, and delivered-message suppression. The original timer, cancellation, and
ordering defects were independently observed red before remediation, and every counterchange was
restored before the next experiment.

The final Core coverage host passes 3,391/3,391 tests. Repository reachability records 76.6940%
line coverage (48,578/63,340) and 69.3636% branch coverage (16,906/24,373). Batching records 96.2%
line coverage (430/447), 91.6% branch coverage (174/190), 80 methods, and no CRAP score above 30.
The accepted artifact is `/private/tmp/vsb-iteration108-final.cobertura.xml`, SHA-256
`a83efb60a9d774fa9879689c7fbece53f4eddc011df5bb7606e3ac6b9ee79b4b`; it contains the final
`TerminateFailedAdmissionAsync` identity.

The first complete post-fix run correctly rejected that private method's missing `Async` suffix.
The bidirectional architecture gate passed after the manual rename. A later complete run exposed a
separate load-sensitive test-policy race: the scheduled-publish integration test used a 30-second
outer operation limit but retained the harness's 1.2-second inactivity limit. Ten isolated runs
passed, and source inspection identified the premature inactivity token as the only empty-sequence
path. Aligning both harness limits to the configured operation policy removes that race without
changing product behavior; the isolated rerun and final complete suite pass.

The final serial Engineering build passes all 77 projects with zero warnings and errors. Both full
format/analyzer gates pass. All 23 hermetic Unit/Architecture hosts pass 6,364/6,364 tests without a
failure or skip. Package verification passes 18 developer journeys, 31 fresh packages, three
isolated provider-testing consumers, and all 30 runtime API assemblies. The unchanged 18,879-line
packed API contract SHA-256 is
`ab7469f985f1e269c5cceb803c06cfdd27cfe19f9b4ca51eded8f6c97857f12f`.

Requirements JSON, Git whitespace, bidirectional async naming, comments, preprocessor directives,
dummy markers, SDK pinning, and empty source-directory checks pass. The sole source occurrence of
`NotImplementedException` is the intentional non-retryable failure-classification pattern. The
first sandboxed MSBuild attempt stalled in named-pipe initialization; exact process inspection and
an outside-sandbox rerun with disabled build servers produced the authoritative result. The
protected `review/` and `TestResults/` trees remain unchanged and unstaged.

## Iteration 109 Core Caching research

The complete `src/ViciOne.ServiceBus/Caching` owner contains 17 production files and 1,691 lines.
All files and comments were read manually. Public cache contracts and the partial `ResourceCache`
implementation correctly belong to the Core assembly under `Caching/`; the `Implementation/`
branch contains only non-public entry, pending-creation, prepared-key, and index helpers in the
matching `ViciOne.ServiceBus.Caching.Implementation` namespace. No project move or compatibility
folder is justified.

All six existing Caching test files were read after a 100/100 focused baseline. They already prove
key identity, atomic multi-index state, single-flight creation, capacity backpressure, absolute and
sliding expiration, deterministic time, observer serialization and reentry protection, lifetime
cancellation, lock ordering, clear/dispose ownership, and asynchronous resource release without
sleeping, random, skipped, or assertion-free cases.

The accepted Iteration 108 Core artifact remains valid as the initial baseline because Caching
product code was unchanged. It records 93.38% line coverage (635/680), 87.89% branch coverage
(225/256), 97 methods, and no CRAP score above 30. Method-level gaps raised two cancellation and
subscription hypotheses. Direct evidence disproved the first: although the internal completion is
faulted with an `OperationCanceledException`, every public asynchronous wrapper completes in the
canceled state. The red-first subscription case confirmed the second: a custom event add accessor
could retain the callback and then throw without a compensating unsubscribe. Synchronous-only
`IDisposable` release, pending direct-add backpressure and cancellation, null-key rejection, and the
empty hit ratio were implemented but lacked direct public-contract evidence.

The accepted correction attempts an unsubscribe after any usage-subscription failure. Both the
original failure and a possible compensation failure remain diagnostic-only and cannot undo an
already committed entry. The original red test observed one retained handler; the corrected test
observes zero while preserving exact cache ownership and disposal. Pending-capacity tests prove
that direct addition does not exceed the hard bound and that caller cancellation leaves an
uncommitted value caller-owned with the exact cancellation token.

Five new tests and one strengthened existing lifecycle test were reviewed against the owning source.
Every blocking wait is bounded, including the cancellation mutation path; all cases have causal
state, identity, token, capacity, or disposal assertions. There are no assertion-free,
self-referential, skipped, random, sleeping, wall-clock-dependent, or swallowed-exception cases.
Three isolated one-cause counterchanges were killed and restored: reversing compensation into a
second subscription, suppressing caller cancellation during capacity wait, and suppressing
synchronous disposal.

The final focused host passes 105/105. Fresh Caching coverage is 94.31% line (646/685) and 90.23%
branch (231/256), across 97 methods with no CRAP score above 30. Remaining low-coverage methods are
defensive exception/invariant paths or trivial alternatives, not unproved public behavior. The
artifact is `/private/tmp/vsb-iteration109-caching-final.cobertura.xml`, SHA-256
`16ea8775fb8206dcdeb4895df17568f5324391e8804363fd2c6cd70802741e20`.

The final owner contains 17 files and 1,701 production lines. All names, namespaces, primary types,
comments, and folder owners remain coherent. `Caching/Implementation` is a genuine non-public Core
namespace, not a nested project. The 77-project Engineering build has zero warnings and errors,
both format gates pass, all 23 hermetic hosts pass 6,369/6,369, and the rebuilt final Core host
passes 3,396/3,396. Package verification passes 18 journeys, 31 fresh packages, three isolated
provider-testing consumers, and 30 runtime API assemblies. The unchanged 18,879-line API contract
SHA-256 is `ab7469f985f1e269c5cceb803c06cfdd27cfe19f9b4ca51eded8f6c97857f12f`.

Two redundant test-side `#nullable enable` directives were removed because nullable analysis is
already enabled centrally. The only remaining directive-shaped lines are data inside a Roslyn test
raw string. Production code has no preprocessor directives, dummy or compatibility identities,
SDK-version pinning, or empty directories. Requirements JSON and Git whitespace pass. Protected
`review/` and `TestResults/` remain unchanged and unstaged.

## Iteration 112 Core Context initial research

The complete `src/ViciOne.ServiceBus/Context` owner contains 15 production files and 2,191 lines.
All 15 files and every source comment were read manually before any product edit. `Activities/`
contains the execute and compensate projections, scopes, and activity-instance bindings;
`Consumption/` contains the untyped and typed message views, payload scopes, outgoing facade,
deserialization lifetime, shared base, and unavailable sentinel. Filenames and primary types align.
The physical subfolders group closely related context types while the deliberately short public
namespace remains `ViciOne.ServiceBus.Context`; introducing public `.Activities` and `.Consumption`
namespaces would add navigation depth without defining independent assemblies or capabilities.

The unchanged focused namespace baseline passes 35/35. Fresh focused reachability is 68.5714% line
(360/525) and 59.0909% branch (78/132), across 296 compiler method records with no CRAP score above
30 and a maximum of 20. The baseline artifact is
`/private/tmp/vsb-iteration112-context-baseline/context.cobertura.xml`, SHA-256
`2d9f93798a70fadf48cdd91d99d113028fd2dd4fcbe897e29d78aa2b41b74c80`. The accepted Iteration 111
complete Core artifact reaches 448/525 owner lines and 103/132 branches, showing that several proxy
paths are exercised indirectly but not owned by direct Context evidence.

Existing direct tests comprehensively enumerate activity-result forwarding, unavailable-context
members, application outgoing operations, route and endpoint behavior, message projection,
conversation identity, and one local payload scope. The main unproved semantic risks are immediate
consume-lifetime ownership of initialized response operations, duplicate or late response-task
registration, proxy parameter and payload behavior, deserializer pending-task completion, exact
response and fault notification ownership, provider-result validation, and all public forwarding
parameters. These require source-owned deterministic tests before any implementation change.

## Iteration 110 Core Clients initial research

The complete `src/ViciOne.ServiceBus/Clients` owner contains 17 production files and 1,805 lines.
All 17 files and every source comment were read manually before any product edit. `ClientFactory`
and `ScopedClientFactory` own factory selection and lifetime; `Contexts/` owns the three bus and
receive-endpoint response contexts; `Endpoints/` owns the five publish/send resolution paths; and
`Requests/` owns response registration, terminal completion, metadata application, and typed
response materialization. Filenames, primary types, namespaces, and folders are aligned.

This is an internal request/response capability of the Core assembly and therefore belongs below
`src/ViciOne.ServiceBus/Clients`. Moving it to a new sibling project would create an artificial
package boundary across Core pipes, contexts, transports, initializers, and outgoing metadata. In
contrast, independent persistence, scheduling, and transport deliverables remain sibling projects
grouped below their provider families. The public contracts in `ViciOne.ServiceBus.Abstractions`
and the `Advanced` facade were also inspected as consumer boundaries; they do not make the Core
implementation directory an umbrella for other assemblies.

The unchanged focused baseline passes 93/93 Client tests. The first source pass confirms that
Request ID, response address, accepted response URNs, transport lifetime, absolute deadline, and
custom outgoing options converge in `ClientRequestHandle.SendAsync`. Receive-endpoint-backed
endpoints gate destination resolution on endpoint readiness, and `ClientFactory.DisposeAsync`
serializes concurrent disposal through one completion source. Remaining questions for direct test
and risk evidence are terminal response/fault/cancellation races, handler connection release,
callback failure identity, initialized-message metadata parity, and exact factory-disposal fault
ownership. No structural move is accepted without corresponding assembly, consumer, package,
runtime, and test evidence.

The final owner still contains the same 17 files; all final product changes are confined to the
three partial `ClientRequestHandle` files in `Requests/`. The review found six concrete lifecycle
defects. A fault could lose to a later response while cleanup was blocked; two response contracts
could both complete successfully; sending began before the mandatory fault observer was connected;
null fault and response connection handles were not rejected at their ownership boundaries; and a
send callback could return a null request message. Terminal response, fault, cancellation, and
disposal ownership now use the same handler lock. Fault observation is connected before send can
start, and every missing provider result fails immediately with an exact diagnostic.

Direct tests also close previously unproved but already correct contracts: a transport lifetime
derived from a deadline requires that deadline; a second send-pipeline invocation rejects and
releases its unowned timer; repeated asynchronous factory disposal preserves one shared failure;
all eight direct factory request shapes, all eight scoped request shapes, and all four scoped-client
resolution shapes preserve address, route, consume scope, typed or initialized message, request ID,
response address, timeout, cancellation token, and timer ownership. The two- and three-response
advanced initialized-message overloads now have exact winning-branch and message assertions.

Thirteen permanent requirement projections cover the new contracts. The focused Clients suite
grows from 93 to 123 cases. Every changed or added test was manually reviewed for causal assertions,
bounded waits, cancellation identity, exception identity, and resource ownership. It contains no
assertion-free, trivial, self-referential, skipped, random, sleeping, wall-clock-dependent, or
swallowed-exception case. Seven isolated one-cause counterchanges were killed and restored:
response-branch ownership, response-before-fault ownership, connect-before-send, explicit scoped
address ownership, consumed initialized-message scope, deadline validation, and duplicate timer
release. Three further provider-result tests were observed red on the unchanged implementation
before their null-contract corrections.

Fresh final Core coverage passes 3,426/3,426 tests. Repository reachability is 76.7457% line
(48,633/63,369) and 69.4156% branch (16,927/24,385). Clients improves from 95.7821% to 98.9831%
line coverage (584/590) and from 75.5814% to 89.6739% branch coverage (165/184), across 137 methods
with zero CRAP scores above 30 and a maximum of 20.12. The six remaining owner lines are private
deterministic race, cleanup, or logging defenses rather than missing API or parameter forms. The
accepted artifact is `/private/tmp/vsb-iteration110-core-final2/core.cobertura.xml`, SHA-256
`3fe785da97559080e7eef14bc4dab1f155ae8ce155c15b8423af847655d83ce6`.

One first full Core coverage attempt reported 3,420/3,421 without retaining an identifiable
failure in its truncated output. The same candidate then passed repeated complete Core runs,
including the accepted 3,426-case coverage run, and the final all-host run. It is recorded as a
non-reproduced observation rather than represented as a successful gate.

Both full format gates pass. The serial Engineering Release build passes 77 projects with zero
warnings and errors. All 23 hermetic Unit and Architecture hosts pass 6,399/6,399 without a failure
or skip. This includes the bidirectional Async convention and source-file, namespace, project, and
folder architecture rules. Package verification passes 18 journeys, 31 freshly packed packages,
three isolated provider-testing consumers, and 30 runtime API assemblies. The packed public API is
unchanged at 18,879 lines with SHA-256
`ab7469f985f1e269c5cceb803c06cfdd27cfe19f9b4ca51eded8f6c97857f12f`.

Requirements JSON and Git whitespace pass. Source scans find no C# preprocessor directives, dummy
markers, MassTransit or compatibility identities, SDK-version pinning, or empty source/test
directories. The retained `NotSupportedException` operations are explicit non-readable,
non-seekable stream members or Event Hubs endpoint-local transport operations that the contracts do
not support; they are not placeholders. The sole `NotImplementedException` reference is an
intentional non-retryable failure-classification case. The protected `review/` and `TestResults/`
trees remain unchanged and unstaged.

## Iteration 111 Core Consumers initial research

The complete `src/ViciOne.ServiceBus/Consumers` owner contains nine production files and 448 lines.
All files and every source comment were read manually. Four root factories own default-constructor,
delegate, object-factory, and caller-owned instance lifetimes; `Contexts/` owns proxy and scoped
consumer contexts; `Conventions/` owns the process-wide ordered registry; and `Metadata/` owns
versioned message-contract snapshots and registration classification. Filenames, primary types,
namespaces, and directories align. This capability belongs inside Core rather than in an external
provider family or an independent assembly.

The unchanged broad Consumer namespace baseline passes 119/119 tests. Direct symbol pairing finds
named tests for all nine source files. Existing tests prove successful synchronous and asynchronous
release, release after a pipeline failure, caller ownership, null and wrong factory results, context
and payload projection, immutable concurrent metadata snapshots, late convention registration and
removal, custom dispatch, and public null boundaries. Architecture tests explicitly bind all nine
filenames to their owners.

The accepted Iteration 110 Core artifact shows complete line coverage for metadata caching and
consumer-context construction, while missing-removal, null object-factory results, probe variants,
and one registration-classification branch remain indirect or uncovered. The principal semantic
risk is stronger than a percentage gap: both owned factories use `finally` disposal, so a disposal
failure can replace an already selected pipeline failure. A deterministic red-first test must
establish whether both failures remain observable. Direct registration-type matrices and stable
version behavior must also distinguish actual missing behavior from already-correct code.

The final owner contains ten production files and 539 lines. All ten files, their types, and every
final source comment were read manually. The added `OwnedConsumerLifetime` is an internal Core
helper shared by the two owned factory implementations. `Contexts/`, `Conventions/`, and
`Metadata/` remain coherent namespace owners; no new project or provider boundary is justified.

Two defect families were confirmed red before correction. Default-constructor, delegate, and object
factories each discarded the selected pipeline failure when consumer release also failed. They now
release through one terminal lifetime operation that preserves exact single failures and reports
ordered operation and release failures together. Custom convention providers could also return a
null message convention, sequence, descriptor, or message type; all four invalid shapes now fail at
the convention boundary with the owning convention and consumer identities. The validated metadata
builder retains first message-type order while allowing a later convention to replace the descriptor
in that position.

Eight permanent requirement projections add 17 focused cases, taking the Consumer namespace from
119/119 to 136/136. Direct evidence also covers no-op convention version stability, later-convention
precedence, the full registration/exclusion matrix, all four factory probe identities, synchronous
and asynchronous release failures, null object-factory results, and scoped-context null parameters.
All changed tests were reviewed for causal and exact assertions, isolation, and determinism. They
contain no sleeps, random input, unbounded waits, skips, assertion-free paths, self-reference, or
swallowed failures. Seven provider/lifetime cases were observed red on the unchanged implementation.
Three isolated one-cause counterchanges were then killed and restored: excluded-consumer admission,
later-descriptor replacement, and duplicate-registration version mutation.

Fresh Consumers coverage passes all 136 focused cases and records 100% line coverage (172/172) and
100% branch coverage (58/58), across 34 methods with no CRAP score above 30 and a maximum of 12. The
accepted artifact is `/private/tmp/vsb-iteration111-consumers-final/consumers-final.cobertura.xml`,
SHA-256 `a8b20e372302554e706a4c3b63664258db2d77477db94c912e803a23ceb92b24`.
The accepted complete Core coverage run passes 3,443/3,443 and records repository reachability of
76.7657% line (48,681/63,415) and 69.4332% branch (16,941/24,399). Its artifact is
`/private/tmp/vsb-iteration111-core-final/core-final.cobertura.xml`, SHA-256
`91dde737706e9d409aac01328c607314b0b57ee6c0decf31458188be462abeff`.

Both format gates pass. The serial Engineering Release build passes all 77 projects with zero
warnings and errors. All 23 hermetic Unit and Architecture hosts pass 6,416/6,416 with no failure or
skip, including bidirectional Async naming and source-file, namespace, folder, and project rules.
Package verification passes 18 journeys, 31 freshly packed packages, three isolated provider-testing
consumers, and 30 runtime API assemblies. The public contract is unchanged at 18,879 lines with
SHA-256 `ab7469f985f1e269c5cceb803c06cfdd27cfe19f9b4ca51eded8f6c97857f12f`.

Requirements JSON, Git whitespace, source directives, dummy and compatibility identities, SDK
version pinning, and empty source directories are clean. The only source reference to
`NotImplementedException` remains the intentional non-retryable classifier case. Protected
`review/` and `TestResults/` remain unchanged and unstaged.

## Iteration 112 Core Context final research

The final owner still contains the same 15 production files and now totals 2,181 physical source
lines. Every file and source comment was read manually; the three changed production files and
their surrounding contracts were reread after the corrections. The final comments describe only
current code and behavior and required no generated or historical wording. The source-file
architecture confirms that `Activities/` and `Consumption/` are coherent physical responsibility
groups inside the Core project while their public types intentionally retain the concise
`ViciOne.ServiceBus.Context` namespace. Independent assemblies remain sibling projects directly
under `src`, and external integrations remain grouped by provider family under `Persistence/`,
`Scheduling/`, and `Transports/`.

Four concrete boundary and lifetime defects were confirmed before correction. Initialized typed
responses could escape asynchronous endpoint resolution without the returned operation being owned
by consume completion. Direct response shapes could register the same pending transport task twice
when endpoint resolution completed synchronously. A receive-context endpoint provider could return
a null task or endpoint and leak an unrelated null-reference or decorator exception. A proxy source
could report a successful typed lookup while returning a null context or null message, again
leaking an unrelated null-reference failure. The accepted implementation registers each distinct
response operation exactly once, establishes ownership before asynchronous preparation can escape,
and rejects every invalid provider result at its owning boundary with an exact diagnostic.

Permanent direct evidence covers all twelve response shapes; all three initialized-message
response shapes; endpoint-provider null task and null result; four fault-generation and
notification branches; complete typed and untyped forwarding; observer identity; proxy metadata,
message and payload projection through a real InMemory delivery; invalid proxy lookup results;
deserializer pending work, exact failure and cancellation; empty and populated payload scopes; and
every newly exercised required parameter. Requirement projection records these behaviors under the
response-lifetime, endpoint-boundary, fault-notification, proxy, deserializer, message-context, and
scope requirements.

The focused Context profile grows from 35 to 96 cases and passes 96/96. Fresh accepted focused
coverage records 100% executable-line reachability (527/527) and 93.75% branch reachability
(120/128), across 294 compiler method records with no CRAP score above 30 and a maximum of 6. The
artifact is `/private/tmp/vsb-iteration112-context-final3.cobertura.xml`, SHA-256
`f8d8c82b050dc8003ca7411080c64299a05a991cc8df689189b6a31f04e5cd92`. The accepted complete Core
coverage run passes 3,504/3,504 and records repository reachability of 76.9119% line
(48,776/63,418) and 69.5429% branch (16,965/24,395). Its artifact is
`/private/tmp/vsb-iteration112-core-final/core.cobertura.xml`, SHA-256
`51ad6d890e9c31ce7652c931f77fefbae7c0c0aeef58edeef33a44729d247820`.

The original implementation produced the expected red evidence for missing initialized-response
ownership, nine duplicate-registration response forms, both invalid endpoint-provider results, and
both invalid proxy lookup results. Three isolated one-cause counterchanges were subsequently killed
and restored: removal of immediate initialized-response ownership, swallowing a successful null
proxy lookup, and removal of null endpoint-result validation. All changed tests were manually
reviewed for exact causal assertions, task and token identity, bounded synchronization, and failure
identity. They contain no sleeping, random input, skips, assertion-free paths, swallowed failures,
or wall-clock timing assumptions.

The first all-host validation found only a private test helper with an asynchronous contract but no
`Async` suffix; the helper was renamed and the bidirectional architecture test passed. The next
all-host attempt exposed a test-only concurrent-list enumeration after the endpoint-start signal;
the timing assumption was replaced by an explicit second-task-registration signal. The corrected
test passes in isolation and the complete Core host passes 3,504/3,504. The definitive run passes
all 6,477 tests across 23 hermetic Unit and Architecture hosts with zero failures and skips.

Both format gates pass. The serial Engineering Release build passes all 77 projects with zero
warnings and errors. Package verification passes 18 journeys, 31 freshly packed packages, three
isolated provider-testing consumers, and 30 runtime API assemblies. The public API remains exactly
18,879 lines with SHA-256
`ab7469f985f1e269c5cceb803c06cfdd27cfe19f9b4ca51eded8f6c97857f12f`.

Requirement JSON, Git whitespace, bidirectional Async naming, source-file and folder ownership,
comments, compiler directives, compatibility identities, and empty source/test directories pass.
No product C# directive, dummy marker, MassTransit identity, or CLI SDK-version pin exists. The
repository `global.json` selects Microsoft Testing Platform only and has no `sdk` block. The explicit
`net10.0` target frameworks are the platform contract, not SDK pinning. The
sole source reference to `NotImplementedException` is the intentional non-retryable exception
classification case, not an implementation placeholder. Protected `review/` and `TestResults/`
remain unchanged and unstaged.

## Iteration 113 Core InMemoryTransport initial research

Iteration 113 starts from remotely verified commit
`410fb01fca19aa101afa14b6f6009d305fa1055a` and annotated tag
`servicebus-a-plus-remediation-iteration-112-2026-09-14`. The implementation subtree of the built-in
in-memory transport contains 56 production files and 3,380 physical lines under
`src/ViciOne.ServiceBus/InMemoryTransport`. The complete owner expands to 66 files and 3,846 lines
when its nine public provider contracts and public selection entry point are included. All 66 files
and every source comment were read manually before any product edit. No source, test, or comment
generator is permitted.

The physical model is intentional. This transport is a process-local Core capability used as the
default runtime and testing transport, so `Addressing/`, `Configuration/`, `DurableSend/`,
`Runtime/`, and `Topology/` belong inside the `ViciOne.ServiceBus` assembly. The projects grouped
under `src/Transports` are independently packaged external-provider integrations; moving the
in-memory implementation there would create a false package boundary and make Core depend on an
optional provider. The current filenames and primary types align with their folders.

The unchanged focused namespace baseline passes 80/80. Fresh focused coverage reaches 86.6071%
line (873/1,008) and 69.8830% branch (239/342), across 244 compiler method records with no CRAP
score above 30 and a maximum of 14.9211. The baseline artifact is
`/private/tmp/vsb-iteration113-inmemory-baseline.cobertura.xml`, SHA-256
`e8d25ab1e2a3c553e1200e8d23fb39ded3fb730ce1c83c34dca359218f1a794c`.

Existing direct tests cover logical delays, scheduling, lifecycle races, durable-send completion,
error and dead-letter movement, publish and point-to-point endpoints, endpoint concurrency, host
isolation, address parsing, topology, header projection, and several configuration boundaries. The
remaining review must distinguish uncovered defensive branches from semantic gaps in provider
results, transport-envelope ownership, cancellation identity, topology mutation, runtime shutdown,
dynamic endpoint configuration, and durable dispatch. Coverage and static pairing are used only to
route the manual code and test review, never to replace it.

## Iteration 113 Core InMemoryTransport final research

The final owner contains 66 production files and 3,846 physical lines across the internal
implementation, its public Core provider contracts, and its public Core selection entry point. All
files and comments were read manually. The physical architecture is deliberate: InMemory is the
process-local default and testing transport and therefore belongs to the Core assembly. Optional
provider integrations remain independent sibling assemblies grouped under `Persistence`,
`Scheduling`, and `Transports`; placing InMemory there would introduce a false package boundary.

The review confirmed seven independently testable defect families. Short addresses accepted
hierarchical, credential, absolute-path, or fragment components. Raw multi-segment host identities
were ambiguous, while canonical endpoint reconstruction failed to escape entity names. Endpoint
configuration captured its host before later host configuration completed. Undefined exchange-type
values crossed the public configuration boundary. Dead-letter movement discarded MIME parameters.
Durable dispatch trusted null messages, invalid catalog success results, and invalid endpoint
provider results. Each correction is placed at the earliest owning boundary and preserves all
existing features.

Permanent evidence covers canonical address round-trips and all invalid component classes; late
host configuration; valid and undefined topology routing; standalone, default, typed, and typed-only
bus ownership; provider-neutral callbacks; both public binding APIs; logical zero delay; runtime
fabric and unsupported-agent contracts; unfiltered publish discovery and a genuine namespace-less
runtime type; complete dead-letter MIME metadata; durable unknown-contract, null-catalog, null-task,
null-result, default-context, and exact pre-cancellation outcomes; and every newly exercised public
parameter. Eighteen requirement projections add 24 focused cases, taking the profile from 80 to
104.

The seven substantive fixes were simultaneously counterchanged. The mutated build remained valid,
and the 49 targeted cases produced exactly 15 expected failures while 34 unrelated cases passed.
The accepted source was restored byte-for-byte and rebuilt. Changed tests were manually reviewed for
exact failure identity, collaborator call counts, token and ownership identity, semantic MIME
comparison, deterministic logical time, and bounded synchronization. They contain no sleeps, random
input, skips, assertion-free paths, swallowed failures, or wall-clock timing assumptions.

Final focused coverage passes 104/104 and reaches 90.4889% line (1,018/1,125) and 76.0101% branch
(301/396) coverage over 272 compiler method records. Maximum CRAP is 18 and no score exceeds 30. The
artifact is `/private/tmp/vsb-iteration113-inmemory-final3.cobertura.xml`, SHA-256
`5388c5139c95c229dc00315fa7a8ca902085fdf75bc36537446a3c3409176de8`. The complete Core run passes
3,528/3,528 and records repository reachability of 77.0444% line (48,887/63,453) and 69.7121% branch
(17,023/24,419). Within that run, the InMemory owner reaches 96.4444% line (1,085/1,125) and 78.7879%
branch (312/396), again with maximum CRAP 18 and none above 30. The complete artifact is
`/private/tmp/vsb-iteration113-core-final3.cobertura.xml`, SHA-256
`b0f878be0ebb78f4ad4c48126e78fde891ef751fc8996a59b634a8d1302ed7a9`.

Both format gates pass. The serial Engineering Release build passes all 77 projects with zero
warnings and errors. The definitive hermetic profile passes all 6,501 tests across 23 Unit and
Architecture hosts with zero failures and skips. A separate architecture run passes 307/307 and
therefore independently reconfirms bidirectional Async naming, Greenfield API rules, requirements,
comments, compiler directives, source-file naming, folders, namespaces, and repository structure.
Package verification passes 18 developer journeys, 31 freshly packed packages, three isolated
provider-testing consumers, and all 30 runtime APIs. The public API remains 18,879 lines with
SHA-256 `ab7469f985f1e269c5cceb803c06cfdd27cfe19f9b4ca51eded8f6c97857f12f`.

Static gates confirm no SDK version pin, product C# directive, empty source/test directory, dummy or
maintenance marker, or MassTransit identity. `global.json` intentionally contains only the
Microsoft Testing Platform runner selection and no `sdk` block. The sole source occurrence of
`NotImplementedException` is an intentional non-retryable BCL exception classification, not a
placeholder. Git whitespace is clean. Protected `review/` and `TestResults/` remain unchanged and
unstaged. The overall A+ goal remains active for the remaining complete source owners and final
repository-wide audit.

## Iteration 114 Core Initializers initial research

Iteration 114 starts from remotely verified commit
`9caad5be9b9993126add663ccf04c705023ffffc` and annotated tag
`servicebus-a-plus-remediation-iteration-113-2026-09-14`. The Initializers owner contains 87
production files and 7,571 physical lines; its direct test owner contains 46 files and 7,384 lines.
All production and test files and every source comment were read manually before correction. No
source, test, comment, or structure generator is permitted.

The physical placement is intentional. `src/ViciOne.ServiceBus` is the Core assembly project root,
not an umbrella around every ServiceBus assembly. Core Initializers therefore belong beneath that
project. Independent assemblies remain sibling projects below `src`; optional provider integration
families remain grouped below `Persistence`, `Scheduling`, and `Transports`. There is no product C#
file directly below the repository `src` root.

The correct namespace baseline passes 178/178. Fresh focused coverage reaches 97.5868% line
(2,305/2,362) and 90.0519% branch (1,041/1,156) coverage across 489 compiler method records. The
baseline artifact is `/private/tmp/vsb-iteration114-initializers-baseline2.cobertura.xml`, SHA-256
`42424c1db8b538ef3db7b4052fa7567c5452a9513ff2d1bccb715230a21be528`. The sole CRAP score above 30
is the monolithic `TypeConverterCache.TryGetTypeConverterCore` at 30.0693. Existing tests provide
broad direct coverage of message construction, conventions, providers, property/header
initialization, scalar/collection conversions, enum and nullable discovery, cancellation, failures,
and public boundaries.

## Iteration 114 Core Initializers final research

The final owner contains 87 production files and 7,593 physical lines, with 46 direct test files and
7,452 test lines. Manual review confirmed coherent filenames, namespaces, subfolders, comments, and
Core ownership. The repository structure distinguishes assembly boundaries from provider-family
grouping and requires no source move.

Custom property and header initializers could return null tasks. The implementation forwarded those
values into `Task.WhenAll`, producing a generic framework failure instead of an owned ServiceBus
contract error. The final boundary turns each invalid task into an explicit faulted task, preserving
parallel observation while producing exact property/header failure identity. Direct tests also
prove that a null header task prevents the downstream pipe. All three convention discovery entry
points now directly prove their required property metadata contract.

The converter cache's single high-risk resolver combined cache lookup, named values, registered
converters, enums, nullable results, and nullable sources. It is now decomposed into four focused
helpers while retaining lookup ordering and dynamically registered converter behavior. Fresh
coverage removes the only CRAP score above 30; maximum owner CRAP is now 28. The DateTime converter
comment was manually corrected to describe invariant text, signed Unix milliseconds, and UTC
instants without falsely claiming every conversion produces UTC.

The two new null-task cases failed against the original implementation with the generic
`ArgumentException`, while 19 related cases passed. A controlled counterchange accepting null as a
completed task caused exactly those two cases to fail while the same 19 unrelated cases passed. The
accepted implementation was restored byte-for-byte. A repository-wide bidirectional Async naming
test subsequently caught the new task-returning helper's missing `Async` suffix; the helper and all
call sites were renamed, and the isolated architecture test passed before the final all-host run.

Final focused coverage passes 181/181 and reaches 97.6589% line (2,336/2,392) and 90.3448% branch
(1,048/1,160) coverage across 494 compiler method records. The artifact is
`/private/tmp/vsb-iteration114-initializers-final3.cobertura.xml`, SHA-256
`621c6186e8f9e4412d4bdfa2a33395a710cf697f5aee62947790454be4233509`. Complete Core coverage passes
3,531/3,531 and records repository reachability of 77.0558% line (48,905/63,467) and 69.7433% branch
(17,032/24,421). Within that run, Initializers reaches 97.7007% line (2,337/2,392) and 90.4310%
branch (1,049/1,160). The complete artifact is
`/private/tmp/vsb-iteration114-core-final3.cobertura.xml`, SHA-256
`0402d07f5a2dfe26c6e63875e835575611ea4e7d55d552b13e0089b33e1e4b40`.

Both final format gates pass. The definitive serial Engineering Release build passes 77 projects
with zero warnings and errors. All 23 hermetic Unit and Architecture hosts pass 6,504/6,504 with no
failure or skip. Package verification passes 18 developer journeys, 31 freshly packed packages,
three isolated provider-testing consumers, and all 30 runtime APIs. The public API remains 18,879
lines with SHA-256 `ab7469f985f1e269c5cceb803c06cfdd27cfe19f9b4ca51eded8f6c97857f12f`.

Requirements, bidirectional Async naming, source/comment/directive/file/folder architecture, dummy
and legacy markers, SDK pinning, empty directories, formatting, and Git whitespace pass. No product
C# file exists directly beneath `src`, and `global.json` has no SDK version. The sole source
occurrence of `NotImplementedException` is the intentional non-retryable exception classification,
not a placeholder. Protected `review/` and `TestResults/` remain unchanged and unstaged. The overall
A+ goal remains active for the remaining complete source owners and final repository-wide audit.

## Iteration 115 Core Reflection and JobService span ownership final research

The original owner comprised 12 Core files and 938 lines. Every file, direct test, and comment was
read manually. Static caller analysis confirmed that `NullSafeTrim` and `TrimEmptyToNull` were dead,
while `SpanSplit` served only JobService Cron parsing. The final architecture therefore retains eight
explicit Reflection files and 817 lines in Core and places the 88-line splitter beside its only
consumer in JobService. Independent assemblies remain direct `src` siblings; optional provider
families remain grouped below `Persistence`, `Scheduling`, and `Transports`.

The former splitter used an empty remaining span as both data and completion sentinel. It therefore
discarded a trailing empty token and accepted malformed Cron lists ending in a comma. The final
enumerator tracks completion independently and retains every empty token for the parser's existing
validation. Three redundant internal interfaces had exactly one implementation and no abstraction
boundary; their removal preserves functionality while reducing indirection.

Reflection emission now validates the bus-marker contract directly and locks the shared collectible
module during different-type emission. Accessors validate metadata ownership, instance property
shape, required accessor, property type, implementation type, and runtime instance at the earliest
boundary. Both caches provide consistent case-insensitive identity, required/optional behavior,
type diagnostics, and `PropertyInfo` ownership, including generated interface implementations.

Red evidence is specific: seven of 37 Reflection cases, two of 136 Cron cases, and one of 23 source
architecture cases failed before correction. The final focused results are 42/42 Reflection,
136/136 Cron, and 23/23 architecture. A controlled four-defect mutation run yielded exactly four
failures in the 3,552-case Core host and was restored byte-for-byte. The five restored source hashes
are `2eaa92696121262ed753ded244384d004f15234f1c3edc96b11a9553aeba0f09`,
`021d8671db6293bf8a631a61746e645b2844d9cd84098cbd714d0b89600e69eb`,
`6b114bbae40b08e75c87a5aefdd1c937779f4c873c0afc286939a331e0379e72`,
`845cdae06916a2489e895c53d5b20164ce54d0ee37ed918138ad178ca00faef2`, and
`184b9ce41bb02c753c10f35aa1a15172246ecbfbb665692cdf1ac569df5ab872`.

Focused Reflection coverage is 95.7393% line and 92.8571% branch with maximum CRAP 30 and none above
30. The JobService splitter has 100% line and branch coverage. Complete Core coverage passes all
3,552 tests and records 77.1131% line and 69.8889% branch overall; executable Reflection sources rise
to 96.4194% line and 96.7033% branch. Artifact SHA-256 values are
`c7d1107f0d9a8b61077b69916e662122bce10238d8fc4beb93c45e40496b7c81` focused Reflection,
`3b780494b7ee9c1d133696bd20b257c0f9cd7396fe5ed4c3a14939f9fc99980c` focused Cron, and
`48eea3d2bbf0e7942d565b84557264105f2fae0a4243051a7ddb9db522bbccd8` complete Core.

A manual test anti-pattern audit accounts for all 16 new or changed test methods. It finds zero
critical, warning, or informational defects: assertions constrain exact boundary identity and
observable state; data cases are explicit; no sleep, clock, random, skip, catch-and-ignore, shared
fixture, mystery dependency, assertion-free, or coverage-only behavior is present. Repository file
access in the architecture case is its intentional system boundary.

Both format gates, the 77-project zero-warning Engineering build, all 6,526 tests across 23 hermetic
hosts, requirements uniqueness, Async/source architecture, and hygiene gates pass. Package/API
validation passes 18 journeys, 31 fresh packages, three isolated provider-testing consumers, and
all 30 runtime APIs. The public API remains 18,879 lines with SHA-256
`ab7469f985f1e269c5cceb803c06cfdd27cfe19f9b4ca51eded8f6c97857f12f`. Protected trees remain
unchanged and unstaged. The overall A+ goal remains active.

## Iteration 116 Core Logging final research

The complete Core Logging owner contains 13 files and 1,341 final source lines. Every production
file, direct test, and source comment was read manually. Its internal `Diagnostics`, `Internal`, and
`Monitoring` namespaces are coherent beneath the Core project. Azure Service Bus owns only its SDK
header adapter in the independent transport project. This confirms the repository convention:
`src/ViciOne.ServiceBus` is one project boundary, while direct `src` children are other assemblies
and provider families are grouped below `Persistence`, `Scheduling`, and `Transports`.

Distributed parent extraction previously marked a valid carrier parent as remote only when no
ambient activity existed. A transport receive running beneath unrelated ambient instrumentation
therefore emitted an incorrect local parent. The final extraction always preserves requested remote
identity. Persistent outbox enqueue and delivery now have direct evidence for operation names,
kinds, parent/trace continuity, and successful completion. Link, New, and carrier-parent modes are
covered independently, and the full message-flow test confirms remote sampling, trace state,
baggage, operation tags, and positive serialized-body size.

The Azure header provider now rejects a missing SDK message immediately and maps Azure's
`Diagnostic-Id` carrier to the canonical internal activity header. Its internal constant no longer
claims that a live Azure interoperability header is legacy. Structured log convenience writers
prove exact values and exception identity, while the single-logger factory proves logger identity
and caller-owned lifetime. Exact serialized body length and a null generic update context are also
directly constrained.

Static caller review found that no transport supplied the former custom tag-array argument. The
parameter and helper were dead and are removed without feature or public API loss. Manual trace
state copying was redundant because activity creation inherits the current W3C state; complete
integration assertions continue to prove exact propagation after its removal. Observation log
messages now describe mutation failures accurately instead of attributing every failure to a
listener.

The original remote-parent test failed exactly on `IsRemote`, while its remaining identity
assertions passed. The original Azure constructor test failed while diagnostic-header mapping
passed. Five concurrent counterchanges caused exactly five failures among 21 targeted Logging cases
and left 16 unrelated cases green; accepted source was restored and rebuilt. Focused profiles pass
21/21 Logging, 45/45 Monitoring, and 2/2 Azure header cases.

The manual anti-pattern audit covers 10 changed test methods and 12 executed cases. All assertions
bind exact state, identity, causality, or boundary ownership. Test doubles throw on every unexpected
call. There are no sleeps, wall-clock assumptions, random inputs, skips, broad catches, swallowed
failures, assertion-free paths, or coverage-only tests. Existing GUID creation in an older message
flow fixture supplies correlation identities and is not an ordering or timing oracle.

Complete Core coverage passes 3,560/3,560 and records 77.1372% overall loaded-product line coverage
(48,959/63,470) and 69.9438% branch coverage (17,060/24,391). Logging reaches 95.7211% line
(604/631) and 100% branch (133/133) coverage over 116 compiler method records. No method has CRAP
above 30; maximum is 28. The accepted artifact is
`/private/tmp/vsb-iteration116-core-final2/core.cobertura.xml`, SHA-256
`c896dc9d95dc7f073237c80630d387c22267da46b598d1ba1bb27bf40e830cd6`.

Both format gates, the serial 77-project zero-warning Engineering build, and all 6,536 tests across
23 canonical serialized hosts pass. One non-canonical parallel-host attempt timed out a Quartz
observation case under CPU contention; the case passed in isolation in 1.393 seconds and the full
documented serialized profile passed, so no runtime or test deadline was weakened. Package/API
verification passes 18 journeys, 31 fresh packages, three isolated provider-testing consumers, and
all 30 runtime APIs. The unchanged 18,879-line public API SHA-256 is
`ab7469f985f1e269c5cceb803c06cfdd27cfe19f9b4ca51eded8f6c97857f12f`.

The final targeted Roslyn architecture run passes 53/53 cases and reconfirms both directions of the
Async contract plus source type/file naming. Requirements JSON, directives, dummy and MassTransit
legacy markers, SDK pinning, empty directories, formatting, and Git whitespace are clean. The sole
source `NotImplementedException` occurrence classifies that real BCL exception as non-retryable.
Protected `review/` and `TestResults/` remain unchanged and unstaged.

## Iteration 117 Core Events final research

All 12 Core Events production files, 642 initial and 639 final physical source lines, direct tests,
the readiness driver, product call sites, and comments were read manually. Their final placement is
coherent: fault contracts, readiness contracts, and receive-lifecycle contracts belong to Core and
are grouped below `Events/Faults`, `Events/Readiness`, and `Events/Receiving`. Core is one project,
not the directory parent of other assemblies. Persistence, scheduling, and transport providers
therefore remain separate sibling projects in their named `src` families.

The previous `FaultEvent<T>` and `ReceiveFaultEvent` implementations independently projected an
`AggregateException`. Both returned an empty array for an aggregate without inner failures and used
only the outer aggregate list for nested failures, leaving nested aggregate nodes and losing their
sibling leaves from the top-level diagnostic list. The shared `FaultExceptionInfo.CreateMany`
boundary now returns one diagnostic for any non-aggregate or empty aggregate and otherwise uses the
BCL flattening contract before applying the existing sixteen-diagnostic bound. Diagnostic
materialization continues to isolate hostile data enumeration and remote metadata, avoids invoking
arbitrary application `ToString`, and preserves bounded wire-safe values.

`ReceiveTransportCompletedEvent` previously read a live metrics object without validating the
semantic relationship between its counters. The event now reads both counters once, rejects either
negative value, rejects peak concurrency above total deliveries, and retains an immutable exact
snapshot. Lifecycle tests now constrain every address as well as state, exception, endpoint, and
counter projection. `BusReadyEvent` has direct identity and required-owner tests. Fault events have
direct deterministic-clock, identifier, payload, host, content-type, message-type, empty aggregate,
nested aggregate, hostile-data, hostile-remote-type, and missing-remote-type evidence.

The baseline focused profile passes 31/31 with 94.7368% line (180/190), 95.2381% branch (120/126),
maximum CRAP 20, and artifact SHA-256
`f04c1ebc174becaa50647e71fbe5bc1082b51d168512cfbeef33c3a150313fd4`. Four aggregate cases are red
before their implementation and three invalid-metric cases are red before theirs. The final focused
profile passes 45/45 and reaches 98.9637% line (191/193) and 98.3607% branch (120/122), maximum CRAP
20, with artifact SHA-256 `37fbd434844348edd1737854abf5af6cb34fa5a5b7a7fb7ae54ce7f8d3beba25`.

The controlled aggregate, readiness owner, delivery-count, and completed-address counterchanges
produce eight failures in the 44-case combined profile; an isolated ready-address counterchange
produces one failure in its one-case profile. Restoration is byte-exact. Accepted SHA-256 values are
`1669276122ba35cb159cc85c5948d7e57b7899a1a4b76161e17530ea70869cb6` for
`FaultExceptionInfo.cs`, `bc1950d933052fa1b83b05a8ffa5326349055c9ed8fd7733a79d81760f8ede81`
for `BusReadyEvent.cs`, `e04831f95d8b7c37c871509a1762110245ba8ba03cffc58490eac917baa4e52c`
for `ReceiveTransportCompletedEvent.cs`, and
`988182f9a234d0f7df6cd3aa08b9ac68af8b9a43e4bbd6964222b927d845f031` for
`ReceiveTransportReadyEvent.cs`.

Complete Core coverage passes 3,574/3,574 and covers all Events executable lines (193/193) plus
120/122 branches. Maximum owner CRAP remains 20 with none above 30. The only uncovered branches are
defensive null fallbacks after enum `ToString` and runtime `Type.FullName`; neither can occur for
the actual instantiated values entering those paths. Loaded product reachability in this Core-host
artifact is 80.1843% line (46,899/58,489) and 72.5950% branch (16,421/22,620); it is not presented as
repository-wide coverage because this host does not load every provider assembly. Artifact SHA-256
is `c7142a4e18e6b9de70eabd8d1fb6c0b3525dc307821bdb5292fac8aebaf574ed`.

The manual anti-pattern audit accounts for 14 new or changed methods and 16 affected cases. It finds
no issue: values and identities are exact, the clock is fake, proxies fail on unexpected calls, and
there are no sleeps, random inputs, skips, broad catches, swallowed failures, shared mutable
fixtures, assertion-free execution, or coverage-only assertions. The bounded integration helper
uses the repository harness and cancellation contract rather than timing assumptions.

Both format gates, the 77-project zero-warning Engineering build, all 6,550 tests in 23 canonical
hosts, requirements uniqueness, and the 30/30 isolated bidirectional Async analysis pass.
Package/API validation passes 18 journeys, 31 fresh packages, three isolated provider-testing
consumers, and all 30 runtime APIs. The API baseline remains 18,879 lines with SHA-256
`ab7469f985f1e269c5cceb803c06cfdd27cfe19f9b4ca51eded8f6c97857f12f`. Directives, dummy and
legacy markers, SDK pinning, source-root files, empty directories, formatting, and Git whitespace
are clean. Protected `review/` and `TestResults/` remain unchanged and unstaged.

## Iteration 118 Core Topology final research

The manually reviewed capability finishes with 100 production files and 4,331 lines. Core owns 43
files under `Advanced/Topology`, `Configuration/Topology`, and `Topology`; Abstractions owns 57
files under the corresponding three folders. This resolves the apparent `src` inconsistency:
`src/ViciOne.ServiceBus` is a single Core project, while direct siblings are independent assemblies.
Provider assemblies therefore remain grouped beneath `Persistence`, `Scheduling`, and `Transports`
instead of being nested into the Core project. An exact six-group manifest now fails if a Topology
file, namespace, or retired `Topology/Configuration` directory drifts.

The prior surface mixed contracts, policy, implementation mechanics, and four one-method observable
wrappers. The final structure keeps public read-only contracts in `Advanced.Topology`, configuration
contracts and extension vocabulary in `Configuration`, and concrete Core behavior in `Topology`.
The empty `IMessageTypeTopologyConfigurator` marker and the four pass-through observables are gone;
root topologies own `Connectable<TObserver>` directly. Correlation selectors and per-message
partition, routing, serializer, and filter implementations are internal because consumers configure
them through the retained public extension and interface contracts.

The cache now uses `ConcurrentDictionary<Type, Lazy<TValue>>` so concurrent first access publishes
one value and caches factory failures consistently. Its unused runtime-type constructor argument is
removed, invalid runtime contracts report the owning parameter, explicit correlation resolvers have
precedence, root conventions are applied consistently to current and future message topologies,
and entity collections independently enforce structural, name, and identifier identity. Entity-name
formatting evaluates at most once under concurrency and rejects empty collaborator results.
Child topology builders retain both delegated and implemented state. Required inputs, null factory
results, and null update results fail immediately at the responsible public boundary.

Application-wide conventions still freeze on first bus topology. Capability packages may add their
metadata afterward without reopening application configuration, including nullable correlation
selectors. The old default exclusion for `JsonElement` is removed because every consume topology
requires a reference message contract and the struct branch was unreachable. A real reference
contract now proves application-wide consume exclusion.

The first mutation group changed cache identity, runtime argument ownership, explicit correlation
precedence, and same-name entity conflict behavior. The 35-case Core Topology profile produced six
causal failures. Accepted hashes are
`683a18c4abc503ca843503b58f0860f7e62008f3cce7d479d170236e4b388988` for
`TopologyConventionCache.cs`,
`94a9559b73c4825ede4e59497f59cbc7d7f23bf2061dec949bf90a193909e2c6` for
`ConsumeTopology.cs`,
`ead08a8f3affd9d167200a7d4b48e60e291e2a5cd9d70bfa4e0b35fdb8474c83` for
`CorrelationIdMessageSendTopologyConvention.cs`, and
`927874f9e609e007d5f7db0ee25cba0138873dec72242da2adb0f615d2b1078e` for
`NamedEntityCollection.cs`.

The second mutation group removed the entity-name double check, discarded consume child-builder
state, accepted a null publish-convention factory result, and inverted the publish-to-send exclusion
contract. Exactly four of 50 Abstractions Topology cases failed. Accepted hashes are
`480b9bfd6e660bcabfb241d810518d7e793460706563f6e2b267a91554c3d50b` for
`MessageTopology.cs`,
`fc01975ef15f651f6a7b0e73de8a0ed7dfd916f45cfac8f9a3442e2085930a6b` for
`MessageConsumeTopologyPipeSpecification.cs`,
`47ad358d9891ea335721660a3ea2f33828b4310b778bfe1e8868225933f66b6f` for
`MessagePublishTopology.cs`, and
`8967dcc15d77607b5043a3f1adc48ae57db96738e96579deebbb8286bb46ce5b` for
`PublishToSendTopologyConfigurationObserver.cs`. Both groups were restored hash-exactly and their
focused profiles returned to 35/35 and 50/50.

Fresh complete coverage passes 3,611 Core and 692 Abstractions tests. Core Topology reaches
520/536 lines and 124/140 branches with maximum CRAP 10; Abstractions Topology reaches 495/495
lines and 157/176 branches with maximum CRAP 8. Combined reach is 1,015/1,031 lines and 281/316
branches, with no CRAP value above 30. The remaining two Core zero-method entries are instrumenter
artifacts for the already-tested one-shot separation lambda and non-generic enumerator forwarding.
The coverage artifacts are `/private/tmp/vsb-iteration118-current/core-final.cobertura.xml`, SHA-256
`7849d9d884f5da04d69434cffb973ba63e9cbf03c214c39b5446b8a7dbb3fea6`, and
`/private/tmp/vsb-iteration118-current/abstractions-final.cobertura.xml`, SHA-256
`a9ca59b210b2f7e8de35bb292cf6664c171918c28e34f9a66c3cd0d30054eedb`.

The complete package comparison records 38 intentional additions and 93 removals. No capability
entry point disappears: 18 developer journeys and three isolated provider consumers compile from
31 freshly packed packages. The 30 runtime assembly APIs match the expressly updated 18,824-line
contract with SHA-256 `493a793a915b88ac2ea9b81cb6be8057ecf9beff80535f063aab8c3040d12a4f`.
The apparent removals are empty markers, pass-through wrappers, or implementation mechanics; the
serializer extension class is renamed while retaining both public overloads.

The final test anti-pattern audit accounts for 86 changed methods and 90 cases. One unnecessary
`Guid.NewGuid` input was replaced with `Guid.Empty`; no issue remains. The concurrency tests use
explicit manual-reset signals and bounded cancellation safeguards, not sleeps, and independently
kill both race counterchanges. Every other case constrains exact state, identity, order, type,
exception, parameter, address, or filter behavior. There are no skips, broad catches, swallowed
failures, shared mutable fixtures, assertion-free paths, or coverage-only assertions.

All three locked restores, both final format gates, and the serial warnings-as-errors build pass.
The build covers all 77 Engineering projects with zero warning and error. The canonical 23-host
profile passes 6,638/6,638 with no skip. The focused Async and source-layout gate passes 54/54.
Requirements comprise 5,027 unique variants across 36 valid JSON files. Topology old names and
paths, source directives, SDK patch pinning, empty directories, formatting, and Git whitespace are
clean. The sole product `NotImplementedException` occurrence classifies the real BCL exception as
non-retryable; RabbitMQ's `NotImplemented` constant is broker reply code 540, not placeholder code.
Protected `review/` and `TestResults/` remain unchanged and unstaged. The overall A+ goal remains
active for the remaining source owners.

## Iteration 119 interim retry/rescue research

Direct consume-policy tests prove that the untyped wrapper previously reported a null context as
the wrong-type `ArgumentException`. Both typed and untyped wrappers now reject null before type
projection. Missing custom policy contexts and missing nested consume contexts have independent
typed/untyped checks. The shared in-memory test fixture also exposed an inaccessible private
DispatchProxy interface; that test-only interface is now publicly accessible to the dynamic proxy.

Four direct rescue-projection cases preserve message, consumer, exception identity, cached failure
snapshots, injected UTC time, and fault headers, and reject every null constructor dependency.
Every concrete rescue projection reaches 100% line and branch execution in the accepted Core run.
The expanded retry-helper suite checks both log-switch overloads, operation-owned cancellation,
asynchronous pre-retry and terminal notification ordering, exact token/failure propagation, and
null callback tasks. Bypassing each asynchronous wait kills exactly its owning test among 19 cases;
both controlled edits are restored byte-for-byte before subsequent cleanup.

Two full Core runs initially report 3,646/3,647 because the requirements projection correctly rejects
22 not-yet-registered new methods. A CTRF report identifies that exact verifier and all tuples;
the projection is updated manually. Subsequent accepted Core runs pass 3,647/3,647 with no skip.
The v4 artifact predates the later shared Split correction and is not a final-source acceptance.
It records owner line 1,213/1,476 (82.1816%), branch 497/638 (77.8997%), and maximum CRAP 30.
The instrumented Core graph is 77.5322% line and 70.2701% branch; no provider-wide coverage claim
is inferred from that one host.

The residual configuration gap reveals a causally related Abstractions defect in
`SplitFilterPipeSpecification`: the adapter never delegates `Validate` to its inner specification.
A public rescue ContextPipe regression is red because invalid inner configuration is applied
without exception. The corrected adapter preserves the exact inner validation sequence, rejects
null sequences and all required input references, and validates builder/filter admission. Its
entire source and all comments are read and corrected manually. Scope includes four new direct
Abstractions tests and three rescue configuration tests. The serial Unit build passes with zero
warnings and errors, and the direct suites pass 9/9 and 4/4. The complete canonical run executes
23 hosts and 6,682 cases: 6,681 pass, one fails, none skip. All 23 CTRF summaries agree. The sole
failure identifies the new asynchronous test
`HandledFailures_ProduceConsumeAwareStateAndTerminalNotification`, whose missing `Async` suffix
is an authoring defect. Both its method name and requirement tuple are corrected manually; the
guard is unchanged. Corrected-source acceptance is rerunning. Final-source mutation/coverage,
repository and package/API acceptance remain open at the intermediate Git checkpoint.
No generator rewrites source, tests, or comments.

### Iteration 119 counterreview causal follow-up

The separate internal reviewer reads all 75 changed production/project paths and all 13 changed
C# test/helper paths at checkpoint 97c1b364bf37ed48387373f5feee3e23f065fd27. It finds five
production defects and one visibility-guard gap. The unchanged product source executes 57 focused
cases with 18 exact failures. The corrected source passes 57/57 and then 59/59 with the added
typed/untyped failed-representation ownership cases. New and strengthened assertions cover exact
observer failure identity, business/effect counts, pending observation, independent callback
cancellation, fractional exponential bounds, positive growth, initial ownership failures, removed
cancellation registrations, and both typed/untyped invalid callback results. Nine new method
projections are registered manually. The detailed requirement map and controlled-counterchange
results are in evidence/WP-F2-SERVICEBUS-SOURCE-ARCHITECTURE-A-PLUS-01/ITERATION-119/VALIDATION.md.

Seven separate runtime counterchanges each compile and kill their causal oracles. Mutation execution
also identifies an author-discovered false-confidence defect: the visibility guard obtains Core
through IBus, which now belongs to Abstractions. An isolated child-namespace visibility mutant survives
the old guard but fails 1/1 with the existing explicit Core anchor and exact assembly identity.
The activity counterchange is repeated separately. Complete manual reads of the adjacent DI,
telemetry, and foundation catalogue rules establish that the DI/telemetry absence checks also need
both foundation assemblies. The existing foundation/removal anchors already identify both correctly.
This scope extension strengthens assurance without changing delivery functionality. Protected trees
remain untouched. Final-source coverage, complete gates, and final publication are still pending.

### Iteration 119 nested lifecycle ownership research

The second checkpoint is verified on origin at 2968483f0a085cbb9a8362a8d6828b5c84dd9654.
Against that unchanged product source, all 15 new nested observer cases fail while the existing
35 cases pass. The five-phase matrix includes synchronous throws, faulted tasks, and pending
barriers, with exact identity/effect/disposal/notification oracles. The first internal payload-and-lease
correction passes 50/50 after a zero-warning/error build. Ownership is exact-exception scoped,
not blanket retry suppression, and active leases clear state after operation completion.
Independent context projections, later reuse of the same context and exception, nested policy
callbacks, and independent policy cancellation now extend the causal boundary checks.
Recognition-removal counterchanges, corrected-source review, and final-source acceptance remain open.

The extended 76-case suite first records five causal callback/independent-cancellation failures
and then passes76/76 after the complete preparation stage is owned. A full read-only internal
review confirms the original ordinary in-memory observer correction but finds four adjacent open
boundaries: redelivery composition, cleanup replay, infrastructure ownership and stale terminal
business payload lifetime. The exact15-file/33-method/76-case scope is recorded in
ITERATION-119/INTERNAL_NESTED_OWNERSHIP_REVIEW.md. The complete current Core host passes3709/3709.

Three separate compilable counterchanges kill their expected oracles: lifecycle recognition
39/76, unreleased ownership exactly2/76 reuse variants, and missing independent source delay
cancellation exactly1/76 timer variant. Each accepted source is restored byte-for-byte.
Coverage analysis additionally finds default DebuggerNonUserCode exclusions omit critical retry
and redelivery state machines; the official Microsoft configuration establishes that defaults
must be explicitly disabled. The handwritten src-scoped coverage profile includes auto-properties
and all source attributes while excluding test assemblies. The accepted-source expanded-profile
repeat will determine the new denominator; historical percentages are not directly comparable.
## T64 assembly discovery packet

Scope: `AssemblyScanner`, `AssemblyFinder`, `AssemblyTypeCache`, and their existing
Core tests. The finder already tests recursive discovery, manifest identity,
invalid images, missing files and executable opt-in. The scanner tests only
caller discovery; static source pairing misses indirect use. A fresh Roslyn
pairing inventory was consumed once for this packet. Requirements: repeated
registration must yield one assembly and one type, and each of four entry
points must independently discover the type; namespace inclusion and
explicit type exclusion must compose; filename inclusion and exclusion must
apply case-insensitively in a path scan. Tests use the public scanner API and
three real copied assemblies with distinct manifest identities, with exact
type/count assertions. No reflection-only
or coverage-only overload tests are planned.
# T108 — ActiveRequest settlement ownership

Target inventory: `src/ViciOne.ServiceBus.Abstractions/Util/ActiveRequest.cs`
and its owner `RequestRateAlgorithm.cs`; existing contract suite is
`tests/ViciOne.ServiceBus.Abstractions.Tests/Util/RequestRateAlgorithmTests.cs`.
The Microsoft find-untested-sources Roslyn pairing was run once at T107;
the target is directly paired, so no repeated project-wide discovery is
needed. Existing xUnit tests use `TestContext.Current.CancellationToken`,
bounded `WaitAsync`, `RequirementCoverage`, and observable request counters.

Acceptance checklist: (1) a lease completed twice must not decrement active
count or release a second request permit; a healthy later lease must still
work; (2) completion after disposal must fail without changing the already
released capacity; (3) completion racing disposal must settle the lease
exactly once, permit either winner, and leave a healthy successor. Existing
source has a non-atomic `_completed` flag and unconditional
`EndRequestAsync` on every completion, so these scenarios can corrupt the
owner's accounting. Tests must assert visible capacity and counts, not just
exception types. This is a product correctness and concurrency packet, not a
coverage-only test.

Read-only Red Team review found that the public `ActiveRequest` constructor
can invent an unowned request without acquiring a permit. Settling it
decrements owner counters and releases capacity never acquired. A fourth
red-first API-boundary test fails on the public constructor; the constructor
must be internal so external package callers cannot fabricate an owned lease.
Friend assemblies remain trusted internals; the current repository has one
production constructor call in `RequestRateAlgorithm.BeginRequestAsync`. The
packed API baseline must reflect that deliberate
pre-release public-surface correction.
# T109 — adaptive request-count transaction

Bounded target inventory: `RequestRateAlgorithm.cs`, its options and
`ActiveRequest.cs`, plus the existing `RequestRateAlgorithmTests.cs`. The
Roslyn source/test pairing from T107 already pairs the algorithm with the
existing suite. xUnit v3/MTP tests use bounded `WaitAsync`,
`TestContext.Current.CancellationToken`, visible `RequestCount` and
`ActiveRequestCount`, and requirement tuples. Microsoft code-testing-agent,
test-gap-analysis, assertion-quality and run-tests guidance was read before
test editing.

Reviewing the complete adaptive-count path finds a real candidate: a full
batch grows request parallelism to four; one empty completion publishes two
as the desired count, then waits to drain two permits. If its caller cancels
after one drain, the current code leaves the published count at two while
the physical semaphore capacity remains three. A later third request can be
admitted above the reported limit. Acceptance requires canceled adjustment
to keep the published count and permit capacity consistent, existing leases
to settle exactly once, and healthy later admission at the correct boundary.
Also check concurrent completions while a shrink waits, since adjustment
ownership must be serial, and disposal while the adjustment is pending.
# T110 — transformation property and nested context contracts

Target inventory: all ten C# files in `src/ViciOne.ServiceBus/Transformation`,
the public `TransformSpecification` and `TransformFilter` entry points, and
the two existing `Transformation` test files. They were read in full before
editing. The existing T97 strict profile has 90 uncovered of 173 observed
physical lines in the nine executable transformation files, mostly envelope
forwarding getters. The T107 Microsoft Roslyn pairing map pairs only
`PropertyTransformContext` directly; the other internal classes are reached
through pipeline composition, so static unpairing is not proof of no runtime
coverage. Relevant Microsoft code-testing-agent, coverage-analysis and
find-untested-sources guidance has been read; the existing pairing inventory
is reused without rerunning project-wide discovery.

Behavioral acceptance: (1) an input-bearing initializer whose inherited
`TransformContext` says `HasInput=false` must not invoke a source-property
provider or user transform callback; (2) a present source, including an
explicit null property value, must pass the exact source input, metadata,
`HasValue` and cancellation token to the callback; (3) raw transform
initialization must reject a null provider task with a clear diagnostic and
must reject pre-cancellation before invoking the provider; (4) nested
property conversion must preserve its source/metadata and observe a pending
initializer to completion, including fault identity and invalid null-task
diagnostics. Tests assert visible callback counts, identity, message value,
metadata and exact failure class, not just code execution.

# T111 — transform-filter operation-token handoff

Inventory: `src/ViciOne.ServiceBus/Middleware/TransformFilter.cs` and its four
`IFilter` paths (send, consume, execute, compensate), the corresponding
`SendTransformContext`/`ConsumeTransformContext`, the `IMessageInitializer`
contract and implementation, and the existing `ActivityTransformAsyncTests`
plus pipeline tests. The existing T107 Roslyn pairing and T97 coverage profile
are reused; no global discovery or measurement is needed during this packet.
The xUnit v3/MTP fixture already observes the initializer's inherited context,
pending completion, downstream call count and fault identity, but does not
record the explicit `InitializeAsync` operation token. All four filter paths
currently call that overload with its default token; `IMessageInitializer`
documents that the token cancels initialization. This is a product-level
handoff gap, distinct from the inherited `PipeContext.CancellationToken`.

Acceptance: each of the four real filter entry paths must pass the exact
context operation token into `InitializeAsync`; a pre-canceled token must
prevent downstream delivery and preserve the original cancellation token in
the failure. A pending accepted initializer must still be awaited, and its
non-cancellation failure must remain the original failure. Tests must assert
token identity, input/context identity, downstream count and result/failure;
coverage-only cases are inadmissible.

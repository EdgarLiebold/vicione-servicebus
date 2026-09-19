# Iteration 223 — active research and source-only checkpoint

The ServiceBus A+ goal remains active. This checkpoint is not an A+ admission:
the lead's personal full-owning-Core-test-project read gate (§4.3) is only
86/702 tracked files at the hash-bound baseline (see
`../core-test-project-full-read/partial-ledger.md`). The historical ordering
deviation remains open; the source inventory's previously admitted total
remains 831/4,118. No new tests were designed, edited, or admitted here.

Three disjoint Sol 5.6 xhigh read-only audits examined saga connector,
ResourceCache and request-client source. Their findings are candidate defects,
not independently mutation-qualified fixes:

- ResourceCache capacity counts indexed entries and pending factories, not
  evicted resources awaiting asynchronous disposal. Repeated `AddAsync` with
  blocked disposal may leave arbitrarily many live cache-owned resources with
  `Capacity=1`. A correct bound needs accounting for retiring ownership and
  capacity-change signaling outside the index and no lock held across await.
- A direct `AddAsync` for a key reserved by a pending `GetOrAddAsync` factory
  can commit the same key, block `RemoveAsync` while the factory remains
  pending, then fault the factory's single-flight callers on duplicate commit.
- Request deadlines/timers are initialized after endpoint acquisition and send
  pipeline execution. An endpoint that never becomes ready can exceed its
  absolute deadline indefinitely; a response received before a hung send task
  completes also suppresses the timeout while response completion awaits send.
  Terminal timeout may not cancel the send if `_sendContext` already completed.
- Reusing a saga specification for a second direct `ConnectSaga` accumulates
  terminal filters in persistent configurators and can duplicate saga
  consumption. Existing tests intentionally assert repeated-build accumulation,
  so a connection-specific snapshot or explicit single-use contract needs
  careful design rather than changing general Build semantics.

One smaller request-client error-precedence defect has a source-only correction:
the constructor starts the send before the caller can register typed response
handlers. If endpoint acquisition faults immediately, `Fail` used to set the
terminal bit and later `ResponseAsync` synthesized cancellation. `Fail` now
stores the exact response exception under the handler lock; a subsequent
registration returns that exception instead of false cancellation. Genuine
cancel/dispose paths still return canceled tasks. This was inspected against
the existing request-client lifecycle tests but has **no new causal regression
or mutation evidence** until the §4.3 gate is dispositioned.

Verification: strict Core Release build, 0 warnings and 0 errors; first full
MTP run 6,216/6,217, one 30-second timeout in
`StateMachineNestedRequestIntegrationTests.NestedRequestCompletion_ResumesTheOriginalRequestWithTheExactResponseAsync`.
The exact test passed alone 1/1 on retry; the following unfiltered full Core
run passed 6,217/6,217, 0 skipped. The one-off failure is retained as an
observed instability, not declared fixed. `git diff --check` passed.

No remote push is made at this checkpoint. The remote-specific private-source
exfiltration confirmation is still absent; this does **not** pause or block
the active local goal and personal read/verification work continues.

Later read-only continuation: the personally complete Core test-project ledger
advanced from 86/702 to 100/702, adding Courier activity/host contracts,
subscription capture, argument integration and InMemory conversation inheritance.
The additional reading does not retroactively qualify the source-only fix or
resolve the historical §4.3 ordering deviation.

Next read-only checkpoint: 110/702 complete, including the shared Courier
fixture, convention/JSON registry, executor and routing-slip request bridge,
success/fault integration and unavailable consume-context overload boundaries.
No source/test files changed since the successful full-suite checkpoint.

Further personal read: 115/702, adding consumed-context outgoing metadata,
deserializer completion/payload ownership, context scope/proxy forwarding and
request/response envelope flow. Only the read ledger/status changed since the
last full-suite verification. The goal stays active after this checkpoint.

Continuation checkpoint: 120/702, adding activity-context adapters, saga
consume-context proxy, outgoing wrapper, response lifetime and endpoint/fault
resolution tests. The effective Core test-project gate remains incomplete;
provider parity and causal mutation evidence remain outstanding. No product or
test file changed since the last full-suite verification.

Continuation checkpoint: 133/702 personally read Core test-project files,
adding the 11-route endpoint matrix, message consume forwarding, consumer
metadata/configuration, batch context/factory/collector lifecycle, instance
registration and disposal contracts. This is a read-ledger checkpoint only;
the effective gate, provider parity and causal mutation evidence remain open.

Continuation checkpoint: 160/702 personally read Core test-project files.
The newest set covers consumer concurrency and dynamic connections, batch
timing/cleanup, initializer converters/providers, job API fixtures and the
embedded requirement-projection assertion. Driver/fixture closure and all
remaining project files are still required before test-design clearance.

Continuation checkpoint: 170/702 personally read, with assembly-wide topology
and correlation bootstrap, timer/key/job fixtures, nested pipe composition,
JSON defaults, topology naming and state-machine ownership/status contracts.
The aggregate test read gate remains incomplete and the goal remains active.

Continuation checkpoint: 180/702 personally read, adding deterministic
partition-hash vectors, retry-policy context budgets, MessageId/JSON wire
boundaries, saga query caching, provider discovery and TCS/readiness contracts.
These focused tests do not replace the remaining runtime/provider acceptance.

Continuation checkpoint: 190/702 personally read. The last ten cover
initializer array/provider fixtures, generated message implementation and
assembly scan cache, InMemory receive context, job identity, metadata codec,
UTC timestamps and sent-message metadata. No product/test code changed.

Continuation checkpoint: 220/702 personally read Core test-project files,
adding health/report metadata, serializer and raw JSON round-trips, scheduler
guards, retry/saga boundaries, future behavior fixture, job clock settings and
message-data API snapshots. Only ledger/status changed; the historical §4.3
ordering deviation and the effective full-read gate remain unresolved.

Continuation checkpoint: 240/702 personally read, including volatile outbox
fault/result ordering, scoped send-pipe ownership, future state/location,
provider object graphs, typed job-state projection, constructor-only JSON and
historical cron DST boundaries. Product and test sources remain unchanged.

Continuation checkpoint: 250/702 personally read Core files, adding
container-discovered job execution, forward-expiration outbox behavior,
fork/join failure retention, serializer/body oracles, saga context factories
and application topology freeze. This does not close the 702-file gate.

Parallel-source checkpoint: 280/702 personally read Core files. Three
disjoint Sol 5.6 xhigh agents are examining Caching, Clients/Requests and
Sagas/Configuration; their source-only patches are not yet accepted or
verified. The Saga connector replay patch has a preliminary lead diff review.
The request deadline candidate conflicts with an existing assertion requiring
post-deadline pipe execution; this will need explicit causal test disposition
after the full-read gate. Ledger/status may be committed independently of
these unverified source edits.

Next personal-read checkpoint: 290/702 Core files. The latest set covers
message-data converters, cron schedules, raw JSON type admission, message
journal input snapshots, initializer request/response flow, activity outcome
and scoped request-filter faulting. The three source-only patches are now
present and under lead review; a combined Core build is in progress. No new
test code has been designed or edited. A build result alone will not qualify
these fixes as causal/mutation-tested A+ closures.

Combined patch build completed successfully in Debug with zero warnings and
errors. The repository's known MTP `dotnet test --project` discovery problem
returned zero tests; direct execution of the built host is required. Its
first unfiltered run reached 5,987 completed and two failures, then stuck in
`ResourceCacheObserverAndDisposalTests.Eviction_AwaitsAsynchronousDisposalAndReleasesTheResourceExactlyOnceAsync`;
that test asserts a committed replacement before the blocked old disposal is
released, which conflicts with the new hard capacity bound. The predicted
`RequestClientMetadataTests.RequestDeadline_DoesNotRestartAfterDelayedEndpointAcquisitionAsync`
failed at its old `PipeOutcome` wait after 30 seconds. The run was cancelled
with two Ctrl-C signals rather than claiming a pass; a filtered rerun is
needed to expose the other failure and unaffected-test result. Lead personal
Core-file reading meanwhile reached 300/702. No test edits before §4.3 gate.

Filtered direct-host rerun, excluding only the hanging cache test and the
30-second deadline test, finished: 6,214 passed, 0 skipped, 1 failed. The
sole failure is
`SagaConnectorSpecificationAdapterDeepContractTests.BuildMethods_AppendInOrderAcrossCallsWithoutMutatingPreviouslyBuiltPipes`:
public direct Build methods must retain accumulated filters and the same
message configurator instance. The Saga source agent was asked to preserve
that contract while making repeat `ConnectSaga` use fresh connection-local
composition. This is a source revision in progress, not a test-oracle change.

Personal Core test-project read advanced to 310/702, adding receiver/fabric
exchange routing, dispatcher, observer, future configuration, rescue
projections, InMemory publish overloads, saga partition keys, request-outcome
TTL and converter contracts. The Saga revision now retains the direct Build
contract and isolates only built-in `ConnectSaga` composition; the combined
rebuild and filtered regression rerun are pending. No new test source work.

Combined revised source built in Debug with zero warnings/errors. The first
filtered full-host run was 6,214 pass and one unrelated Batching integration
failure; that exact Batching test passed alone (1/1). The next filtered
full-host run passed 6,215/6,215 with zero skips, excluding only the two
documented cache/deadline tests whose old oracles now hang. The 11-file
source-only correction was committed as `4fdb4063b`; this is staged
engineering progress, **not** A+ acceptance or causal mutation proof. The
personal Core project read advanced to 320/702, adding mediator request/body,
cron, JSON body, topology, endpoint cache, scoped DI, transaction and
outgoing-options contracts. The historical §4.3 ordering deviation remains.

Further continuation: personal Core read is 330/702. The added files cover
application Protobuf/XML JSON shapes, retry factories, reflection property
cache, cron identity, bus probes, InMemory saga repository and scoped
scheduling, JSON polymorphism, in-memory outbox scheduler argument boundaries
and application schedule options. The committed source patch passed a fresh
Release Core build with zero warnings/errors. A filtered direct Release-host
test run, excluding the same two documented stale/hanging oracles, is in
progress; neither an unfiltered green result nor provider acceptance is
claimed.

The filtered direct Release-host run completed 6,215/6,215 passed, zero
skipped, with only the same two explicitly excluded legacy-oracle tests.
This confirms the unaffected Core surface under Release; it does not resolve
the two test gaps, source-read gate, mutation/coverage/CRAP or real-provider
acceptance. The active A+ goal continues.

Personal Core-file reading advanced to 340/702. The additional ten cover
message observers, dictionary headers, transport-text normalization, scoped
bus contexts, probe contracts, job progress/distribution, Future request
consumers, the state-machine scheduler fixture and receive lifecycle events.
No source or test files changed after the source-only correction. The
unfiltered gate remains red because the two test oracles still need causal
revision after full owning-project reading.

Source-read reconciliation packet: the lead personally reread all 11 current
files changed by the cache, request-deadline and saga patches and recorded
their exact line counts and SHA-256 in
`../iteration228/source-admission.tsv`. Every row was rechecked against the
working tree (11/11 matched). This is an incremental current-content
admission only; the historical 831-file aggregate includes chain metadata
whose early per-path manifests are not retained, so no exact whole-source
set-equality claim follows from it. The 4,118-file goal remains active.

Personal Core read advanced to 350/702 while three disjoint Sol 5.6 xhigh
agents perform read-only source audits of Scheduling, RetryPolicies and
Serialization/Admission. The ten new files cover state-machine cancellation,
scoped consume endpoint providers, job bus lifecycle/property metadata,
pipe-supervisor failure/shutdown, multi-consumer harness, endpoint convention
overloads, request initializer forwarding and virtual-time redelivery. Agent
audits are discovery only and do not count toward lead personal source reads.

Personal Core read advanced to 360/702. The next ten cover host handle,
state-machine callback, InMemory scheduled publish, inactivity observation,
bus health, Courier activity harness, consumer-agent lifecycle, dynamic
contracts, Future dispatch and InMemory outbox attempt isolation. Three
disjoint Sol 5.6 xhigh audits identified candidate source defects in
Scheduling, RetryPolicies and Serialization/Admission. Those agents are now
working on source-only corrections in non-overlapping directories; their
reading does not count as lead personal source admission. No new test source
work is authorized before the full §4.3 owning-project read.

Personal Core read reached 370/702. The ten new files cover polymorphic
fault dispatch, job partition-key topology, mediator receive context,
dynamic InMemory endpoints, concurrency limits, temporal conversion,
multi-bus scope isolation, schedule initializer forwarding, MessageData
property loading and virtual-host relay. The Scheduling and Retry source
agents have submitted uncommitted source-only corrections; Serialization
admission work is still in progress. These are not yet admitted, mutation-
proven or covered by newly authored tests.

Personal Core read reached 380/702, including the complete 801-line payload
admission transport integration file. Its exact JSON boundary oracles exposed
seven new false rejections from the source agent's lazy buffer: Utf8JsonWriter
can demand a larger contiguous span near the cap than its eventual written
bytes. The agent is revising growth while retaining small-message laziness.
A first filtered Release Core run exposed 302 failures from treating missing
payload runtime as an error for direct non-DI InMemory harnesses; the lead
restored that path's previous no-runtime behavior. A fresh Release Core build
then had zero warnings/errors, the representative formerly failing harness
test passed, and the filtered full Core run reached 6,208 pass / 7 exact
JSON-boundary failures (zero skips). The two previously documented stale or
hanging cache/deadline tests remain excluded. This is not an unfiltered green
or admission closure; direct no-runtime transports still have no payload
admission, and the architecture test's literal source check is stale.

The Scheduling/Retry/Serialization source-only patch now has a ten-file
personal current-content admission in `../iteration233/source-admission.tsv`;
all 10 line/SHA-256 pairs were rechecked. The latest full Release Core build
has zero warnings/errors. Its direct-host filtered run passed 6,215/6,215,
zero skips, again excluding only the known cache/deadline stale-oracle tests.
A Release Architecture build also has zero warnings/errors, but the focused
`PayloadAdmissionArchitectureTests.CommonPhysicalSendBoundary_AdmitsBeforeObserversAndProviderIo`
fails solely because it searches for the obsolete literal
`_ = transportContext.Body.Length`; the current source materializes and
measures the same Body through a local variable. No test file is changed
before the Core §4.3 read gate. The source patch is engineering progress,
not A+ closure: causal new tests, mutations, MessagePack/custom evaluator
parity, complete test admission and unfiltered acceptance remain open.

The lead's personal Core test-project read advanced to 395/702 tracked files;
each ledger path/blob pair is present in the current tree. The fifteen newly
inspected files span initializer factories/converters, retries, MessageData,
outbox, JSON runtime isolation, kill-switch, topology and scoped request
clients. A separate source review found two concrete compatibility regressions
in the uncommitted payload-admission patch: a custom public evaluator throws
for even a tiny pre-materialized body, and MessagePack's already validated
envelope is rechecked as though entirely body bytes. A dedicated disjoint
Sol 5.6 xhigh source agent is correcting those in Serialization/Admission
and, if required, MessagePack; no tests or other source scope are delegated.
The source manifest must be regenerated after that correction. The goal is
still active; no unfiltered-green or A+ completion claim is made.

Personal Core-project reading reached 410/702 tracked files; all 410 ledger
path/blob pairs match the current tree. The fifteen later files cover
InMemory error/dead-letter movement, initializer and MessageData contracts,
request/outbox and mediator behavior, temporal JSON, host retry, consume
payload propagation, JSON type mappings, Futures, topology and test harness.
The Serialization agent's parity design now uses the public evaluator path
for bounded opaque Copy bytes and a first-party MessagePack admission marker;
its code/build/testing are still under review, so no source patch is yet
admitted or committed. Historical and current A+ gates remain open.

Personal Core read reached 420/702, with all ledger path/blob rows confirmed
against the current owning-project tree. The ten newly read files cover topic
exchange routing, send transport, scalar/property-provider conversion,
recurring scheduler contracts, consume observers, a 500-message fault storm,
DI endpoint exclusion, job start and transport formatting. The lead requested
instance-bound admission evidence for the first-party JSON bodies as well as
MessagePack, to prevent same-length replacement bytes inheriting stale
tracking flags. The agent's parity correction is still in progress.

Personal Core read reached 435/702; all ledger path/blob pairs match the
current owning-project tree. Two disjoint read-only Sol 5.6 xhigh reviews
resolved the two excluded Core-test oracles without touching files. The
cache test asserts Count=1 while the evicted entry has already left the map
and still owns retiring capacity; that failed assertion prevents releasing
the disposal probe and traps async teardown. The deadline test waits for a
pipe callback that correctly need not run once the absolute deadline has
canceled the gated send. Their post-§4.3 revisions must retain causal hard-
capacity and deadline assertions, always release probes and use bounded
waits. No current unfiltered green claim follows. The payload agent is
preserving public custom-buffer SPI; independent host threshold decisions
will be enforced post-serialization without pretending an arbitrary custom
buffer's internal allocations can be bounded by an outer wrapper.

The personal Core read is now 440/702; the five added files cover machine
definition equivalence, scoped consume-context lifetime, dictionary
initializer graph mapping, mediator observers and the shared observable
time-provider fixture. Path/blob set reconciliation is still clean. The
custom payload evaluator's public buffer customization remains supported;
the agent is adding independent immutable-policy postchecks and retains
before/during-read limits for opaque Copy. Remaining custom-buffer allocation
behavior is a documented contract boundary, not an excuse to remove SPI.

The personal Core read advanced to 456/702; ledger path/blob reconciliation
remains clean. The current 14-file source-admission manifest matches its
line/SHA-256 evidence, and the Core Release build is warning-free. A fresh
filtered direct-host Core replay had 6,214 successes and one 31-second
`NestedRequestCompletion_ResumesTheOriginalRequestWithTheExactResponseAsync`
failure; the two pre-existing cache/deadline stale oracles remained excluded.
Its exact failure is under isolated review; this is not an unfiltered green.
Independent counterreview found four concrete Copy/durable-replay admission
defects in the uncommitted source patch: envelope bytes were misclassified as
application-body bytes, offload evidence vanished on replay, validation read
different bytes from those eventually sent, and extra full-size buffers could
exceed embedded memory budgets. Separate Sol 5.6 xhigh agents are correcting
the serialization path and reviewing durable persistence/replay, without test
edits before §4.3. Source closure and commit wait on those fixes and fresh
verification; the goal remains active.

Personal Core test reading reached 475/702 tracked files with a clean
path/blob reconciliation. The nested-request full-run timeout was diagnosed
as a real cross-queue Started/Completed race; a source-only default bounded
missing-instance redelivery is now under review. Durable admission metadata
now captures the originally evaluated body size, offload evidence and a
SHA-256 binding to the persisted envelope; replay dispatchers consume it.
The active Copy path uses an owned on-wire snapshot and JSON/raw extraction,
and MessagePack body extraction is being integrated by a separate agent.
The Core Release build after initial integration had zero warnings/errors;
the final combined build and tests have not yet run. The proof hash assumes
a trusted durable store, not authentication against an adversarial DB writer.

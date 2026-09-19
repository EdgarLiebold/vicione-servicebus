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

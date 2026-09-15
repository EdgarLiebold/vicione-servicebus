# Complete middleware, transaction and in-memory saga reading

Original whole-product A+ goal: active. This is personal reading and scoped
review progress, not complete Core-owner acceptance or an A+ certificate.

## Secured input and authority

Repository: `repositories/vicione-servicebus`; branch:
`feature/servicebus-a-plus-api`; input commit:
`da77c920b0df07bf867b3858176f58b545a0ddc3`.
Core owner tree: `e3be6b831183b3b65636c3b5e167c165696037d2`.
The input is the actually committed, normally atomically pushed and independently
branch/tag/peeled verified iteration-124 checkpoint, actual terminal exits 0:

- Tag: `servicebus-a-plus-iteration-124-job-and-reliability-reading-checkpoint-2026-09-15`.
- Annotated object: `96cacff1d75cff58b2dbfac982d91dc881d0621b`.
- Push receipt SHA256: `74df1fe0750c5d0393828df7db53425d8a463c2c7b87f54354b03f5a9fae2e46`.
- Independent remote receipt SHA256: `4e877953156d5ecfa8792e9de77d123e1732410494a326c8b6ff59ea5e525ac8`.
- Keyed three-reference comparison SHA256: `70f85295fe74104ba33e4edf8df2fe992d07e5b27ae569b67011dba03a22d6f4`.

Previously completely personally read authority is unchanged at this input and
is revalidated by exact matching hashes, not replaced by an advisor's summary:

| Authority | SHA256 |
| --- | --- |
| AI_WORKING_AGREEMENT.md | e6d5f60db535ad6228fca5445b68abaa7a29cd6e24b5d2f876352bc7de875d2e |
| GLOSSARY.md | 7ce780b178a971e40b57ee7ffb3bec472becdff96ef946726e0143339793adf7 |
| DECISIONS.md | 43d9d6a2969e16284706e4b644de73573930cd9fe408fd8db856340849599cd4 |
| current/README.md | a7bd61f878b84fb6f93f48402a21becd37ed253e20bab62b7026bdd165469a39 |
| CURRENT_ORDER.yaml | 49691c76d63dea1591fd5a7fa17450d1e254ec62d616ef4705aee082517bfc3d |
| FINDINGS.md | 9a913a7937a5d216edc3ce83940c215a8d47e81e5823ccab286aff0cf2eea397 |
| Selected DEVELOPMENT_SLICE.json | 5a9cd605540cd826a756e24ca3f813cb3ce9a1eaf1fc4f881b6cecfbee9c7199 |

Agreement §4.3 requires complete owning tracked test inputs, effective shared
build/package policies, fixtures, data and execution inputs before test design,
changes or Lead acceptance. Passing execution cannot substitute for reading.
Reading-only evidence does not itself require expensive unchanged full runs.
No new Core test design, test edit or full-owner acceptance occurs here.

## Actual complete personal reads and method accounting

The main completely reads all 54 files below, including every field, test method,
data arrangement, nested type, helper and comment. Long files are read in
continuous bounded ranges; an output truncated by context is not credited and
is reread before credit. All 392 methods and their actual assertion paths are
personally reviewed. No advisor is used or substitutes for main reading.

The manually authored sorted manifest binds unchanged input/final bytes. Paths
are relative to `tests/ViciOne.ServiceBus.Tests/`. Cases are independently
reconciled historical native records, not new execution or proof of strong
assertions. Each method is included in the reconciliation; none is skipped.
Per-file accounting avoids duplicating hundreds of already inspected method
names as repetitive disposition rows without reducing the actual reading scope.

| File | Lines | Methods | Historical cases | SHA256 |
| --- | ---: | ---: | ---: | --- |
| Middleware/CircuitBreaker/CircuitBreakerFilterTests.cs | 973 | 17 | 17 | 059c00067f3569cb07169c7e5f47ca83ec6f8bd1c6250c527ee5af0ef0b2ebc6 |
| Middleware/ConsumeContextPayloadPropagationTests.cs | 208 | 2 | 2 | 5b92836b39753718161abe888b6efdf1978af36e64b8deb336b7e6ff3d012ed4 |
| Middleware/ContextFilterTests.cs | 92 | 3 | 5 | 98fbca7a15619fae572e88c6d56f9c29709d70705e0bd581b5dc8fa3a6da33d4 |
| Middleware/Contexts/BindContextProxyTests.cs | 101 | 3 | 3 | b0cb3df3d3889b3ea5644021d0a6b9dab3591387d7b18b69a9a6db48a4df1535 |
| Middleware/DynamicRoutingTests.cs | 187 | 4 | 4 | f59a48e8d51d82f2863f63c663424b0b1282f9ae228d53dbb3302effa50b1586 |
| Middleware/FilterObserverTests.cs | 129 | 2 | 2 | 3412b1b7c0fc8391708e5fe21b0ab293f4d62c32a7d48e12d93e140a904e058a |
| Middleware/ForkFilterTests.cs | 83 | 3 | 3 | 284feb12bdd650a97d60b49593f1043a96c90b6adf8f27b07dcadfbf6a8cff38 |
| Middleware/InMemoryOutbox/InMemoryOutboxCheckpointTests.cs | 452 | 8 | 8 | f43a6eb522d39deee008dbe6cdf87e21df995b551a624b71bf5f7d18bb2b5916 |
| Middleware/InMemoryOutbox/InMemoryOutboxFilterTests.cs | 212 | 6 | 6 | 0b94563a111bea595404cc5f8f268fdaa143021ab2a778877443384ce6733585 |
| Middleware/InMemoryOutbox/InMemoryOutboxMessageSchedulerBoundaryTests.cs | 148 | 2 | 2 | 3329936122c48cc1ad636d61388f4c87fdc8fd638a64ff4f2bd9285853120beb |
| Middleware/LatestFilterTests.cs | 71 | 2 | 2 | e2c9af555a72deb0b617f39423b6e4817cf2add0f118674f5d11ac224a5207c3 |
| Middleware/Lifecycle/PipeContextHandleLifecycleTests.cs | 665 | 22 | 22 | b1d8fd66d85af8b734028b715089328e173875895e5cfe9c898f2361cb2991dd |
| Middleware/Lifecycle/PipeContextSupervisorFailureTests.cs | 361 | 10 | 10 | 816fa2a0bb0822f47eaaf2f4ecd4f2a0bd60231b68171aa0376c631e9bd172d4 |
| Middleware/Lifecycle/PipeContextSupervisorShutdownTests.cs | 164 | 1 | 1 | 89c8e31eb4cef5959b6eae18dc844e0daa80d12f3abc361f5dad0ea09338b5a1 |
| Middleware/Lifecycle/PipeContextSupervisorTests.cs | 158 | 3 | 3 | 17052f6f2307ce828632628de66e0e5f9fc016207cac1dc51471963ce6544691 |
| Middleware/Lifecycle/SupervisorAgentCreationTests.cs | 423 | 11 | 11 | a81e9dbdb38702ed7040c32d88892e56e0b140fe641eff45a1573657c8d6d62b |
| Middleware/Outbox/BusOutboxNotificationTests.cs | 100 | 5 | 5 | d936434f6744ae891ab2445e5f22f6e8d116af450608d241408fd74c8bf08d13 |
| Middleware/Outbox/ExpiredForwardingOutboxTests.cs | 81 | 1 | 1 | e89035bb1c37e043520ee4200ffe07e9883b40405bd509c0461acd0ca77632fd |
| Middleware/Outbox/InMemoryOutboxAttemptIsolationTests.cs | 175 | 1 | 1 | 4fda2291c45d8d94a0ded7dac909f537da68205b075e9d25346fbf3e10649bfb |
| Middleware/Outbox/InMemoryOutboxFaultTests.cs | 76 | 1 | 1 | 8a854afd08368caa092285393a4a44d40accd42a001b838203f9fa8dfc8a1526 |
| Middleware/Outbox/InMemoryOutboxMessageRepositoryTests.cs | 70 | 3 | 3 | bc41dfe5c17dd04274e0c31583d1c718cfe7121cdd877869935216587f1113a3 |
| Middleware/Outbox/InMemoryOutboxRedeliveryTests.cs | 205 | 1 | 3 | b63fb6446b681f1ad36de2d6dac011e5a779b6eed84ee8a8dd9e53cf7e0218f5 |
| Middleware/Outbox/OutboxSendEndpointBoundaryTests.cs | 200 | 3 | 3 | 892b316632d78c9eb772dcba9f0386f1bce71363fccff981022045ff8e9f46b9 |
| Middleware/Outbox/RequestClientOutboxTests.cs | 203 | 2 | 2 | 79d156a3ca618b9e835c482c8f8f4faa60150b5f5b0da0cd0753774c964b9fa3 |
| Middleware/OutboxConsumerIdentityTests.cs | 21 | 1 | 1 | 277a4386a4136e73a5b99741b8a5da0b9f018ad834f3d4bcac9ace1b161d029f |
| Middleware/PartitionerTests.cs | 450 | 12 | 12 | 5ef9cdabf65393adf4cdddcf175575233bba696fddc6d0be9f97486b204b5335 |
| Middleware/PipeCompositionTests.cs | 32 | 1 | 1 | 1654440a69015ee0e9c212d5282ffbce0c396bc2c0cd720c43d58e5e735a8ff1 |
| Middleware/RateAndConcurrencyLimitTests.cs | 966 | 19 | 20 | feb76d311ee99867a3ac6dfcab82e3c642a8ffb7a6a661ebe12df704994c4377 |
| Middleware/Redelivery/DelayedRedeliveryIntegrationTests.cs | 414 | 4 | 4 | d69db70ace6173bd467359daf89cca0670b51e2c5f64f9eb48aced73705b64a6 |
| Middleware/RequestFilterFaultTests.cs | 119 | 1 | 1 | 29e83aa954a5db059aeb918e3404189569cda1148e65bf626efdbf811a4a5c33 |
| Middleware/RescueContextProjectionTests.cs | 132 | 4 | 4 | b36e7187a0b514b3bd253147a0e8e75faa83a33000cf66ca491c8db7b9e88e34 |
| Middleware/RescueFilterTests.cs | 231 | 9 | 9 | c52d928764bc9bfe81c36e4cf5e295aec16b8713fcde86350c2a25eaff984e62 |
| Middleware/RetryBusObserverTests.cs | 52 | 2 | 2 | 17659eef67d66556021b390bafa157e528cea9268daad60408cb101b8623e0b5 |
| Middleware/RetryFilterTests.cs | 1645 | 33 | 76 | 2ca4fa3ad01bf7a2dbe7f6e5955af665ff5e0dc9afaf578f0afb3016c42560d6 |
| Middleware/RetryOperationOwnershipTests.cs | 1590 | 24 | 106 | 512f7a0040028cb13d477d9b322a7e5ce1e7987db2b7628d47e930b03bcc72f1 |
| Middleware/TenantScopeIntegrationTests.cs | 279 | 1 | 7 | eb0a1e981a3252c9ad2b963d452dd830da1b24bce634d8cac0f9b64681ad242f |
| Middleware/Timeout/TimeoutCancellationIntegrationTests.cs | 355 | 5 | 5 | 7225197991a9e0d598edeb8d128deb5df43522a97548abcb002a7a89b28f52ca |
| Middleware/Timeout/TimeoutConfigurationSurfaceTests.cs | 333 | 3 | 15 | 80327fb53ded63b3df4e4ae487b0c0798132e1e0ac78e6d7e4dbcf65e2111e64 |
| Middleware/Timeout/TimeoutFilterTests.cs | 286 | 8 | 11 | c231a848819e9c350d0e308bf29a5b1a97764eba25ec2f76732a443ddde3c3f0 |
| Middleware/TransactionFilterTests.cs | 142 | 5 | 5 | 89ec557bf5d26a34a9c2f6b49e4e4e297a50639e57b3bc4ae4c6f3d7008d8b71 |
| Saga/InMemoryRepository/InMemorySagaConsumeContextTests.cs | 100 | 3 | 8 | 0283c4a3aaecfd97be39d009706fcaf556c36c1117ecd4379619bf9347562c54 |
| Saga/InMemoryRepository/InMemorySagaRepositoryContextFactoryTests.cs | 146 | 6 | 15 | a0d10a9c4000cf7dd1041e25546f9ebf870b76eb90aa363efe1a41314d1b587f |
| Saga/InMemoryRepository/InMemorySagaRepositoryContextTests.cs | 735 | 17 | 48 | 038452ad5421798068064a02e4f1b49bebc34baf6921a2be464b9a9ee2bd9fed |
| Saga/InMemoryRepository/InMemorySagaRepositoryQueryTests.cs | 197 | 4 | 11 | e51a35b7700502a9c148bde790f90bad19421224001fda2cbb2fb2795e84d6bd |
| Saga/InMemoryRepository/IndexedSagaDictionaryTests.cs | 922 | 34 | 52 | d725da5782659678fd4d5a5bac7374bbc5d9e820d44d8554cf0fb8e4ed8cbaaf |
| Saga/InMemoryRepository/IndexedSagaPropertyTests.cs | 446 | 18 | 32 | c8518cc8f3d83c798e400cd629a1e59a9418cab221e57e65265c92c8dc5ab87f |
| Saga/SagaConsumeContextFactoryTests.cs | 82 | 3 | 11 | d946d0ae92cc9d76a8a9ffdee4cc3bb0cfc4fe9b360e04eadedbee1ccf1f8b8c |
| Saga/SagaQueryTests.cs | 40 | 2 | 2 | 54b4e3c5a87011ababdf47c06743d31687eb1178d86715cdb437da16ccb57822 |
| Transactions/AmbientTransactionBusTests.cs | 592 | 15 | 15 | efd5fe420544ccb440e78eb617721d8214d319225b57d5039461d5e32bed1f61 |
| Transactions/BufferedBusTests.cs | 835 | 19 | 20 | e5760e51202f4e7925951bdffed84760f2e42d208642725e1d8f38e75364f36f |
| Transactions/DeferredBusContractTests.cs | 310 | 5 | 5 | 26c5db799c8757a81f2ced37c056fcd7085dbd4ef7506c571951b35f6a6dcfcb |
| Transactions/DeferredBusRegistrationTests.cs | 492 | 12 | 13 | ee477132adff2aa34f0b70c2e265031f768f67788d5fc15c32ac870d6add41f0 |
| Transactions/SystemTransactionContextTests.cs | 89 | 3 | 3 | 7f08729eec86e7c1f30ee250a6c8857868f23cb382cbcacc394d8d00bdaeb443 |
| Transactions/TransactionContextExtensionsTests.cs | 89 | 3 | 3 | a56417a14fb8821917e2ff4a5e540c6593602dd22f8803d596147768b8d29457 |

Actual new reading: **54 files / 17,659 lines**: forty Middleware files, six
Transactions files and eight Saga files. All are new relative to the exact
[preceding 92-file reading record](../ITERATION-124/CORE_OWNER_READ_PROGRESS.md),
whose 25,713 lines and input bytes are revalidated without overlap. Cumulative
Core-owner reading: **146/557 files / 43,372 lines**; **411 files remain**.
The three selected folder scopes are complete at this owner tree, not the whole
owner. No additional productive-src reading is credited by this test packet.

## Settled scoped findings

**0 Critical / 4 High / 2 Medium / 1 Low; all seven remain open.** These are
manually reasoned existing-test weaknesses or readability issues, not seven
demonstrated productive defects. Acceptance targets are deferred until complete
owner admission; no replacement test is designed or authored in this packet.
Candidate mutations below are not claimed as actual executed or killed mutants.

### MT-01 — High: incomplete argument preservation oracle

`Transactions/DeferredBusContractTests.cs:129`,
`ForwardedBusMembers_PreserveEveryArgumentAndResultAsync`, arranges independent
collaborators and checks all seventeen invocation names and returned identities.
It does not compare eight forwarded argument positions:

| Invocation index | Missing exact input |
| ---: | --- |
| 3 | consume pipe, argument 0 |
| 4 | request pipe, argument 1 |
| 5 | consume-message observer, argument 0 |
| 6 | consume observer, argument 0 |
| 7 | receive observer, argument 0 |
| 8 | receive-endpoint observer, argument 0 |
| 9 | endpoint-configuration observer, argument 0 |
| 11 | endpoint configuration callback, argument 1 |

Because the recorder returns the same arranged handle without validating those
inputs, a wrong pipe/observer/callback can pass the existing assertions.
Deferred acceptance: compare each missing position to its independently arranged
instance with exact identity, retaining invocation order, options, request ID,
existing argument comparisons, handle identities, wrapped endpoint capabilities
and cancellation tokens. Single-cause input substitution must reach an assertion
failure. Legitimate recording proxies at an observable forwarding boundary are
not themselves an over-mocking or private-reflection finding.

### MT-02 — High: foreign-checkpoint non-destruction is not arranged

`Middleware/InMemoryOutbox/InMemoryOutboxCheckpointTests.cs:94`,
`CheckpointFromAnotherOutbox_IsRejectedWithoutDiscardingItsActionsAsync`, captures
one owner's checkpoint and rejects it in another owner with the exact
`checkpoint` parameter diagnostic. Neither owner is given a pending action.
Discarding pending actions before throwing can therefore pass this method.
Deferred acceptance: independent retained work on both relevant owner contexts,
observable exact owner-qualified action sequences after valid drain and no lost
or wrongly executed work when the foreign checkpoint is rejected. Retain the
original exact rejection and actual owner separation. A destructive-before-
rejection regression must fail an independent retained-action assertion, not
merely the exception assertion. The sibling failed-attempt and parent/child
checkpoint methods already have exact retained-tail and schedule-token oracles;
this is not a global claim that rollback is untested.

### MT-03 — High: failure-sensitive waits and gate cleanup are inconsistent

Continue iteration-124 JR-03, scoped to newly personally read examples:

- `Middleware/ForkFilterTests.Fork_StartsBothBranchesAndCompletesOnlyAfterBothFinishAsync`:
  entry and final operation waits are unbounded; held gates are released only
  after successful assertions.
- `Middleware/Lifecycle/PipeContextHandleLifecycleTests`:
  `PipeContextAgent_ConcurrentDisposalWaitsForOneSharedSuccessfulOperationAsync`,
  `PipeContextAgent_ConcurrentDisposalSharesTheExactFailureAndLeavesLifecyclePendingForRetryAsync`,
  `PipeContextAgent_DisposalWaitsForAndDisposesAPendingContextAsync` and
  `ConstantHandle_ConcurrentDisposalSharesOneOperationAndItsExactFailureAsync`
  contain acquisition/completion waits using only the test cancellation token
  or no token, without local deadlines and consistently unconditional
  coordinated release/drain.
- `Middleware/PartitionerTests.Partitioner_SerializesEqualKeysWhileDifferentPartitionsCanOverlapAsync`
  and `Partitioner_DisposalDrainsAcceptedOperationsAndRejectsNewOnesAsync`:
  local operation waits are bounded, but held release signals are reached only
  after assertions; cleanup must also work when an early assertion fails.
- `Middleware/RateAndConcurrencyLimitTests.ConcurrencyLimit_AppliesADecreaseFollowedByAnIncreaseToTheRunningPipeAsync`
  and `ConcurrencyLimit_CanceledDecreaseReturnsAcquiredSlotsAndKeepsThePreviousLimitAsync`:
  multiple held calls are not protected by unconditional release/drain around
  all assertions and acquisition waits.
- `Transactions/BufferedBusTests.CancellationDuringDispatch_DoesNotRetryTheAttemptedActionAndPreservesTheTailAsync`
  and selected capacity/dispatch scenarios, plus
  `Middleware/RetryFilterTests.RetryDelay_AdvancesOnlyOnTheContextTimeProviderAsync`
  and `SourceCancellationDuringDelay_PropagatesTheExactTokenWithoutAnotherAttemptAsync`:
  some entry, operation or cancellation waits lack the validated local bound.

Deferred acceptance: each named failure-sensitive wait uses the existing
validated OperationTimeout and the correct operation/test token; coordinated
gates are released in finally and accepted tasks are observed/drained within
bounded cleanup even if an assertion or wait fails. Preserve exact branch
completion, partition-key exclusion, maximum concurrency, failure identity,
token identity and retained-tail order. Broken completion/cancellation and
early-assertion regressions must terminate predictably, without leaked held
operations or replacing the original business failure with cleanup noise.
This is not a blanket finding against every await. SupervisorShutdown already
has finally release, several buffer scenarios have correct finally gates,
and circuit-breaker contention has bounded acquisition and unconditional drain.

### MT-04 — High: asynchronous completion alone is a weak virtual-time negative

`Middleware/Timeout/TimeoutFilterTests.cs:20`,
`ConfiguredDeadline_CancelsTheActivePipelineStageOnlyWhenContextTimeAdvancesAsync`,
and `Middleware/Timeout/TimeoutCancellationIntegrationTests.cs:17,87`,
`PipelineTimeout_PublishesOneFaultAndDoesNotContinueTheHandlerAsync` and
`BuiltPipeline_IsUnaffectedByLaterConfiguratorMutationAsync`, use an immediate
`send.IsCompleted` or `faultTask.IsCompleted` false assertion after advancing the
wrong clock or to one tick before the deadline. Cancellation may already have
occurred while asynchronous continuations or fault publication have not yet made
that task complete. Later advancing the correct clock and observing the right
exception does not necessarily distinguish this race from correct behavior.
This is a static oracle weakness; no historical failure or actual surviving
mutation is claimed.
Deferred acceptance: inspect an independently captured active-stage cancellation
token or equivalent causally completed timer/admission observation at each
negative phase, proving no deadline cancellation before the exact boundary and
no influence from the later/wrong provider. Then prove cancellation at the exact
configured tick/provider and retain exact timeout diagnostic, wrapped failure
and bounded fault observation. A one-tick-early deadline or wrong-provider
regression must deterministically fail the corresponding negative phase.
The explicit-provider unit method already verifies zero context-provider timers
and one configured-provider timer; do not misreport that method as missing all
provider identity proof. Rate-limit stale callbacks and retry timing deserve the
same causal-oracle follow-up, without declaring their whole features uncovered.

### MT-05 — Medium: nested request uses an unrelated one-second timeout

`Middleware/Outbox/RequestClientOutboxTests.cs:180`, `OuterConsumer`, creates its
inner request client with a fixed one-second RequestTimeout. The enclosing
harness and outer request use the validated OperationTimeout. A slow execution
environment can exhaust the inner deadline while the allowed overall interval
has not expired. No actual flakiness is observed in this packet.
Deferred acceptance: forward the existing validated request timeout policy to
the inner request, preserving the exact nested request identity, successful
response and five-stage trace that proves request bypass with deferred side
effects. Do not weaken the bypass oracle, inflate arbitrary delays or describe
this as a productive request-timeout defect.

### MT-06 — Medium: transaction filter success proves flow, not actual commit

`Middleware/TransactionFilterTests.cs:13`,
`CompletedScope_CommitsAndFlowsAcrossAnAsyncContinuationAsync`, proves a non-null
ambient transaction flows with the same identity across a genuine continuation
and that the completed pipeline does not throw. It does not observe commit of
the transaction owned by the filter. Omitting the filter's successful commit
could evade this method if the pipeline still returns normally.
Deferred acceptance: an independently arranged participant or safe post-lifetime
transaction observation must prove exactly one successful commit and no rollback
for this actual pipeline, retaining the non-null/same-before-and-after flow
assertions and local execution bound. The omitted-commit regression must fail
that independent observation. `SystemTransactionContextTests` already asserts
Committed/Aborted terminal status, and AmbientTransactionBus tests prove real
commit/rollback action routing. This is a filter-composition gap, not global
absence of transaction or commit tests.

### MT-07 — Low: compound fixture methods reduce review readability

Continue iteration-124 JR-06 with the compact `RecordingOutbox.AddAsync` fixture
in `Middleware/Outbox/RequestClientOutboxTests.cs`. Normalize compound method
bodies manually when the owner is admitted, preserving the intentional non-
execution/capture behavior and names. This is a readability candidate, not an
observed formatter failure or evidence of a productive dummy implementation.

## Positive contracts and adjacent-proof qualifications

The review uses the test-anti-patterns skill and its .NET extension for assertion
causality, boundary validation, isolation, async waits and failure semantics.
All productive implementation conclusions stay separate from test fixtures.

**Transactions:** all six files are fully reviewed. Ambient transaction methods
exercise actual immediate dispatch, exact original failure, commit abortion,
commit versus rollback FIFO work, InDoubt closure, independent transactions,
enqueue cancellation and transaction-owned dispatch after the enqueue token
ends. Eight barrier-coordinated long-running writers have bounded acquisition
and an independently expected exactly-once action set. Buffered methods preserve
all typed/initializer publish/send variants, zero pre-flush transport effects,
serialized snapshots, reentrant failure, maximum concurrency one and the exact
unattempted FIFO tail after fault/cancellation. Capacity two, canceled writers
and reservation reuse have observable follow-up behavior. Registration methods
verify public capability separation, default/typed owner bindings, actual scoped
resolution, singleton versus scoped lifetimes, idempotence, both mixed-owner
orders, capacity conflicts, secondary-bus routing and original reflection fault
identity/stack trace. SystemTransactionContext has real terminal statuses and
exact canceled-commit token. TransactionContextExtensions has genuine async flow
and exact constructor-boundary diagnostics.

**Middleware primitives:** consume-owned versus external send/publish filters
observe independently arranged headers and payloads. Context filters cover both
accept/reject decisions, held asynchronous decisions and null-task boundaries.
BindContextProxy tests prove bound-value precedence without invoking left
factories and genuine unmatched left-context mutation. Filter observers retain
exact success/failure order, context and exception identity. Fork really holds
both branches, checks that neither alone completes the fork and retains both
independent failures; its broad exception capture is qualified by exact aggregate
membership. Latest checks first/pending/latest transitions, one created instance
and retention of the actual failure-associated context. Dynamic routing checks
type/key matching, exact disconnect exclusion, parent continuation once and
specific converter failures. Consumer identity tests qualify the same name by
bus owner and endpoint; independent algorithm golden proof remains adjacent.

**Outbox and checkpoint:** signal-before-wait, exact virtual poll interval,
concurrent-waiter rejection and pre-cancellation have real connected notification
observations. Expired forwarding advances the injected clock and proves zero
persistent storage calls. Attempt isolation drains the actual harness before
exact execution/failure/success counts. The fault method actually queues its
response before throwing, then checks the exact request fault and zero response;
this limits iteration-123's weak response test finding to its named state-machine
method rather than declaring all outbox response failure untested. Immediate and
provider-owned delayed delivery failures have expected attempt snapshots,
zero dispatched side effects and terminal command identity. Endpoint tests
validate all required inputs and preserve observer handles and cancellation
identity without entering the transport. Nested requests have a meaningful
five-event order and independently expected correlation/result. Checkpoint
siblings preserve exact retained actions, parent/child rollback, schedule tokens
and failed-cancellation final cleanup. The in-memory filter verifies actual
wrapper/scope identity and exact set/consume/drain/restore traces on success and
failure. Scheduler-boundary tests reject missing inputs across the supported
overload arrangements before scheduler resolution and preserve requested tokens.

**Lifecycle:** all five files, including coordinated contexts and disposal
fixtures, are read. First creation outcome, exact fault/cancellation identity,
late-context disposal, active shutdown and one shared concurrent disposal are
observable. Failed disposal leaves lifecycle pending and a later supervisor stop
retries it with exact disposal counts. Supervisor failure paths preserve the
primary exception when stop/disposal/fault notification/logging also fail and
perform exact independent cleanup; current LogContext is restored in finally.
Borrowed-context stop failure does not skip owned shutdown. Cache/invalidation
methods assert exact old/new context traces, creation/disposal counts and
empty/active/stopped probe states. Agent creation methods validate every required
input, reject invalid outcomes, surface exact publication/factory failures and
transfer the selected cancellation token. A synchronous void StartAgent launch
is not automatically an Async-name violation merely because it owns an agent
that continues asynchronously.

**Circuit breaker and flow control:** independent throughput/ratio arrangements,
inclusive boundaries, saturation and lazy sampling distinguish closed/open
semantics without timers. Exact RetryAfter one second/one tick and a real
33-contender barrier prove one recovery probe and 32 distinct rejections.
Escalation/saturation/reset, independent dependency cancellation, already-
requested caller cancellation, unrequested matching tokens and classifier faults
have causally different follow-up outcomes. Retry/breaker/concurrency composition
retains actual retry counts and exactly two active slots. OTel tests arrange
listeners and check the complete independent signal/tag sets, version, kind and
measurement values; throwing listeners preserve actual circuit semantics.
Rate control exercises actual one-shot timer allocation/disposal, faulted permits,
last-waiter cancellation, explicit old queued callbacks, dynamic changes and
canceled adjustment rollback. Cross-message endpoint budgeting synchronizes
actual receive arrival with timer creation before virtual advance. Concurrency
tests observe configured limits one and 32, maximum/executing counters, repeated
changes, independent permit/timestamp rollback, stale diagnostics and target
routing. Diagnostic bounded polling is not an arbitrary functional delay.

**Retry ownership:** all 57 methods / 182 cases in the two large Retry files and
their complete nested policy/projection fixtures are read and reviewed. Exact
observer event sequences, zero business replay on lifecycle/infrastructure
failure, independent nested budgets, typed dispatch, source versus selected
policy token and terminal diagnostic ownership are asserted. The 20,000-budget
method observes 20,001 actual attempts; its lack of an independently measured
stack bound does not erase the real large-budget execution evidence. Pending
callback scenarios genuinely hold work and check disposal remains zero until
failure/cancellation, then one, without replaying business effects. Reused
contexts/exceptions, concurrent operations and sequential/parallel children
distinguish independent ownership. Cross message/activity redelivery tests assert
zero schedules on observer failure; stage tests preserve exact schedule/ack counts,
compound exception order, token identity and zero retained associations. Payload
getter/admission/diagnostic faults are independently arranged and check both
failure identity and absence of unnecessary callbacks. Broad catches confined
to bounded teardown do not silently turn the main behavioral assertion into
success. External custom-SPI feature equivalence is still not certified.

**Delayed redelivery, rescue, request filtering and tenants:** real in-memory
provider advance drives exactly the configured one/two/three-hour sequence;
after shutdown the recorded attempt/schedule totals are exactly four/three.
Identity replacement preserves the original UUID, matching scheduled/new UUID,
JSON content type and supported URN. Distinct policies produce the right delay,
redelivery counts and exactly one terminal fault after drain. Outbound delivery
has its independent payload and no inherited redelivery header. Rescue retains
exact handled/ignored/aggregate/failing-rescue identities and custom/context-pipe
projections; invalid inner validation prevents Apply. Fault projections cache
snapshots and include the exact injected timestamp/header schema. A scoped
request filter emits the exact typed request fault while the consumer count stays
zero. Seven tenant pipeline arrangements exercise open/typed send/publish,
retry and activity composition, initializing dependent scoped components before
resolution and checking the known tenant/correlation/attempt count. The local
TenantDbContext is a scope-order fixture, not an EF/cloud acceptance claim.

**Timeout:** all three files are reviewed. Public capability tests independently
enumerate supported scopes, setter-only options and internal implementation
visibility; scope callbacks are actually invoked to produce one specification,
not merely captured as non-null. Unit tests preserve selected caller cancellation
including a child layer, exact independent cancellation, deferred consume
completion, timer cleanup and configured-provider override. Real harness stop
is distinguished from handler-independent cancellation and pending retry stop
has exactly one attempt/no fault after shutdown. These positives do not repair
MT-04's timing-negative weaknesses or certify every scope runtime path.

**Saga:** all eight files and complete state/index/lease fixtures are reviewed.
Removed loads do not release another owner's saga lease; repeated/concurrent
disposal cannot release a newly acquired lease. Required inputs take precedence,
explicit versus consume cancellation is selected exactly, live mutable state
identity remains unchanged and stale context removal cannot evict a replacement.
Initial dictionary ownership is retained during held creation/removal and only
released after the final active user or exact factory failure; barriers are
bounded and release/drain occurs in finally. Query results preserve independently
captured registered UUID snapshots despite callback retirement and ID changes.
Dictionary/property tests distinguish reference identity from equal wrappers,
mutable hashes/keys, null buckets and bucket-versus-wrapper counts. Registration
is staged before publication, failed/reentrant admission remains reusable and
exact correlation invariants remain enforced. Interface, explicit/default,
inherited/overridden, hidden and closed-generic property shapes have independent
getter counts and exact fault/retry behavior. Unsupported metadata gets property-
specific diagnostics. Callback membership/value snapshots are materialized and
callbacks can observe the dictionary from another thread outside its lock.
SagaQuery retains its original expression/cached delegate while evaluating actual
mutable values. None of these in-memory fixtures certifies external persistence.

Adjacent priorities remain qualified: immediate-completing NestedPipe does not
alone prove awaiting a held child; a total of 100 partition deliveries does not
alone prove a unique per-ID set; timeout snapshots before final drain are not
global no-duplicate fault proof; registration shape tests are not every runtime
feature path. Strong sibling methods limit local findings but never justify
assuming unseen owner files or productive implementations are correct.

## Actual diagnostics and proportionate validation

Own raw directory: `/private/tmp/vsb-iteration125-core-middleware-read.X9XrSI`.
Read-only diagnostic stdout is preserved and personally inspected; all three
binding/enumeration commands terminate 0. The manual-report and tracked owned-diff
checks also terminate 0. The new untracked packet is checked separately for
trailing whitespace, CRLF and its final newline; Git's tracked diff check alone
does not inspect an untracked file. Binary `.b` equality avoids encoding-sensitive source
comparisons; textual source/report parsing uses UTF8 and compatible APIs.

| Actual receipt | SHA256 | Outcome |
| --- | --- | --- |
| read-bindings.log | 13b0bf5df0f5e54cb7495403843488488260c33611d13664480920a43f1e0a37 | 54 unchanged files / 17,659 lines; owner 557 |
| previous-read-binding.log | 100d89d1c17e071d229dbb5edbae96bbef872f0c8dfea08e5952d443f1d60059 | 92 prior unchanged files / 25,713 lines |
| reviewed-native-methods.log | 8e389a1458e9089f6b05c4c57c90d52c16c2af00e21b254a5b7910005fa64116 | Every selected declaration matches 392 native methods / 637 historical passed cases |
| manual-report-check.log | 0e549b63cbed1178b6f26b17420b2f12df70b49d7f723fba009dbc4a581e07b0 | 54 handwritten sorted unique manifest rows; all line/hash/method/case values and three raw hashes exactly match |
| owned-diff-check.log | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 | Tracked owned diff whitespace 0, empty stdout |
| new-report-whitespace.log | 07369fa19c8ea6b34bd91706299c434c76997835f5e69d095d661dcb1042ee1e | New packet has no trailing whitespace/CRLF and ends with a newline |

The historical native input is iteration-123's actual complete Core report:
`/private/tmp/vsb-iteration123-saga-core-read.oI6Ada/final-core-tests/edgar.liebold_Edgars-iMac-2_2026-09-15_17_43_15.966675.ctrf`,
SHA256 `69fdb2715a26518d9ead5d57ba55be7d433356527cbbdfda802c310f24802ea6`.
Its summary and all 4,007 records are independently verified passed, no other
status. The Core tree is unchanged, so exact selected case membership is usable
historical evidence, not fresh iteration-125 execution or mutation effectiveness.
Lightweight Fact/Theory declaration reconciliation is not a language-complete
parser. Whole-owner admission still requires the prescribed full parser and
exact `Git files = personally read files` closure with all effective inputs.

The latest actual fresh strict builds and native runs remain iteration 123:
Core 4,007/4,007 and Architecture 439/439, zero failures/skips, with the actual
bidirectional Async case passed. No productive source, test, project, dependency,
directive or execution gate is changed here. Consequently no identical full
build/test replay is performed for this reading-only packet. Current gates are
exact input/prior-read bindings, handwritten manifest/method/case reconciliation,
receipt hashes, unchanged executable source/test scopes and four owned-file
whitespace checks. Future code/comment/test changes must use their actual
affected proof contracts; this is not a permanent execution waiver.

Only this manually authored packet and three test-agent history prefixes are
written with apply_patch. No generator writes code, comments, tests or reports.
Protected `review/**` and `TestResults/**` are not enumerated, read, edited or
staged by the main. Unrelated user-indexed/worktree changes are preserved.

## Source grouping and continuation

The source-root question is resolved against the actual Git tree: the only
direct src-root file is shared `Directory.Build.props`; no C# lives there.
`src/ViciOne.ServiceBus` owns the Core assembly, not an umbrella for optional
SDK projects. Retain independent Abstractions, Sagas, Courier, JobService,
Mediator and other capability/package owners as siblings. Persistence,
Scheduling and Transports are provider/integration families; individual owners
remain below those useful categories. A file belonging to Core must be inside
Core's own type/namespace structure. Do not nest independent projects below
Core merely for visual uniformity, create recursive SDK source-exclusion
workarounds or remove features to fit a directory label. No relocation occurs.

Continue the remaining 411 owning tracked files in larger connected packets,
then exact independent repairs and connected runtime/API contracts with real
red/green and effective one-cause mutations. Earlier NST/SMR/JR findings,
declaration/cache/pre-initialization identity, rollback/recovery/cancellation,
full personal productive-src reading/manual comments, type/file/folder and
greenfield feature equivalence, legacy/directive/dummy closure, API metadata,
package baseline, genuine durable/external-provider acceptance and current
whole-product line/branch coverage plus CRAP stay in the original active goal.
Checkpoint security is credited only after actual normal commit, annotated tag,
atomic non-force push and independently keyed branch/tag/peeled verification,
not by the planned security action in this reading record.

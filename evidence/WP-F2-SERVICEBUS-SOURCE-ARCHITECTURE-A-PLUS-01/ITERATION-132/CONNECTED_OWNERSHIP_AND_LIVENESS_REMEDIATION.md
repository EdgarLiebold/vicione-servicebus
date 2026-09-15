# Connected ownership and liveness remediation — iteration 132

## Outcome and scope

The original whole-product A+ goal remains active. This iteration completes the
effective Core reading admission and closes four narrowly identified findings:
CS01 constructor-failure allocation proof, MD01 shared mutable Mediator MIME state,
H01 unbounded synchronous-filter test join, and H02 scheduler-based readiness-test
assumptions. The final unfiltered native Core run passes **4,011/4,011** cases with
zero failed, skipped, pending or other cases. Five independently compiled semantic
counterchanges are killed at the intended assertions and then SHA-restored.

Secured input: `3e4eae03435f3b7343bb63a1eac66eeca2269139`.
Input Core tree: `e3be6b831183b3b65636c3b5e167c165696037d2`.
Input source tree: `6cdb45a5e6a1e5205c2bf5dccab70e8f20df2bf6`.
No feature, public API, provider, transport, shared test-helper runtime, dependency,
workflow or platform-selection contract is removed. The unused non-generic mutable
MIME holder is an internal implementation detail, not a compatibility feature.
All changes to source, assertions, comments and bindings, and this report, are
manually authored. No authoring generator is used; ordinary existing compiler/
protobuf build tooling is not repurposed to write code comments, tests or reviews.

## Main FULL reading and admission

The main completes all 43 direct support-project inputs through EOF: 4,311 physical
lines, including every helper, fixture, comment and both complete package locks.
The preceding Core owner reading remains 557/557 inputs and 143,127 physical input
lines. Eight additional graph inputs/478 lines and nine productive project files
are fully read. The five previously read graph inputs/443 lines are retained.

The exact sorted union is 621 distinct paths at the secured input. A read-only
check verifies Git tracked paths equal the admitted read set, and every work-file
byte equals the input Git blob. Full JSON/XML parsing and the strict Release
compiler succeed before any new Core test edit. Evaluated Compile exactly matches
all 553 tracked Core C# inputs: no tracked Core C# file is excluded. Generated proto
and compiler inputs are not mislabelled as tracked handwritten source.

MSBuild preprocessing is fully XML-parsed. Exactly seven repository-owned imports
are identified and individually hash-bound: root Directory.Build.props/targets,
Directory.Packages.props, signing.props, tests Directory.Build.props/targets and
the Core project. All seven are personally FULL read. The 142 external SDK/package/
generated imports are separately hash-bound, not claimed as personally read SDK
implementation. MSBuildAllProjects alone is not treated as an exhaustive import set.

Additional graph inputs: Directory.Packages.props (75), the sole native-tests.yml
workflow (215), signing.props (8), tests/testsettings.json (57), tests/testconfig.json
(6), global.json (5), the Unit .slnx (79), and src/Directory.Build.props (33).
The Unit execution path is read completely; unrelated provider-job validator code
is not claimed as read. global.json selects MTP and contains no SDK version pin.

All productive project edges in the Core closure are read: Abstractions, Core,
Courier, Futures, Initializers, JobService, Mediator, Sagas and Testing. All ten
direct project references, six central packages and two canonical copied JSON
configuration items reconcile against the evaluated Core project.

### Complete support-input manifest

Paths below are relative to `tests/Testing/`. Counts include an unterminated final
physical line where present; the Infrastructure lock contains 99 such lines.

| Support input | Lines |
| --- | ---: |
| ViciOne.ServiceBus.Tests.Infrastructure/Brokers/BrokerOutageControlClient.cs | 176 |
| ViciOne.ServiceBus.Tests.Infrastructure/Configuration/TestConfigurationProvider.cs | 219 |
| ViciOne.ServiceBus.Tests.Infrastructure/Configuration/ViciOneTestOptions.cs | 569 |
| ViciOne.ServiceBus.Tests.Infrastructure/Databases/TestDatabaseName.cs | 60 |
| ViciOne.ServiceBus.Tests.Infrastructure/Requirements/RequirementCoverageAttribute.cs | 25 |
| ViciOne.ServiceBus.Tests.Infrastructure/Requirements/RequirementCoverageProjectionVerifier.cs | 325 |
| ViciOne.ServiceBus.Tests.Infrastructure/ViciOne.ServiceBus.Tests.Infrastructure.csproj | 24 |
| ViciOne.ServiceBus.Tests.Infrastructure/packages.lock.json | 99 |
| ViciOne.ServiceBus.Tests.InternalAccess/ApiSurfaceGlobalUsings.cs | 13 |
| ViciOne.ServiceBus.Tests.InternalAccess/Brokers/ReceiveEndpointRecoveryObserver.cs | 88 |
| ViciOne.ServiceBus.Tests.InternalAccess/DurableSend/DurableSenderDeliveryTestDriver.cs | 84 |
| ViciOne.ServiceBus.Tests.InternalAccess/DurableSend/DurableSenderTestFactory.cs | 18 |
| ViciOne.ServiceBus.Tests.InternalAccess/Events/HostReadyEventTestDriver.cs | 12 |
| ViciOne.ServiceBus.Tests.InternalAccess/InMemoryOutbox/InMemoryOutboxCheckpointDriver.cs | 31 |
| ViciOne.ServiceBus.Tests.InternalAccess/InMemoryOutbox/InMemoryOutboxTestContextFactory.cs | 484 |
| ViciOne.ServiceBus.Tests.InternalAccess/InMemoryOutbox/OutboxConsumerIdentityTestDriver.cs | 37 |
| ViciOne.ServiceBus.Tests.InternalAccess/Initializers/InitializerConventionRegistryTestDriver.cs | 39 |
| ViciOne.ServiceBus.Tests.InternalAccess/Internals/ReflectionImplementationTestDrivers.cs | 49 |
| ViciOne.ServiceBus.Tests.InternalAccess/KillSwitch/KillSwitchTestDriver.cs | 295 |
| ViciOne.ServiceBus.Tests.InternalAccess/MessageJournal/MessageJournalEntryTestFactory.cs | 29 |
| ViciOne.ServiceBus.Tests.InternalAccess/MessageJournal/MessageJournalWriterTestDriver.cs | 64 |
| ViciOne.ServiceBus.Tests.InternalAccess/Monitoring/MessagingSystemNormalizerTestDriver.cs | 9 |
| ViciOne.ServiceBus.Tests.InternalAccess/Monitoring/OutboxTelemetryTestDriver.cs | 33 |
| ViciOne.ServiceBus.Tests.InternalAccess/Operations/ProbeResultBuilderTestDriver.cs | 18 |
| ViciOne.ServiceBus.Tests.InternalAccess/Rescue/RescueContextTestFactory.cs | 40 |
| ViciOne.ServiceBus.Tests.InternalAccess/Retry/RetryFilterTestFactory.cs | 188 |
| ViciOne.ServiceBus.Tests.InternalAccess/Runtime/ServiceBusRuntimeLifecycleTestDriver.cs | 194 |
| ViciOne.ServiceBus.Tests.InternalAccess/SagaStateMachine/SagaStateMachineExecutionTestDriver.cs | 25 |
| ViciOne.ServiceBus.Tests.InternalAccess/Serialization/ForwardingExpirationTestDriver.cs | 16 |
| ViciOne.ServiceBus.Tests.InternalAccess/Serialization/PayloadAdmissionSerializationTestDriver.cs | 22 |
| ViciOne.ServiceBus.Tests.InternalAccess/Topology/GlobalTopologyTestDriver.cs | 37 |
| ViciOne.ServiceBus.Tests.InternalAccess/Transactions/AmbientTransactionBusTestDriver.cs | 25 |
| ViciOne.ServiceBus.Tests.InternalAccess/Transactions/AmbientTransactionNotificationTestDriver.cs | 32 |
| ViciOne.ServiceBus.Tests.InternalAccess/Transactions/BufferedBusTestDriver.cs | 31 |
| ViciOne.ServiceBus.Tests.InternalAccess/Transactions/TransactionFilterTestDriver.cs | 156 |
| ViciOne.ServiceBus.Tests.InternalAccess/Transactions/UnusedBus.cs | 20 |
| ViciOne.ServiceBus.Tests.InternalAccess/Transports/AsyncBusHandleLifecycleTestDriver.cs | 107 |
| ViciOne.ServiceBus.Tests.InternalAccess/Transports/BusDepotTestDriver.cs | 82 |
| ViciOne.ServiceBus.Tests.InternalAccess/Transports/HostedServiceLifecycleTestDriver.cs | 148 |
| ViciOne.ServiceBus.Tests.InternalAccess/Transports/ReceiveMessageLimitsTestContext.cs | 68 |
| ViciOne.ServiceBus.Tests.InternalAccess/Transports/RiderCollectionTestDriver.cs | 136 |
| ViciOne.ServiceBus.Tests.InternalAccess/ViciOne.ServiceBus.Tests.InternalAccess.csproj | 14 |
| ViciOne.ServiceBus.Tests.InternalAccess/packages.lock.json | 170 |

### Productive source/comment review

Nine complete productive files/1,848 current physical lines are personally read:
ResourceCache.cs (366), ResourceCache.Observers.cs (102), ResourceCache.Resources.cs
(319), ResourceCache.Lifecycle.cs (163), ResourceCacheOptions.cs (75),
TestingServiceProviderExtensions.cs (237), AsyncElementList.cs (277), SentMessageList.cs
(130), and the final MediatorReceiveContext.cs (179). Every comment is checked
against the current code. No comment rewrite is necessary in these nine files.
Prior source reads are not double-counted as a global new-source total.

## Remediation and requirement evidence

| Finding / requirement | Concrete evidence |
| --- | --- |
| CS01: release failed-constructor ownership and preserve the primary fault | TimerCreationFailure_ReleasesAllocatedOwnershipAndPreservesTheOriginalException; two linked/unlinked lifetime rows, exact exception identity, independent disposed CTS.Token/semaphore.Wait probes, live caller-owned lifetime source and unconditional fixture cleanup. |
| Keep successful construction and timer ownership intact | SuccessfulConstruction_ForwardsCleanupPolicyAndDisposesItsTimerExactlyOnceAsync; literal seven-second due/period, one allocation, no premature disposal, shared disposal task, one timer release and rejected post-disposal admission. |
| MD01: do not share mutable MIME state | ContentTypeMutation_IsIsolatedAcrossReadsDeliveriesAndMessageContractsAsync; repeated reads, two deliveries of one contract and another contract, exact canonical media value, distinct object identities and absent caller-only parameters. Getter returns a fresh ContentType from the canonical raw JSON constant. |
| H01: prove producer progress while a filter executes without hanging the test | SnapshotFilter_DoesNotBlockAConcurrentProducer; bounded Join inside the active predicate, captured producer exception and independent final join after the predicate unwinds. |
| H02: prove configured-clock timeout rather than scheduler timing | ConnectPublishHandler_TimesOutOnTheHarnessClockWhenTheEndpointCannotBecomeReadyAsync; one configured-provider timer with literal due/period, pending underlying readiness, actual faulted connection and exactly one endpoint stop. |

Three requirement catalogue bindings are appended by hand. Full JSON parsing
reconciles 2,925 records; previous records remain unchanged. The new cache theory
adds two cases, its successful-construction fact adds one, and MIME isolation adds
one: the existing 4,007 cases grow to 4,011. Existing test names/cases are preserved.
New asynchronous methods end in Async; synchronous fault probes do not. New nested
fixture types remain in their owning test file. No pragma or warning suppression
is introduced. The failed-constructor reflection seam is intentionally narrow:
there is no successfully returned public cache through which those private resource
allocations could otherwise be observed. No productive diagnostic API is added.

## Actual compiled mutation evidence

Each counterchange is applied manually, compiled with warnings-as-errors, executed
in a fresh native process with strict verdict, and reversed before the next source
scope. A test failure is accepted only after checking exact intended method and
assertion, not merely a nonzero process exit. No mutant or failed binary is shipped.

| Counterchange | Compiler exit | Native exit / intended failures | Discriminator |
| --- | ---: | --- | --- |
| Omit constructor CTS disposal | 0 | 2 / both lifetime rows | Missing ObjectDisposedException at cancellation.Token, test line 35. |
| Omit constructor observer gate disposal | 0 | 2 / both lifetime rows | Missing ObjectDisposedException at semaphore.Wait, test line 36. |
| Restore the exact original global mutable MIME holder | 0 | 2 / one MIME case | Same-instance assertion, test line 53; source equals original input bytes during this counterchange. |
| Use TimeProvider.System instead of the harness provider for readiness | 0 | 2 / one readiness case | Configured provider timer count is zero instead of one, test line 100; not a watchdog timeout false-pass. |
| Hold the snapshot predicate under the actual inherited producer monitor | 0 | 2 / one producer case | Five-second in-predicate Join fails at line 225; final join completes after monitor release rather than hanging the host. |

The monitor counterchange uses temporary reflection solely to select the existing
private producer monitor; it adds no permanent reflection or synchronization path
to productive SentMessageList. All three unchanged counterchange source files
return to their pre-mutant SHA. The corrected Mediator file returns to its final
SHA. The final nine-path code/data/source freeze check succeeds exactly.

## Builds, full execution and honest coverage scope

SDK 10.0.302; xUnit v3 MTP-v2 4.0.0; CodeCoverage 18.10.0. Known sandbox IPC/compiler
restrictions are handled through approved out-of-sandbox local execution, not
blind restore/clean retries. Every MSBuild invocation gets its own unique binlog.
Commands use Release, no restore, disabled build servers, single-node MSBuild,
disabled shared compilation and warnings-as-errors. Final restored build: exit 0,
zero warnings/errors, 5.94 seconds. Fresh focused run: 25/25, exit 0. Final unfiltered
native run: 4,011/4,011, exit 0, 23.213 seconds, global five-minute watchdog,
minimum 4,011, strict zero-tests policy, fail-skips/fail-warns on and progress off.

Coverage is collected in that same final unfiltered run, not a duplicate test run.
The manually written collector configuration includes exactly the nine loaded
productive modules, all auto-properties, and no default attribute exclusions.
Test assemblies and native/static instrumentation are excluded. This is native
collection through run-tests, not a coverage-analysis/CRAP or report-generator
pipeline. XML configuration and Cobertura document parse fully. Configuration
syntax is verified against [Microsoft Code Coverage configuration](https://github.com/microsoft/codecoverage/blob/main/docs/configuration.md).

Weighted observed-module line coverage is **50,608/64,568 = 78.3794%**.
Weighted observed-module branch coverage is **17,445/24,298 = 71.7960%**.

| Observed productive module | Line % | Branch % |
| --- | ---: | ---: |
| ViciOne.ServiceBus | 80.3235 | 73.9934 |
| ViciOne.ServiceBus.Abstractions | 59.4224 | 53.6382 |
| ViciOne.ServiceBus.Courier | 89.1005 | 75.4425 |
| ViciOne.ServiceBus.Futures | 90.5686 | 85.4839 |
| ViciOne.ServiceBus.Initializers | 100.0000 | 100.0000 |
| ViciOne.ServiceBus.JobService | 95.6283 | 89.7525 |
| ViciOne.ServiceBus.Mediator | 90.8197 | 77.5974 |
| ViciOne.ServiceBus.Sagas | 64.2705 | 57.8763 |
| ViciOne.ServiceBus.Testing | 94.3834 | 77.9841 |

These are not entire-product coverage percentages: persistence/scheduling/transport
adapters outside this Core module closure are not measured by this run. Historical
81.0997%/73.3110% figures are not automatically comparable; their exact denominator
and collector provenance must be reconciled before a regression conclusion. No
current whole-product CRAP result or 100% API/parameter/correctness claim is made.

## Counterreview, failures and retained findings

Internal read-only Lead counterreview `iteration132_lifetime_counterreview` fully
checks the scoped diff, 24 declared methods/25 focused cases and connected runtime.
It finds zero concrete current Critical/High/Medium/Low defects. This is an internal
advisor, not independent external product-team/Red Team acceptance. All five actual
counterchanges and the final execution remain the main agent's separate evidence.

Failed diagnostics are retained honestly: initial xUnit1051 missing runner token
on semaphore.Wait, xUnit1031 guarded Task.Result, and two fixture mistakes (exact CTS
type versus linked subtype; missing mandatory explicit Mediator message limits).
All are manually corrected without weakening product policy or introducing pragmas.
An accidentally started stale-DLL run after a failed compiler is explicitly rejected
as current evidence; only the later successful-build/fresh-run pair is accepted.
The first import-check log is empty, with no surviving process/exit proof; it receives
no credit. A separate full xmllint parse plus checked comment import reconciliation
succeeds. A read-count arithmetic error and orchestration syntax error likewise
receive no successful credit. No source failure is inferred from these mistakes.

New support observations remain qualified and OPEN: BusDepotTestDriver creates a
new depot per StopEmptyAsync, so repeated calls do not prove same-instance stop
idempotence. Outbox recording overloads do not execute their pipe/initializer values
or preserve endpoint destination, limiting forwarding proof. Broker control's
losing timeout/probe tasks can outlive an operation; malformed field kinds need
discriminators. Configuration ownership, durable-driver partial construction/
disposal, ambient runtime log restoration and real transaction outcome proof need
separate completion. Recovery observer destination/generation isolation, shared
ledger concurrency, callback publication atomicity, helper-comment correctness and
multi-type file organization also remain open; narrow negative-control fixtures
with intentional unused poison members are not falsely labelled productive dummy code.

Positive fixture clarification is preserved: Rider's 32 concurrent starts use a
real shared readiness/release gate; that specific missing-gate suspicion is resolved,
not the separate stop-overlap proof. Initializer convention registration exercises
real before-freeze repeat/default and after-freeze rejection; its retained snapshot
is genuine. Original fault identity, held callbacks, literal wire values, stop
ledgers and owned resource controls from earlier iterations retain their strength.

Closed here means exactly CS01 allocation/regression ownership, MD01, H01 and H02.
Portable cache cleanup interval policy and other CS/CT findings remain OPEN; broader
grouped test findings are not closed by fixing two of their examples. MD02 observer/
validation consistency and PA01 hard writer-reservation versus encoded-size policy
remain OPEN. Whole-src main reading/manual comments, Greenfield API/naming/structure,
global bidirectional Async audit, formatting/directives/dummy/legacy axes, all API/
parameter coverage, entire-product metrics and genuine durable-provider cloud
acceptance remain part of the unchanged active goal.

## Evidence and Git handoff

Own raw root: `/private/tmp/vsb-iteration132-core-admission.ZBqS3S`.
Sorted 621-path readset log SHA:
`d7c5c1a71d9f6e0132c53c61f796cea997f845291c85845d053cad5c9f0bf3ba`.
Import reconciliation log SHA:
`143b059d05163a310804cde170b0a2060620eaba50f33396bad8994ce0cddb7e`.
Focused passing CTRF SHA:
`98d4d1c4ba1d2fe29dc35fd7f14dc34448a95736f460d7eb6b11a10f30b02a52`.
Final passing CTRF SHA:
`e6866417e148215d31d5974cf9da9a18008dc6843a22e3b3b5c35ca89829d37d`.
Final Cobertura SHA:
`70db188063769098ba364a8c5c80a3679d0f33283ddcfc7e2ce10c05866de071`.
Collector configuration SHA:
`c049632b61e7fcf5be95962df1629c620a9f66e77366dae0e7ddf7c91f0d4a13`.
Final native log SHA:
`33c2db104cc0c578378b07bf7c88e3a1782483122b96fd1a8f07bdcbd0cc1769`.
Final build log SHA:
`bc6f4c81e994d150b384fdda6664a097d1b94cd6f17114c2a6469baf20bc17ed`.

Mutant CTRF SHAs, in CTS/gate/MIME/clock/monitor order:
`c9c9c6183f64abfae1eb771ede8564ceb5e816e19042b6e7a52a3b0367603d29`,
`19939fcbac76087d96432c47cd4758ce8540652c82f62792fc46a3f3e4b205d9`,
`09418b8b4561ff52958e7550cbd5ecbe6bf7dde3c9a16b2ff0f5470c120c2c7c`,
`796788dddc2920681601f83c7d1ba46e90daf4994990a6b617afe10c1cbd88ad`,
`dd893cd2acc0c314d7b0a6e27f8b47d3cb1fcd0b7af77c3bdab381b6b2b5acdc`.

Final source SHA: MediatorReceiveContext
`164f9a60cc891462c1dbbf29e348dcfca01c195411ad9b2ca0ba813b7289293d`.
Unchanged restored cache source SHA:
`1d6a4dd32d80e54a41d865a6b735f6729e0a7234e185f7d9aee6eefe798a95fb`.
Unchanged restored TestingServiceProviderExtensions SHA:
`5a89de4e002025ca8f29b47b4aba42db49db87e54a0bf88fe33a8bc60343e078`.
Unchanged restored SentMessageList SHA:
`f4a530b79047038a65678813db98ddff886f9abd8d09a1a2b006cd15fdd17574`.

Ten owned paths are prepared for normal commit, a new annotated iteration-132 tag
dated 2026-09-16, and the approved atomic normal branch/tag push. No force-push.
Remote-security claims require separately keyed branch/tag-object/peeled-tag refs
and exact clean owned HEAD/index/work verification after push. review/, TestResults/,
unrelated work and earlier history tails remain untouched. Raw telemetry stays in
the own temporary root; the handwritten evidence/dispositions travel with Git.

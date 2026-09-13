# A+ remediation research

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

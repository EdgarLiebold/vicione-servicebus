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

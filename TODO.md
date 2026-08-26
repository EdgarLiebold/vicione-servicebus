# Deferred engineering work

This file contains bounded work that was deliberately kept out of the active implementation slice.
It is not a second feature catalog, architecture, changelog, or license record. An item is removed
only after its acceptance evidence is committed.

## Normalize product source paths

Run this as a dedicated mechanical slice after the native test reconstruction has been promoted from
`tests2` to `tests`.

The following product directories repeat a segment without introducing a corresponding namespace or
architectural boundary:

- `src/ViciOne.ServiceBus/Configuration/Configuration`;
- `src/ViciOne.ServiceBus/DependencyInjection/DependencyInjection`;
- `src/ViciOne.ServiceBus/Futures/Futures`;
- `src/ViciOne.ServiceBus/InMemoryTransport/InMemoryTransport`;
- `src/ViciOne.ServiceBus/JobService/JobService`;
- `src/ViciOne.ServiceBus/SagaStateMachine/SagaStateMachine`;
- `src/ViciOne.ServiceBus/SqlTransport/SqlTransport`;
- `src/ViciOne.ServiceBus/Topology/Topology`.

Before moving anything, inventory path-sensitive build inputs, generated files, `CallerFilePath`
usage, SourceLink, packaging, documentation, and tooling. Then move only redundant directories while
preserving namespaces, public API, capabilities, assembly names, package IDs, and project boundaries.

Acceptance requires:

- locked restore and zero-warning Release builds;
- all applicable unfiltered native test profiles;
- public-API and package-content comparison against the pre-move commit;
- explicit disposition of every non-byte-identical assembly, PDB, SourceLink, or package entry;
- no stale reference to an old source path;
- source and test folders following the same project and namespace mapping.

The repository groups `src/Persistence`, `src/Scheduling`, and `src/Transports` remain project-group
boundaries. They are mirrored under `tests` but do not become C# namespace segments.

## Normalize reflection-property metadata ownership and visibility

The completed type-relationship cohort found a separate inherited policy ambiguity across
`ReadOnlyPropertyCache<T>`, `ReadWritePropertyCache<T>`, `WritePropertyCache<T>`, message-property
discovery and the public helpers currently placed under `ViciOne.ServiceBus.Internals`.
`ReadWritePropertyCache<T>(bool includeNonPublic)` in particular combines `CanWrite`, a later
`SetMethod` check and the flag in a way that may leave the flag ineffective. This is not safe to
change from one call site or to encode as a test assumption.

Run one path-complete product slice that:

- inventories every initializer, serializer, message-data, text-table, dependency-injection proxy,
  topology and metadata consumer;
- defines explicit public/non-public getter and setter policy for each owned operation;
- replaces or repairs `includeNonPublic` so its observable contract is truthful;
- gives the public API an architectural owner and namespace rather than exposing an ambiguous
  `Internals` surface;
- preserves every useful capability without a backward-compatibility alias; and
- proves exact property discovery, hiding, indexers, proxy generation and serialization behavior
  with source-owner tests and one-cause mutations.

Acceptance requires an explicit public-API disposition, complete consumer and package comparison,
locked restore, zero-warning Release builds, and every applicable unfiltered native profile.

## Normalize inherited formatting

The inherited product tree contains pre-existing whitespace findings outside files changed by the
native test reconstruction. Normalize them as a dedicated mechanical slice instead of mixing a broad
rewrite into behavior cohorts.

Acceptance requires a repository-wide formatter/analyzer gate, a path-complete reviewed diff, zero
behavior or public-API changes, locked restore, zero-warning Release builds, and every applicable
unfiltered native test profile.

## Consolidate channel executors

`src/ViciOne.ServiceBus/Util/ChannelExecutor.cs` and `TaskExecutor.cs` currently provide overlapping
queue-execution capabilities. Consolidate them in a dedicated product slice after the native test
reconstruction has captured both behavior sets. Preserve every meaningful capability, including
bounded backpressure, concurrency, synchronous waiting where still required, cancellation, draining
disposal, and the ActiveMQ call sites. Remove historical comparison language and the redundant type
only after all consumers, benchmarks, public API, and package contents have explicit dispositions.

## Normalize task-utility API names

`src/ViciOne.ServiceBus/Util/TaskUtil.cs` still exposes inherited generic helper names such as
`GetTask` and `Default`, and its `Util` location does not describe an architectural owner. Treat the
complete utility surface and every call site as one dedicated product-normalization slice after the
native reconstruction has captured all behavior. Select intent-revealing .NET names and a stable
source owner, preserve every meaningful capability, and migrate all internal and public consumers
atomically. Acceptance requires a public-API disposition, package comparison, zero stale names,
locked restore, zero-warning Release builds, and every applicable unfiltered native test profile.

## Consolidate the two cache engines

`src/ViciOne.ServiceBus/Caching` and `src/ViciOne.ServiceBus/Internals/Caching` are two independent
cache engines with overlapping factory, pending-value, capacity, usage, and expiration behavior.
The second engine remains production-critical for core send endpoints and for ActiveMQ, Event Hubs,
and Amazon SQS resources; it must not be removed as a naming cleanup.

After the complete native reconstruction has captured both engines and all transport-specific call
sites, select one coherent cache architecture and migrate consumers in a dedicated product slice.
Preserve single-flight creation, queued fallback after factory faults, exact removal/disposal,
capacity and usage-aware retention, deterministic TTL through `TimeProvider`, metrics, and every
transport resource-lifetime contract. Acceptance requires explicit API and capability disposition,
one engine and one naming model at the end, no compatibility wrapper left behind, all applicable
native profiles, transport integration tests, package comparison, and targeted concurrency
mutations.

## Scope System.Text.Json options per bus

`SystemTextJsonMessageSerializer.Options` is mutable process-global state, and
`ConfigureJsonSerializerOptions` replaces it while configuring one bus. Two independently
configured buses or parallel consumers can therefore observe each other's serializer policy. The
native compatibility tests isolate and restore that state, but a test boundary is not a product
architecture solution.

Replace the global mutable field with an immutable serializer-options snapshot owned by each bus
configuration and passed to its serializer/deserializer instances. Preserve envelope and raw JSON,
per-message options, custom converters, source-generated metadata, and every public configuration
capability. Decide the obsolete/static API transition explicitly rather than retaining a hidden
compatibility bridge.

Acceptance requires concurrent buses with different naming and converter policies, proof of no
cross-bus or post-start mutation leakage, all System.Text.Json and MessagePack compatibility tests,
all applicable unfiltered native profiles, a public-API disposition, and targeted mutations that
reintroduce shared state or late option replacement.

## Continue deterministic time normalization beyond envelope materialization

Envelope materialization is complete: System.Text.Json and MessagePack now consume one internal
serializer-independent projection for identifiers, addresses, headers, message types, host data and
times. A standard .NET `TimeProvider` is attached once to the pipeline context, each projection uses
at most one UTC snapshot, and zero or negative TTL values are materialized exactly without a
serializer-owned grace period. Payload encoding remains owned by each serializer. Target-contract
tests, cross-format comparison and four effective clock/clamp/drift mutations protect the rule.

The separate send-context projection in `ForwardMessagePipe<T>` remains intentional: transports,
observers, middleware and caller pipes need that state before body serialization. Do not merge these
causally different stages merely because they carry some of the same values.

The generic-forwarding policy is already resolved and implemented: after the caller pipe, only a
positive TTL can revive an expired inherited envelope; otherwise the forward is logged as
`FORWARD-EXPIRED` and discarded before transport dispatch, persistent-outbox storage or mediator
dispatch. This is intentionally different from the one-second response/fault grace. This TODO must
not reopen that policy or move it back into individual serializers or transports.

Testing observation primitives are also complete. `AsyncElementList<T>` and
`AsyncInactivityObserver` now use one injected standard `TimeProvider`, existing constructors retain
their `TimeProvider.System` defaults, synchronous callbacks execute outside the list monitor, and
source-query failures remain observable. Their native fake-time, cancellation, concurrency and
mutation proofs must be replayed but not redesigned by this TODO.

Recorded-message timestamps are complete as well. Sent, published and received observations, their
lists, bus observers, consumer/saga/handler registrations and mediator harness now derive their
immutable snapshots from the harness `TimeProvider`. Exact metadata, typed and untyped success,
consume faults and independent process-clock regressions are covered by native tests.

The remaining work is deliberately a separate path-complete product slice. Inventory and normalize
the wall-clock reads used by `AsyncTestHarness`/`ContainerTestHarness` budgets,
`RollingTimer`/`InactivityTestObserver`, saga polling, scheduling,
delayed redelivery, copy-context TTL, request state, outbox/transport deadlines, health waits and
retained adapters. Preserve domain-owned timestamp
providers where they are actual public behavior; replace accidental process-clock reads with the
same standard context/provider model only after every caller and persisted/wire consequence is
understood. Acceptance requires deterministic boundary tests, RabbitMQ and every retained affected
adapter, public-API/package comparison, and targeted one-cause time mutations. It also replays the
completed envelope and forwarding contracts without redefining them.

## Normalize receive-transport reconnection

`src/ViciOne.ServiceBus/Transports/ReceiveTransport.cs` still owns a separate inherited retry loop,
uses process-clock `Task.Delay`, swallows delay cancellation and adds a fixed one-second breather
after every loop in addition to the configured receive-transport policy. It is not the host send
retry path and must not be changed from host-retry evidence alone.

Analyze the complete receive supervisor, ready/completed/fault notification, stop and adapter
lifecycle before choosing the A+ execution shape. The result must have one retry executor, one
configured delay owner, explicit `TimeProvider`, exact stop/caller cancellation semantics, no
swallowed cancellation, no tight reconnect loop, and unchanged useful readiness/fault behavior for
every retained transport.

Acceptance requires hermetic deterministic lifecycle tests, real RabbitMQ coverage, all affected
retained adapter tests, exact observer and supervisor-state assertions, one-cause cancellation and
delay mutations, public-API/package comparison, zero-warning Release builds and every applicable
unfiltered native profile.

## Complete external benchmark scenarios

The transport- and SQL-Server-backed benchmark scenarios remain tracked in
[`benchmarks/ToDo.md`](benchmarks/ToDo.md). Complete them only with their real infrastructure and do
not replace them with inventory-only or skipped green results.

## Complete MessageJournal external provider validation

The current provider contract is covered hermetically and against ephemeral PostgreSQL and Azurite
instances. Before release, run the same bounded append, retention, concurrency, failure-isolation
and public-composition contract against short-lived real SQL Server, Azure SQL and Azure Table
resources in the `External` profile. Do not represent an emulator result as proof of a cloud
service's transaction, concurrency or storage-limit behavior, and do not weaken the provider set to
avoid external infrastructure.

Acceptance requires unfiltered native xUnit/MTP execution, zero skips, isolated per-run resources,
secret-free durable evidence, exact resource cleanup and a documented disposition of every semantic
difference from the local PostgreSQL/Azurite results.

## Finalize solution composition after native-test promotion

`ViciOne.ServiceBus.slnx` is now a product/package solution and no longer compiles inherited
`tests/**` projects. Do not reintroduce those projects merely to make the old NUnit/VSTest graph
available. Replace their meaningful behavior in the source-owner native projects and retire each
inherited project only when its obligation set is terminal.

The remaining finalization is to promote `tests2` to `tests` and then revalidate that the product,
unit, local-integration and engineering solutions each contain every retained project belonging to
their declared role. Acceptance requires locked restore, zero-warning Release builds, all applicable
unfiltered profiles, and architecture tests that fail when a retained project is omitted from the
relevant solution.

## Retire the inherited Python/VSTest/NUnit verification stack at promotion

`tools/ci/**`, `build/verification/**`, the Python `unittest` suites, the inherited NUnit/VSTest
projects and their `build.yml` jobs are transition evidence only. They are not part of the accepted
xUnit 4 / Microsoft Testing Platform 2 architecture and must not survive the atomic `tests2` to
`tests` promotion. Removing them earlier would destroy the executable lower bound for cohorts that
have not yet been replaced.

Before promotion, disposition every script and job by capability. Delete test-discovery, receipt,
verdict, execution-sentinel and duplicate completeness machinery. Retain genuinely useful
engineering capabilities such as API/package comparison, infrastructure orchestration, legal
change-list verification or vulnerability inventory only at their correct non-verdict owner; if
they require executable tests, those tests use the same native xUnit/MTP architecture. Do not carry
Python `unittest`, VSTest, NUnit, `VERIFICATION_MODEL`, or a second test verdict into the final tree.

Acceptance requires zero stale workflow/documentation reference, one native CI truth, full closure
of every inherited obligation before deletion, all applicable unfiltered profiles, locked restore,
zero-warning Release builds and mutations proving that omitted projects or test cohorts fail the
native gates.

## Retire the inherited TestFramework product project

`src/ViciOne.ServiceBus.TestFramework` is inherited test material and is not a retained shipping
package. Move each still-useful fixture behavior into its source-owner native test project, move
reusable test-only infrastructure into the bounded `tests` support projects, and rebuild the useful
restaurant scenario as the already-decided standalone sample. Do not preserve helper APIs merely
for backward compatibility.

The current project graph has exactly twelve references to this project, all from inherited
`tests/**` projects, plus its membership in the inherited root `ViciOne.ServiceBus.slnx`. No retained
product project, `tests2/**` project, tool, benchmark or sample references it. Preserve that clean
boundary while the twelve inherited consumers are retired; do not introduce a temporary reference
from the native test tree.

In particular, do not migrate `GetReceiveEndpointAddresses` as a JSON-reparsing abstraction: native
tests now consume the structured product `ProbeResult` directly. Before deleting the project,
inventory the remaining `ToJsonString` callers and retain only genuinely useful structured
diagnostic or sample behavior at its correct owner.

Acceptance requires a path-complete capability disposition, no production reference to the old
project or namespace, no shipped TestFramework package, all migrated native requirements green,
the sample in its own non-shipping project, locked restore, zero-warning Release builds, and every
applicable unfiltered native profile.

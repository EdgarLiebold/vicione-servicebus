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

## Complete external benchmark scenarios

The transport- and SQL-Server-backed benchmark scenarios remain tracked in
[`benchmarks/ToDo.md`](benchmarks/ToDo.md). Complete them only with their real infrastructure and do
not replace them with inventory-only or skipped green results.

## Finalize solution composition after native-test promotion

The inherited `ViciOne.ServiceBus.slnx` still contains old test projects. Three of them are not
executable Microsoft Testing Platform projects and therefore correctly fail the xUnit 4 executable-
project gate. Do not retrofit those projects merely to make the inherited solution green. Replace
their meaningful behavior in the source-owner projects, remove them when their obligation sets are
terminal, and then rebuild the root solution composition.

The final product and engineering solution closure must include every retained `src/**` project and
every promoted native test, sample, benchmark, and engineering tool that belongs to its declared
role. Acceptance requires locked restore, zero-warning Release builds, all applicable unfiltered
profiles, and an architecture test that fails when a retained project is omitted from the relevant
solution.

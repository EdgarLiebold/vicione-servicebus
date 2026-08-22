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

## Complete external benchmark scenarios

The transport- and SQL-Server-backed benchmark scenarios remain tracked in
[`benchmarks/ToDo.md`](benchmarks/ToDo.md). Complete them only with their real infrastructure and do
not replace them with inventory-only or skipped green results.

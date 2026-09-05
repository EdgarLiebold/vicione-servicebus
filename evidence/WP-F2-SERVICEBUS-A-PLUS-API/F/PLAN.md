# Capability package plan

## Scope

Work package F separates optional capabilities from the core assembly without removing behavior. The work proceeds in dependency order so every intermediate boundary can be compiled and verified.

1. Record the source and registration coupling baseline.
2. Introduce the `ViciOne.ServiceBus.Advanced.Registration.IConsumerKind` extension point and preserve consumer endpoint behavior in the core.
3. Extract `ViciOne.ServiceBus.Sagas` and its state-machine and registration integration.
4. Extract `ViciOne.ServiceBus.Courier`, `ViciOne.ServiceBus.Futures`, `ViciOne.ServiceBus.JobService`, `ViciOne.ServiceBus.Mediator`, and `ViciOne.ServiceBus.Initializers` in dependency order.
5. Split Entity Framework Core only where saga ownership requires it, then update persistence and visualization references.
6. Normalize repeated directory segments and update projects, solutions, workflows, package locks, capability documentation, and developer journeys.
7. Add `samples/SuiteComposition` and architecture coverage for the core/package boundary and runtime assembly closure.
8. Pack every capability, run SuiteComposition with the SQLite reliable store, run the developer-journey gate, then execute the three canonical builds and the UnitArchitecture profile three consecutive times.

## Package dependency direction

- `ViciOne.ServiceBus.Sagas` -> `ViciOne.ServiceBus`
- `ViciOne.ServiceBus.Courier` -> `ViciOne.ServiceBus`
- `ViciOne.ServiceBus.Futures` -> `ViciOne.ServiceBus.Sagas`, `ViciOne.ServiceBus.Courier`
- `ViciOne.ServiceBus.JobService` -> `ViciOne.ServiceBus.Sagas`
- `ViciOne.ServiceBus.Mediator` -> `ViciOne.ServiceBus`
- `ViciOne.ServiceBus.Initializers` -> `ViciOne.ServiceBus`
- Optional persistence and visualization packages -> the capability package whose contracts they implement or visualize

No capability package may become a dependency of the core assembly.

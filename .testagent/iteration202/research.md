# Iteration 202 research

## Admitted source scope

- `DependencyInjectionSagaRegistrationExtensions.cs`: 166 baseline lines covering seven generic
  and runtime class-saga registration entry points plus their private registrar adapters.
- `DependencyInjectionSagaStateMachineRegistrationExtensions.cs`: 181 baseline lines covering
  seven generic and runtime state-machine registration entry points plus their private adapters.
- `RegistrationServiceCollectionExtensions.cs`: 62 baseline lines covering repository descriptor
  registration and removal.

The lead read all three files completely before implementation delegation. They are new unique
source admissions, moving the cumulative exact count from 650 to 653 of 4,118 files (15.857%).

## Material findings

- Public registration methods delegate or inspect runtime types before owning missing service
  collections, registrars and runtime type arguments in signature order.
- Runtime saga and definition paths rely on reflection/generic-constraint failures for abstract,
  interface, open-generic or wrong-family inputs, producing unstable diagnostics and risking effects
  before admissibility is known.
- State-machine registration has the same boundary problem and can add repeated machine/service
  descriptors before its registration registry deduplicates the saga-state registration.
- Private registrar adapters are closed nested classes with parameterless constructors; their
  `Activator.CreateInstance` null fallbacks appear structurally unreachable.
- Repository-service registration delegates null receivers to framework extensions and needs direct
  proof of exact descriptor lifetime, identity, order, repeat-call semantics and narrowly scoped
  removal.

## Test strategy

Use direct `RequirementCoverage` mappings for exact public shape, receiver-first guard matrices,
runtime type-family diagnostics, pre-mutation failure, definition forwarding, stable registration
identity, state-machine activation identity, descriptor lifetime/order, repeat-call behavior and
repository removal scope. Exercise real `ServiceCollection` and container registration rather than
mocking descriptor behavior.

# Iteration 209 research

## Scope

Lead read eight previously unadmitted dependency-injection saga sources:

- `DependencyInjection/DependencyInjectionLoadSagaRepository.cs`
- `DependencyInjection/DependencyInjectionQuerySagaRepository.cs`
- `DependencyInjection/DependencyInjectionSagaRepository.cs`
- `DependencyInjection/DependencyInjectionSagaRepositoryContextFactory.cs`
- `DependencyInjection/Registration/Sagas/SagaRegistration.cs`
- `DependencyInjection/Registration/Sagas/SagaRegistrationConfigurator.cs`
- `DependencyInjection/Registration/Sagas/SagaRepositoryRegistrationConfigurator.cs`
- `DependencyInjection/Registration/Sagas/SagaStateMachineRegistration.cs`

On admission, cumulative lead-read progress becomes 725/4,118 sources (17.606%).

## Initial findings

- Load/query adapters own DI scope creation, service resolution, callback/token/result forwarding
  and async-first disposal across both success and failure.
- The repository and context factory own correlation prerequisites, existing-versus-created scope
  lifetime, scoped consume-context restoration, scheduler rebinding and cleanup failure behavior.
- Saga and state-machine registrations cache definitions and compose decorators, definitions,
  configure actions, observers and endpoint specifications in a precise order; failed cache
  initialization and repeated configuration are material risks.
- Registration configurators own exclusion and repository-only boundaries before callback effects;
  the repository configurator is a live `IList<ServiceDescriptor>` facade and requires exact
  collection and constructor semantics.

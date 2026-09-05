# Work package F coupling decisions

## Measurement boundary

The review baseline reported capability references outside their nominal folders in 141 saga files,
129 Courier files, 33 Job Service files, 32 initializer files, 13 transformation files, 11 future
files, 10 mediator files, and 7 MessageData files. A source inventory at commit `120714c4` refined the
working set to 1,839 tracked core C# files and 177,997 lines. The nominal capability folders accounted
for 313 files; the remaining lexical candidates were 96 saga, 88 Courier, 33 Job Service, 43
initializer, 13 future, and 15 mediator files. Lexical matches were treated only as candidates.

The final decision uses evaluated MSBuild ownership and compiled declaring assemblies. The evaluated
Release `Compile` set of `ViciOne.ServiceBus` contains 1,390 files. Reflection over the final binaries
then proves that no capability-owned type is declared by either `ViciOne.ServiceBus.Abstractions` or
`ViciOne.ServiceBus`, and that neither assembly references a capability assembly. A linked source item
belongs to the project that compiles it; every extracted file is removed from its former project and
compiled exactly once by its capability project.

## Decision summary

| Capability | Decision (a): owned by package | Decision (b): extension seam | Decision (c): retained core contract |
|---|---|---|---|
| Consumers | ordinary consumer registration and dispatch remain core | core fallback implementation of `IConsumerKind` | `IConsumer<T>`, contexts, endpoint and pipeline contracts |
| Sagas and state machines | `ViciOne.ServiceBus.Sagas` | `SagaConsumerKind`, registration completion participant, generalized correlation/exclusion SPIs | consume-pipeline observer seam required to attach optional saga specifications |
| Courier activities | `ViciOne.ServiceBus.Courier` | `ActivityConsumerKinds` | the minimal activity-context pipeline vocabulary used by retry, timeout, outbox, scoping, and telemetry |
| Futures | `ViciOne.ServiceBus.Futures` | `FutureConsumerKind` | none |
| Job Service | `ViciOne.ServiceBus.JobService` | `JobConsumerKind` plus the generalized service-instance host contract | none |
| Mediator | `ViciOne.ServiceBus.Mediator` | capability registration and its own registration context | none |
| Initializers | `ViciOne.ServiceBus.Initializers` | named-value SPI consumed by the core conversion pipeline | the internal named-value bridge only |
| Transformation | remains in `ViciOne.ServiceBus` by explicit requirement | not required | all transformation pipeline contracts and implementations |
| MessageData | remains in `ViciOne.ServiceBus` by explicit requirement | provider repository contracts | all core MessageData admission, conversion, encryption, and transformation behavior |
| EF saga persistence | `ViciOne.ServiceBus.EntityFrameworkCore.Sagas` | normal package registration | base EF package retains only reliable messaging and journal persistence |

## Decision (a): references moved into their owning block

The project item manifests are the complete file-level ownership list. Their principal source sets are:

- Sagas: `ViciOne.ServiceBus.Abstractions/{CorrelatedBy.cs,ILoadSagaRepository.cs,ISaga.cs,Saga/**,
  SagaStateMachine/**}`, saga endpoint definitions, `ViciOne.ServiceBus/{Sagas/**,SagaStateMachine/**,
  **/*Saga*.cs}`, saga registration, middleware, repository, topology, and state converters. These are
  removed from both foundation projects and compiled by `ViciOne.ServiceBus.Sagas`.
- Courier: `ViciOne.ServiceBus.Abstractions/Courier/**`, routing-slip exceptions and execution,
  activity configurators and definitions, plus the core-source activity registration, routing-slip,
  activity scope, activity filter, Courier context, and endpoint-dispatch files enumerated by
  `ViciOne.ServiceBus.Courier.csproj`. They are removed from the foundation projects and compiled by
  `ViciOne.ServiceBus.Courier`.
- Futures: abstraction and definition files plus `Futures/**`, `Configuration/Futures/**`, and future
  registration files enumerated by `ViciOne.ServiceBus.Futures.csproj`.
- Job Service: `JobService/**`, `IJobConsumer`, job contexts and exceptions, job configuration,
  conventions, filters, serialization, registration, and service-instance endpoint files enumerated
  by `ViciOne.ServiceBus.JobService.csproj`.
- Mediator: mediator abstractions, request extensions, runtime, configuration, registration context,
  container registrar, and scoped mediator enumerated by `ViciOne.ServiceBus.Mediator.csproj`.
- Initializers: anonymous-value extension entry points, `InVar`, initializer variables, and Advanced
  initializer contracts enumerated by `ViciOne.ServiceBus.Initializers.csproj`.
- EF saga persistence: saga repository contexts, entity maps, job sagas, future persistence, and their
  registration extensions enumerated by `ViciOne.ServiceBus.EntityFrameworkCore.Sagas.csproj`.

Package references follow one direction: Sagas, Courier, Mediator, and Initializers depend on core;
Futures depends on core, Sagas, and Courier; Job Service depends on core and Sagas; EF Core Sagas
depends on base EF Core, Sagas, Futures, and Job Service. No capability package is referenced by either
foundation assembly.

## Decision (b): references replaced by an extension seam

The former type switches in endpoint materialization are replaced by these exact core files:

- `src/ViciOne.ServiceBus/Advanced/Registration/IConsumerKind.cs` defines planning, registration,
  endpoint configuration, definition, endpoint-name, companion-endpoint, service-instance, and test-
  harness contracts.
- `src/ViciOne.ServiceBus/DependencyInjection/Registration/Consumers/ConsumerKind.cs` implements the
  ordered fallback category for ordinary consumers.
- `src/ViciOne.ServiceBus/DependencyInjection/Configuration/BusRegistrationContext.cs` discovers all
  registered kinds, resolves one explicit owner ahead of the fallback, groups endpoint definitions,
  configures companion endpoints, and marks each materialized registration exactly once.
- `src/ViciOne.ServiceBus/Configuration/RegistrationContextExtensions.cs` configures registered kinds
  without naming an optional capability.
- `src/ViciOne.ServiceBus/DependencyInjection/Configuration/ServiceCollectionBusConfigurator.cs` and
  `RegistrationContext.cs` register and query the generalized kind contracts.
- `src/ViciOne.ServiceBus/Transports/ReceiveEndpointDispatcherFactory.cs` asks registered kinds to
  create a dispatcher rather than switching on Saga, Activity, Future, or Job types.
- `src/ViciOne.ServiceBus.Testing/DependencyInjection/Configuration/TestHarnessRegistrationConfigurator.cs`
  invokes every kind's harness contribution. Unknown third-party kind names are accepted when their
  observed shape is a consumer, saga, state machine, or activity and fail with an actionable
  configuration error otherwise.

The optional implementations are `SagaConsumerKind`, `ActivityConsumerKinds`, `FutureConsumerKind`,
and `JobConsumerKind` in their respective packages. `IRegistrationCompletionParticipant`,
`IConsumerKindHost`, `MessageContractExclusionAttribute`, and `IMessageCorrelation` generalize the few
remaining completion, host, exclusion, and correlation hooks without a reverse package dependency.

## Decision (c): references retained as core pipeline contracts

Removing the following references would alter middleware ordering, payload identity, outbox behavior,
or telemetry. They remain deliberately in the foundation assemblies as Advanced contracts, not as
capability registration or domain behavior:

- Saga observer seam: `Configuration/{ConfigurationObserver.cs,BusFactoryConfigurator.cs,
  EndpointConfiguration.cs,ConsumePipeSpecification.cs}`, `Configuration/InMemoryOutbox/
  OutboxConsumePipeSpecificationObserver.cs`, and `DependencyInjection/Configuration/
  ScopedConsumePipeSpecificationObserver.cs`. These six files expose the observer connection point by
  which the optional saga package contributes pipeline specifications.
- Activity pipeline vocabulary: `Advanced/ActivityContext.cs`,
  `Advanced/ActivityContextVariableExtensions.cs`, `Contexts/Context/{ActivityContextProxy.cs,
  ActivityContextScope.cs,ExecuteContextProxy.cs,ExecuteContextScope.cs,CompensateContextProxy.cs,
  CompensateContextScope.cs,RetryExecuteContext.cs,RetryCompensateContext.cs,
  HostExecuteActivityContext.cs,HostCompensateActivityContext.cs}`; the matching retry, redelivery,
  timeout, in-memory-outbox, scope-filter, scope-provider, configuration-observer, and telemetry files.
  This is the shared pipeline context used by middleware. Activity registration, routing slips,
  activity factories, and Courier contexts are package-owned under decision (a).
- Initializer bridge: `Advanced/Initializers/INamedInitializerValue.cs`,
  `Advanced/InternalInitializerEndpointExtensions.cs`, and the named-value converter/provider files
  compiled by core. They allow serializers and the object-conversion pipeline to recognize a value
  supplied by the optional initializer package without retaining the public overload set or `InVar`.
- Transformation: `Transformation/**`, `Configuration/Transformation/**`, and `Middleware/
  TransformFilter.cs` remain core exactly as required.
- MessageData: `MessageData/**`, `MessageDataExtensions.cs`, the MessageData converters, admission
  evaluator, and AES-GCM implementation remain core exactly as required.
- Shared topology: `Advanced/MessageContractExclusionAttribute.cs`, `Advanced/Topology/
  IMessageCorrelation.cs`, and their internal core implementations remain message-contract SPIs.

The evaluated core set has no remaining Future, Job Service, or Mediator type reference. The retained
Saga and Activity names above are only the documented pipeline seams; the corresponding user-facing
domain and registration types are declared by their capability assemblies.

## Executing proof

`CapabilityPackageArchitectureTests` verifies declaring assemblies, duplicate-type absence, the
one-way project graph, the EF split, and the three direct SuiteComposition product dependencies. It
passes 3/3 in isolation and is included in the complete 202/202 architecture module and each of the
three 3,703-test acceptance runs.

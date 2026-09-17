# Iteration 181 research

## Bounded source inventory

- `src/ViciOne.ServiceBus.Courier/Configuration/ActivityPipeConfiguratorExtensions.cs`
- `src/ViciOne.ServiceBus.Courier/Configuration/CourierHostConfiguratorExtensions.cs`
- `src/ViciOne.ServiceBus.Courier/Configuration/CourierRegistrationConfiguratorExtensions.cs`
- `src/ViciOne.ServiceBus.Courier/Configuration/CourierRegistrationConfiguratorRuntimeExtensions.cs`
- `src/ViciOne.ServiceBus.Courier/Configuration/CourierRegistrationExtensions.cs`
- `src/ViciOne.ServiceBus.Courier/Configuration/DependencyInjectionActivityRegistrationExtensions.cs`
- `src/ViciOne.ServiceBus.Courier/Configuration/DependencyInjectionCourierReceiveEndpointExtensions.cs`
- `src/ViciOne.ServiceBus.Courier/Configuration/DependencyInjectionExecuteActivityRegistrationExtensions.cs`
- `src/ViciOne.ServiceBus.Courier/Configuration/IRoutingSlipConfigurator.cs`
- `src/ViciOne.ServiceBus.Courier/Configuration/RoutingSlipConfigurator.cs`

All ten files and all comments were read completely. Direct owning tests read completely:

- `tests/ViciOne.ServiceBus.Tests/Courier/CourierConfigurationSurfaceTests.cs` (authored and reviewed
  completely in this iteration)
- `tests/ViciOne.ServiceBus.Tests/Courier/CourierRegistrationBoundaryTests.cs`
- `tests/ViciOne.ServiceBus.Tests/Courier/CourierConsumerKindContractTests.cs`
- `tests/ViciOne.ServiceBus.Tests/Courier/RoutingSlipHostConfigurationTests.cs`
- `tests/ViciOne.ServiceBus.Tests/Courier/CourierHostResultContractTests.cs`
- previously admitted `ActivityEndpointConfigurationTests.cs` and
  `ActivityRegistrationLifecycleTests.cs` were rechecked at their relevant helpers and call sites.

## Existing conventions

- xUnit 4.0.0 on Microsoft Testing Platform v2 under .NET SDK 10.
- Behavioral requirements use `RequirementCoverageAttribute` and reconcile through
  `CoreRequirements.json`.
- Public guard tests assert exact `ParamName`; configuration forwarding tests use narrowly scoped
  `DispatchProxy` recorders and inspect actual endpoint specifications.

## Acceptance checklist

- Every public Courier host overload rejects a missing receiver before any other invalid argument.
- Every host overload positively constructs exactly one matching endpoint specification and invokes
  its callback exactly once.
- DI host overloads validate receiver, address and registration context and forward one scoped host
  specification.
- Typed activity-pipe adapters validate both inputs and forward one split specification.
- The routing-slip configurator validates, exposes and builds its exact specification set.
- Runtime registration rejects non-concrete activities and definitions before changing services.
- Explicit scans preflight filter failures and ambiguous definitions before changing services.
- Valid typed/runtime/scan registration and existing execute-versus-compensatable rules remain intact.
- Comments, namespaces, imports, formatting and synchronous naming remain current and clean.

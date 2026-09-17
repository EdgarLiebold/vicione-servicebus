# Iteration 182 research

## Bounded source inventory

- `src/ViciOne.ServiceBus.Abstractions/Advanced/ActivityContext.cs`
- `src/ViciOne.ServiceBus.Abstractions/Advanced/Courier/CompensateActivityContext.cs`
- `src/ViciOne.ServiceBus.Abstractions/Advanced/Courier/ExecuteActivityContext.cs`
- `src/ViciOne.ServiceBus.Abstractions/Advanced/Courier/ExecuteContext.cs`
- `src/ViciOne.ServiceBus.Abstractions/Advanced/Courier/CompensateContext.cs`
- `src/ViciOne.ServiceBus/Context/Activities/HostExecuteActivityContext.cs`
- `src/ViciOne.ServiceBus/Context/Activities/HostCompensateActivityContext.cs`
- `src/ViciOne.ServiceBus/Transports/Receiving/IReceiveEndpointDispatcherFactory.cs`
- `src/ViciOne.ServiceBus.Courier/Advanced/ICourierContext.cs`
- `src/ViciOne.ServiceBus.Courier/Advanced/IRoutingSlipExecutor.cs`
- `src/ViciOne.ServiceBus.Courier/Context/BaseCourierContext.cs`
- `src/ViciOne.ServiceBus.Courier/Context/CourierContextProxy.cs`
- `src/ViciOne.ServiceBus.Courier/Context/CourierContextScope.cs`
- `src/ViciOne.ServiceBus.Courier/Courier/HostCompensateContext.cs`
- `src/ViciOne.ServiceBus.Courier/Courier/HostExecuteContext.cs`
- `src/ViciOne.ServiceBus.Courier/Courier/CompensateActivityHost.cs`
- `src/ViciOne.ServiceBus.Courier/Courier/ExecuteActivityHost.cs`
- `src/ViciOne.ServiceBus.Courier/Courier/SanitizedRoutingSlip.cs`
- `src/ViciOne.ServiceBus.Courier/Transports/ExecuteActivityReceiveEndpointDispatcher.cs`

All nineteen files and their comments are being read completely. Direct owning tests read completely
before implementation:

- `tests/ViciOne.ServiceBus.Tests/Courier/CourierContextContractTests.cs`
- `tests/ViciOne.ServiceBus.Tests/Courier/CourierHostResultContractTests.cs`
- `tests/ViciOne.ServiceBus.Tests/Courier/CourierConsumerKindContractTests.cs`
- `tests/ViciOne.ServiceBus.Tests/Courier/RoutingSlipExecutorContractTests.cs`
- `tests/ViciOne.ServiceBus.Tests/Courier/CourierConfigurationSurfaceTests.cs`
- `tests/ViciOne.ServiceBus.Tests/Courier/CourierTestSupport.cs`

The required Roslyn static pairing pass ran once against an isolated Courier source/test mirror so
that protected trees were not traversed. It classified 97/138 source files as paired. In this packet,
the three context wrappers, both activity hosts, `HostExecuteContext`, and the dispatcher were paired;
`ICourierContext`, `IRoutingSlipExecutor`, and `HostCompensateContext` were not directly paired. Static
pairing is only a symbol-reference heuristic and is not line or branch coverage evidence.

## Existing conventions

- xUnit 4.0.0 on Microsoft Testing Platform v2 under .NET SDK 10.
- Behavioral requirements use `RequirementCoverageAttribute` and reconcile through
  `CoreRequirements.json`.
- Public guard tests assert exact parameter names. Pipeline tests use deterministic context, pipe,
  observer and probe recorders rather than timing guesses.
- Coverage and CRAP evidence is regenerated under `/private/tmp`; protected `TestResults/**` is never
  used.

## Acceptance checklist

- Every public context member and behaviorally relevant parameter has direct positive or negative
  evidence, including activity binding and exact proxy/scope forwarding.
- Routing-slip snapshots reject invalid identity, detach every mutable collection, preserve
  case-insensitive variable semantics, and deserialize execution and compensation data correctly.
- Execute and compensation factories cover every result shape, null boundary, itinerary/log boundary,
  and compensation-address rule without losing an existing overload.
- Execute and compensation hosts prove success, missing-result fallback, ordinary failure, activity
  cancellation, bus cancellation, notification ordering, next-pipe sequencing, and diagnostic cleanup.
- Dispatcher creation derives the exact queue, forwards one registration callback, and validates both
  collaborators.
- Every genuinely asynchronous public operation retains an `Async` suffix and no synchronous operation
  gains one; comments describe only current behavior.
- Final tests contain meaningful equality, exception, structural and state/side-effect assertions with
  no assertion-free or trivial-only case.

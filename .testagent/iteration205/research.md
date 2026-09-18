# Iteration 205 research

## Admission scope

The lead read twelve newly unique sources completely before delegation:

- `InitiatedByOrOrchestratesSagaConnectorFactory.cs` (37 lines)
- `InitiatedBySagaConnectorFactory.cs` (36 lines)
- `ObservesSagaConnectorFactory.cs` (45 lines)
- `OrchestratesSagaConnectorFactory.cs` (40 lines)
- `ISagaConnectorFactory.cs` (11 lines)
- `ISagaConnector.cs` (20 lines)
- `ISagaMessageConnector.cs` (29 lines)
- `ISagaConnectorCache.cs` (8 lines)
- `BehaviorContextRetryConfigurator.cs` (41 lines)
- `CorrelatedByEventCorrelationBuilder.cs` (32 lines)
- `CorrelatedByFaultEventCorrelationBuilder.cs` (32 lines)
- `EventMissingInstanceConfigurator.cs` (44 lines)

These 375 physical lines move cumulative exact unique source coverage to 686/4,118 files
(16.659%). `SagaConnector.cs` and `SagaConnectorCache.cs` were reread for context but were already
admitted and are not counted or delegated.

## Findings to test

- Four role factories construct distinct policies/filters and expose one explicit generic connector
  factory method; exact saga mismatch failure, connector identity, message type and policy behavior
  need direct contracts.
- The connector factory, connector, message connector and cache interfaces need exact accessibility,
  inheritance, constraints, members, nullability and synchronous result contracts.
- Retry configuration currently permits missing policy factories and observers; causal ownership,
  observer handle identity, filter forwarding and policy exception/result identity need tests.
- Correlation builders pass machine/event into nested configurators and select different nested
  correlation paths for ordinary versus fault messages; null ownership and built correlation
  behavior need direct verification.
- Missing-instance configuration must guard callbacks before pipe creation and preserve discard,
  fault correlation, sync/async callback context, task and exception identity.

## Test strategy

Use three disjoint Sol 5.6 xhigh workstreams for role factories, public connector contracts, and
retry/correlation/missing-instance behavior. Central integration owns requirements, formatting,
serial builds, regressions, compiled mutations, coverage/CRAP, evidence, commit and tag.

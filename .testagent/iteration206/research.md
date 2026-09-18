# Iteration 206 research

## Scope

Lead read 12 previously unadmitted sources (488 baseline lines):

- `Configuration/ConcurrencyLimit/ConcurrencyLimitSagaConfigurationObserver.cs`
- `Configuration/IEventCorrelationBuilder.cs`
- `Configuration/IRequestStateMachineMissingInstanceConfigurator.cs`
- `Configuration/ISagaDefinition.cs`
- `Configuration/IStateMachineInterfaceType.cs`
- `Configuration/InMemoryOutbox/InMemoryOutboxSagaConfigurationObserver.cs`
- `Configuration/MissingInstanceRedeliveryConfigurator.cs`
- `Configuration/Partition/PartitionSagaSpecification.cs`
- `Configuration/RedeliverRequestStateMachineSpecification.cs`
- `Configuration/Redelivery/DelayedRedeliverySagaConfigurationObserver.cs`
- `Configuration/Redelivery/ScheduledRedeliverySagaConfigurationObserver.cs`
- `Configuration/Retry/MessageRetrySagaConfigurationObserver.cs`

Cumulative lead-read progress is 698/4,118 sources (16.950%).

## Initial findings

- Concurrency limiting, in-memory outbox, delayed/scheduled redelivery and message retry are all
  saga-message observer adapters; their constructor ownership and exact specification ordering need
  one coherent matrix.
- The missing-instance redelivery path separates configuration, validation and build. Callback,
  configurator, policy-factory and terminal-pipe ownership must be deterministic before pipeline
  publication.
- The public correlation, state-machine connector, saga-definition and request-missing interfaces
  need exact reflection/nullability coverage.
- The partition specification already owns its constructor/apply guards and shared partitioner/key
  identity; tests must freeze those semantics without replacing the valid implementation.

Three disjoint Sol 5.6 xhigh work packets own observer adapters, missing-instance redelivery, and
public/partition contracts respectively. Central integration owns requirements, formatting, serial
builds, regression, mutation proof, coverage, manifests, evidence, commit and tag.

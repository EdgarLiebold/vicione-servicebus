# Configuration composition and validation — product-path analysis

## Scope

This cohort replaces 29 inherited R0 obligations from eight NUnit fixtures. The inherited files are behavior evidence only. Their fixture hierarchy, assertion-free smoke tests, reflection order, and delayed observer callbacks are not preserved as architecture.

## Product paths read end to end

### Configuration observers

Bus and endpoint observer connections flow through the four typed observables into consumer, handler, saga, saga-message, state-machine, execute-activity, and compensate-activity specifications. Receive-endpoint validation runs before those specifications configure the pipeline. Observer-added specifications must therefore be applied once before the first validation snapshot; delaying notification until `Configure` makes their validation unreachable.

`ConfigurationObserverNotification` now owns that lifecycle once for all affected configuration types. It permits exactly one successful notification. Re-entry fails immediately. A callback failure permanently faults the configuration object and rethrows the captured first exception on later validation attempts; retrying a callback after it may have mutated the configurator would duplicate non-transactional side effects.

### Send and publish composition

The root specification owns base and direct per-message specifications. Each concrete message specification composes, in order, implemented-message specifications, a parent specification, its direct specification, and its root base specification. Topology is a parent contribution. Parent and implemented specifications remain validated by their owning roots, so the child validates only its direct specifications and the root returns a materialized point-in-time result.

A newly requested message specification is published in the root dictionary before observer notification and implemented-contract expansion. This is required for same-message observer re-entry and recursive interface discovery. A failed initialization removes the incomplete root entry.

### Saga discovery and instance creation

Saga contracts are discovered once per closed saga type. Message ordering is ordinal within a semantic role. Role precedence is `InitiatedBy`, `Orchestrates`, `InitiatedByOrOrchestrates`, then `Observes`; the first role owns a duplicate message contract. Reflection order is not a product contract.

The former public `SagaInterfaceType`, asynchronous `Task.Run` factory replacement, and unused metadata interface were implementation remnants. The target uses an internal immutable discovery cache, internal connector descriptors, and one synchronously compiled constructor delegate. Both supported instance shapes are retained: a public `Guid` constructor, or a public parameterless constructor plus writable `CorrelationId`.

### Runtime consumer instances

The object, delegate, and runtime-`Type` registration paths converge on the same consumer connector while retaining the exact supplied instance. Public null boundaries fail before endpoint mutation.

### Invalid configuration

Observer-injected child specifications participate in the first validation result. Invalid consumer/message contracts and empty retry policies fail at their actual configuration boundary. A hosted registration without a transport fails on start with one direct `ConfigurationException`.

## A+ disposition

- Preserve every useful capability, not legacy API shape or incidental reflection order.
- Use one observer-notification lifecycle instead of nine duplicated integer flags.
- Materialize validation results at the call boundary.
- Keep topology, parent, implemented-contract, direct-message, and root-base ownership distinct.
- Keep both public saga construction capabilities and all four saga roles.
- Replace assertion-free smoke tests with exact order, identity, lifecycle, failure, and integration assertions.

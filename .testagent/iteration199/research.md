# Iteration 199 research

## Admitted source candidates

The lead read all three current sources in full before delegation:

- `InMemorySagaRepositoryServiceCollectionExtensions.cs`: 23 baseline lines and one public generic
  registration method.
- `DependencyInjectionSagaReceiveEndpointExtensions.cs`: 56 baseline lines and three public
  receive-endpoint overloads.
- `SagaExtensions.cs`: 55 baseline lines and two public configuration/connection overloads.

This is 134 baseline physical lines and six public methods. All three sources are newly unique,
moving personal-read coverage from 640 to 643 of 4,118 current source files (15.614%). Static
source/test pairing found no focused owner tests for these three extension classes.

## Contract gaps and test axes

- In-memory registration reaches `IServiceCollection` immediately without an owned receiver guard.
  Tests must prove registration shape, lifetimes, capability coverage, dictionary/factory identity
  and repeat-registration stability without over-constraining legal DI enumeration.
- Dependency-injection endpoint overloads construct repositories or resolve a state machine before
  owning the `configurator`, `context` and explicit `stateMachine` boundaries. Tests must prove
  deterministic ordering, exact context/repository/state-machine identity and optional callback
  forwarding across all three overloads.
- The core `Saga` endpoint overload already owns receiver/repository validation, but must prove
  configurator construction, callback ordering, callback failure isolation and endpoint
  specification identity.
- `ConnectSaga` does not own a null specification array or null elements. It must preserve
  specification order and identity, forward the exact connector/repository and return the exact
  connect handle without partial connection on invalid input.

The mandatory code-testing pipeline, static source/test pairing, pseudo-mutation gap analysis and
assertion-quality review require exact public-surface checks, causal boundary assertions, DI
descriptor inspection, collaborator identity proofs and isolated compiled mutations.

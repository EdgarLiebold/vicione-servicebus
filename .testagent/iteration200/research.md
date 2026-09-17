# Iteration 200 research

## Admitted source candidates

The lead read all three current sources in full before delegation:

- `SagaPipeConfiguratorExtensions.cs`: 26 baseline lines and one public generic method.
- `SagaStateMachineReceiveEndpointExtensions.cs`: 49 baseline lines and two public generic methods.
- `SagaRegistrationConfiguratorRuntimeExtensions.cs`: 69 baseline lines and two public runtime-type
  methods plus their private registration adapters.

This is 144 baseline physical lines and five public methods. All three sources are newly unique,
moving personal-read coverage from 643 to 646 of 4,118 current source files (15.687%). No prior
admission manifest names these owners.

## Contract gaps and test axes

- `UseFilter` owns its configurator but defers a null filter into a later specification failure.
  Tests must prove receiver-before-filter ordering and exact single specification/filter identity.
- Direct state-machine endpoint registration does not validate its receiver before state machine and
  repository inputs. The connector form owns none of its three required collaborators. Both must
  prove callback/specification ordering, failure isolation, exact state-machine/repository/connector
  identity and returned handle identity.
- Runtime saga registration does not own its receiver or `sagaType` boundaries. Invalid value,
  open-generic, non-saga and wrong-family types currently leak reflection or helper diagnostics.
  Tests must distinguish valid saga and state-machine registration, definition forwarding, result
  identity and effect-free deterministic rejection.

The mandatory code-testing pipeline, static source/test pairing, pseudo-mutation gap analysis and
assertion-quality review require exact public-surface checks, causal boundary assertions, runtime
type matrices, collaborator identity proofs and isolated compiled mutations.

# Iteration 200 — saga pipe, runtime registration and connection admission

## Outcome

The saga-filter pipe, runtime registration and direct state-machine endpoint/connection surfaces
are admitted across all five public methods. Every required collaborator is now rejected at its
immediate public boundary in signature order, runtime types receive stable family diagnostics, and
valid calls retain exact filter, machine, repository, callback, registration and connect-handle
identity.

The lead personally read all three current sources before delegation. They are newly unique, moving
cumulative exact unique source coverage to 646/4,118 files (15.687%). The packet contained 144
physical lines before the change and 184 after admission.

## Corrections and direct contracts

- `UseFilter` owns both configurator and filter boundaries, adds exactly one
  `SagaFilterSpecification`, retains filter identity and preserves add-failure identity.
- Both direct state-machine entry points own their receivers, machine and repository before any
  callback, endpoint or connector effect. Callback ordering/failure isolation, exact collaborator
  forwarding and disconnect-handle ownership are fixed by direct tests.
- Runtime `AddSaga` accepts only closed reference saga types, keeps state-machine instances in the
  state-machine family and emits stable `sagaType` diagnostics without DI mutation.
- Runtime `AddSagaStateMachine` accepts exactly one closed `ISagaStateMachine<TSaga>` family. Its
  generic interface constraint already guarantees a closed reference state implementing
  `ISagaStateMachineInstance`; the redundant unreachable state branch was removed.
- Both activation adapters are private, closed and have parameterless constructors. Their
  `Activator.CreateInstance` null fallbacks were therefore unreachable and were removed rather than
  excluded from coverage.
- Valid runtime registrations preserve the exact definition types, owner configurator,
  registrations, state-machine type and resolved machine identity.

## Source manifest

Each record is `path<TAB>content-sha256<LF>`, sorted ordinally. Manifest SHA-256 is
`eeff8132c7f5ab49d78b67830c849c9e2e51bfc118030a948e7eb39fb91ae273`. Chaining that hash from
iteration 199 yields
`56ae41c69811dc0241e67f6d1fe6e342157f1943cfa5a3fb4c639421aaceb49d`.

| Source file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `src/ViciOne.ServiceBus.Sagas/Configuration/SagaPipeConfiguratorExtensions.cs` | 26 | `034faacf372235d81a4bb9b70788c60b8fca86f9569f7ba51be797e9f573d02d` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/SagaRegistrationConfiguratorRuntimeExtensions.cs` | 100 | `9dd115f4b8a493d176242726db0cb695d9209bae9c40dbf095ad445147eadaee` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/SagaStateMachineReceiveEndpointExtensions.cs` | 58 | `071edb1e691b7aa2fa54035582d0b5b75dd0f274779814d24fb6e1c93f71ecd3` |

## Test manifest, assertions and requirements

The three classes contain 14 test methods and 14 unique requirement variants. They assert exact
public shape, causal guard ordering, failure isolation, filter/specification identity, callback and
connection ordering, runtime invalid-type matrices including multiply implemented state-machine
families, stable diagnostics, DI mutation isolation, definition forwarding, registration ownership
and returned configurator/handle identity. The mandatory code-testing workflow, static source/test
pairing, pseudo-mutation gap analysis and assertion-quality audit drove the
research-to-plan-to-test sequence.

Test manifest SHA-256 is
`d252d9a3a3dbf11c707af3ab768067dfa422858c4843e9be183e6cdf64d86e6b`; chained from iteration
199 it yields
`c14ba7a26f11fea2b5b27fb4f192201cbdc717e749f1eb750406c15a17075a05`.

| Test file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `tests/ViciOne.ServiceBus.Tests/Configuration/SagaPipeConfiguratorExtensionsDeepContractTests.cs` | 161 | `621ae8835ce915758a3a2d3a905a10b8d3a9712152c1ccfdfa3e81074c045ba7` |
| `tests/ViciOne.ServiceBus.Tests/Configuration/SagaRegistrationConfiguratorRuntimeExtensionsDeepContractTests.cs` | 294 | `6bfda4bf3f9ed39393a767ebf03b386abee0dd66b8dcdb42bc2b30081898818b` |
| `tests/ViciOne.ServiceBus.Tests/Configuration/SagaStateMachineReceiveEndpointExtensionsDeepContractTests.cs` | 319 | `11902a0c4ba93b838dc2ee26f5ad0cbd09525d4ec3461077c0ef4d2293da18ce` |

`CoreRequirements.json` SHA-256 is
`3405359cb787c3d6c6414bb0e187c0498f742ac72fde03a7044dd41217bf146d`.

## Mutation proof

Six compiled, isolated, material single-cause mutants were killed and restored:

1. the filter boundary guard was removed;
2. the endpoint configurator receiver guard was removed;
3. the consume-pipe connector receiver guard was removed;
4. the runtime saga configurator receiver guard was removed;
5. the runtime state-machine configurator receiver guard was removed; and
6. the closed-reference saga-type predicate was weakened.

Each mutant caused its owning deep-contract method to fail at the exact intended invariant. The
final no-incremental build restored every product and test-host artifact.

## Coverage, CRAP and gates

Final Cobertura is
`/private/tmp/vicione-servicebus-iteration-200-results/iteration200-final.cobertura.xml`, SHA-256
`34730ad2cbf6bdeeb7db30dcdf3571b4f280274d7af18c0e6eb5fd4c5a9fe2a0`. Exact
source-filename selection includes compiler-generated classes of the same source while excluding
foreign sources. Unique executable line numbers and branch conditions use the maximum coverage
across duplicate generated entries.

| Owner source | Lines | Branches | Maximum method CRAP |
| --- | ---: | ---: | ---: |
| `SagaPipeConfiguratorExtensions.cs` | 5/5 | 0/0 | 1 |
| `SagaRegistrationConfiguratorRuntimeExtensions.cs` | 28/28 | 16/16 | 6 |
| `SagaStateMachineReceiveEndpointExtensions.cs` | 20/20 | 16/16 | 8 |
| **Total** | **53/53** | **32/32** | **8** |

Sorted display-name SHA-256 is
`0d917fddbe311b8a8b737c33649f6202cba9f36d4c4afb899c39a531de6bbce9` across 5,426 unique
displays. The strict Core CTRF artifact is
`/private/tmp/vicione-servicebus-iteration-200-results/iteration200-final.ctrf.json`, SHA-256
`8c111b2afec8ffb26fc7f59077f1cfe217cd9eae68ad680ed162a72bd6f5fd90`.

| Gate | Result |
| --- | --- |
| Three final owned classes | 14/14 passed |
| Configuration/Sagas namespace regression | 654/654 passed |
| Full Core Release | 5,426/5,426 passed, 0 failed, 0 skipped |
| Full EF unit Release | 249/249 passed, 0 failed, 0 skipped |
| Core-test / EF-unit / EF-local Release builds | 0 warnings, 0 errors |
| Core/EF/EF-local requirement projections | 1/1, 1/1 and 1/1 passed |
| Scoped format, JSON and diff checks | Exit 0; no formatting or whitespace errors |
| Mutation probes | 6/6 compiled isolated material mutants killed |

No unresolved product correctness, callback, connection, runtime-type, overload-shape,
compatibility, coverage or architecture finding remains in this admitted packet.

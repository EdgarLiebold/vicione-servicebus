# Iteration 199 — saga configuration and registration extension admission

## Outcome

The in-memory repository registration, dependency-injection receive endpoint and direct
saga/connector extension surfaces are admitted across all six public methods. Immediate public
boundaries now fail deterministically before resolution, registration, endpoint or connector
effects while retaining exact collaborator, descriptor, callback, specification and result
identity.

The lead personally read all three current sources before delegation. They are newly unique, moving
cumulative exact unique source coverage to 643/4,118 files (15.614%). The packet contained 134
physical lines before the change and 149 after admission.

## Corrections and direct contracts

- In-memory saga registration owns its `IServiceCollection` boundary. Its singleton dictionary,
  scoped context factories, load/query capabilities and valid repeat-registration shape are fixed
  by direct descriptor and provider evidence.
- All three dependency-injection endpoint overloads validate receiver and context in signature
  order; the explicit state-machine overload also owns the machine boundary. Container resolution
  occurs exactly once and every repository retains the exact registration context.
- `Saga` preserves callback-before-endpoint ordering, exact configurator identity and zero endpoint
  mutation on callback failure.
- `ConnectSaga` validates the full pipe array and every element before any specification or
  connector effect, then preserves pipe order/identity and the exact connector, repository and
  disconnect handle chain.
- Both optional debug-logging branches are exercised under an isolated async-flow logging context
  and do not change registration semantics.

## Source manifest

Each record is `path<TAB>content-sha256<LF>`, sorted ordinally. Manifest SHA-256 is
`8c48e70002b2a862676cd53f38a5e6108ac00e386e84b48db2c4391c4c354606`. Chaining that hash from
iteration 198 yields
`0f48351d284221dae49771a8ab9b35fa7d26f790e466ab61085b97e43e0244cb`.

| Source file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `src/ViciOne.ServiceBus.Sagas/Configuration/DependencyInjectionSagaReceiveEndpointExtensions.cs` | 66 | `bef2d9fb556452bd29b0ce4237da98b5a85468c658fdf990d10a7b5e0907cefd` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/InMemorySagaRepositoryServiceCollectionExtensions.cs` | 25 | `542a14d90207737437aa4cd87bc8e9911ade54671096f970862c62d0f543e9dc` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/SagaExtensions.cs` | 58 | `5d271ce8157dae15c68f5302ef7abe0371bab40de07cbc903fb50c2bc2e2d5d8` |

## Test manifest, assertions and requirements

The three classes contain 14 test methods and 14 unique requirement variants. They assert exact
public shape, causal guard ordering, DI descriptor/lifetime and provider identity, repeat
registration, state-machine resolution, callback ordering/failure isolation, pipe ordering,
collaborator forwarding, handle identity and both logging paths. The mandatory code-testing
workflow, static source/test pairing, pseudo-mutation gap analysis and assertion-quality audit drove
the research-to-plan-to-test sequence.

Test manifest SHA-256 is
`bb7436fd64c4d124f34dec3cfb8bd047cbe43cecf7ed8c2b7965852318b795eb`; chained from iteration
198 it yields
`286b480496398763433f4ee2bbc1df675df14fb276ec492fdf30c117b7cf8086`.

| Test file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `tests/ViciOne.ServiceBus.Tests/Configuration/DependencyInjectionSagaReceiveEndpointExtensionsDeepContractTests.cs` | 241 | `19b45cbe6b8c7e168661792c32a2820b80c7bb7b206fe3f79c27add39b40ef9d` |
| `tests/ViciOne.ServiceBus.Tests/Configuration/InMemorySagaRepositoryServiceCollectionExtensionsDeepContractTests.cs` | 225 | `668e0488cb0b712a68c4415d072773b8695eeb0c567cd1acfc0d950b6c953582` |
| `tests/ViciOne.ServiceBus.Tests/Configuration/SagaExtensionsDeepContractTests.cs` | 347 | `b652f25e08a6a608c2ee00c3eb66dcfa1c640e0f28496086765431093901d5e4` |

`CoreRequirements.json` SHA-256 is
`cdd8a920745c75bed14a12006ddb262ca9867e702dec389cefc3ddd6d6b1108b`.

## Mutation proof

Six compiled, isolated, material single-cause mutants were killed and restored:

1. the service-collection receiver guard was removed;
2. `TryAddSingleton` was replaced by duplicate-producing `AddSingleton`;
3. the first dependency-injection endpoint receiver guard was removed;
4. the explicit state-machine guard was removed;
5. the pipe-array guard was removed; and
6. the complete pipe-element prevalidation pass was removed.

Each mutant caused its owning deep-contract method to fail at the exact intended invariant. The
final no-incremental build restored every product and test-host artifact.

## Coverage, CRAP and gates

Final Cobertura is `/private/tmp/vicione-servicebus-iteration-199-final.cobertura.xml`, SHA-256
`3f84e2dfefee7cc81022e02bf2ef1098a8c03c67e7997e1f90ecfa9656c3a51d`. Exact source-filename
selection includes any compiler-generated classes of the same source while excluding foreign
sources.

| Owner source | Lines | Branches | Maximum method CRAP |
| --- | ---: | ---: | ---: |
| `InMemorySagaRepositoryServiceCollectionExtensions.cs` | 7/7 | 0/0 | 1 |
| `DependencyInjectionSagaReceiveEndpointExtensions.cs` | 17/17 | 0/0 | 1 |
| `SagaExtensions.cs` | 21/21 | 18/18 | 10 |
| **Total** | **45/45** | **18/18** | **10** |

Sorted display-name SHA-256 is
`41385e5bf47c3819f31c26dcb7b64a1726c84f205be785d2b367dab6d49d246b` across 5,412 unique
displays. The strict Core CTRF artifact is
`/private/tmp/vicione-servicebus-iteration-199-results/iteration199-final.ctrf.json`, SHA-256
`f45dfb8df92b487db856ab59e17da310b92cdd95a2e16385dd22acce0fb66764`.

| Gate | Result |
| --- | --- |
| Three final owned classes | 14/14 passed |
| Configuration/Sagas namespace regression | 285/285 passed |
| Full Core Release | 5,412/5,412 passed, 0 failed, 0 skipped |
| Full EF unit Release | 249/249 passed, 0 failed, 0 skipped |
| Core-test / EF-unit / EF-local Release builds | 0 warnings, 0 errors |
| Core/EF/EF-local requirement projections | 1/1, 1/1 and 1/1 passed |
| Scoped format, JSON and diff checks | Exit 0; no formatting or whitespace errors |
| Mutation probes | 6/6 compiled isolated material mutants killed |

No unresolved product correctness, lifecycle, callback, DI-lifetime, overload-shape, compatibility,
coverage or architecture finding remains in this admitted packet.

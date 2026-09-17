# Iteration 203 — saga repository provider and completion admission

## Outcome

Saga repository-provider boundaries now own their required configurators, and registration
completion constructs one deterministic, prevalidated repository plan from a stable provider
snapshot. Duplicate registered or repository-only requirements configure once, exact existing
repositories are preserved, and the canonical registration identity wins when requirements
overlap. The public registration contracts are frozen by exact reflection and nullability tests.

The lead personally read all ten current sources before delegating three mutually exclusive
provider, public-contract and completion source/test pairs to Sol 5.6 xhigh agents. They are newly
unique, moving cumulative exact unique source coverage to 663/4,118 files (16.100%). The packet
contained 229 physical source lines before the change and 272 after admission.

## Corrections and direct contracts

- `InMemoryRepository` and `SetInMemorySagaRepositoryProvider` reject a missing configurator at
  their own boundary; the repository callback is stateless and the fluent input/result identity is
  preserved.
- The in-memory provider forwards the exact configurator through the canonical extension path.
  The missing provider rejects a missing configurator before producing its stable, saga-specific,
  actionable repository diagnostic.
- The six public registration interfaces and `DefaultSagaDefinition<TSaga>` have exact
  accessibility, inheritance, variance, generic-constraint, declared-member, callback, result and
  nullability coverage. Two misleading XML comments were corrected without changing API shape.
- `Ensure`, `RequireRepository` and `Complete` own their configurator guards. The participant's
  provider setter rejects `null`, and one provider snapshot is used for the entire completion run.
- Registered and repository-only saga types are coalesced by runtime type. The first canonical
  `ISagaRegistration` is retained, repeat requirements configure once, and exact existing
  `ISagaRepositoryContextFactory<TSaga>` descriptors suppress provider work.
- The complete plan is ordered ordinally by full type identity. Every registration, runtime saga
  type and reflection activation is validated before the first provider callback, preventing
  partial provider effects from late malformed registrations.
- Provider exceptions propagate without wrapping and stop the remaining ordered plan. The
  unreachable `Activator.CreateInstance` null fallback was removed after explicit prevalidation.

## Source manifest

Each record is `path<TAB>content-sha256<LF>`, sorted ordinally. Manifest SHA-256 is
`2dfcc6336fefe35fd4961c7611bf66135a414cfa64736392567fe57cab0eeb8a`. Chaining that hash from
iteration 202 yields
`41a65b2babaa92a713c70cf8a78e0a530513c6055eaf7ea2a697ace9381e4daf`.

| Source file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `src/ViciOne.ServiceBus.Sagas/Configuration/DependencyInjection/DefaultSagaDefinition.cs` | 9 | `30b292fe3cb17d3852e85afc8a0d28846116682f4011989f0730ea1098fe16ee` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/DependencyInjection/ISagaRegistration.cs` | 27 | `4bebe3adc4c2bf0fa3aac9267dfa4c8f5ca5722dff5bba9b1f320a7ba7e391a3` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/DependencyInjection/ISagaRegistrationConfigurator.cs` | 31 | `da999349a8719fe6c1986ed2518b3659ef6c3872df780dddd596167530c482bf` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/DependencyInjection/ISagaRepositoryDecoratorRegistration.cs` | 12 | `a6d67ad36acc12a0f285d63ca36eec7646f6e3a2e21e15051ded6afa27726570` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/DependencyInjection/ISagaRepositoryRegistrationConfigurator.cs` | 11 | `10570c59446959f95715feb213e2876525e498301ce21c3fbdcfc29aaf1cab52` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/DependencyInjection/ISagaRepositoryRegistrationProvider.cs` | 11 | `12786dbd79f8ca0449fd0d6f9edee5bba7645b5bedc80752b0cc8570a50d4976` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/DependencyInjection/InMemorySagaRepositoryRegistrationExtensions.cs` | 28 | `3b72a20fdcf5d917f3fd22343a3b8017f17978bbe5863fb687f28321c4a17b3f` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/DependencyInjection/InMemorySagaRepositoryRegistrationProvider.cs` | 13 | `63019e0e4631804cbdd2685cdfe8e0e4f90311a267256ccbb1073f8424db4327` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/DependencyInjection/MissingSagaRepositoryRegistrationProvider.cs` | 18 | `4f451cf674ec65cd2c531e85efadf575c46e9d0eec59b370c6f8aeb51ce40cf3` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/SagaRegistrationCompletionParticipant.cs` | 112 | `9e08bbeafc598297a0313975a5732c361fd072361bd62ec61c0fb3004a8ba52a` |

## Test manifest, assertions and requirements

The three classes contain 21 test methods and 21 unique requirement variants. They assert exact
public/internal surface, constraint and nullability metadata, causal guards, callback and fluent
identity, descriptor effects, stable diagnostics, canonical participant/provider/registration
identity, duplicate coalescing, existing-repository suppression, global order, provider snapshot
isolation, full preflight and fail-fast exception propagation. The mandatory code-testing workflow,
static source/test pairing, pseudo-mutation gap analysis and assertion-quality audit drove the
research-to-plan-to-test sequence.

Test manifest SHA-256 is
`1982604f5bd094836d76e617dc2f66363706699aa8c871322b81ddb99b1610e9`; chained from iteration
202 it yields
`bdb703598e7a420cdc2d61023f7656bc1f09ece644ffc1a976999b16683292e0`.

| Test file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `tests/ViciOne.ServiceBus.Tests/Configuration/SagaRegistrationPublicContractTests.cs` | 396 | `cdbce70ece2350e94e23682c44d0ec9201158f2f4b0fe4bad770bc5d1d1efb53` |
| `tests/ViciOne.ServiceBus.Tests/Configuration/SagaRepositoryRegistrationProviderDeepContractTests.cs` | 342 | `b5882143dea156bbae7aeb69da3be369d3918d2155c15409375e91201f600859` |
| `tests/ViciOne.ServiceBus.Tests/Configuration/SagaRegistrationCompletionParticipantDeepContractTests.cs` | 362 | `5251a34c10c97e9e07615dc9975405b46ac9bf0c43ec8277db1b29dda455f7c3` |

`CoreRequirements.json` SHA-256 is
`0171b5dc39227030aaeaab0f757625d43f7a22f3134d237578f63b09ad254638`.

## Mutation proof

Six compiled, isolated, material single-cause mutants were killed and restored:

1. the in-memory repository extension's receiver guard was removed;
2. the missing provider's receiver guard was removed;
3. the completion participant accepted a `null` provider;
4. deterministic completion order was reversed;
5. duplicate registrations replaced the first canonical registration identity; and
6. the provider was reread during callbacks instead of using one completion snapshot.

Each mutant caused its owning deep-contract method to fail at the exact intended invariant. The
final no-incremental build restored every product and test-host artifact.

## Coverage, CRAP and gates

Final Cobertura is
`/private/tmp/vicione-servicebus-iteration-203-results/final/iteration203-final.cobertura.xml`,
SHA-256 `d411e49efcd9cbcd83274cf86128f9e0672f4e5593b673660e4d0eef4f6ce902`.
Exact source-basename selection includes compiler-generated classes belonging to the same source.
Unique executable line numbers and branch conditions use maximum coverage across duplicate
generated entries.

| Owner source | Lines | Branches | Maximum method CRAP |
| --- | ---: | ---: | ---: |
| Six declarative contract/default-definition sources | 0/0 | 0/0 | 0 |
| `InMemorySagaRepositoryRegistrationExtensions.cs` | 6/6 | 2/2 | 2 |
| `InMemorySagaRepositoryRegistrationProvider.cs` | 3/3 | 0/0 | 1 |
| `MissingSagaRepositoryRegistrationProvider.cs` | 7/7 | 0/0 | 1 |
| `SagaRegistrationCompletionParticipant.cs` | 44/44 | 26/26 | 14 |
| **Total executable** | **60/60** | **28/28** | **14** |

All executable owner lines and branches are covered. No admitted method exceeds the CRAP threshold
of 30.

Sorted display-name SHA-256 is
`da0e571205547ed846c4c0a0622e8ed186bf60e69a8f102b535b80f9c46c3a2b` across 5,495 unique
displays. The strict Core CTRF artifact is
`/private/tmp/vicione-servicebus-iteration-203-results/final/iteration203-final.ctrf.json`,
SHA-256 `b195198a45e14bcedc676f211ab38d495fb0270e27bd749195befc2a1eeecd48`.

| Gate | Result |
| --- | --- |
| Three final owned classes | 21/21 passed |
| Configuration/Sagas namespace regression | 723/723 passed |
| Full Core Release | 5,495/5,495 passed, 0 failed, 0 skipped |
| Full EF unit Release | 249/249 passed, 0 failed, 0 skipped |
| Core-test / EF-unit / EF-local Release builds | 0 warnings, 0 errors |
| Core/EF/EF-local requirement projections | 1/1, 1/1 and 1/1 passed |
| Scoped format, JSON and diff checks | Exit 0; no formatting or whitespace errors |
| Mutation probes | 6/6 compiled isolated material mutants killed |

No unresolved product correctness, public-contract, ordering, identity, preflight, provider,
compatibility, coverage-risk or architecture finding remains in this admitted packet.

# Iteration 201 — saga registration, definition and discovery admission

## Outcome

Generic saga registration, saga definitions, endpoint definitions and bulk discovery now own their
public boundaries and plan every registration before mutating the service collection. Runtime
definition types are restricted to their exact closed concrete family, concurrency and collaborator
contracts fail locally with stable diagnostics, derived endpoint names track the current public
definition, and discovery is deterministic across explicit types, assemblies and namespaces.

The lead personally read all four current sources before delegation. They are newly unique, moving
cumulative exact unique source coverage to 650/4,118 files (15.784%). The packet contained 456
physical lines before the change and 665 after admission.

## Corrections and direct contracts

- Generic `AddSaga` and `AddSagaStateMachine` validate the receiving configurator first. Runtime
  definition types must be closed, concrete classes implementing the exact
  `ISagaDefinition<TSaga>` family before any DI mutation.
- `SagaDefinition<TSaga>` rejects non-positive concurrency limits, owns all three configuration
  collaborators in signature order, guards endpoint-name formatters and recomputes derived names
  when its public endpoint definition changes. An explicit endpoint name remains authoritative.
- Endpoint callbacks commit only after successful completion, retaining the prior definition when a
  callback fails.
- `SagaEndpointDefinition<TSaga>` owns both settings and formatter boundaries and forwards the exact
  saga type and formatter result.
- Bulk discovery now provides exact zero-argument and filter-only entry points, validates arrays and
  null elements, admits only distinct closed concrete candidates, and excludes consumer-kind-owned
  state machines.
- Both registration families eagerly plan filters, state-machine/state pairing and unique
  definitions before the first service mutation. Duplicate definitions and late filter failures are
  therefore atomic.
- Namespace discovery uses ordinal case-sensitive exact/child matching. Nullable explicit-type
  filters accept all candidates; state-machine filters select when either the machine or state type
  matches.

## Source manifest

Each record is `path<TAB>content-sha256<LF>`, sorted ordinally. Manifest SHA-256 is
`7ee65b615d580ab0c1df3da9b6b5af4bd7dd28084b9ca22c064c5b005471d634`. Chaining that hash from
iteration 200 yields
`6d749ce62c8ae293aa9a4e3e6c22bf3ca1c9a732f8a4329267d695da7aac12dd`.

| Source file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `src/ViciOne.ServiceBus.Sagas/Configuration/SagaDefinition.cs` | 99 | `125101ca9474ca2152f0825c8e46741527bbf4b358c04db5a5b356c112f42698` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/SagaEndpointDefinition.cs` | 25 | `22c7946083f04581c8323eb28513522800d7fcb77a8a1f356259c89a99d56f44` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/SagaRegistrationConfiguratorExtensions.cs` | 127 | `f45ef7881f381702b1aa6619951fc0f14860828751510de7654d5a4e877c332e` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/SagaRegistrationExtensions.cs` | 414 | `6eeb1b93b1b763da8804bb23eae1a6b024e295fd3f51f4c38826a1f0ef3f2de0` |

## Test manifest, assertions and requirements

The four classes contain 23 test methods and 23 unique requirement variants. They assert exact
public shape, causal guard ordering, stable runtime-family diagnostics, DI mutation isolation,
callback commit behavior, endpoint-name dynamics, collaborator identity, closed/concrete/owned
selection, unique definitions, ordinal namespaces, filter semantics and eager planning atomicity.
The mandatory code-testing workflow, static source/test pairing, pseudo-mutation gap analysis and
assertion-quality audit drove the research-to-plan-to-test sequence.

Test manifest SHA-256 is
`6f62bcd860606da89a787b6048df2767c0bcbb1a2a930e913846a1d0399449f6`; chained from iteration
200 it yields
`28fb0143c801ed3a424e6b64d0622ca00f209a82cf85bc8a4c089e9d9ce13406`.

| Test file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `tests/ViciOne.ServiceBus.Tests/Configuration/SagaDefinitionDeepContractTests.cs` | 221 | `f87356dc846f42941d63c539a941dca818390ee2994fdba170aba581789cad23` |
| `tests/ViciOne.ServiceBus.Tests/Configuration/SagaEndpointDefinitionDeepContractTests.cs` | 108 | `d6f3295094f59f43ffcf3c41639a687e1764b30f7a8f27c3f2f85e3fc70681a6` |
| `tests/ViciOne.ServiceBus.Tests/Configuration/SagaRegistrationConfiguratorExtensionsDeepContractTests.cs` | 429 | `fc86baca785aa978e98f11af7b4e703d0c7b4f65419477f34f69b5d62c27e7ec` |
| `tests/ViciOne.ServiceBus.Tests/Configuration/SagaRegistrationExtensionsDeepContractTests.cs` | 410 | `5acb308e50ac553cb5d74310aaa12b8afbc0ed59ec3f82ed2982c1da472b2713` |

`CoreRequirements.json` SHA-256 is
`8e9f7bacf9f190e8e61cb6fdb68e368f86dcb9c4c295538c875c9c4a99605d0d`.

## Mutation proof

Six compiled, isolated, material single-cause mutants were killed and restored:

1. definition validation was removed from generic `AddSaga`;
2. definition validation was removed from generic `AddSagaStateMachine`;
3. the endpoint-configurator guard was removed from `SagaDefinition.Configure`;
4. stale derived endpoint-name caching was reintroduced;
5. the explicit `Type[]` null-element guard was removed; and
6. namespace comparison was weakened from ordinal to ordinal-ignore-case.

Each mutant caused its owning deep-contract method to fail at the intended invariant. The final
no-incremental build restored every product and test-host artifact.

## Coverage, CRAP and gates

Final Cobertura is
`/private/tmp/vicione-servicebus-iteration-201-results/postbuild/iteration201-final.cobertura.xml`,
SHA-256 `e914fb58598676654b19bcb1e22f0cbd564fb276d4696e44ac0381238090b358`.
Exact source-basename selection includes compiler-generated classes belonging to the same source.
Unique executable line numbers and branch conditions use maximum coverage across duplicate generated
entries.

| Owner source | Lines | Branches | Maximum method CRAP |
| --- | ---: | ---: | ---: |
| `SagaRegistrationConfiguratorExtensions.cs` | 38/38 | 12/12 | 10 |
| `SagaDefinition.cs` | 28/28 | 14/14 | 6 |
| `SagaEndpointDefinition.cs` | 4/4 | 2/2 | 2 |
| `SagaRegistrationExtensions.cs` | 143/158 | 86/94 | 16.0136 |
| **Total** | **213/228** | **114/122** | **16.0136** |

The fifteen residual discovery lines comprise natural-overload closing sequence points, the two
namespace-less-type diagnostics, multiply implemented state-machine/definition defensive catches,
and the private namespace helper's repeated null defense. They are exceptional structural guards,
not an unverified accepted registration path. All normal overloads, selection decisions, planning
failures and mutation points execute directly. No admitted method exceeds the CRAP threshold of 30.

Sorted display-name SHA-256 is
`e02b70fb4b37562285b26a5fcd9c468e506b5d7b9fd53236bd178a1b3a9275f6` across 5,449 unique
displays. The strict Core CTRF artifact is
`/private/tmp/vicione-servicebus-iteration-201-results/postbuild/iteration201-final.ctrf.json`,
SHA-256 `40c0c1d1f510d3f76fe175e6ac5e482447840ea7a17e2f5942c458f4be176ea4`.

| Gate | Result |
| --- | --- |
| Four final owned classes | 23/23 passed |
| Configuration/Sagas namespace regression | 677/677 passed |
| Full Core Release | 5,449/5,449 passed, 0 failed, 0 skipped |
| Full EF unit Release | 249/249 passed, 0 failed, 0 skipped |
| Core-test / EF-unit / EF-local Release builds | 0 warnings, 0 errors |
| Core/EF/EF-local requirement projections | 1/1, 1/1 and 1/1 passed |
| Scoped format, JSON and diff checks | Exit 0; no formatting or whitespace errors |
| Mutation probes | 6/6 compiled isolated material mutants killed |

No unresolved product correctness, overload-shape, mutation-order, namespace-selection,
compatibility, coverage-risk or architecture finding remains in this admitted packet.

# Iteration 202 — saga DI registration and repository-service admission

## Outcome

Class-saga and state-machine DI registration now reject invalid collaborators and runtime types in
causal signature order before local descriptor mutation. Valid registrations retain canonical
registration identity, exact definition identity, singleton machine aliasing and bus-owner
isolation. Repository service registration owns its receiver and the internal repository reset now
removes the closed descriptors actually produced by all three registration families while
preserving unrelated descriptors and survivor order.

The lead personally read all three current sources before delegating three mutually exclusive
source/test pairs to Sol 5.6 xhigh agents. They are newly unique, moving cumulative exact unique
source coverage to 653/4,118 files (15.857%). The packet contained 409 physical lines before the
change and 546 after admission.

## Corrections and direct contracts

- All seven class-saga DI overloads own `collection`, `registrar`, runtime saga and runtime
  definition boundaries in signature order.
- Typed and runtime class sagas must be closed, concrete, non-abstract `ISaga` implementations.
  State-machine instances retain a dedicated diagnostic directing callers to the state-machine
  registration family.
- Class-saga definitions must be closed concrete classes implementing exactly the matching
  `ISagaDefinition<TSaga>` contract. Admissibility is complete before descriptor effects.
- All seven state-machine DI overloads apply equivalent receiver/registrar ownership. Runtime
  machines must be closed concrete classes implementing exactly one
  `ISagaStateMachine<TSaga>` family; definitions must be closed, concrete and exactly paired.
- State-machine implementation and interface descriptors now use `TryAddSingleton`, so repeat calls
  preserve one canonical machine instance and the canonical saga registration.
- Valid definition registration preserves implementation/interface singleton identity; owner-
  qualified registrars keep registrations and definitions isolated from the default bus owner.
- Reflection registrar adapters are private closed nested classes with parameterless constructors.
  Their unreachable `Activator.CreateInstance` null fallbacks were removed after prevalidation.
- Repository helpers retain their exact ordered lifetime blocks: scoped consume/repository
  factories and singleton query/load facades followed by scoped factories. Repeat calls remain
  intentionally additive.
- `RemoveSagaRepositories` precomputes its full removal plan, recognizes the seven exact open or
  closed generic repository-service families, removes those descriptors and preserves every
  unrelated descriptor in order.

## Source manifest

Each record is `path<TAB>content-sha256<LF>`, sorted ordinally. Manifest SHA-256 is
`714439d74c0616d4eb42096308bf6b9db2acac8a955fed7de4ef0fba3a956b4c`. Chaining that hash from
iteration 201 yields
`d67d98a1a7111b58215df08b952ce76b08bd3b7781ef8ab580eb7d045c558e9f`.

| Source file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `src/ViciOne.ServiceBus.Sagas/Configuration/DependencyInjection/DependencyInjectionSagaRegistrationExtensions.cs` | 208 | `9e758334f726abd3e98b4200f26c25c93e118d4159daf32015487344208c3084` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/DependencyInjection/DependencyInjectionSagaStateMachineRegistrationExtensions.cs` | 248 | `45a5660ce89dc24203ddd8cf93dace5862f9e65da2d2660a9161b30280987757` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/DependencyInjection/RegistrationServiceCollectionExtensions.cs` | 90 | `4c170910e15e82359506df2b87ce331c5c75d0ce0abdbac9f62e8223591fc53e` |

## Test manifest, assertions and requirements

The three classes contain 25 test methods and 25 unique requirement variants. They assert exact
public/internal shape, causal guard order, typed and runtime admissibility matrices, stable
diagnostics, pre-effect failures, canonical registration identity, descriptor lifetime/order,
singleton alias identity, owner isolation, additive repository blocks, exact open/closed removal
scope and preflight mutation isolation. The mandatory code-testing workflow, static source/test
pairing, pseudo-mutation gap analysis and assertion-quality audit drove the research-to-plan-to-test
sequence.

Test manifest SHA-256 is
`a66c92735d5c4230bab78e589ac691423e879ef70308c2c394106774dc9ec270`; chained from iteration
201 it yields
`c15593d79a8a825fb8e4105da396068f7c32b999f2f8680bcf04820a9dfa6fd8`.

| Test file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `tests/ViciOne.ServiceBus.Tests/Configuration/DependencyInjectionSagaRegistrationExtensionsDeepContractTests.cs` | 483 | `00fc7f0aa05fdc2920c9684d7e7f558596a39b277a466f74433d230d5a10ae8d` |
| `tests/ViciOne.ServiceBus.Tests/Configuration/DependencyInjectionSagaStateMachineRegistrationExtensionsDeepContractTests.cs` | 547 | `1267381145f7cc0f1881de815da82fbff30cba56c7965c09896a56b1f31398bc` |
| `tests/ViciOne.ServiceBus.Tests/Configuration/RegistrationServiceCollectionExtensionsDeepContractTests.cs` | 413 | `6608a9bdd7c67d94cd91885a99d790f2d62db3fae497ad1bdfce78e4ae44d913` |

`CoreRequirements.json` SHA-256 is
`f0f5520cb0bfd04cee9eeddf0aed2ffae41d046d33b0eef84aedca963c90b0d2`.

## Mutation proof

Six compiled, isolated, material single-cause mutants were killed and restored:

1. the class-saga collection guard was removed, exposing the later registrar error;
2. abstract class sagas were admitted;
3. abstract class-saga definitions were admitted to the downstream registrar;
4. multiply implemented runtime state-machine families were accepted;
5. state-machine singleton deduplication was weakened to additive registration; and
6. repository removal stopped recognizing closed generic service families.

Each mutant caused its owning deep-contract method to fail at the exact intended invariant. The
final no-incremental build restored every product and test-host artifact.

## Coverage, CRAP and gates

Final Cobertura is
`/private/tmp/vicione-servicebus-iteration-202-results/final2/iteration202-final.cobertura.xml`,
SHA-256 `6f9e6c07867d264ffb2b992a1c6cd4a5105720b907a838a1c9280e08b0f3b3f9`.
Exact source-basename selection includes compiler-generated classes belonging to the same source.
Unique executable line numbers and branch conditions use maximum coverage across duplicate generated
entries.

| Owner source | Lines | Branches | Maximum method CRAP |
| --- | ---: | ---: | ---: |
| `DependencyInjectionSagaRegistrationExtensions.cs` | 61/61 | 24/24 | 10 |
| `DependencyInjectionSagaStateMachineRegistrationExtensions.cs` | 72/73 | 31/32 | 10.1372 |
| `RegistrationServiceCollectionExtensions.cs` | 31/31 | 6/6 | 4 |
| **Total** | **164/165** | **61/62** | **10.1372** |

The sole residual line/branch is the defensive rejection of a state type that does not implement
`ISagaStateMachineInstance` after a successfully closed `ISagaStateMachine<TSaga>` contract has
already been obtained. The generic interface itself constrains `TSaga` to that exact contract, so
the CLR cannot construct the contradictory closed interface. All representable public paths,
including every successful overload, execute directly. No admitted method exceeds the CRAP
threshold of 30.

Sorted display-name SHA-256 is
`f2a2eb2dc8b2caab3e16119bea6cd17740585d109002bb60b505c326c410f1e8` across 5,474 unique
displays. The strict Core CTRF artifact is
`/private/tmp/vicione-servicebus-iteration-202-results/final2/iteration202-final.ctrf.json`,
SHA-256 `e03a0f3d9840102bd46ad36ebc6034d748bdcccac3c6147af9b0bf42242f9030`.

| Gate | Result |
| --- | --- |
| Three final owned classes | 25/25 passed |
| Configuration/Sagas namespace regression | 797/797 passed |
| Full Core Release | 5,474/5,474 passed, 0 failed, 0 skipped |
| Full EF unit Release | 249/249 passed, 0 failed, 0 skipped |
| Core-test / EF-unit / EF-local Release builds | 0 warnings, 0 errors |
| Core/EF/EF-local requirement projections | 1/1, 1/1 and 1/1 passed |
| Scoped format, JSON and diff checks | Exit 0; no formatting or whitespace errors |
| Mutation probes | 6/6 compiled isolated material mutants killed |

No unresolved product correctness, overload-shape, DI-lifetime, runtime-type, descriptor-removal,
compatibility, coverage-risk or architecture finding remains in this admitted packet.

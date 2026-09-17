# Iteration 195 — saga public-role, repository and registration admission

## Outcome

Public saga roles, repository/query capabilities and registration-runtime dispatch are admitted.
The observed saga role now carries the same saga/class ownership constraint as the surrounding
API. Query helpers and registration dispatch reject invalid inputs at their immediate boundary,
before unrelated collaborator lookup or deferred enumeration. Public declaration contracts have
direct reflection evidence for inheritance, variance, constraints, nullability, cancellation
defaults and task shapes.

The lead personally read all 16 current sources before delegation. They are newly unique, moving
cumulative exact unique source coverage to 624/4,118 files (15.153%). The packet contained 467
physical lines before the change and 484 lines after the admitted guards and iterator split.

## Corrections and direct contracts

- `IObserves<TMessage, TSaga>` requires `TSaga` to be a class implementing `ISaga`.
- Both query-property extraction overloads reject a null query with the exact public parameter
  name before dereferencing its expression.
- Registration discovery rejects a null consumer-kind context when called, rather than only once
  deferred enumeration begins.
- Typed saga-configuration callbacks are validated before container-selector lookup, preserving
  ownership and deterministic generic diagnostics even for an unknown registration.
- Nested registration forwarding rejects a null endpoint context at its own boundary.
- Public role interfaces preserve exact inheritance, variance, exclusions and member shapes.
- Repository interfaces preserve load/query composition, task shapes, Async naming, cancellation
  defaults, out-nullability and generic constraints.
- Registration planning, filtering and execution preserve order, definition identity, callback
  identity, exact inputs and dispatcher eligibility.
- Metadata classification distinguishes saga types, definitions, state-machine ownership and
  capability interfaces without broad false positives.

## Source manifest

Each record is `path<TAB>content-sha256<LF>`, sorted ordinally. Manifest SHA-256 is
`d41dda82f4bf11c288bbe614d1e15f606ebc2cc8b9ad7ab5afada977d6958ab1`. Chaining that hash from
iteration 194 yields
`fb371742bc4dd307154ae39be39ff8c5f2d3b62fd4da7228be8e230b634ba6cb`.

| Source file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `src/ViciOne.ServiceBus.Sagas/Sagas/IInitiatedBy.cs` | 13 | `628c272278ed0a95c695444615def60dc555004c6dfde542b891db88184dcadc` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/IInitiatedByOrOrchestrates.cs` | 13 | `b9878347eb77160aeb16e8b1b4599953f54a140114bb3ac43153c8a7ba9c369c` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/ILoadableSagaRepository.cs` | 10 | `dc98d1365d4387faf510e4e79ed2a51e9cdc8fc9d512a80340c00205f078af1f` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/IMissingInstanceRedeliveryConfigurator.cs` | 25 | `6e48f99c9319dbf5c3a3de17b5ab0c829da49a5d64775e5a7eb8e569770f2c94` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/IObserves.cs` | 18 | `bf3e944b634f1abeeb353c2db3df98b80f1aeba3fe80027a37b5724b008772e8` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/IOrchestrates.cs` | 13 | `6d8515debe24e5043bccc5f0edadbcde5b94115ffe33877ce65db1c8fbec959a` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/IQuerySagaRepository.cs` | 18 | `48fa68f44b39709b71eb6f704e5db525930bc7d4a0ec40144ca53e96c5c9e02b` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/IQueryableSagaRepository.cs` | 8 | `739604857a7be1fbe8a3175bcb04f3662552c4aac48d37718babc651d7f3b172` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/ISagaQueryFactory.cs` | 18 | `943f63e4f3143e8ab66176dad45065b6bd26d29d130011d9aa8b6d448a19f9ea` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/ISagaRepository.cs` | 35 | `cae62b96489eddcd9be616fff289415c6c8ce45d7fb1e8a9cc062ced78804d1a` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/ISagaStateMachineInstance.cs` | 7 | `75e93703cc681bd80bccae7bdedd05f6f1a10dd7a9a4f7dc98be47ee0572d1a8` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/ISagaVersion.cs` | 9 | `0a20ce16e6d38a3268c5b9452327232471ab79cd8f75c5c7892071404a1e71fe` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/SagaConsumerKind.cs` | 163 | `0ed7f545397027b66111aa26df1c318f25eb0fc0cc78a9df56b4b37f401def85` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/SagaFactoryMethod.cs` | 10 | `047994e0724e75becc830ddab5694a62005f0344c042e5f6d3de87f00b3b3c32` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/SagaQueryExpressionPropertyExtensions.cs` | 72 | `4106d8dfa5d48e5e83b541c61b2066227cc29026e02d809602e9446945bea17e` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/SagaRegistrationMetadata.cs` | 52 | `0e91468dd4793cad3d3e0e9d94d339d088b11b7ea05c23013630c5aad92e1a3b` |

## Test manifest, assertions and requirements

The three classes contain 18 test methods, 18 expanded cases and 18 unique requirement variants.
Every test contains causal assertions; the portfolio includes equality, identity, exception,
parameter-name, collection/order, negative-path, state and structural assertions. No assertion-free
or trivial-only case remains. The mandatory code-testing workflow, static source/test pairing,
pseudo-mutation gap analysis and assertion-quality audit drove the research-to-plan-to-test
sequence.

Test manifest SHA-256 is
`8dd941259cfffa644779d49dac21f8b8aef1ad84b78e3d3dc07d074937d7f83a`; chained from iteration
194 it yields
`d559638a369d08e64a6740a40d8aaec3c72644fae08491940540332830f1bcd1`.

| Test file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `tests/ViciOne.ServiceBus.Tests/Sagas/SagaPublicRoleContractTests.cs` | 284 | `07bac27fb639f2effcd1a3afc5108097b1505adda333a935b5648e826399d478` |
| `tests/ViciOne.ServiceBus.Tests/Sagas/SagaRepositoryPublicContractTests.cs` | 410 | `e75121fd54dde39fd4b37c836c9d713e726a13ad20fdb59a5152880da617bbba` |
| `tests/ViciOne.ServiceBus.Tests/Sagas/SagaRegistrationRuntimeDeepContractTests.cs` | 689 | `8e72706605a8ef364ec0a2195a4a12a5142c0f19b82eff444c188078221e1290` |

`CoreRequirements.json` SHA-256 is
`53199803543cf48de24068b25be179d52edbee0c8126949c687eed552dc9e22d`.

## Mutation proof

Six compiled, isolated single-cause mutants were killed and restored:

1. the `IObserves` saga constraint was removed;
2. the untyped query null guard was removed;
3. the typed query null guard was removed;
4. immediate registration-context validation was moved back behind iterator deferral;
5. typed callback validation was moved after selector lookup; and
6. nested registration context validation was removed.

Each mutant failed the exact owning test and the final no-incremental build restored the product and
test-host artifacts.

## Coverage, CRAP and gates

Final Cobertura is `/private/tmp/vicione-servicebus-iteration-195-final.cobertura.xml`, SHA-256
`a1b47e7e431c3329f7249e846a055befdddc947099a5e41e92893c915d2d4187`.
The three executable owners reach 128/128 lines (100.000%) and 77/82 branches (93.902%):
`SagaConsumerKind.cs` reaches 76/76 lines and 23/24 branches,
`SagaQueryExpressionPropertyExtensions.cs` 30/30 and 28/28, and
`SagaRegistrationMetadata.cs` 22/22 and 26/30. The other 13 sources are declaration-only.
Maximum method CRAP is 14 for `TryGetPropertyValue`, with complete line coverage.

The residual `SagaConsumerKind` branch is the compiler representation of a null-coalescing guard
after successful construction of a known internal concrete type. The four residual metadata
branches are compiler-mapped short-circuit paths in the role classifier; direct matrix tests cover
every role/definition result. All executable lines and material behaviors are covered.

Sorted display-name SHA-256 is
`9054075132bfb47718ebfb7e43125f13460fae143ed5e4db0ac6a46ef46a46c4` across 5,353 displays.
The strict Core CTRF artifact is
`/private/tmp/vicione-servicebus-iteration-195-results/iteration195-final.ctrf.json`, SHA-256
`a369baf4907a818f30ef5f16ce0b251c769685136ee8150f2b6b77c0f00b3e02`.

| Gate | Result |
| --- | --- |
| Three final owned classes | 5/5, 7/7 and 6/6 passed |
| Complete Sagas namespace regression | 132/132 passed |
| Full Core Release | 5,353/5,353 passed, 0 failed, 0 skipped |
| Full EF unit Release | 249/249 passed, 0 failed, 0 skipped |
| Core-test / EF-unit / EF-local Release builds | 0 warnings, 0 errors |
| Core/EF/EF-local requirement projections | 1/1, 1/1 and 1/1 passed |
| Scoped format, JSON and diff checks | Exit 0; no formatting or whitespace errors |
| Mutation probes | 6/6 compiled isolated mutants killed |

No unresolved correctness, lifecycle, cancellation, compatibility, coverage or architecture
finding remains in this admitted packet.

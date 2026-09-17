# Iteration 190 — saga repository admission

## Outcome

The saga repository query, runtime dispatch and new-instance factory/policy boundaries are admitted.
The packet makes invalid collaborator outcomes explicit, preserves caller task/fault/cancellation
identity, and rewrites expression parameters by identity so same-typed nested scopes remain correct.
Supported behavior is retained.

This admission adds 23 personally read current source files. Cumulative exact source coverage is
533/4,118 files (12.943%).

## Corrections

- Repository query contexts and factories reject every missing required owner with the exact public
  parameter name, while forwarding valid operations, tokens, results and enumeration unchanged.
- `LoadSagaRepository`, `QuerySagaRepository` and the composite repository expose deterministic
  failures for impossible null Tasks/results without replacing valid faulted or canceled Tasks.
- Cached loaded-saga lookup honors pre-cancellation before creating a consume context.
- Filter conversion binds only the exact message parameter, is serialized for safe converter reuse,
  supports whole-message expressions and cannot capture a same-typed saga parameter accidentally.
- State-expression combination replaces only the outer state parameter and preserves nested lambda
  scope, including expressions that reference both the child and outer instance.
- Factory-method and new-saga policy boundaries validate required owners, null factory results and
  null downstream Tasks while preserving exact state, context, cancellation and exception identity.
- Removed-instance diagnostics validate and identify the exact saga type and correlation id.

## Source manifest

The manifest record is `path<TAB>content-sha256<LF>`, sorted ordinally. Manifest SHA-256 is
`63b534372331e20d640574311e816e0bc6d53c28b97c67345f679375584fc410`. Chaining that hash from
iteration 189 yields
`3fadd7497731910cda193f51a18282dc1d1c8d615011e78200c49dc5bd332b52`.

| Source file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `src/ViciOne.ServiceBus.Sagas/Saga/DefaultSagaRepositoryQueryContext.cs` | 177 | `01ef402a80b63a0741a250e129c4159e5be0caac5c8e8a28082f605140f51cc2` |
| `src/ViciOne.ServiceBus.Sagas/Saga/ExpressionSagaQueryFactory.cs` | 39 | `5d7cd09de0c1e20b04df53ff2a1d91c57cef2bb68e1497c659cb4aafe4a03a37` |
| `src/ViciOne.ServiceBus.Sagas/Saga/FactoryMethodSagaFactory.cs` | 60 | `71288cb1f4038b4621c449217d3c0ceffa92edd81593a64532accebe0db74a5c` |
| `src/ViciOne.ServiceBus.Sagas/Saga/ILoadSagaRepositoryContext.cs` | 17 | `14406f32372772ab108c9c5858d230e16ec12a8a19f1860a578a19a399a6a80c` |
| `src/ViciOne.ServiceBus.Sagas/Saga/ILoadSagaRepositoryContextFactory.cs` | 20 | `e14d857ff64dabfd9e8ea21fcd0a7b7afb94f11c623de126d6c13f766b483fba` |
| `src/ViciOne.ServiceBus.Sagas/Saga/IPropertyExpressionPropertyValue.cs` | 9 | `c5fd52df9733a5fc1fd6a47d373ff9dfebf667555f483d74ebc0fe5d3a6d5d49` |
| `src/ViciOne.ServiceBus.Sagas/Saga/IQuerySagaRepositoryContext.cs` | 18 | `ad2994355129b259b5251c440c17e8f978b90f1438de9e8ae7553d62558db8f2` |
| `src/ViciOne.ServiceBus.Sagas/Saga/IQuerySagaRepositoryContextFactory.cs` | 20 | `b54de54ffcb31d0ec430e4de7a9c96a05a131b02b910c2f6fbe326a2954c31ca` |
| `src/ViciOne.ServiceBus.Sagas/Saga/ISagaRepositoryContext.cs` | 63 | `e3a9a1f7ef35c498dbdc8d1e94e49f3fda402c5751a4df5d79d9acf4e8f39090` |
| `src/ViciOne.ServiceBus.Sagas/Saga/ISagaRepositoryContextFactory.cs` | 27 | `876a384f1e33fd308c5753a5d114ea373547fb89e60da488b78ac91cf5ebc8a1` |
| `src/ViciOne.ServiceBus.Sagas/Saga/ISagaRepositoryQueryContext.cs` | 29 | `fcc22387a0694cc269eeb427f5fb74480042576c61c18c1c49ae02e0df6666f1` |
| `src/ViciOne.ServiceBus.Sagas/Saga/InMemoryRepository/SagaInstanceRemovedException.cs` | 11 | `2a660012eea1195ce13fa956204e763c4717393faa9c78e558b27b5151719640` |
| `src/ViciOne.ServiceBus.Sagas/Saga/LoadSagaRepository.cs` | 45 | `b9146fae9185d70363141e46a8ed2c3e89d7c96c806e884998f71fb7764d5a0e` |
| `src/ViciOne.ServiceBus.Sagas/Saga/LoadedSagaRepositoryQueryContext.cs` | 186 | `596f42af4e1c800d12724110af4e2744bb1af4f17ee7444adfac5685964bdeac` |
| `src/ViciOne.ServiceBus.Sagas/Saga/NewSagaPolicy.cs` | 68 | `ff4a37d61c7535b73696a2d869af9a744e93a64ed43e021e0124fd1bb892ac4a` |
| `src/ViciOne.ServiceBus.Sagas/Saga/PropertyExpressionPropertyValue.cs` | 16 | `eaa27c1d78d421d754a3b6b5337cf0dc88a17e9796d3cf44bd2f946ac56cec60` |
| `src/ViciOne.ServiceBus.Sagas/Saga/PropertyExpressionSagaQueryFactory.cs` | 71 | `24b7d8317a37f04b2ae134c3bceb9b1cce8fae7bd33561d8a6427c7ed138ae96` |
| `src/ViciOne.ServiceBus.Sagas/Saga/QuerySagaRepository.cs` | 62 | `85937d9af3896dda4598e63779fe956ba6e2f276c278a2fc8fb9163d4f57796c` |
| `src/ViciOne.ServiceBus.Sagas/Saga/SagaConsumeContextMode.cs` | 14 | `8b7e7b7fc83b657fdc46ee8ccb2749a9c374482b5d87925fa8406013560f6ba1` |
| `src/ViciOne.ServiceBus.Sagas/Saga/SagaFilterExpressionConverter.cs` | 87 | `8beb2de79a0188976fdeb678ad862594a088c934ba4a7d2b5c0467d6ca47cecb` |
| `src/ViciOne.ServiceBus.Sagas/Saga/SagaInstanceFactoryMethod.cs` | 10 | `e951f912c20ed4365c5f3b4fb0933db49fb9c760dcdd01cc22beaeae25a90550` |
| `src/ViciOne.ServiceBus.Sagas/Saga/SagaRepository.cs` | 199 | `c05b3048530236fc86a686775a03076719fb007da9d7424e1f96342bb8c219b0` |
| `src/ViciOne.ServiceBus.Sagas/Saga/StateExpressionVisitor.cs` | 66 | `35051d2c1892ff3c0768c77587f57dee841c3f8f2377a3f7a78c8fd02537da1c` |

## Test manifest and requirements

Test manifest SHA-256 is
`ac99dbbed650369dde95a892d83771aab460ff5ee1c25e5b5522954592d0176b`; chained from iteration
189 it yields
`66bcfafcc04e21b207163014aa150d694fa37f3945e83921cd86af9ecbd948dd`.

| Test file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `tests/ViciOne.ServiceBus.Tests/Sagas/SagaRepositoryFactoryPolicyDeepContractTests.cs` | 559 | `599701ed96f95321026ad3ee86c2ebda3c5e8e1b02a9f0d7b324dc93b9d64dd3` |
| `tests/ViciOne.ServiceBus.Tests/Sagas/SagaRepositoryQueryDeepContractTests.cs` | 450 | `05b27814f1bf1b230198121cd2507076159a14e63d680829c987afef08e81d10` |
| `tests/ViciOne.ServiceBus.Tests/Sagas/SagaRepositoryRuntimeDeepContractTests.cs` | 561 | `8afae2dd044ab4487a0fa1256f3a579b5ea58804223377b98f570c2b2cd41f97` |

The three classes contain 35 test methods, 56 expanded cases and 35 unique requirement variants.
`CoreRequirements.json` SHA-256 is
`a01d2af3e91315e230a0633bd374db9d6c21ead2f741b9f07baa8ab3d5daab14`.

## Proof

Eight compiled, isolated single-cause mutants were killed and restored:

1. default query results accepted null as an empty collection;
2. filter conversion matched parameters by type instead of identity;
3. state composition replaced same-typed nested parameters;
4. factory creation accepted a null saga result;
5. load execution exposed a null factory Task;
6. repository dispatch exposed a null factory Task;
7. cached saga lookup ignored pre-cancellation; and
8. missing-saga policy exposed a null factory Task.

The third probe initially survived and caused the nested-lambda test to be strengthened so the inner
child and outer saga state are both behaviorally significant. The unchanged mutant then failed at
the exact state-composition assertion.

Final Cobertura is
`/private/tmp/vicione-servicebus-iteration-190-final2.cobertura.xml`, SHA-256
`b157144af7775c7d479164c1c3010b4424ecf6cc75d91a30363bb53db437a3ef`.
Admitted coverage is 268/269 executable lines (99.628%) and 89/94 branches (94.681%). The sole
uncovered line is the invariant failure after visiting a typed lambda in `StateExpressionVisitor`.
The five residual branches are not supported product behavior: the known property-reflection lookup
cannot miss; a non-null expression body cannot visit to null; the member-evaluation helper is reached
only for the exact message parameter; the typed lambda remains a lambda after standard visitation;
and the private negation branch has no caller. Maximum admitted method CRAP is 6.

Sorted display-name SHA-256 is
`3ef33df56a1c6ff93c5bf2cb842e0bca42d458912fbf77a62dc4f3c0efe61175` across 5,174 displays.

| Gate | Result |
| --- | --- |
| Three final owned classes | 6/6, 18/18 and 32/32 passed |
| Saga namespace regression | 83/83 passed |
| Full Core Release | 5,174/5,174 passed, 0 failed, 0 skipped |
| Full EF unit Release | 249/249 passed, 0 failed, 0 skipped |
| Core-test / EF-unit / EF-local Release builds | 0 warnings, 0 errors |
| Core/EF/EF-local requirement projections | 1/1, 1/1 and 1/1 passed |
| Scoped format and diff checks | Exit 0; no whitespace errors |
| Mutation probes | 8/8 compiled isolated mutants killed |

No unresolved correctness, lifecycle, cancellation, concurrency or architecture finding remains in
this admitted packet.

# Iteration 208 — saga request and context admission

Date: 2026-09-18

Branch: `feature/servicebus-a-plus-api`

Scope: 10 previously unadmitted `ViciOne.ServiceBus.Sagas` sources

## Outcome

The advanced saga contracts, request envelope contracts, request state and state machine, and saga
consume-context proxy are admitted. One owner correction was made: `SagaConsumeContextProxy` now
rejects a missing saga context at construction with the stable `sagaContext` parameter name.

The public contracts, request-state persistence surface, correlation strategies, request lifecycle,
cancellation behavior and remaining forwarding behavior already satisfied their intended contracts
and were preserved unchanged.

## Source manifest

Each record is `path<TAB>content-sha256<LF>`, sorted ordinally. Manifest SHA-256 is
`a8351b385c6a7919d7c2e701a17a548cc31053b53c73adae7979a0832d53d210`. Chaining that hash from
iteration 207 yields
`cef253a3e7cbda1894b7517d82e927e6dba129803ddb45e8d258364ce4908d64`.

| Source file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `src/ViciOne.ServiceBus.Sagas/Advanced/ICorrelatedBy.cs` | 11 | `8a04785eee3ff7fff55c61eed6fb082e5025cb41330b091d9fa55cef542df2ca` |
| `src/ViciOne.ServiceBus.Sagas/Advanced/ILoadSagaRepository.cs` | 17 | `55d0d669b15c9af56b42045c789987ff23810eeb6dde2adb7b4b1a6fd6826856` |
| `src/ViciOne.ServiceBus.Sagas/Advanced/ISaga.cs` | 11 | `8b34480731226823d02a4d7b70bdfde2dfb9297043cbe825032eae267a1effd3` |
| `src/ViciOne.ServiceBus.Sagas/Components/RequestState.cs` | 32 | `2421f26ea85ba680e0f0bdecc84792e93ddd42c2c7b702d116368eb3c10f10b9` |
| `src/ViciOne.ServiceBus.Sagas/Components/RequestStateMachine.cs` | 83 | `2968e7a1fbd390c77c1ea61991731bcbbb888b67c43c16feeef7aba790c06f42` |
| `src/ViciOne.ServiceBus.Sagas/Context/SagaConsumeContextProxy.cs` | 42 | `84767ace167005228b838447d682b5485a25ccb40345b0751127b2389a452fa0` |
| `src/ViciOne.ServiceBus.Sagas/Contracts/IRequestCompleted.cs` | 19 | `185dad4bda8cc44a664bd87c7c886c67d6b929def296d0d5a4b40e5726c0e1a5` |
| `src/ViciOne.ServiceBus.Sagas/Contracts/IRequestFaulted.cs` | 16 | `f965ae94eab8a46b82a0c50e6490d09592aab5d2ecf89a15991b7939aa4ec134` |
| `src/ViciOne.ServiceBus.Sagas/Contracts/IRequestStarted.cs` | 28 | `25df860c954920a03d22089f21c562bdcfc29e38a6cacf55a81a5d37be3a0579` |
| `src/ViciOne.ServiceBus.Sagas/Contracts/IRequestTimeoutExpired.cs` | 24 | `fa3a080f5f17b04bb9fd284d359c1bc03739f023cfeca1d72359b7dcf45339ea` |

Lead-read progress is 717 of 4,118 C# sources, or 17.411%.

## Test manifest, assertions and requirements

The mandatory code-testing workflow, static source/test pairing, pseudo-mutation gap analysis and
assertion-quality audit drove the research-to-plan-to-test sequence. Three classes contain 15 test
methods, 17 executed cases and 15 unique requirement variants.

Test manifest SHA-256 is
`f1fdb8fe69861c68e8d403b63f5aa186ed3fba89642dec1e9c118a6dee729560`; chained from iteration
207 it yields
`378dc9af1415bd3b74dadc56d2d64bfae0c64201660bdcb4e0a0ea3eedf6e5a5`.

| Test file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `tests/ViciOne.ServiceBus.Tests/Components/RequestStateMachineDeepContractTests.cs` | 284 | `ee4c094b3965eaeabc3dd1cb04d3e7e9bcc6cf37001f6557652b2dc97b0e9139` |
| `tests/ViciOne.ServiceBus.Tests/Context/SagaConsumeContextProxyDeepContractTests.cs` | 276 | `dae7d87e066cbfd8f766a25790455de869f1701972b11783e2240aca0f83ad3f` |
| `tests/ViciOne.ServiceBus.Tests/Contracts/SagaAdvancedRequestContractTests.cs` | 420 | `742be24b4851c3e5b0902981b296ec0aac8f6acb44cd7929563ec07bd496dc88` |

`CoreRequirements.json` SHA-256 is
`f069047987d404247bc3d59b143997093abfe7bd17ea13519fb39a3b4138467f`.

## Mutation proof

Six compiled, isolated, material single-cause mutants were killed and restored:

1. the context proxy accepted a missing saga-context owner;
2. the context proxy exposed the message correlation instead of the saga correlation;
3. completion discarded the caller's cancellation token;
4. request initialization discarded the conversation identifier;
5. request initialization accepted a missing source address; and
6. a completed request remained pending instead of finalizing.

Every mutant failed its owning test at the intended invariant. The final no-incremental build
restored every product and test-host artifact.

## Coverage, CRAP and gates

Final Cobertura is
`/private/tmp/vicione-servicebus-iteration-208-results.hRIkdR/iteration208-final.cobertura.xml`,
SHA-256 `da838c2190e24113ac7d9010ab7c508711c6e06f9dfee6ec684f54658a9982f6`.
Exact source-basename selection includes compiler-generated classes belonging to the same source.
Unique executable lines and branch conditions use maximum coverage across duplicate entries. The
seven interface-only sources contain no executable owner lines.

| Owner source group | Lines | Branches | Maximum method CRAP |
| --- | ---: | ---: | ---: |
| Request state | 9/9 | 0/0 | 2 |
| Request state machine | 48/48 | 26/26 | 10 |
| Saga consume-context proxy | 7/7 | 2/2 | 2 |
| **Total executable** | **64/64** | **28/28** | **10** |

Every executable owner line and representable owner branch is covered, and no method exceeds the
CRAP threshold of 30.

Sorted display-name SHA-256 is
`127d84ed3c01e7e22f267d66f4b4850799c213a39dc75d9d32d85227c4d5678e` across 5,600 unique
displays. The strict Core CTRF artifact is
`/private/tmp/vicione-servicebus-iteration-208-results.hRIkdR/iteration208-final.ctrf.json`,
SHA-256 `903c1345f00614242084a192103fae6cf24252d81f207c5ae6ebedbba8339bdc`.

| Gate | Result |
| --- | --- |
| Three final owned classes | 17/17 passed |
| Saga-wide regression (`*Saga*`) | 837/837 passed |
| Full Core Release | 5,600/5,600 passed, 0 failed, 0 skipped |
| Full EF unit Release | 249/249 passed, 0 failed, 0 skipped |
| Core-test / EF-unit / EF-local Release builds | 0 warnings, 0 errors |
| Core/EF/EF-local requirement projections | 1/1, 1/1 and 1/1 passed |
| Scoped format, JSON and diff checks | Exit 0; no formatting or whitespace errors |
| Mutation probes | 6/6 compiled isolated material mutants killed |

No unresolved public-contract, request-state, correlation, lifecycle, context-forwarding,
cancellation, null-boundary, compatibility or coverage-risk finding remains in this admitted
packet.

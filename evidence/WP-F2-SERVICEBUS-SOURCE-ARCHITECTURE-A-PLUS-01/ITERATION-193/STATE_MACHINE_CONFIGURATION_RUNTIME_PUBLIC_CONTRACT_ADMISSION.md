# Iteration 193 — state-machine configuration, runtime and public-contract admission

## Outcome

State-machine configuration, request/schedule metadata, concrete state runtime and their public
contracts are admitted. Required collaborators now fail at their exact boundary; request, schedule
and state metadata preserve their declared storage and lifecycle semantics; and state identity is
consistently ordinal and non-null.

The lead personally read all 20 current sources. They are all newly unique, moving cumulative exact
unique source coverage to 585/4,118 files (14.206%). The packet contained 1,496 physical lines before
the change and 1,537 lines after admitted documentation and implementation.

## Corrections

- Uncorrelated-event metadata rejects a missing event at construction while preserving its exact
  owned event and validation-only runtime contract.
- Request metadata validates names, settings, identifier generation, header callbacks and filters;
  it preserves accepted response types, timeout behavior and fault-time identifier clearing.
- Schedule metadata validates names, expressions and settings and protects delay/token accessors
  before delegation while preserving event and token storage.
- Concrete states validate callbacks, names, observers, probes, binds and event/state arguments;
  observer tasks cannot silently return null.
- State equality, hashing and ordering use ordinal names, and null no longer equals an empty-name
  state. Hierarchy, lifecycle-event exclusion and parent unhandled-event fallback remain intact.
- Public interfaces have direct reflection-based evidence for inheritance, variance, constraints,
  hidden members, cancellation defaults and synchronous/asynchronous shapes.
- Documentation now states the exact composite-event, state-lifecycle, request-fault and saga
  completion semantics exposed by these contracts.

## Source manifest

The manifest record is `path<TAB>content-sha256<LF>`, sorted ordinally. Manifest SHA-256 is
`a94f6d462692e0b3be0055b6549f35737438984468e93c9c7c03520b954da0e1`. Chaining that hash from
iteration 192 yields
`25fe1cf6bea2355b1d6f310ce8bc6b1da07ba7fa1d3b274628b197b779269491`.

| Source file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `src/ViciOne.ServiceBus.Sagas/Sagas/CompositeEventStatus.cs` | 84 | `a8cba5fc800104effebc01a18c682a02c73a4591082f0663da89978d411e384d` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/ConcurrencyMode.cs` | 10 | `928b0be97c59439f81b5bb692a9a2d5c7cca844eb07de58b583f36464d633149` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/Configuration/CompositeEventOptions.cs` | 20 | `b8785559640a765c6250b6b8f77111109a8749a3b46c83a4737bbfedb9ab189d` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/Configuration/IEventCorrelationConfigurator.cs` | 85 | `16d109cb1a71eeca77200ed55c80b948a648f13c49f3b40bb7347df43eec244f` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/Configuration/IMissingInstanceConfigurator.cs` | 30 | `8065f8410a267c8f635b6b5c95b2ee9489c40764d031d99f33092217ba4576d7` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/Configuration/IRequestConfigurator.cs` | 80 | `7201b0ae3fbdb0da34a1fc2e9bd79bd94c874f8d67413cb195cfbdac3f9073ad` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/Configuration/IScheduleConfigurator.cs` | 23 | `fb64bceb1b56c4996559a76a4774a8f0e814a35baf0ca75d9b943062aa67acb9` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/Configuration/IScheduleSettings.cs` | 17 | `5079275eabce36f602e515eb216e0be05653de6ae7554698812ea10e9eb5d350` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/Correlation/ViciOneServiceBusStateMachine.UncorrelatedEventCorrelation.cs` | 48 | `18d3f78766ded606c296f4b4d0d6e7fd63df34e1effdfea0c528b26978e927a1` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/IEvent.cs` | 21 | `d5d5020646e45b642d79133803472ee17a1a5c5c082c555c80315a7bdb85ebc3` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/IRequest.cs` | 93 | `c22034e6f1a5f634dc7dcd35cefb6aee01482a3ad56afd4cfa77c8bb9fa58240` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/IRequestSettings.cs` | 71 | `39e9994b6b5363ed246535cf90d47b0b6d206d50509a9641f32965456b903159` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/ISagaStateMachine.cs` | 20 | `438bc1dcd7818168cef669fb7a7dfc476d03a58e89cb8f5addfdfeef5b198295` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/ISchedule.cs` | 47 | `825244c5bf8a602d03a0b97806e27f6360428a479c93a4688a608fa887e3d2f9` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/IState.cs` | 93 | `38a000c411f835a7ec870796a3dec0293fa7177229060347e0611b03778f9770` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/IStateMachine.cs` | 92 | `1868732dddab4793a4e1f9f3750d7192076e255ab37e69133ab22730bd478f41` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/ViciOneServiceBusStateMachine.StateMachineEvent.cs` | 17 | `2d35f64a56058d9de9bb10a22957ffb88a870a69bda93b8ee927f571888344aa` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/ViciOneServiceBusStateMachine.StateMachineRequest.cs` | 200 | `8a4f81aff423377be074a0548949e0b6b8deaf36320daa948feee3039e7f23f4` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/ViciOneServiceBusStateMachine.StateMachineSchedule.cs` | 76 | `5f6cbeba6c49444103b52377b8f198bc8706fc5be82cad1a665565bb99b12304` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/ViciOneServiceBusStateMachine.StateMachineState.cs` | 410 | `eb5c63aaefd1db24d0767be28ddbf6320197fda246f86e1cb61983b5ec018c40` |

## Test manifest and requirements

Test manifest SHA-256 is
`1ac49331a04e361d707a874912cf0a0d8953145f05ffb9b26f418f4f1b64ed46`; chained from iteration
192 it yields
`8ff99200bb5a46bcc72cf6a84eb9ebb67eef11c231cdc9ce3ea6700b61599789`.

| Test file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `tests/ViciOne.ServiceBus.Tests/SagaStateMachine/SagaPublicContractShapeTests.cs` | 499 | `bd99f5a4ac83f9a67177baf7b533e2a2636ba228cef8f8d7d9d7e0a464743da5` |
| `tests/ViciOne.ServiceBus.Tests/SagaStateMachine/StateMachineConfigurationMetadataDeepContractTests.cs` | 74 | `125ed92cd596813a421b359f4938a6bf966a0e3854fa22a3b9c56b588be25e52` |
| `tests/ViciOne.ServiceBus.Tests/SagaStateMachine/StateMachineRuntimeMetadataContractTests.cs` | 455 | `7c6eaf4030050114be1fe0ca14ef66984303dea62859af67ab7575ff004cda9d` |

The three classes contain 17 test methods, 17 expanded cases and 17 unique requirement variants.
`CoreRequirements.json` SHA-256 is
`bed0cff0a83db18f3601d86c30085aa889e7f9253db47e74f48079b6e786ee12`.

## Mutation proof

Six compiled, isolated single-cause mutants were killed and restored:

1. the uncorrelated-event constructor accepted a null event;
2. request identifier generation accepted a null saga instance;
3. a positive request time-to-live was incorrectly filtered;
4. schedule token storage discarded the supplied token;
5. an empty-name state was treated as equal to null; and
6. parent unhandled-event fallback caught the wrong exception type.

## Coverage and dispositions

Final Cobertura is `/private/tmp/vicione-servicebus-iteration-193-final4.cobertura.xml`, SHA-256
`10c923d1741d81976932a362ec5e1dea4482f8607d792a40f507a34a6798a14f`.
Admitted coverage is 262/262 executable lines (100.000%) and 92/100 branches (92.000%). Per
executable owner: uncorrelated correlation 12/12 lines, composite status 17/17 lines and 6/6
branches, state-machine event 6/6 lines, request 56/56 lines and 19/20 branches, schedule 20/20
lines, and state 151/151 lines and 67/74 branches. Declaration-only interfaces and enums naturally
contain no executable lines.

Maximum method CRAP is 12 with full line coverage. The eight residual branches are compiler-created
null/coalescing or short-circuit paths around already validated collaborators and equivalent guard
expressions. Every executable line and each externally causal success, validation, fallback,
filtering, fault and cancellation outcome in the packet has direct evidence.

Sorted display-name SHA-256 is
`0fb6caaca971695194c77d9811f1cd4fda53a9bdcd46d5f9ea356c5c039ad380` across 5,308 displays.
The strict Core CTRF artifact is
`/private/tmp/vicione-servicebus-iteration-193-results/iteration193-final.ctrf.json`, SHA-256
`980eef49ddff8d92211d278e2ac8423ef4c6c71a651dad493d52b2a304f7a67a`.

| Gate | Result |
| --- | --- |
| Three final owned classes | 3/3, 5/5 and 9/9 passed |
| SagaStateMachine namespace regression | 218/218 passed |
| Full Core Release | 5,308/5,308 passed, 0 failed, 0 skipped |
| Full EF unit Release | 249/249 passed, 0 failed, 0 skipped |
| Core-test / EF-unit / EF-local Release builds | 0 warnings, 0 errors |
| Core/EF/EF-local requirement projections | 1/1, 1/1 and 1/1 passed |
| Scoped format and diff checks | Exit 0; no whitespace errors |
| Mutation probes | 6/6 compiled isolated mutants killed |

No unresolved correctness, lifecycle, cancellation, compatibility or architecture finding remains
in this admitted packet.

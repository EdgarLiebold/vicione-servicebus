# Iteration 194 — saga pipeline, behavior and runtime-surface admission

## Outcome

Saga pipeline configuration, connector ownership and the behavior/activity/runtime public surfaces
are admitted. Required collaborators and invalid partition counts fail at their immediate boundary;
partition keys, observer identity, retry/rescue specifications, option storage and partial-connection
cleanup preserve their exact ownership semantics. Declaration-only public contracts have direct
reflection evidence for inheritance, variance, constraints, overloads, cancellation defaults and
Task shapes.

The lead personally read all 23 current sources before delegation. They are newly unique, moving
cumulative exact unique source coverage to 608/4,118 files (14.764%). The packet contained 1,744
physical lines before the change and 1,768 lines after the admitted documentation and guards.

## Corrections and direct contracts

- Both saga partitioner overloads reject a partition count below one before configuring a pipe.
- Connector, configurator and specification owners reject missing state-machine, repository,
  observer, builder and specification collaborators at their exact boundary.
- Correlations are validated before value-type filtering; validation failures retain configuration
  ownership, and a generic saga mismatch names the generic parameter.
- Every successfully created partial connection is disposed if a later connection fails.
- Guid/text partition keys preserve exact bytes, explicit/default encodings and a deterministic
  diagnostic when a text key provider returns null.
- Retry, redelivery, outbox, timeout, rescue and registration overloads preserve exact input,
  callback, token, specification and context identity.
- State-machine configurator forwarding preserves concurrency, observer-handle disconnect,
  options-object identity, lookup and selection order.
- Behavior, context, activity, binder, selector, correlation, observer, accessor, modifier,
  visitor, unhandled-context and visitable contracts have exact public-shape evidence.

## Source manifest

Each record is `path<TAB>content-sha256<LF>`, sorted ordinally. Manifest SHA-256 is
`4067f1491ec8d1d3a155046f3f23f5d42f17ca54ea5827845340b4b13dca8800`. Chaining that hash from
iteration 193 yields
`8ff08a9f34c81bcb84fbc335b414bd52b12b924ce841b9ae3010fa8108ce7242`.

| Source file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `src/ViciOne.ServiceBus.Sagas/Sagas/Configuration/SagaPipelineConfigurationExtensions.cs` | 377 | `d17f0752717477fbdfe7037dadef0addb0124ab016f2fd399026eedb6d7ca1d3` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/Configuration/ViciOneServiceBusStateMachine.StateMachineConnector.cs` | 106 | `0398fc03086a6cbf8e38f238c89aa97f9a5597473e6dda3e6d793296d00baff4` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/Configuration/ViciOneServiceBusStateMachine.StateMachineSagaConfigurator.cs` | 97 | `5e24fd6500c16dfa183a56ef75f6d78eaa8827aabd044c721a745f6375a54d3b` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/Configuration/ViciOneServiceBusStateMachine.StateMachineSagaSpecification.cs` | 41 | `e005469caf100615879ac2b7fed4b194a9e31b0fbfc2d799fadf3f72a7f98ad3` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/IBehavior.cs` | 61 | `eefc03490faa6ecdfe82e8fd08a54a31844ac2eff7eb0899a9794adb3b2a202d` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/IBehaviorContext.cs` | 77 | `0c11d7347cf4c94c81aff529291a57d4af8d1573e9b8eca2b59edee274df2533` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/IBehaviorExceptionContext.cs` | 44 | `a6a9169d8c18c63d366af5c0adca8d3ccf2a9d9fba31523c4306404cf898ceca` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/IEventActivities.cs` | 14 | `fa12389d8c2387b6fe9fb209bae286e442a945c53bdeec2d9ca1a69f2582e359` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/IEventActivityBinder.cs` | 141 | `12901637507e3798172439d521a0617dec2f613597b642963a99f8457c624f62` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/IEventCorrelation.cs` | 36 | `76e56541eaf43a24334f87b32e0a324b523158ca28da79c9667d6118c4eb9865` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/IEventObserver.cs` | 48 | `65666aa0cfa32bad161f931f384dfc1c730a7f358ed1205c10da0ea9c5462470` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/IExceptionActivityBinder.cs` | 131 | `9140eb3d4abddd7be0d3a85f897a9d1df909c352da5c972556c1c0df1b080e63` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/IStateAccessor.cs` | 31 | `b92f53ec583093b47b4e667b2831e4b49ee9e5877dd2c16302702d66dffb0aca` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/IStateMachineActivity.cs` | 74 | `0506d33fedf63ceceeaf06a7bdd10b3416c6f9ca377023a345c3016423d0d750` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/IStateMachineActivitySelector.cs` | 34 | `24aade5c2b0df7d50e06b88faecd28545cb164f7a2b8f630580fe9d8fd65dcf8` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/IStateMachineEventActivitiesBuilder.cs` | 71 | `6890af93fe86233249a86805dfd9c2de20a338ea350c0f1585833daa646c5e0d` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/IStateMachineExceptionActivity.cs` | 11 | `454e6746c46b25f2bcc02b9ee1c2b868676544d2627fc225a8784a8170cffc51` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/IStateMachineFaultedActivitySelector.cs` | 40 | `d026d8a0eed3032c08c39fbbde1c242b1890495e7ac81a35481078a20747f6cd` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/IStateMachineModifier.cs` | 217 | `e9a3812a0c1bf447fbaf9382549772a8affef89746b2176b5e5c7cd9965d74fd` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/IStateMachineVisitor.cs` | 68 | `b382605bab97293c0d7bb61ff4793e193a98a03d9c050b0b2f564205f082493f` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/IStateObserver.cs` | 16 | `4f26ffec8b22a891c609eb300f8eb3be5f46b4cde842a0aebe8a07b196ce4f6b` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/IUnhandledEventContext.cs` | 23 | `0db35c05ad405fa50eceb623d117975191af3d0f8f3ec63a321b786e1c2dff33` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/IVisitable.cs` | 10 | `e8bdc63dfca957135079a25ade300b8aeadcd1410398d4f3e2abd1b3c2f2b709` |

## Test manifest, assertions and requirements

The three classes contain 27 test methods, 27 expanded cases and 27 unique requirement variants.
Every test contains causal assertions; the portfolio includes equality, identity, type, exception,
parameter-name, collection/order, negative-path, state/side-effect and structural assertions. No
assertion-free or trivial-only case remains. The mandatory code-testing workflow, static pairing
scan, pseudo-mutation gap analysis and assertion-quality audit drove the research-to-plan-to-test
sequence.

Test manifest SHA-256 is
`16323f18ae8055238c8a3c38d2f30b64a947ba2e0843365b072a4d802a712523`; chained from iteration
193 it yields
`5cb1a8d16b4d019b853827e6a9608d71e0e3ab33a6b2c4bfe783fa34cd1c06f5`.

| Test file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `tests/ViciOne.ServiceBus.Tests/Sagas/SagaPipelineAndStateMachineConfigurationDeepContractTests.cs` | 999 | `9cb11a32a5828787d4c85781cc3d7e6320ac327e14c8247d3b50c9aa4ad49b3d` |
| `tests/ViciOne.ServiceBus.Tests/SagaStateMachine/StateMachineBehaviorActivityPublicContractTests.cs` | 887 | `10e7aeecd561d6cb326d01d3afaf54c624eb3a9d73bc07e0dad367c8eedb701f` |
| `tests/ViciOne.ServiceBus.Tests/SagaStateMachine/StateMachineRuntimeSurfacePublicContractTests.cs` | 517 | `abef190ad3e4cbdafedf27dfd25bf45357b03ca33058c0a24c537ba4ab7a7c52` |

`CoreRequirements.json` SHA-256 is
`9e176876494b7df0c307209a89480b26bdce293fb88b578926db54891321fb78`.

## Mutation proof

Six compiled, isolated single-cause mutants were killed and restored:

1. the valid partition minimum changed from one to two;
2. the explicit null text-key diagnostic was removed;
3. the connector state-machine null guard was removed;
4. value-type correlations were filtered before validation;
5. completed partial connection handles were not disposed; and
6. observers were notified on every validation instead of exactly once.

The subsequent coverage-gap closure additionally pins the positive observer, retry/rescue,
registration and configurator-forwarding paths, including handle disconnect and option identity.

## Coverage, CRAP and gates

Final Cobertura is `/private/tmp/vicione-servicebus-iteration-194-final3.cobertura.xml`, SHA-256
`7ad23bae836bf65fa03fa2a4e80eb7657a4776df406c7e344cccb63850c49266`.
The four executable owners reach 198/198 lines (100.000%) and 32/32 branches (100.000%): pipeline
extensions 126/126 and 18/18, connector 36/36 and 14/14, configurator 26/26, specification 10/10.
The remaining 19 interfaces are declaration-only. Maximum method CRAP is 6 with full line coverage.
No coverage gap needs a disposition.

Sorted display-name SHA-256 is
`ccd46c76e93617545eca23032257ee3f0076d69993717106afdda9d449e3ab69` across 5,335 displays.
The strict Core CTRF artifact is
`/private/tmp/vicione-servicebus-iteration-194-final3-results/iteration194-final3.ctrf.json`, SHA-256
`8705f7856bab3f18b82dca81043b73469daa70f44ab87ebcac5ddfc3f4bca8dd`.

| Gate | Result |
| --- | --- |
| Three final owned classes | 13/13, 6/6 and 8/8 passed |
| Sagas / SagaStateMachine namespace regressions | 114/114 and 232/232 passed |
| Full Core Release | 5,335/5,335 passed, 0 failed, 0 skipped |
| Full EF unit Release | 249/249 passed, 0 failed, 0 skipped |
| Core-test / EF-unit / EF-local Release builds | 0 warnings, 0 errors |
| Core/EF/EF-local requirement projections | 1/1, 1/1 and 1/1 passed |
| Scoped format, JSON and diff checks | Exit 0; no formatting or whitespace errors |
| Mutation probes | 6/6 compiled isolated mutants killed |

The final proof ran in an isolated worktree so already-started, path-disjoint iteration-195 work
could continue without entering iteration 194's build, tests, requirements or hashes. No unresolved
correctness, lifecycle, cancellation, compatibility, coverage or architecture finding remains in
this admitted packet.

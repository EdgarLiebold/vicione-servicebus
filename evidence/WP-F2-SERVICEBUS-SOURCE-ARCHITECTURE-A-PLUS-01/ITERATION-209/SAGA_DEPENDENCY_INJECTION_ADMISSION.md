# Iteration 209 — saga dependency-injection admission

Date: 2026-09-18

Branch: `feature/servicebus-a-plus-api`

Scope: 8 previously unadmitted `ViciOne.ServiceBus.Sagas` sources

## Outcome

The dependency-injection saga repositories, scope/context factory, registration owners and
registration configurators are admitted. The implementation now rejects missing owners at their
own boundaries, publishes saga definitions once and only after complete endpoint selection,
prevents repository-only endpoint callbacks from running, and gives null scoped tasks stable
diagnostics.

Scope ownership is explicit across borrowed, created, async-disposable and synchronous-disposable
paths. Restore and scope release are both attempted. A processing failure is rethrown unchanged
when cleanup succeeds; one cleanup failure retains exact identity; simultaneous operation,
restore and release failures are preserved in causal order in an `AggregateException`. The live
service-collection facade and the complete registration configuration order are behaviorally
proved. A second API-to-test and assertion-quality audit found and closed the initial concurrency,
lifecycle, probe, default-definition, configure-order and failure-composition gaps.

## Source manifest

Each record is `path<TAB>content-sha256<LF>`, sorted ordinally. Manifest SHA-256 is
`25191c894fea4aa99796862a1d5a830a17340dd7c770a5a1cf73ce57805b6e95`. Chaining that hash from
iteration 208 yields
`6b90e5044cd95149decb9e34d779bed4b1a14d4190bcfd0c0e25cebe911cc11e`.

| Source file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `src/ViciOne.ServiceBus.Sagas/DependencyInjection/DependencyInjectionLoadSagaRepository.cs` | 72 | `b4fb2fe1a5b7e32f6f55c705e2279a9ac3fcc192d3d95495176feb749e8b4463` |
| `src/ViciOne.ServiceBus.Sagas/DependencyInjection/DependencyInjectionQuerySagaRepository.cs` | 71 | `1ef41cab4a5554ec960581bdb9e110967160ac6e016503ed1dfb05cf9476860a` |
| `src/ViciOne.ServiceBus.Sagas/DependencyInjection/DependencyInjectionSagaRepository.cs` | 85 | `e0dc9ee83ea0d7fdc213b8d887766e9424147cd57d95cbf3226b63b893491a21` |
| `src/ViciOne.ServiceBus.Sagas/DependencyInjection/DependencyInjectionSagaRepositoryContextFactory.cs` | 234 | `2c8ea57384de49afdf3e206242de456d117cc4dc967657ef1cd6ba4ffad7a94e` |
| `src/ViciOne.ServiceBus.Sagas/DependencyInjection/Registration/Sagas/SagaRegistration.cs` | 105 | `e8e76678219e34786fdce69c67df251baf17b7d2b86be4f0cb7352331d68e30e` |
| `src/ViciOne.ServiceBus.Sagas/DependencyInjection/Registration/Sagas/SagaRegistrationConfigurator.cs` | 69 | `24f2e5bcee69e37e1cc103c33a9220aecb300097db450c664952c50bcccbdf9f` |
| `src/ViciOne.ServiceBus.Sagas/DependencyInjection/Registration/Sagas/SagaRepositoryRegistrationConfigurator.cs` | 108 | `f48537079ee1a652d00a0433134cf36a30f58122a11a2fcac6b663b31615c503` |
| `src/ViciOne.ServiceBus.Sagas/DependencyInjection/Registration/Sagas/SagaStateMachineRegistration.cs` | 124 | `a5e741ff0b356988f1cf5e68b984aba18caf981efe8317af6e07afe4706f2f94` |

The exact source packet is 868 lines. Lead-read progress is 725 of 4,118 C# sources, or 17.606%.

## Test manifest, API mapping and requirements

The mandatory code-testing workflow, static source/test pairing, pseudo-mutation gap analysis,
assertion-quality audit and independent parallel counter-audits drove the research-to-plan-to-test
sequence. Three classes contain 35 test methods, 46 executed cases and 35 unique requirement
variants. Every test maps to one of the eight owners and every compile-visible or behaviorally
material member is mapped back to direct evidence, including concrete and interface registration
surfaces.

Test manifest SHA-256 is
`f47be2c631f1c9715272cd8a626f8472ca5b5f1d8a6ce059b5c53a9216c9459c`; chained from iteration
208 it yields
`f600dac00f3672d969e2eaa675a7951bca7d552b62ef563ef141f767021b5795`.

| Test file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `tests/ViciOne.ServiceBus.Tests/DependencyInjection/DependencyInjectionSagaRepositoryRuntimeDeepContractTests.cs` | 1,188 | `d919160b98453a8790844455d5c389e0054dc0962d87d317c3ed8ad9a2c74015` |
| `tests/ViciOne.ServiceBus.Tests/DependencyInjection/SagaRegistrationConfiguratorDeepContractTests.cs` | 335 | `5d149ef90f4e5201c90cd43079536d1107ab41e9384733129942d3ff296f250b` |
| `tests/ViciOne.ServiceBus.Tests/DependencyInjection/SagaRegistrationRuntimeDeepContractTests.cs` | 708 | `fa0477fd5cab0b701fc7cd1facb07f742c512ca414b657ff97f472ba89a0480c` |

The exact test packet is 2,231 lines. `CoreRequirements.json` SHA-256 is
`c79bd68b8f1ecd37049af8cc820a4e3a4ca7f6d99aec9fa868d63c13b3bbacde`.

## Mutation proof

Eight compiled, isolated, material single-cause mutants were killed and restored:

1. remove the public load-provider boundary;
2. replace the query null-task diagnostic with the incidental null dereference;
3. release a created context only through restore and omit scope disposal;
4. publish the saga definition before endpoint selection completes;
5. publish the state-machine definition before endpoint selection completes;
6. invoke a repository-only endpoint callback before rejecting the operation;
7. discard the primary operation failure when cleanup also fails; and
8. remove the state-machine registration's direct configurator boundary.

Every mutant compiled and failed its owning test at the intended invariant. The final
no-incremental build restored every product and test-host artifact.

## Coverage, CRAP and gates

Final Cobertura is
`/private/tmp/vicione-servicebus-iteration-209-admission.orTR0m/iteration209-final.cobertura.xml`,
SHA-256 `4ecc096ef04ccaa04c791d60bb97f1fce67c0e2550710f0677dacd1e61a2e04e`.
Exact source-basename selection includes compiler-generated classes belonging to the same source.
Unique executable lines use maximum coverage across duplicate entries.

| Owner source group | Lines | Branches | Maximum method CRAP |
| --- | ---: | ---: | ---: |
| Load repository | 27/27 | 0/0 | 2 |
| Query repository | 27/27 | 0/0 | 2 |
| Repository forwarding | 23/23 | 0/0 | 2 |
| Scope/context factory and cleanup helper | 105/106 | 0/0 | 14 |
| Saga registration owner | 44/44 | 0/0 | 8 |
| Registration configurator | 21/21 | 0/0 | 6 |
| Repository configurator collection | 22/22 | 0/0 | 2 |
| State-machine registration owner | 52/52 | 0/0 | 10 |
| **Total executable** | **321/322** | **0/0** | **14** |

The one uncovered compiler-emitted line is the add-factory lambda required by
`AddOrUpdatePayload` after `TryGetPayload` has already proved that the same scope exposes a
`MessageSchedulerContext`. `ConsumeContextScope.AddOrUpdatePayload` therefore takes its inherited
payload update path; that path, scheduler-factory identity and rebound scheduler context are
covered. Executing the fallback would require a deliberately self-contradictory payload context
whose answer changes during the same synchronous operation. It is retained only because the
two-factory API requires the argument. All 321 reachable owner lines are covered, the coverage
format exposes no representable owner branch conditions, and no method exceeds the CRAP threshold
of 30.

Sorted display-name SHA-256 is
`1cb6980af9dbe0a879e24cf6a7530e533ca902d1a8cfe67b285bc3bd48e4c66f` across 5,640 unique
displays. The strict Core CTRF artifact is
`/private/tmp/vicione-servicebus-iteration-209-admission.orTR0m/iteration209-final.ctrf.json`,
SHA-256 `ab7b34abb022a1fcd4b0a880d371d090753c941465d8cb1e4566d319cfcef4f5`.

| Gate | Result |
| --- | --- |
| Three final owned classes | 46/46 passed |
| Saga-wide regression (`*Saga*`) | 877/877 passed |
| Full Core Release | 5,640/5,640 passed, 0 failed, 0 skipped |
| Full EF unit Release | 249/249 passed, 0 failed, 0 skipped |
| Core-test / EF-unit / EF-local Release builds | 0 warnings, 0 errors |
| Core/EF/EF-local requirement projections | 1/1, 1/1 and 1/1 passed |
| Scoped format, JSON and diff checks | Exit 0; no formatting or whitespace errors |
| Mutation probes | 8/8 compiled isolated material mutants killed |

All genuine asynchronous APIs in the packet retain the `Async` suffix; synchronous constructors,
properties, `Probe` and collection members do not. No unresolved repository lifetime,
forwarding, concurrency, cleanup, registration-order, public-contract, null-boundary,
compatibility, requirement-projection or material coverage-risk finding remains in this admitted
packet.

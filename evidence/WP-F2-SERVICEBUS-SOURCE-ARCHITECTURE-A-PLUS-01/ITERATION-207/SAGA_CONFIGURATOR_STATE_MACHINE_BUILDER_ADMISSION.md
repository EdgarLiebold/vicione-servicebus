# Iteration 207 — saga configurator and state-machine builder admission

Date: 2026-09-18

Branch: `feature/servicebus-a-plus-api`

Scope: 9 previously unadmitted `ViciOne.ServiceBus.Sagas/Configuration` sources

## Outcome

The remaining saga configurator/specification, state-machine builder/modifier, request/schedule and
timeout-observer sources in this configuration packet are admitted. Two owner corrections were
made:

- state-machine activity publication is now idempotent after a successful commit, stays retryable
  after a failed commit, rejects mutation after publication, validates every required activity
  input before machine effects and rejects a callback that returns no activity; and
- the timeout saga observer now rejects a missing owning configurator or required callback at its
  constructor boundary.

The saga configurator, rescue/filter specifications, saga specification, state-machine modifier,
request settings and schedule settings already satisfied their intended runtime contracts and were
preserved unchanged.

## Source manifest

Each record is `path<TAB>content-sha256<LF>`, sorted ordinally. Manifest SHA-256 is
`f3b6a9432ec4e4d0611620bd05e860a3db83ccd8f4a9ae0bce40ac772940676a`. Chaining that hash from
iteration 206 yields
`9749e2b8ee6a527a55cfc0f730aa0b0fceea66a79f638f8502e852abc53d0334`.

| Source file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `src/ViciOne.ServiceBus.Sagas/Configuration/SagaConfigurator.cs` | 123 | `4bb737acc62d46207b6f192c39b4fc8e8a63bbb9b36f9b7b9e2682661dfb4637` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/SagaConsumeContextRescuePipeSpecification.cs` | 38 | `a18c404463a12e2c017122a6aee3a02fe0d1bca80f55f57448ddbdb21210f6aa` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/SagaFilterSpecification.cs` | 37 | `515542e5390c626651199f1fa16a8cdf53ef2b8ee4d78e65b9bf12823be35f60` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/SagaSpecification.cs` | 123 | `723cc2ba6290163b1bb4d87d70df43601cecadc59c598e313d3cd1e58e8bd217` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/StateMachineEventActivitiesBuilder.cs` | 309 | `30fa691f6b92fa27dfe54faa30adbc9bae2fb3de8a6d120365adf79bafc4d151` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/StateMachineModifier.cs` | 250 | `a1109e2ccaad1f5f1e543a47019732e169b8a0572dd4b731f014eafc9cc2d347` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/StateMachineRequestConfigurator.cs` | 99 | `f142170db44fb2b4fbe7037eae4b2dde34bdf4ab0b0b10f25d1660c72741db1d` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/StateMachineScheduleConfigurator.cs` | 39 | `dc64bafe80ce874a038323f8f1928e63d7cf7a38a04315073ec3e6097551f22c` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/Timeout/TimeoutSagaConfigurationObserver.cs` | 35 | `85c8ab918146e50400197ec9fdcf0c5d7d6f2e4c16fb3f5809db8f222422d264` |

Lead-read progress is 707 of 4,118 C# sources, or 17.169%.

## Test manifest, assertions and requirements

The mandatory code-testing workflow, static source/test pairing, pseudo-mutation gap analysis and
assertion-quality audit drove the research-to-plan-to-test sequence. Three classes contain 18 test
methods and 18 unique requirement variants.

Test manifest SHA-256 is
`1185e56d8aa31d20d80838fce875b9349aff29fea5c85a8a6c732ac8aa737743`; chained from iteration
206 it yields
`52faea2fb02b5fb83df2ee92f82f83e852a7c6c7e5e68b0115501c135a187e72`.

| Test file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `tests/ViciOne.ServiceBus.Tests/Configuration/SagaConfiguratorPipeDeepContractTests.cs` | 427 | `e0ef8907e39cb66518dc62351aa474c4cc14ac382d1fce6a6f3e9be407972b1a` |
| `tests/ViciOne.ServiceBus.Tests/Configuration/StateMachineBuilderModifierDeepContractTests.cs` | 423 | `dbdd0dad25d3a8f23e12d53ca85d1ec0a4d45af95b2637a076c83218c3e22e17` |
| `tests/ViciOne.ServiceBus.Tests/Configuration/StateMachineRequestScheduleTimeoutDeepContractTests.cs` | 254 | `c86fa8a2100b2b519a49507d470d38f6e9d57129bf48c62f864e7ed872c134a6` |

`CoreRequirements.json` SHA-256 is
`d931d0f60404a64a28bef86bafbbe8d4ec08f460f2fc873ec219a9c963d5e84a`.

## Mutation proof

Six compiled, isolated, material single-cause mutants were killed and restored:

1. repeated activity commit republished the same snapshot;
2. a committed activity builder accepted another activity;
3. an activity callback was allowed to return no activity;
4. the timeout observer accepted a missing owning configurator;
5. the timeout observer accepted a missing configuration callback; and
6. the activity builder accepted a missing configuration callback and failed later with the wrong
   exception after machine interaction.

Every mutant failed its owning test at the intended invariant. The final no-incremental build
restored every product and test-host artifact.

## Coverage, CRAP and gates

Final Cobertura is
`/private/tmp/vicione-servicebus-iteration-207-results.eVg1HK/iteration207-final.cobertura.xml`,
SHA-256 `9515e81d1aa09accdaa3c0c8d410cd76329aeb738a2553188d85610028e46ffa`.
Exact source-basename selection includes compiler-generated classes belonging to the same source.
Unique executable lines and branch conditions use maximum coverage across duplicate entries.

| Owner source group | Lines | Branches | Maximum method CRAP |
| --- | ---: | ---: | ---: |
| Saga configurator and specification | 57/57 | 18/18 | 4 |
| Rescue and split-filter specifications | 17/17 | 6/6 | 2 |
| State-machine activity builder and modifier | 174/174 | 18/18 | 6 |
| Request, schedule and timeout configuration | 39/39 | 4/4 | 4 |
| **Total executable** | **287/287** | **46/46** | **6** |

Every executable owner line and representable owner branch is covered, and no method exceeds the
CRAP threshold of 30.

Sorted display-name SHA-256 is
`51c3a935499e85d5e670fe3fa8d05847ebbe00a861e6ce54aa4f6ae0a615994b` across 5,583 unique
displays. The strict Core CTRF artifact is
`/private/tmp/vicione-servicebus-iteration-207-results.eVg1HK/iteration207-final.ctrf.json`,
SHA-256 `30bc634d297c4228c2363562fab194878bdc7a7b7b646ea7329eb6e8baa23c35`.

| Gate | Result |
| --- | --- |
| Three final owned classes | 18/18 passed |
| Saga-wide regression (`*Saga*`) | 826/826 passed |
| Full Core Release | 5,583/5,583 passed, 0 failed, 0 skipped |
| Full EF unit Release | 249/249 passed, 0 failed, 0 skipped |
| Core-test / EF-unit / EF-local Release builds | 0 warnings, 0 errors |
| Core/EF/EF-local requirement projections | 1/1, 1/1 and 1/1 passed |
| Scoped format, JSON and diff checks | Exit 0; no formatting or whitespace errors |
| Mutation probes | 6/6 compiled isolated material mutants killed |

No unresolved configurator, specification, builder-commit, forwarding, request/schedule, timeout,
nullable-contract, compatibility, coverage-risk or architecture finding remains in this admitted
packet.

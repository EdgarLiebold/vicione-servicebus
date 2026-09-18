# Iteration 206 — saga observer, redelivery and partition admission

Date: 2026-09-18

Branch: `feature/servicebus-a-plus-api`

Scope: 12 previously unadmitted `ViciOne.ServiceBus.Sagas/Configuration` sources

## Outcome

Saga-message observer adapters, missing-instance redelivery, public definition/correlation
contracts and the partition specification are admitted. Four owner-boundary corrections were made:

- the in-memory-outbox registration overload now distinguishes a null registration context from a
  context that lacks scoped-consume support, and both overloads reject a missing saga configurator;
- delayed and scheduled redelivery observers reject missing configurators and required callbacks at
  construction, allowing their execution paths to invoke the proven callback directly; and
- request-state-machine redelivery rejects a missing callback or missing configurator before
  pipeline construction and invokes its stored non-null callback directly.

Concurrency limiting, message retry, internal missing-redelivery, public interfaces and partition
behavior already satisfied their intended contracts and were preserved unchanged.

## Source manifest

Each record is `path<TAB>content-sha256<LF>`, sorted ordinally. Manifest SHA-256 is
`335a1be71dded60014592b809fba938bc4ed6c1dfd6afe6ef104404f93592c45`. Chaining that hash from
iteration 205 yields
`63a4f1619451022a5c9f8333bccb0400fd25d26f9ba6cffe51970728a16f75c1`.

| Source file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `src/ViciOne.ServiceBus.Sagas/Configuration/ConcurrencyLimit/ConcurrencyLimitSagaConfigurationObserver.cs` | 46 | `5fb014d4b1d141c1109b1a039b713d4f16af09a5167781a34621b73e372ef7b4` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/IEventCorrelationBuilder.cs` | 9 | `86fa9f5802be9e03832628b6de7980e5f21558587b654368d013a9bc810f6852` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/IRequestStateMachineMissingInstanceConfigurator.cs` | 14 | `8182f1d07d9aa2ab6dccbb7cd92de89cdffb2fa9ee97209fab1d86443b0d47ad` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/ISagaDefinition.cs` | 36 | `de28b1f1c7b69e9a97ef20d4fcf94e8393084019c2f52f67f7767a7c56ae40b8` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/IStateMachineInterfaceType.cs` | 11 | `6bce2dfb7e418b86b00de1e58f8e2ad919b619eff1ad4ddbad149f2cf6c116fe` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/InMemoryOutbox/InMemoryOutboxSagaConfigurationObserver.cs` | 64 | `d6888f83c4afe5a0183b9b145da78c6e2d5655316d1faa6aad942b8f585e153e` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/MissingInstanceRedeliveryConfigurator.cs` | 77 | `a9c08210baf0f0a525aaa5dd73fc69c651e4019514b788268a1d8d2a84bc83f5` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/Partition/PartitionSagaSpecification.cs` | 40 | `532255b33231e264ed5ae0e7e15840b234dccba0d23bf996dc79dcc867043ad6` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/RedeliverRequestStateMachineSpecification.cs` | 37 | `ea2dab4b87e5e3da54f29513234e7c61a51a1a0ca9d5b8e645f1deb3ba5ffe1a` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/Redelivery/DelayedRedeliverySagaConfigurationObserver.cs` | 52 | `ab2f753af85bbdb28ef71d2e11584d272167ba73c59ce76445b3f231670bbe49` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/Redelivery/ScheduledRedeliverySagaConfigurationObserver.cs` | 52 | `675ef3261d861dc7acc29af9ece75b440114f584583df5f9a4ec7642ea84134a` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/Retry/MessageRetrySagaConfigurationObserver.cs` | 59 | `41b7f784bc237ad79e88c1873513957b1c152fc10110e05ec9aeba1b4a95ddb0` |

Lead-read progress is 698 of 4,118 C# sources, or 16.950%.

## Test manifest, assertions and requirements

The mandatory code-testing workflow, static source/test pairing, pseudo-mutation gap analysis and
assertion-quality audit drove the research-to-plan-to-test sequence. Three classes contain 17 test
methods and 17 unique requirement variants.

Test manifest SHA-256 is
`9abc82a28c985c6caede0dd05e5d3299963e30a767793a6991c02c29644405ab`; chained from iteration
205 it yields
`b3d928ca0e0e725aec2fe44748531938277bd9cc9c523afb852a3d4251397157`.

| Test file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `tests/ViciOne.ServiceBus.Tests/Configuration/SagaPublicPartitionContractTests.cs` | 444 | `4bdd69a4e555efb7e277108cbc2bfb34f293978fd9926900dd95ce15a74365c1` |
| `tests/ViciOne.ServiceBus.Tests/Configuration/SagaMessageObserverAdapterDeepContractTests.cs` | 277 | `89dd1c61ba1d606c61f412cf4ad94b49a0cea65a97a882c387d4be91bd7ff1b5` |
| `tests/ViciOne.ServiceBus.Tests/Configuration/SagaMissingInstanceRedeliveryDeepContractTests.cs` | 202 | `a0d9aed8d57e96848970e762d4eea7284ff470147ba46c4b8d7e32ac2ecd5213` |

`CoreRequirements.json` SHA-256 is
`e815ed123154a80caef56441e68429339b354440e3c23b1a74f8cb65e8173f2e`.

## Mutation proof

Six compiled, isolated, material single-cause mutants were killed and restored:

1. the outbox observer accepted a missing saga configurator;
2. a null registration context was misclassified as unsupported instead of missing;
3. the delayed-redelivery observer accepted a missing saga configurator;
4. the delayed-redelivery observer accepted a missing callback;
5. the request-redelivery wrapper accepted a missing callback; and
6. the scheduled-redelivery observer accepted a missing saga configurator.

An additional removal of the request wrapper's apply guard was intentionally not counted: the
underlying public extension enforces the same precondition, so that probe was behaviorally
redundant. The six counted mutants failed their owning tests at the intended invariant, and the
final no-incremental build restored every product and test-host artifact.

## Coverage, CRAP and gates

Final Cobertura is
`/private/tmp/vicione-servicebus-iteration-206-results.mRZIBq/iteration206-final.cobertura.xml`,
SHA-256 `85e3f9c99c4c8d202adf888f32b9ffdd45ae456f0330f2a742804982f110cfe8`.
Exact source-basename selection includes compiler-generated classes belonging to the same source.
Unique executable lines and branch conditions use maximum coverage across duplicate entries.

| Owner source group | Lines | Branches | Maximum method CRAP |
| --- | ---: | ---: | ---: |
| Concurrency observer | 9/9 | 2/2 | 2 |
| In-memory-outbox observer | 20/20 | 8/8 | 4 |
| Missing-instance redelivery | 30/30 | 8/8 | 4 |
| Partition specification | 10/10 | 0/0 | 1 |
| Request redelivery wrapper | 11/11 | 2/2 | 2 |
| Delayed and scheduled observers | 30/30 | 8/8 | 4 |
| Message-retry observer | 13/13 | 6/6 | 4 |
| Four declarative interfaces | 0/0 | 0/0 | 0 |
| **Total executable** | **123/123** | **34/34** | **4** |

Every executable owner line and representable owner branch is covered, and no method exceeds the
CRAP threshold of 30.

Sorted display-name SHA-256 is
`167d2a38be717b4be48f86fb9d14deb957f1f078bd2b47444f0674fd6d2c7fea` across 5,565 unique
displays. The strict Core CTRF artifact is
`/private/tmp/vicione-servicebus-iteration-206-results.mRZIBq/iteration206-final.ctrf.json`,
SHA-256 `0a9b22dde3616fd6ade188034bc850b81c867295e8e97b39118d1124d0ea17e9`.

| Gate | Result |
| --- | --- |
| Three final owned classes | 17/17 passed |
| Saga-wide regression (`*Saga*`) | 820/820 passed |
| Full Core Release | 5,565/5,565 passed, 0 failed, 0 skipped |
| Full EF unit Release | 249/249 passed, 0 failed, 0 skipped |
| Core-test / EF-unit / EF-local Release builds | 0 warnings, 0 errors |
| Core/EF/EF-local requirement projections | 1/1, 1/1 and 1/1 passed |
| Scoped format, JSON and diff checks | Exit 0; no formatting or whitespace errors |
| Mutation probes | 6/6 compiled isolated material mutants killed |

No unresolved observer, redelivery, partition, public-contract, nullable-contract, compatibility,
coverage-risk or architecture finding remains in this admitted packet.

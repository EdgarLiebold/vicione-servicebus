# Iteration 205 — saga connector, retry and correlation admission

Date: 2026-09-18

Branch: `feature/servicebus-a-plus-api`

Scope: 12 previously unadmitted `ViciOne.ServiceBus.Sagas/Configuration` sources

## Outcome

The four role-specific connector factories, their public connector abstractions, retry-policy
configuration, direct event/fault correlation builders and missing-instance configuration are
admitted. The packet adds stable owner-boundary guards and diagnostics without changing valid
message-processing behavior:

- retry factories and observers reject null at the configurator boundary; retrieval reports a
  missing factory and a null factory result deterministically;
- direct correlation builders reject a missing machine or event before constructing the internal
  configurator;
- synchronous and asynchronous missing-instance callbacks reject null at the public boundary; and
- connector interface documentation now describes the generic argument as saga state rather than
  an unrelated value type.

The connector factory behaviors, cache identity, explicit-interface mismatch behavior, policy
composition, correlation paths, lifecycle outcomes and public nullable surface are frozen by 23
deep-contract tests and requirement variants.

## Source manifest

Each record is `path<TAB>content-sha256<LF>`, sorted ordinally. Manifest SHA-256 is
`21c33e32497959c37df6a0bd7a1423df33ea426194334b41f9f3e97f0e65b251`. Chaining that hash from
iteration 204 yields
`25d3ea702dbf17b302dc85cea1157ff7ecfac7a7aadc66f85cf6ee1e01a9e8c4`.

| Source file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `src/ViciOne.ServiceBus.Sagas/Configuration/BehaviorContextRetryConfigurator.cs` | 49 | `53e86dc10a9f7746dd8e64d721a97fb4961bf79542259ba5e54cd154691064de` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/CorrelatedByEventCorrelationBuilder.cs` | 35 | `6bf5736f2fc2cf7ddb8aba6f74bdffdc63b5f14b3f87bb9e2070ceb78b944f80` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/CorrelatedByFaultEventCorrelationBuilder.cs` | 35 | `adf12ad9257b6b0d48ad2145614aa6c96c0dbfbc31f5265046edac50add8703e` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/EventMissingInstanceConfigurator.cs` | 48 | `692ad6fb3e8897307d03f9fd90ccfefae92a88631bafa63536c5ac775c52e092` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/ISagaConnector.cs` | 20 | `30d3d43dc60807cbb0a05f6cd6429eadd6f01ce4901c050a104d17618b58f780` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/ISagaConnectorCache.cs` | 8 | `a18c2bd3bd3d68e91e67728e496acc32507896029c5b737a869e0d73ddef1a77` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/ISagaConnectorFactory.cs` | 11 | `72a04483149bb4d6b38247869e67df05005df3946a2ff2d3c98cf0792e87c59a` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/ISagaMessageConnector.cs` | 29 | `c9590925207a413c76126515890404870337b9fc39169e169cba28bbd8ada89e` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/InitiatedByOrOrchestratesSagaConnectorFactory.cs` | 37 | `ab7577a7eb9b2a416dc73bcfb1fcd11ae4772b7fa62d7bf1fff13e60da7bdbe2` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/InitiatedBySagaConnectorFactory.cs` | 36 | `4d66bf98b126d9209a8989e5dc31aae576238bf4a4d6eaa0217874e992bc2bcd` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/ObservesSagaConnectorFactory.cs` | 45 | `971076047034bb1dee2c7ed4aaefedff399e3d4de52a9a338d160d106f93633e` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/OrchestratesSagaConnectorFactory.cs` | 40 | `f411626199a545a4b8560443a6621f4e64a9c9c6a2300e70e1f9851defba31af` |

Lead-read progress is 686 of 4,118 C# sources, or 16.659%.

## Test manifest, assertions and requirements

The mandatory code-testing workflow, static source/test pairing, pseudo-mutation gap analysis and
assertion-quality audit drove the research-to-plan-to-test sequence. The three classes contain 23
test methods and 23 unique requirement variants.

Test manifest SHA-256 is
`17f96260fff65295ebb58a7dd7b58b84fea486a6c7e56e29628fa3f984e6eee8`; chained from iteration
204 it yields
`9c2583fb0e01b2f2d3fe10923852b33d8881398c159b1c2234e91ee72204ef82`.

| Test file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `tests/ViciOne.ServiceBus.Tests/Configuration/SagaConnectorFactoryDeepContractTests.cs` | 569 | `f5e34a399fe958f8ad45454d90de2b07856cbc1965d1dc5ffae780b90cf165e5` |
| `tests/ViciOne.ServiceBus.Tests/Configuration/SagaConnectorPublicContractTests.cs` | 292 | `7162a2c65c6675f01844216c0f0fb4b4d2b766c94d991be50ac9ba3882a4f5fe` |
| `tests/ViciOne.ServiceBus.Tests/Configuration/SagaRetryCorrelationMissingInstanceDeepContractTests.cs` | 506 | `135f2709b1dcd0b85ac2c5360ecbfb529997f0f0d5c314d8c64b8df77ed3b19a` |

`CoreRequirements.json` SHA-256 is
`2a40c231983b3ca87de7825dd7fe6889168e82c6585349b2a68affcb2079b479`.

## Mutation proof

Seven compiled, isolated, material single-cause mutants were killed and restored:

1. retry-observer null ownership was removed;
2. a null retry-policy result was accepted;
3. the direct event builder's machine guard was removed;
4. the direct event builder's event guard was removed;
5. the direct fault builder's machine guard was removed;
6. the synchronous missing-instance callback guard was removed; and
7. the asynchronous missing-instance callback guard was removed.

Each mutant failed its owning deep-contract method at the intended invariant. The final
no-incremental build restored every product and test-host artifact.

## Coverage, CRAP and gates

Final Cobertura is
`/private/tmp/vicione-servicebus-iteration-205-results.KLqtN2/iteration205-final.cobertura.xml`,
SHA-256 `0f98f6b545f1ffb7bad75aea352b4bb807aa5b5deaed2febd5c3e349022551e7`.
Exact source-basename selection includes compiler-generated classes belonging to the same source.
Unique executable lines and branch conditions use maximum coverage across duplicate entries.

| Owner source | Lines | Branches | Maximum method CRAP |
| --- | ---: | ---: | ---: |
| `BehaviorContextRetryConfigurator.cs` | 12/12 | 4/4 | 4 |
| Two direct correlation builders | 16/16 | 4/4 | 2 |
| `EventMissingInstanceConfigurator.cs` | 7/7 | 4/4 | 2 |
| Four connector factories | 44/44 | 17/18 | 4 |
| Four connector interfaces | 0/0 | 0/0 | 0 |
| **Total executable** | **79/79** | **29/30** | **4** |

The single residual branch is the compiler-emitted false arm of the orchestrates correlation
delegate after the constructor has already proven the message contract. All executable owner lines
are covered and no method exceeds the CRAP threshold of 30.

Sorted display-name SHA-256 is
`c71d063c1f66e4d446c80f7f006140796e18aaf87d7dc85332fff3ee2ec9c364` across 5,548 unique
displays. The strict Core CTRF artifact is
`/private/tmp/vicione-servicebus-iteration-205-results.KLqtN2/iteration205-final.ctrf.json`,
SHA-256 `a2d035d8fe0b631540076261a2b5db33e603d2d310ba8eed11a832b82c3819b8`.

| Gate | Result |
| --- | --- |
| Three final owned classes | 23/23 passed |
| Saga-wide regression (`*Saga*`) | 803/803 passed |
| Full Core Release | 5,548/5,548 passed, 0 failed, 0 skipped |
| Full EF unit Release | 249/249 passed, 0 failed, 0 skipped |
| Core-test / EF-unit / EF-local Release builds | 0 warnings, 0 errors |
| Core/EF/EF-local requirement projections | 1/1, 1/1 and 1/1 passed |
| Scoped format, JSON and diff checks | Exit 0; no formatting or whitespace errors |
| Mutation probes | 7/7 compiled isolated material mutants killed |

No unresolved connector, retry, correlation, missing-instance, nullable-contract, compatibility,
coverage-risk or architecture finding remains in this admitted packet.

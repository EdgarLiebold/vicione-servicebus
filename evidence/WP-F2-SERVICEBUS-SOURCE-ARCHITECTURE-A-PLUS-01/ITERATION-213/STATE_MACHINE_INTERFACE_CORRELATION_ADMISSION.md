# Iteration 213 — state-machine interface correlation admission

Date: 2026-09-18

Branch: `feature/servicebus-a-plus-api`

Scope: seven previously unadmitted `ViciOne.ServiceBus.Sagas` state-machine interface, connector,
correlation-builder, configurator and event-policy sources

## Outcome

The state-machine interface adapter now rejects missing owners at their exact construction boundary
and requires the exact closed saga generic type when selecting its cached connector. Direct and
fault correlation-id builders retain the original identifier, missing-identifier and exception
outcomes. Connector creation preserves the state machine, event, policy and filter-factory
identities; a null filter-factory result fails at that owner before any builder effect.

State-machine message composition applies the optional message filter before the required saga
filter, exactly once each, and selects the configured topology overload. Missing factories,
invalid arguments, null collaborator results and collaborator failures retain deterministic
precedence and identity.

The event-correlation configurator validates every required input before mutation, inherits only
compatible existing-correlation filters and returns fresh snapshots isolated from later changes.
All selector, property, expression, saga-factory and missing-instance overloads preserve their
exact delegates and collaborators. Optional factories, filters and missing pipelines remain
honestly nullable.

`MessageEventCorrelation` now guards all required owners in signature order. Its lazy policy is a
stable identity: initial events receive the configured `NewOrExistingSagaPolicy`; other events
receive the configured `AnyExistingSagaPolicy`. Writable validation does not traverse the state
machine, while pre-insertion and initial-event read-only conflicts remain independent and both are
reported when simultaneously present.

## Source manifest

Each record is `path<TAB>content-sha256<LF>`, sorted ordinally. Manifest SHA-256 is
`9f59da42055a75ccf1ae0c0603383c89c126e2316366e4e2689cdd7b95cf2f6c`. Chaining that hash from
iteration 212 yields
`8b06255a8707d65dea1b1e474d71103998ea54e25ca2bccdd7486543ef31bc9b`.

| Source file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `src/ViciOne.ServiceBus.Sagas/Configuration/StateMachineInterfaceType.MessageCorrelationIdEventCorrelationBuilder.cs` | 40 | `2466596c528d5a0c03ddd7a86c4b2fbc6c24dbfb834841458d7acaf220b288d2` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/StateMachineInterfaceType.MessageCorrelationIdFaultEventCorrelationBuilder.cs` | 40 | `5a3a3a3757fc7f394a92d52721c4902c479fd9082fcc35021f90c98724b37cee` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/StateMachineInterfaceType.StateMachineEventConnectorFactory.cs` | 55 | `7e15ef29790658922884dc03feedc9bbc037e0e632c3c5d0cfb5aac4bc8872e4` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/StateMachineInterfaceType.StateMachineSagaMessageConnector.cs` | 61 | `942e663337e08abe4a942d316c8c4691c2ca3e3263795a86cb198c7cc31c2c5c` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/StateMachineInterfaceType.ViciOneServiceBusEventCorrelationConfigurator.cs` | 216 | `46add3fac37d0f4fa53152a1a79121b0ce84e2a62b884c7041deacd6a4798bd9` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/StateMachineInterfaceType.cs` | 33 | `b33b3befb2a88b7e737116f9e22137022aae24f03beb0f8534be9dc61268d770` |
| `src/ViciOne.ServiceBus.Sagas/SagaStateMachine/MessageEventCorrelation.cs` | 99 | `e71d2c5582fdf24368a11a63e02c252112b95353a619d6c3ae3f2109992b9019` |

The exact final source packet is 544 lines. Lead-read progress is 758 of 4,118 C# sources, or
18.407%.

## Test manifest, API mapping and requirements

The mandatory code-testing workflow, static source/test pairing, pseudo-mutation gap analysis,
assertion-quality review and independent parallel counter-audits drove the research-to-plan-to-test
sequence. Three classes contain 25 test methods, 27 executed cases and 25 unique requirement
variants. Every compile-visible or behaviorally material member is mapped to direct evidence.

Test manifest SHA-256 is
`c5f4bbe9077e56d8dfc05075415c495409607daa4410986368b62fa63b3d4043`; chained from iteration
212 it yields
`c3da08da69da48d68d57e8b3f70e369817a0c14e4477ddfa70e6affe82205b18`.

| Test file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `tests/ViciOne.ServiceBus.Tests/Configuration/StateMachineEventCorrelationConfiguratorDeepContractTests.cs` | 637 | `688523475a9e1f3448a9adb2bec5af13b18ca2774a962225ef6679e087a88d94` |
| `tests/ViciOne.ServiceBus.Tests/Configuration/StateMachineInterfaceTypeFactoryDeepContractTests.cs` | 633 | `258ae29d5b97d9cd270248d56d00f95772b577aa5e79f65f2fa3f4e54cd37551` |
| `tests/ViciOne.ServiceBus.Tests/Configuration/StateMachineSagaMessageConnectorDeepContractTests.cs` | 543 | `ec22232fa7a9ced1e68ff79474e6d4a550f93470d177ef0b808bb3b452cca8ad` |

The exact test packet is 1,813 lines. `CoreRequirements.json` SHA-256 is
`51fa1fea43a77886381d68fbb4778cf8af6e09d102ec51008cb4c1c0a55a3467`.

## Independent final re-audit

The initial disjoint counter-audits found missing owner guards, incomplete factory-result and
failure-identity evidence, filter/topology branch gaps, dishonest optional-pipeline nullability,
unproved current-event ownership and incomplete correlation validation. The first remediations
closed those findings.

The final configurator re-audit then found missing direct correlation guards, unproved lazy policy
composition and stable identity, unnecessary state-machine traversal for writable validation and
an omitted simultaneous-conflict case. Those findings were corrected and independently rechecked.
Three ultimate read-only Sol-xhigh audits now report no findings across their disjoint factory,
connector and configurator/policy packets.

## Mutation proof

Sixteen compiled, isolated, material single-cause mutants were killed and restored:

1. change the direct correlation constructor nullability contract;
2. discard the correlation message filter in the connector factory;
3. wrap a fault correlation extractor exception;
4. reorder connector configuration null guards;
5. reverse message- and saga-filter order;
6. swap `InsertOnInitial` and `ReadOnly` during correlation build;
7. clear the message filter before validating a `SelectId` selector;
8. inherit the existing factory only when a message filter exists;
9. make the optional missing pipeline non-nullable through suppression;
10. weaken the two read-only validation conditions from conjunction to disjunction;
11. remove the direct saga-factory constructor guard;
12. discard the configured initial-policy insertion flag;
13. discard the configured existing-policy missing pipeline;
14. recreate the selected policy on every property read;
15. inspect initial-state membership before checking whether validation is read-only; and
16. turn the two independent read-only failures into an `if`/`else if` choice.

Every mutant compiled and failed its intended focused invariant. Every source was restored before
the final strict builds and instrumented run.

## Coverage and CRAP

Final Cobertura is
`/private/tmp/vicione-servicebus-iteration-213-admission.sbQ4BQ/final/iteration213-final.cobertura.xml`,
SHA-256 `318f3fb9b23f7c0d06712b8ca98ccbdc5066e379303cc3e6c3262afbbae668ef`.
Exact source-basename selection includes compiler-generated classes belonging to the same source;
unique lines and branches use maximum coverage across duplicate entries.

| Owner source | Lines | Branches | Maximum method CRAP |
| --- | ---: | ---: | ---: |
| Message correlation-id builder | 12/12 | 2/2 | 2.0 |
| Fault correlation-id builder | 12/12 | 2/2 | 2.0 |
| Event connector factory | 25/25 | 8/8 | 4.0 |
| Saga message connector | 19/19 | 8/8 | 6.0 |
| Event-correlation configurator | 72/72 | 8/8 | 4.0 |
| State-machine interface adapter | 8/8 | 2/2 | 2.0 |
| Message event correlation policy | 33/33 | 10/10 | 8.0 |
| **Total executable** | **181/181** | **40/40** | **8.0** |

There is no owner coverage residual. Sorted display-name SHA-256 is
`e65a04e15bc5e9537c81372201f008eb6d623c00c5437b92703e5ef32a6a0efc` across 5,873 unique
displays. The final Core CTRF artifact is
`/private/tmp/vicione-servicebus-iteration-213-admission.sbQ4BQ/final/iteration213-final.ctrf.json`,
SHA-256 `368c1a85e9feb38ba058a0ecf529c00edfb9468b3e49cdac1a8f06f1dcbc28e4`.

## Gates

| Gate | Result |
| --- | --- |
| Three final owned classes | 27/27 passed |
| Saga-wide regression (`*Saga*`) | 1,092/1,092 passed |
| Full Core Release | 5,873/5,873 passed, 0 failed, 0 skipped |
| Full EF unit Release | 249/249 passed, 0 failed, 0 skipped |
| Core / EF-unit / EF-local Release builds | 0 warnings, 0 errors |
| Core / EF-unit / EF-local requirement projections | 1/1, 1/1 and 1/1 passed |
| Scoped format, JSON and diff checks | Exit 0; no formatting or whitespace errors |
| Mutation probes | 16/16 compiled isolated material mutants killed |

All genuine asynchronous APIs in the packet retain the `Async` suffix; synchronous construction,
properties, validation, pipeline build and connection APIs do not. No unresolved state-machine
typing, correlation, composition ordering, topology, lifecycle, ownership, nullability,
public-contract, formatting, coverage or material test-risk finding remains in this admitted
packet.

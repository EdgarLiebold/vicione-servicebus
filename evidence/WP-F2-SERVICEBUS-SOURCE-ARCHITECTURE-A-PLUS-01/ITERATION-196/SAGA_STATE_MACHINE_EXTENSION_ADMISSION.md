# Iteration 196 — saga state-machine extension admission

## Outcome

Container/request activities, state query/runtime access and behavior/transition composition are
admitted. All required receivers, callbacks, factories, activities, expressions and state inputs
now fail at deterministic public boundaries. Query/filter composition, state access, cancellation,
next-event introspection, redelivery configuration and normal/faulted activity wrapping preserve
their exact ownership and identity semantics.

The lead personally read all 10 current sources before delegation. They are newly unique, moving
cumulative exact unique source coverage to 634/4,118 files (15.396%). The packet contained 726
physical lines before the change and 844 lines after the admitted guards, validation and async
boundary split.

## Corrections and direct contracts

- All four container-activity overloads validate the binder and configuration callback before
  selector construction or callback execution.
- Request lifecycle helpers validate the binder plus factory/request-event inputs before adding an
  activity; they preserve exact activity type, factory identity and returned binder identity.
- Query/filter helpers validate machine, predicate, state array and null elements before accessor
  work, then compose the same caller and state predicates.
- Both state-access helpers preserve the original task, context and cancellation token.
- Explicit transition validates context/state before cancellation, returns a task canceled with the
  original token, captures one owning machine and canonicalizes the target by name.
- Next-event introspection has a synchronous public null boundary, captures one owning machine,
  forwards state/context/token and retains a stable null-state diagnostic.
- All 15 Then/Execute overloads validate binder and callback/activity inputs consistently while
  preserving delegate, factory, activity and SlimActivity adaptation identity.
- All eight TransitionTo/Finalize overloads validate owners and targets, resolve the owning
  machine's target/accessor and select the exact normal or faulted wrapper.
- Missing-instance redelivery preserves configure/validate/build order, direct configure failures
  and configuration-owned wrapping of build failures.
- Both service-address exception delegates retain exact contravariance, constraints, context shape
  and invocation identity.

## Source manifest

Each record is `path<TAB>content-sha256<LF>`, sorted ordinally. Manifest SHA-256 is
`ded5279aa5a5c9b3deccf2b183734b70e225789932883db53c191fb2d25f87bd`. Chaining that hash from
iteration 195 yields
`bca4c33d86b8231dd53ad0715759c393ed05b49d98ef2f8ec81db23091ea2600`.

| Source file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `src/ViciOne.ServiceBus.Sagas/Sagas/ContainerActivityExtensions.cs` | 85 | `d2d777d4768eceb77ebd830304638da1076ef99ece65c6c8f573bd6ab9e60ce6` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/MissingInstanceRedeliveryExtensions.cs` | 44 | `8ba131fc66f17db8ff6ebbbaff6b68fb4bc6f1a2e9686623bfc5294d50cad3bb` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/RequestEventExtensions.cs` | 83 | `09b1a2199c904a4fa0aad703b7e861ecc0591114db50f533c4b21c17bd238466` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/SagaStateMachineExtensions.cs` | 56 | `356b7adc8faa73330a9664f92d8f8b138933eba7598c08affcf60ff3115ded1f` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/ServiceAddressExceptionProvider.cs` | 24 | `b0032b060ae16048463977106b1e24e1f12b2c2673866164fb4618d3dc207d2f` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/StateAccessorExtensions.cs` | 39 | `e36e5651ee3ed83e934ea94035015e8fe08dbf62ec10c0e4f2d3da0f7db9e755` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/StateMachineExtensions.cs` | 36 | `56ffd7ecdb18acf1ab5482438bedfc41c3083c0459f0e67c4ea7ace05fb2cd1e` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/StateMachineIntrospectionExtensions.cs` | 31 | `6243933190551b0002d12406fe76e777b4fb58cdf906fc705eb2001b569b4b48` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/ThenExtensions.cs` | 275 | `4fb382b0d9f7edfbc6924d44feff80ad3f93e733f52da1fb2f7a79a41e2c3dfe` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/TransitionExtensions.cs` | 171 | `9451a7e91050e143e4f61a798db606833a2d5cc2535fec4e2a58bc33f1ccca46` |

## Test manifest, assertions and requirements

The three classes contain 18 test methods, 18 expanded cases and 18 unique requirement variants.
Every test contains causal assertions; the portfolio includes equality, identity, exact type,
exception, parameter-name, task/token, collection/order, non-invocation, state and structural
assertions. No assertion-free, trivial-only or self-referential case remains. The mandatory
code-testing workflow, static source/test pairing, pseudo-mutation gap analysis and
assertion-quality audit drove the research-to-plan-to-test sequence.

Test manifest SHA-256 is
`546d973fc7110548409a100201af7b304d5fe4344d72ce7738dcd2ee14938761`; chained from iteration
195 it yields
`51559bc2a7df0d0aa5fb98af90a0aaa4146e887eb09711dbfd0832fce6884d72`.

| Test file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `tests/ViciOne.ServiceBus.Tests/Sagas/SagaActivityRequestExtensionDeepContractTests.cs` | 328 | `b043de16bf339d5e326adbcc2933b9a71b744715e0d1f997093a88e9c17600f4` |
| `tests/ViciOne.ServiceBus.Tests/Sagas/SagaStateQueryRuntimeExtensionDeepContractTests.cs` | 427 | `7f7eab437793460ca512637ecd8e5bc90721fbfce0c882197773f8093443b56a` |
| `tests/ViciOne.ServiceBus.Tests/Sagas/SagaBehaviorTransitionExtensionDeepContractTests.cs` | 472 | `036ff12172b686a3cf74c44d4ad02075ac4c87dc96f9d6a360c2571e76f9ffc1` |

`CoreRequirements.json` SHA-256 is
`b08b844e2fdf145f53a96fb469256767243ca1d4d4b027f8aa3ce121bacfb745`.

## Mutation proof

Six compiled, isolated material single-cause mutants were killed and restored:

1. a container-activity binder guard was removed;
2. the request-event guard was removed;
3. query state-array validation was bypassed;
4. explicit transition state validation was removed;
5. next-event introspection reacquired the context's machine after awaiting; and
6. a faulted transition bypassed `ExecuteOnFaultedActivity`.

One additional removal of the extension-level request message-factory guard retained the same exact
observable exception because `RequestCompletedActivity` independently validates that factory at
construction, before any binder effect. It is an equivalent mutation, explicitly excluded from the
kill denominator rather than misreported. The final no-incremental build restored every product and
test-host artifact.

## Coverage, CRAP and gates

Final Cobertura is `/private/tmp/vicione-servicebus-iteration-196-final.cobertura.xml`, SHA-256
`11b779aa6808b58f97a2a58f3c0964f8a789fe07ab6bbdc27f746de5a0b331f8`.
The nine executable owners reach 173/173 lines (100.000%) and 6/6 branches (100.000%): container
activity 16/16, missing redelivery 9/9, request event 10/10, saga query/filter 14/14 and 4/4,
state access 6/6, explicit transition 10/10 and 2/2, introspection 7/7, Then/Execute 61/61, and
transition/finalization 40/40. `ServiceAddressExceptionProvider.cs` is declaration-only. Maximum
method CRAP is 4 for `ValidateStates`, with complete line and branch coverage. No coverage gap needs
a disposition.

Sorted display-name SHA-256 is
`6afd2d6117bc50b089947260c9a2ef66259341e7e2f74956302bcdf838c9060c` across 5,371 displays.
The strict Core CTRF artifact is
`/private/tmp/vicione-servicebus-iteration-196-results/iteration196-final.ctrf.json`, SHA-256
`0d60256b35bea7b4a9d039e5997e67f585ef2d9044b2922c02632c911f9f39ee`.

| Gate | Result |
| --- | --- |
| Three final owned classes | 6/6, 7/7 and 5/5 passed |
| Sagas / SagaStateMachine namespace regressions | 144/144 and 232/232 passed |
| Full Core Release | 5,371/5,371 passed, 0 failed, 0 skipped |
| Full EF unit Release | 249/249 passed, 0 failed, 0 skipped |
| Core-test / EF-unit / EF-local Release builds | 0 warnings, 0 errors |
| Core/EF/EF-local requirement projections | 1/1, 1/1 and 1/1 passed |
| Scoped format, JSON and diff checks | Exit 0; no formatting or whitespace errors |
| Mutation probes | 6/6 material compiled isolated mutants killed; 1 equivalent excluded |

No unresolved correctness, lifecycle, cancellation, compatibility, coverage or architecture
finding remains in this admitted packet.

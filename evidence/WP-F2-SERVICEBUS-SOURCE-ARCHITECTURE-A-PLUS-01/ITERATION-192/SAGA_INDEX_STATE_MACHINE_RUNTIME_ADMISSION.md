# Iteration 192 — saga-index and state-machine runtime admission

## Outcome

The in-memory saga indices, state-machine condition/callback delegates, behavior proxies and event/
state observers are admitted. Staged index publication now has deterministic single-owner lifecycle
semantics, proxy boundaries reject invalid inputs, observers preserve filtering and asynchronous
terminal states, and unhandled-event failures are returned as faulted tasks.

The lead personally read all 19 current sources. They are all newly unique, moving cumulative exact
unique source coverage to 565/4,118 files (13.720%). The packet contained 1,344 physical lines before
the change and 1,501 lines after admitted documentation and implementation.

## Corrections

- A staged saga-index registration can publish exactly once. Concurrent, repeated and reentrant
  publication is rejected without losing rollback ownership; successful rollback is idempotent and
  failed rollback remains retryable.
- Required index getters, wrappers and lifecycle callbacks fail at their exact boundary. Captured
  keys remain stable, duplicate captures do not acquire false rollback ownership, and registration
  rejects late wrapper invalidation before publication.
- Behavior and exception proxies validate owners, events, messages and exceptions while preserving
  exact machine, saga, event, message, exception, completion and initializer views.
- Event/state observables fan out exact contexts and await incomplete tasks while preserving faults
  and cancellation. Selected and non-transition observers validate inputs, reject null Tasks and
  filter typed and untyped notifications correctly.
- Unhandled-event ignore/throw paths preserve cancellation and expose a faulted Task with exact
  machine/event/state diagnostics.
- Delegate signatures, variance, constraints and synchronous/asynchronous task shapes are directly
  pinned without changing their compatible public API.

## Source manifest

The manifest record is `path<TAB>content-sha256<LF>`, sorted ordinally. Manifest SHA-256 is
`0bf3cc145b14afb5430669df32af113398e3dd955e5b6723fdccc1b5210281c0`. Chaining that hash from
iteration 191 yields
`312e6722db2ed5df6696b949e09c80937c813f63541ebc30347226683a48fb67`.

| Source file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `src/ViciOne.ServiceBus.Sagas/Saga/InMemoryRepository/IIndexedSagaProperty.cs` | 42 | `da8891f0bd9ac5cc83ed9c8e045c032ac89c83501e15df75bd5d342dfbbbd309` |
| `src/ViciOne.ServiceBus.Sagas/Saga/InMemoryRepository/IStagedSagaIndex.cs` | 15 | `49b32fe2b191a7ea946cf7a4c6c63893ec0123fada691f9cee4c6019225b1cb4` |
| `src/ViciOne.ServiceBus.Sagas/Saga/InMemoryRepository/IndexedSagaDictionary.cs` | 275 | `035823eaa435ceca70c5eddd543dd2e18ff9dc2350f2e382e4a25aa5fde31b90` |
| `src/ViciOne.ServiceBus.Sagas/Saga/InMemoryRepository/IndexedSagaProperty.cs` | 248 | `0eee33a5c455f58a719c0ca4451365ab43fc161609dba875d366b8e0ec794d88` |
| `src/ViciOne.ServiceBus.Sagas/Saga/InMemoryRepository/SagaIndexRegistration.cs` | 90 | `00768fa6640683ba1e8fc3d894db3aabdab83d3b6a0a5737a228b5bb09ad2562` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/SendContextCallback.cs` | 22 | `162960d57be15615bf11c58e1ec9d58776c2436989159d875882408697cbe14d` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/SendExceptionContextCallback.cs` | 30 | `8cd221b510814064f28359c12ec6c4f20cdf7a63e49d2a7aace37f28615a1331` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/StateMachineAsyncCondition.cs` | 20 | `4a05d01ecfa03643bdea821b50eb5c58ab2ca64cd2d3abb19d0a084720e20575` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/StateMachineAsyncExceptionCondition.cs` | 26 | `1a1e143ca0e58b86e1219613850ec09974d5f1ae2410858b7bd4abfc6eb9da35` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/StateMachineCondition.cs` | 18 | `7bd1c10593fc903af44ce2ea44a20d0832a21831baec412f1397b36869509080` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/StateMachineExceptionCondition.cs` | 24 | `2797a11de08d4ec277d2881ec61a1e451866ae6a44675703aebfd2155945d093` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/UnhandledEventCallback.cs` | 10 | `ebff53f846631c9dec0aa8980948c9433f91efaa512c376f68980710ba9dcf8c` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/ViciOneServiceBusStateMachine.BehaviorContextProxy.cs` | 224 | `fe53621cb99b7e11375da1f2fe2dd61416a816c27e5164cc553d6efe23bfce5d` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/ViciOneServiceBusStateMachine.BehaviorExceptionContextProxy.cs` | 92 | `a4e368fd98c24faed2b7919eca32845813170ce023059f0b84cae756a2e95183` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/ViciOneServiceBusStateMachine.EventObservable.cs` | 81 | `d0bcc5b99412cf62993843476003c1f16f6090c164ff6ab897c15db22214adc1` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/ViciOneServiceBusStateMachine.NonTransitionEventObserver.cs` | 90 | `d5e8fa8a987f65ac39d6beac63d1013f11346a5a4b678fe92cc892a6c2ab35e8` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/ViciOneServiceBusStateMachine.SelectedEventObserver.cs` | 111 | `b03d716b8c5edc9c2411ca2707c4101bfa4c4544642e2145fa4f73b2c0680bcf` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/ViciOneServiceBusStateMachine.StateObservable.cs` | 29 | `57b0ffa2fb69c5042f1b3709cd58dfd9c6062918ddb6d8edacd2aa912201186b` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/ViciOneServiceBusStateMachine.UnhandledEventBehaviorContext.cs` | 54 | `0d5b3c886a4dd93e41e82596526b8ee96f92414ec2c08477a9073adabb669581` |

## Test manifest and requirements

Test manifest SHA-256 is
`42012d29421e51e21e72ac039b829aed3db8960775ab1402d34ef1025162e9df`; chained from iteration
191 it yields
`de3d944ccd9ea88eefb8697f51e147fbeb5cf3a00f36bb40e94c3f85a62336c6`.

| Test file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `tests/ViciOne.ServiceBus.Tests/Saga/InMemorySagaIndexDeepContractTests.cs` | 495 | `8dd52b78ae230693fb997a70ffa32d06cf131adfa45572f804697959db0fca11` |
| `tests/ViciOne.ServiceBus.Tests/SagaStateMachine/StateMachineBehaviorProxyObserverDeepContractTests.cs` | 689 | `8d712f9974fa35688953314bb45400bb1c3c4b208fd755d7309fd8398c52baba` |
| `tests/ViciOne.ServiceBus.Tests/Sagas/StateMachineConditionCallbackDelegateContractTests.cs` | 363 | `dce6b492c1ef015ca29ec870cd106fdeafa336dd2139e743ddaa77ababf6562a` |

The three classes contain 34 test methods, 70 expanded cases and 34 unique requirement variants.
`CoreRequirements.json` SHA-256 is
`e72db5fadf4128e3ac78e48ade9ef2bad06f2e26df59598e03c1f075cc7b6740`.

## Mutation proof

Eleven compiled, isolated single-cause mutants were killed and restored:

1. concurrent rollback was permitted to execute the callback twice;
2. a staged property rollback was detached from its exact wrapper;
3. a duplicate staged capture falsely acquired rollback ownership;
4. a failed publication was treated as an owned addition;
5. a failed rollback was made terminal instead of retryable;
6. behavior-context completion was replaced by an unrelated completed Task;
7. typed event raising was detached from state-machine execution;
8. exception-proxy creation discarded the exact exception;
9. observable fan-out detached incomplete, faulted and canceled observer Tasks;
10. typed transition notifications escaped the non-transition filter; and
11. unhandled-event diagnostics swapped event and state identity.

## Coverage and dispositions

Final Cobertura is `/private/tmp/vicione-servicebus-iteration-192-final5.cobertura.xml`, SHA-256
`4df5d2c8a278b8ac5c4bc8a5524b75498e2d52d2dd461129848b04cbb3d6362f`.
Admitted coverage is 425/443 executable lines (95.937%) and 221/238 branches (92.857%). All
behavior-proxy, exception-proxy, observable and selected/non-transition observer executable lines
are covered. `SagaIndexRegistration` is 36/36 lines; its one residual compiler branch has no
unproven lifecycle outcome.

Maximum method CRAP is 38.511 in `IndexedSagaDictionary.Add`; `IndexedSagaProperty.AddCaptured` is
17.976. Their uncovered lines are the defensive multi-index cleanup for collection/hash-publication
failures. Supported stable hash keys are closed primitive/enum/string/Guid/temporal types, wrapper
maps use reference identity, and ordinary inputs cannot deterministically inject a collection
failure; reaching these arms requires runtime allocation failure or invasive fault injection.
`BuildIndices` has CRAP 28 at full line coverage. Its residual declaring-type and failed-interface-
activation arms are invariants of `Type.GetProperties` and the exact constructed
`IndexedSagaProperty<,>` implementation. The internal unhandled-context constructor's remaining
null branches are unreachable from its validated state-machine call sites. The pending-removal
readmission interval has no public deterministic scheduling hook; its guard is retained as the
concurrency invariant and was not replaced by a reflective state-forcing test.

Sorted display-name SHA-256 is
`e2087c574c9b41b6248ebceb47f42ae9712da01d68152ccfc6da7d68da4c5dc1` across 5,291 displays.

| Gate | Result |
| --- | --- |
| Three final owned classes | 17/17, 7/7 and 46/46 passed |
| Saga / Sagas / SagaStateMachine namespace regressions | 57/57, 95/95 and 201/201 passed |
| Full Core Release | 5,291/5,291 passed, 0 failed, 0 skipped |
| Full EF unit Release | 249/249 passed, 0 failed, 0 skipped |
| Core-test / EF-unit / EF-local Release builds | 0 warnings, 0 errors |
| Core/EF/EF-local requirement projections | 1/1, 1/1 and 1/1 passed |
| Scoped format and diff checks | Exit 0; no whitespace errors |
| Mutation probes | 11/11 compiled isolated mutants killed |

No unresolved correctness, lifecycle, cancellation, concurrency, compatibility or architecture
finding remains in this admitted packet.

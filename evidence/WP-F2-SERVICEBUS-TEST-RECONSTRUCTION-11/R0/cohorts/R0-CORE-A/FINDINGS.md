# R0-CORE-A — findings

Cohort `R0-CORE-A`. Baseline `ae73c6da748e3bc3257dffa4971ee8680e086207`.
133 files read completely, 415 obligations recorded, anchor matched 415 / 415 in both directions.

Every file:line reference below was read in full; nothing here is inferred from a file name or a
search hit alone.

---

## 1. Rows that need a Lead decision (`QUESTION`, non-terminal)

| Obligation | Identity | Why |
|---|---|---|
| `OBL-R0-CORE-A-0022` | `SagaStateMachineTests.Automatonymous.Declaring_groups_in_a_state_machine.Should_allow_parallel_execution_of_events` | The method body is empty. The capability it is named for — `RunParallel(p => { p.Start<FillTank>(...); p.Start<CheckOil>(...); })` — is commented out in the fixture's own model (`Automatonymous/Group_Specs.cs:80-84`), and the `FillTank` / `CheckOil` machines it would start are defined but never used. Either parallel sub-machine execution is a removed product capability, which needs a decision and a removal record before `REMOVED_WITH_PRODUCT_CAPABILITY`, or it is an obligation still owed. R0 does not decide this. |
| `OBL-R0-CORE-A-0211` | `SagaStateMachineTests.Dynamic_Modify.Declaring_groups_in_a_state_machine.Should_allow_parallel_execution_of_events` | Same, and the file says so: `Dynamic Modify/Group_Specs.cs:7` carries the comment *"This test was pulled from the non-dynamic set; Seems incomplete with the commented out code below."* |
| `OBL-R0-CORE-A-0310` | `SagaStateMachineTests.Dynamic_Modify.When_an_event_is_declared.It_should_create_configured_events` | The assertion cannot fail. `Dynamic Modify/Event_Specs.cs:26` asserts `_eventB` (declared `Event<B>` and produced by `Event("EventB", x => x.CorrelateById(...), out _eventB)`) `Is.InstanceOf<TriggerEvent>()`. `MessageEvent<TMessage>` derives from `TriggerEvent` (`src/ViciOne.ServiceBus/SagaStateMachine/SagaStateMachine/MessageEvent.cs:6-8`), so every event this builder can produce satisfies the assertion. Nothing about the correlation configuration is observed. The intended obligation must be named before it can be rebuilt. |
| `OBL-R0-CORE-A-0062` | `SagaStateMachineTests.Automatonymous.SubStateOnEnter_Specs.Should_raise_both_enter_events` | The test contradicts itself. `Automatonymous/SubStateOnEnter_Specs.cs:26` carries the inline comment `// go to s21 --> Enter s2 is missing here!` while line 29 asserts that the observer on `s2.Enter` saw exactly 2 notifications. Either the comment records a known product gap that the assertion then locks in, or the comment is stale. The rebuild needs the Lead to state which `Enter` events fire when a substate is entered, and in what order, before this can become an executing test. |

---

## 2. Quality defects for the rebuild (section 11 of the Lead plan, TLP-011)

### 2.1 Shared mutable static state (highest severity in this cohort)

Three fixtures mutate process-wide product configuration from a **static constructor** and never undo it.
NUnit runs the assembly in one process, so the mutation is visible to every other test in
`ViciOne.ServiceBus.Tests`, including cohorts owned by other agents, and its timing depends on when the
CLR happens to run the type initializer.

- `SagaStateMachineTests/CorrelateUsingTopology_Specs.cs:60-63` → `GlobalTopology.Send.UseCorrelationId<BeginTransaction>` / `<CommitTransaction>`
- `SagaStateMachineTests/DynamicEvent_Specs.cs:16-20` → `GlobalTopology.Send.UseCorrelationId<Start>` / `<Stop>`
- `SagaStateMachineTests/SimpleStateMachine_Specs.cs:219-223` → `GlobalTopology.Send.UseCorrelationId<Start>` / `<Stop>`

A fourth case mutates static product configuration from an instance method:

- `SagaStateMachineTests/RequestRequest_Specs.cs:53-58` → `RequestStateMachine.RedeliverOnMissingInstance(...)` is a static call made from `ConfigureInMemoryBus`, with no reset.

And `EndpointConvention.Map<T>(...)` is a process-wide registration made without reset in
`SagaStateMachineTests/RequestRequest_Specs.cs:46,78` and
`SagaStateMachineTests/CompositeEventUpgrade_Specs.cs:136-137`.

### 2.2 Fixed sleeps used as synchronisation or as measurement

| Location | Use |
|---|---|
| `SagaStateMachineTests/InMemoryDeadlock_Specs.cs:27` | `await Task.Delay(990)` lines up a race against two 1000 ms activities (lines 77, 85) |
| `SagaStateMachineTests/RemoveWhen_Specs.cs:122` | `await Task.Delay(50)` between the response and the repository check |
| `SagaStateMachineTests/Automatonymous/Telephone_Sample.cs:25,74,110` and `Dynamic Modify/Telephone_Sample.cs:25,86,124` | `await Task.Delay(50)` is the *source* of the elapsed time the case then asserts (`ElapsedMilliseconds >= 45`) |
| `SagaStateMachineTests/Ignore_Specs.cs:34-44` | hand-written polling loop over `DateTime.Now` with `await Task.Delay(10)` |
| `SagaStateMachineTests/ScheduleTimeout_Specs.cs:29` | asserts wall-clock `Stopwatch.Elapsed >= 800 ms` as the behavioural claim |

The shared helpers these fixtures depend on carry the same pattern:
`src/ViciOne.ServiceBus/Testing/ExtensionMethodsForSagas.cs:29-40` and
`src/ViciOne.ServiceBus/Testing/StateMachineSagaTestingExtensions.cs:98-111` both poll with
`DateTime.Now` deadlines and `Task.Delay(10)`, and they are not cancellable.

`SagaStateMachineTests/UncorrelatedMessage_Specs.cs` is the one fixture in the cohort that already does
this correctly: it uses a retry observer's `TaskCompletionSource` as the ordering signal and says so in
its own comment (lines 18-19, 22-26). That is the pattern the rebuild should generalise.

### 2.3 Unbounded / blocking waits

`Task.Wait()` with no timeout and no token appears **60 times across 24 files** of this cohort
(`grep -rho '\.Wait()' | wc -l`), always inside `[OneTimeSetUp]`. Examples: `Automatonymous/Observable_Specs.cs:49-50,168-170,317-320`,
`Automatonymous/Activity_Specs.cs:25,128,199`, `Dynamic Modify/Observable_Specs.cs:63-64,192-194,375-378`.
`Saga/Locator/SagaExpression_Specs.cs:67-84` holds the only four `Wait(<token>)` calls in the cohort —
cancellable, but still synchronous blocking inside setup.

Two cases block on `.Result`: `Automatonymous/State_Specs.cs:130` and
`Dynamic Modify/State_Specs.cs:114` (`_machine.GetState(_instance).Result`).

### 2.4 Unobserved / fire-and-forget tasks

The returned `Task` is dropped entirely (no `await`, no `.Wait()`) at:

- `Automatonymous/Activity_Specs.cs:75` and `Dynamic Modify/Activity_Specs.cs:77` — the whole assertion of `When_specifying_an_event_activity_using_initially` depends on this task finishing first
- `Automatonymous/Introspection_Specs.cs:61` and `Dynamic Modify/Introspection_Specs.cs:87` — `NextEvents` is then asserted against the state that raise was supposed to produce
- `Automatonymous/Transition_Specs.cs:160` and `Dynamic Modify/Transition_Specs.cs:163` — the raise happens inside the observer's `using` block, so the observed count depends on the task completing before the subscription is disposed

`SagaStateMachineTests/Outbox_Specs.cs:103` calls `Bus.ConnectHandler<Fault<Start>>(...)` and drops the
returned `ConnectHandle` without disposing it; the handler's `count` is then read after
`await InactivityTask`.

`SagaStateMachineTests/SagaConfigurationObserver_Specs.cs:25` creates a bus with
`Bus.Factory.CreateUsingInMemory(...)` that is never disposed.

### 2.5 `async` without `await`

`async Task` test methods with no `await` in the body (they complete synchronously and the compiler
warns): `Automatonymous/Combine_Assigned_Specs.cs:30,146` and `Automatonymous/Retry_Specs.cs:13,24`
(the latter two use `Assert.That(async () => ..., Throws...)` rather than awaiting),
`SagaStateMachineTests/RequestRequest_Specs.cs:17`.

There is **no `async void`** anywhere in this cohort.

### 2.6 Order-dependent tests sharing fixture state

`InMemoryTestFixture` builds one bus and, in every fixture here, one saga repository per **fixture**,
not per case (`src/ViciOne.ServiceBus.TestFramework/InMemoryTestFixture.cs`; the repositories are created
inside `ConfigureInMemoryReceiveEndpoint`, which runs once). Fourteen fixtures in this cohort run more
than one case against that shared repository:

`Fault_Specs` (4), `CompositeEventUpgrade_Specs` (5), `Respond_Specs` (3), `UncorrelatedMessage_Specs` (3),
`RemoveWhen_Specs` (2), `Outbox_Specs` (2), `DynamicEvent_Specs` (2), `SimpleStateMachine_Specs` (2 per
fixture × 3 fixtures), `RequestRequest_Specs` (2), `Testing_Specs` (2), `NewOrExisting_Specs` (2),
`SagaExpression_Specs` (2).

Only `UncorrelatedMessage_Specs` documents the hazard and defends against it by giving each case its own
service name (lines 18-19). `Respond_Specs` (`Should_receive_the_response_message`,
`Should_start_and_report_status`) and `Outbox_Specs` create fresh correlation ids per case, which is
safe; the rest rely on `Guid.NewGuid()` / `NewId.NextGuid()` per case without stating why that is enough.

### 2.7 Non-reproducible randomness

`SagaStateMachineTests/WhenEnterRequest_Specs.cs:44` — the state machine under test awaits
`Task.Delay(new Random().Next(50, 1000))` inside a `WhenEnter` activity. Unseeded, non-reported and
inside the code path being asserted.

### 2.8 Swallowed exceptions

`Saga/InitiateSaga_Specs.cs:189-196` — the second `Send` is wrapped in `try { } catch (SagaException sex)`
with the only assertion inside the catch. If no exception is thrown the case passes silently. This is the
only swallowed-exception construct in the cohort.

### 2.9 Names that contradict what is asserted

- `Should_have_initial_state_with_zero` asserts `CurrentState == 3` in `Automatonymous/Combine_Specs.cs:120`, `Automatonymous/Combine_Assigned_Specs.cs:171` and `Dynamic Modify/Combine_Specs.cs:119`. Three is the index the machine assigns to `Waiting` (`StateAccessorIndex` seeds `[null, Initial, Final, ...declared]`, `src/ViciOne.ServiceBus/SagaStateMachine/SagaStateMachine/Accessors/StateAccessorIndex.cs:18`), so the assertion is right and the name is wrong.
- `When_message_correlation_is_not_configured.Should_retry_the_status_message` (`CorrelationUnknown_Specs.cs:13`) asserts a `ConfigurationException` at connect time; nothing is retried and there is no status message.
- `Raising_an_unhandled_event_when_the_state_machine_ignores_all_unhandled_events.Should_silenty_ignore_the_invalid_event` — spelling, both mirrors.

---

## 3. Strengthening obligations — behaviour the product still has that the test only asserts weakly

### 3.1 Cases that assert nothing at all (20 identities)

`ShouldContainSagaInState` and `ShouldContainSaga` return `default` on timeout instead of throwing
(`src/ViciOne.ServiceBus/Testing/StateMachineSagaTestingExtensions.cs:111` and
`ExtensionMethodsForSagas.cs:40`), so a dropped result is a silent pass, not a slow failure.

**The single worst case in the cohort:**
`SagaStateMachineTests/FaultRescue_Specs.cs:21` assigns
`Guid? saga = await _repository.ShouldContainSagaInState(..., _machine.FailedToStart, TestTimeout);`
and never asserts it. The `UseRescue` pipe body it is testing is entirely commented out (line 35).
The case therefore passes whether or not the saga ever reaches `FailedToStart`, and costs one
`TestTimeout` of wall time doing so. The obligation is real and the product capability exists; the test
does not check it.

The other nineteen fall into three groups, all `PROPOSED_REPLACED_EXECUTING` with a strengthening note
on the row:

- **Awaited-signal only** (a failure appears as a hung await, not a named assertion):
  `Saga.Configuring_a_message_in_a_saga.Should_include_all_the_stuff`,
  `SagaStateMachineTests.Configuring_a_message_in_a_saga.Should_include_all_the_stuff`,
  `Saga.Using_the_universal_saga_repository.Should_reach_the_saga`,
  `Saga.Using_the_universal_saga_repository_with_insert_on_initial.Should_reach_the_saga`,
  `Responding_from_within_a_saga.Should_receive_the_response_message`,
  `Responding_through_the_outbox.Should_receive_the_response_message`,
  `A_long_running_state_machine_initiated_by_a_request.Should_complete_the_request`.
- **Console output only** (obligation degenerates to "does not throw"):
  `Automatonymous.Telephone_Sample.Visualize.Draw`, `Dynamic_Modify.Telephone_Sample.Visualize.Draw`,
  `Automatonymous.When_visualizing_a_state_machine_again.Should_show_the_goods`,
  `CompositeEventOnRequestResponsesTests.Should_the_graph`,
  `CorrelationExpression_Specs.Should_convert_a_simple_correlation_expression`,
  `When_a_state_machine_fault_event_is_correlated_by_id.Should_not_require_explicit_configuration`
  (starts the harness and stops).
- **"No exception" only** (state and side effects after the ignored event are never checked):
  `Raising_an_ignored_event.Should_silently_ignore_the_invalid_event` (both mirrors),
  `Raising_an_unhandled_event_when_the_state_machine_ignores_all_unhandled_events.Should_silenty_ignore_the_invalid_event`
  (both mirrors), plus the two empty `Should_allow_parallel_execution_of_events` bodies already
  raised as `QUESTION` in §1.

### 3.2 Asserts that something happened but not which value, order or side effect

| Obligation | What is asserted | What the product does that is not asserted |
|---|---|---|
| `Automatonymous.When_using_retry_in_a_state_machine` (4 cases) | the exception surfaces, or a catch runs | `Instance.AttemptCount` is incremented on every attempt (`Retry_Specs.cs:62,79`) and is never asserted — the retry **count** for `Intervals(10,10,10)` is unverified |
| `When_a_send_faults_in_the_outbox` | some `InstanceCompleted` was published for the id | the machine can publish `Result = "Success"` (`OutboxFault_Specs.cs:170`) or `Result = "Faulted"` (line 152). The two outcomes are the whole point of the fixture and are not distinguished |
| `Scheduling_a_message_from_a_state_machine` | `Stopwatch.Elapsed >= 800 ms` | the received `CartRemoved.MemberNumber` is never asserted, so correlation of the scheduled message is not checked; and the assertion is on wall-clock time rather than on the schedule |
| `Saga.Locator.SagaExpression_Specs` (2 cases) | some instance matched | the fixture seeds two instances with different `Name` values (`SagaExpression_Specs.cs:61-86`); it never asserts that the non-matching one was **excluded**, which is the actual filter semantics |
| `Introspection_Specs.The_next_events_should_be_known` (both mirrors) | `events.Count == 3` | which three events, and their identity, are not asserted |
| `Saga.Injecting_properties_into_a_saga` | the instance exists | the injected `IDependency` set by `UseExecute` (`Injecting_Specs.cs:45`) is never read back |
| `Partitioning_a_saga` (both, 100 instances each) | all 100 instances exist | the partitioner (`UsePartitioner(4, ...)`) is configured but nothing asserts that messages for one correlation id stayed on one partition; the `Stopwatch` is printed, never asserted |
| `InMemoryDeadlock_Specs` | a later, unrelated instance can be created | the `Cancel` message's own effect on the first instance is never asserted, and "no deadlock" is inferred rather than observed |
| `Correlation_a_state_machine_by_guid`, `Using_topology_for_event_correlation` | the instance is found in `Active`/`Final` | both configure `UsePartitioner(4, ...)`; the partitioning is not asserted |
| `Specifying_no_topology.Should_not_bind_the_event_handler` | the instance is still in `Running` | "not bound to the consume topology" is inferred from the state being unchanged; the endpoint's topology itself is not inspected |
| `When_an_event_is_defined_as_ignored_for_state` | no `Fault` observed in 3 s | the instance state **after** the ignored duplicate is not re-checked; the final assertion relies on `?.` short-circuiting to `null` (`Ignore_Specs.cs:29`) |
| `Automatonymous/Dynamic_Modify Telephone_Sample` (6 cases) | `ElapsedMilliseconds >= 45` | there is no upper bound, so a timer that was never stopped also passes; `Number` captured from `PhoneServiceEstablished` is never asserted |
| `Saga.When_an_existing_saga_receives_an_initiating_message` | `sex.MessageType` inside a catch | passes when no exception is thrown at all (§2.8) |
| `Using_a_base_state_machine` | both events were consumed | the inherited `Happy` / `GoLucky` / `Finished` states are never observed, although the case is named `Should_initialize_all_states_and_events` |

---

## 4. Environment classification (assignment item 4)

All 415 obligations are hermetic. **None needs a broker, a database, an emulator or any external
service.** Profile `UnitArchitecture` for all 415.

| Environment | Obligations | What it means |
|---|---:|---|
| `hermetic-none` | 336 | pure state machine: `new machine()` + `RaiseEvent` / `TransitionToState`, no bus, no transport |
| `hermetic-inmemory` | 72 | `InMemoryTestFixture` / `InMemoryTestHarness` — the in-memory transport only |
| `hermetic-harness` | 7 | `ServiceCollection().AddViciOneServiceBusTestHarness(...)` — DI container plus the in-memory transport |

The classification was cross-checked mechanically against each source file (presence of
`InMemoryTestFixture`, `AddViciOneServiceBusTestHarness`, `InMemoryTestHarness` or
`CreateUsingInMemory`): **0 mismatches**.

Two constructs deserve the Lead's attention even though they are hermetic:

- `ScheduleTimeout_Specs` uses `UseDelayedMessageScheduler()` and `CompositeEventUpgrade_Specs` uses
  `UsePublishMessageScheduler()`. Both are in-process schedulers — no Quartz, no external timer.
- `Choir_Specs` and four other fixtures use the DI harness; the container is created and disposed inside
  the case.

---

## 5. Saga semantics coverage (assignment item 3) — and where the risk sits

Every semantic the assignment names is present in this cohort. The table gives the obligation count and
the strongest evidence file.

Counts below are measured: each ledger row carries a `sagaSemantics=` tag list in `notes`, and the
count is the number of the 415 obligations whose tag list contains that semantic. One obligation can
carry several semantics, so the column does not sum to 415.

| Semantic | Obligations | Anchor evidence |
|---|---:|---|
| Fault and compensation paths (`fault path`, `Catch`, `compensation`, `rescue pipe`, `exception filtering`, `request fault`) | 84 / 69 / 3 / 1 / 1 / 5 | `Exception_Specs.cs`, `Fault_Specs.cs`, `CatchFault_Specs.cs`, `CatchInitial_Specs.cs`, `Faulted_Specs.cs`, `FaultRescue_Specs.cs`, `FilterFault_Specs.cs` |
| State definition and transitions (`state definition`, `state transitions`, `TransitionTo`, `state transition events`, `BeforeEnter`, `WhenEnter`, `Enter/Leave hooks`) | 40 / 29 / 8 / 33 / 12 / 39 / 6 | `State_Specs.cs`, `Activity_Specs.cs`, `Transition_Specs.cs`, `EnterEvent_Specs.cs`, `AnyStateTransition_Specs.cs` |
| Composite events (incl. `CompositeEventOptions`, `IncludeInitial`, `across multiple states`) | 46 / 4 / 1 / 2 | `Combine*_Specs.cs`, `Composite*_Specs.cs`, `CompositeEventUpgrade_Specs.cs` |
| Observers (`observers`, `state observer`, `event observer`, `saga configuration observer`) | 66 / 6 / 4 / 1 | `Observable_Specs.cs`, `EventObservable_Specs.cs`, `SubStateOnEnter_Specs.cs`, `SagaConfigurationObserver_Specs.cs` |
| Substates | 41 | `Observable_Specs.cs`, `SubStateOnEnter_Specs.cs`, `Telephone_Sample.cs` |
| Lifecycle completion and removal (`lifecycle completion`, `saga removal`, `Finalize`, `Finally`) | 70 / 6 / 4 / 6 | `Finalize_Specs.cs`, `RemoveWhen_Specs.cs`, `CatchFault_Specs.cs`, `CatchInitial_Specs.cs` |
| `During` / `DuringAny` / `Ignore` / `OnUnhandledEvent` / unhandled events | 20 / 2 / 9 / 2 / 13 | `Anytime_Specs.cs`, `UnobservedEvent_Specs.cs`, `Ignore_Specs.cs` |
| Introspection and `NextEvents` | 70 / 18 | `Introspection_Specs.cs`, `AutomatonymousStateMachine_Specs.cs`, `Combine_Assigned_Specs.cs` |
| State accessors and state expressions (`state accessor`, `int state accessor`, `state expression (repository query)`, `state serialization`) | 22 / 13 / 18 / 2 | `StateExpression_Specs.cs`, `State_Specs.cs`, `SerializeState_Specs.cs` |
| Event correlation — `CorrelatedBy` / topology convention / by property / `SelectId` / correlation expression / saga query / `Fault<T>` correlation / configuration validation | 4 / 7 / 8 / 1 / 5 / 4 / 5 / 1 | `InitiateSaga_Specs.cs`, `CorrelateUsingTopology_Specs.cs`, `CorrelateGuid_Specs.cs`, `MissingInstance_Specs.cs`, `SagaExpression_Specs.cs`, `CorrelationExpression_Specs.cs`, `CorrelateFaultById_Specs.cs`, `CorrelationUnknown_Specs.cs` |
| Missing-instance behaviour (incl. redelivery) | 8 / 3 / 2 | `MissingInstance_Specs.cs`, `Fault_Specs.cs`, `Respond_Specs.cs`, `UncorrelatedMessage_Specs.cs`, `RequestRequest_Specs.cs` |
| Initial vs. existing instance (`initial instance creation`, `initial vs existing instance`, `InsertOnInitial`, `saga factory`, `InitiatedByOrOrchestrates`, `Observes`, `Orchestrates`) | 12 / 5 / 2 / 2 / 2 / 1 / 1 | `InitiateSaga_Specs.cs`, `NewOrExisting_Specs.cs`, `RepositoryContext_Specs.cs`, `Initiator_Specs.cs` |
| `Request` / `Response` and timeout (incl. multiple response types, `RequestStarted/Completed/Faulted`, `Pending`) | 9 + 9 / 6 / 2 / 2 / 1 | `Request_Specs.cs`, `Request2_Specs.cs`, `Request3_Specs.cs`, `RequestRequest_Specs.cs`, `CompositeEventUpgrade_Specs.cs`, `WhenEnterRequest_Specs.cs` |
| `Schedule` and message scheduling (incl. automatic correlation, redelivery) | 2 / 2 / 6 / 1 / 1 | `ScheduleTimeout_Specs.cs`, `ScheduleCorrelation_Specs.cs`, `OutboxFault_Specs.cs` |
| Outbox | 14 | `Outbox_Specs.cs`, `OutboxFault_Specs.cs`, `Request*_Specs.cs`, `CompositeEventUpgrade_Specs.cs` |
| Duplicate and out-of-order events (incl. duplicate suppression, competing events) | 7 / 4 / 1 / 1 | `Combine_Specs.cs` (`RaiseOnce`), `CompositeCondition_Specs.cs`, `InitiateSaga_Specs.cs`, `Ignore_Specs.cs`, `Outbox_Specs.cs` |
| Concurrency and locking (`concurrency`, `concurrency/partitioner`, `partitioner`, `in-memory repository locking`, `optimistic concurrency remedy`) | 4 / 2 / 3 / 1 / 1 | `Choir_Specs.cs`, `InMemoryDeadlock_Specs.cs`, `PartitionSaga_Specs.cs`, `Partitioning_Specs.cs` |
| Saga repository semantics (in-memory) | 2 / 1 / 1 | `RepositoryContext_Specs.cs`, `InMemoryDeadlock_Specs.cs` |
| Saga pipe configuration, message headers, publish/send from a saga | 3 / 3 / 2 + 1 | `Message_Specs.cs`, `MessageSaga_Specs.cs`, `Publish_Specs.cs`, `Send_Specs.cs`, `Initiator_Specs.cs` |
| Retry | 9 | `Retry_Specs.cs`, `FilterFault_Specs.cs`, `Outbox_Specs.cs`, `Choir_Specs.cs` |
| Visualization | 9 | `Visualizer_Specs.cs` (both mirrors), `Visualizer2_Specs.cs`, `Telephone_Sample.cs` |

### The three areas R0-CORE-A considers highest risk for the rewrite

1. **Composite events (47 obligations).** The largest single cluster and the one with the most
   configuration-order dependence: whether `CompositeEvent` is declared before, between or after the
   `During` bindings of its constituents changes nothing (`CompositeEventMultipleStates_Specs`), the
   completion bitmask is built from the declaration order of the constituent array
   (`ViciOneServiceBusStateMachine.cs`, `CompositeEvent(...)` → `complete` mask), and the set of states
   the tracking activity is bound into is chosen by a filter over `_stateCache` at declaration time —
   so a state declared *after* the composite event never gets the tracking activity. `IncludeInitial`
   and `IncludeFinal` open exactly that door. `CompositeEventUpgrade_Specs` then stacks four composite
   events over the same four request outcomes. A rewrite that changes declaration order, or that
   materialises states lazily, will break this silently, and half of the cluster only asserts a boolean
   flag.
2. **Fault, catch and compensation (~60 obligations).** `Catch<T>` matching by assignability, exactly
   one catch running, the activity after the throw not running, `Retry` composed with `Catch`,
   `Catch` inside an `IfElse` branch, `Finalize` inside a catch, `Respond`/`Publish` from inside a
   catch, an activity's own `Faulted` hook compensating a partial write. This is where the ordering
   guarantees are densest and where the current tests are weakest: the retry **count** is never
   asserted, and `FaultRescue_Specs` asserts nothing at all.
3. **Correlation and missing-instance behaviour (28 obligations).** Five different correlation
   mechanisms — `CorrelatedBy<Guid>`, `CorrelateById(message)`, `CorrelateById(instance, message)` with
   `SelectId`, `CorrelateBy(property)`, and the `GlobalTopology` send convention — plus automatic
   `Fault<T>` correlation and automatic correlation of scheduled `CorrelatedBy` messages. Three of these
   fixtures currently establish the contract by mutating global static state, which means the rebuild
   has to re-derive the contract rather than port the test.

---

## 6. Gaps found in the current product surface with no obligation in this cohort

Read for this: `src/ViciOne.ServiceBus/SagaStateMachine/**` (144 files enumerated, `ViciOneServiceBusStateMachine.cs`
read in full, plus `MessageEvent.cs`, `TriggerEvent.cs`, `Activities/CompositeEventActivity.cs`,
`Accessors/StateAccessorIndex.cs`), `src/ViciOne.ServiceBus/Sagas/**` (68 files enumerated; read in full: `InMemorySagaRepository.cs`,
`Saga/NewSagaPolicy.cs`, `NewOrExistingSagaPolicy.cs`, `AnyExistingSagaPolicy.cs`,
`InMemoryRepository/IndexedSagaDictionary.cs`), `src/ViciOne.ServiceBus/Testing/ExtensionMethodsForSagas.cs`
and `StateMachineSagaTestingExtensions.cs`, and the abstractions
`Saga/{ConcurrencyMode,ISagaVersion,ISagaQuery,ISagaPolicy,InitiatedBy,InitiatedByOrOrchestrates,Observes,Orchestrates}.cs`,
`SagaStateMachine/{Event,EventCorrelation,CompositeEventStatus}.cs` and
`SagaStateMachine/Configuration/{CompositeEventOptions,IEventCorrelationConfigurator,IMissingInstanceConfigurator,IRequestConfigurator,IScheduleConfigurator}.cs`.

These are observable product behaviours with **no** covering obligation anywhere in the 415. They are
new-gap candidates for the Lead's second ledger input, not deletions:

| Gap | Product evidence | Grep result in this cohort |
|---|---|---|
| `CompositeEventOptions.IncludeFinal` | `CompositeEventOptions.cs:18` and the state filter in `ViciOneServiceBusStateMachine.CompositeEvent(...)` | no occurrence |
| `IMissingInstanceConfigurator.Discard()` | `IMissingInstanceConfigurator.cs:11` | no occurrence (`Fault()` and `ExecuteAsync` are covered) |
| `ConcurrencyMode.Optimistic` / `Pessimistic` | `Saga/ConcurrencyMode.cs` | no occurrence — the cohort tests concurrency only through retry + outbox (`Choir_Specs`) and repository locking (`InMemoryDeadlock_Specs`) |
| `ISagaVersion` (version-based optimistic concurrency) | `Saga/ISagaVersion.cs` | implemented once, in `Visualizer2_Specs.cs:17` (a sample instance), never asserted |
| `SetCompleted(Func<...>)` overloads | `ViciOneServiceBusStateMachine.cs:301-311` | no occurrence — only `SetCompletedWhenFinalized()` is exercised |
| `Name(machineName)` | `ViciOneServiceBusStateMachine.cs:270` | no occurrence |
| `CompositeEvent` guard rails: null array, empty array, more than 31 events, an uninitialised event | `ViciOneServiceBusStateMachine.cs`, `CompositeEvent(Event, accessor, options, events)` — four `ArgumentException` / `ArgumentNullException` paths | no occurrence |
| `IndexedAttribute` secondary indices in the in-memory repository | `IndexedSagaDictionary.BuildIndices()` indexes `CorrelationId` plus every `[Indexed]` property | no occurrence — every index path exercised here is the `CorrelationId` one |
| `UnknownStateException` / `UnknownEventException` | `ViciOneServiceBusStateMachine.cs:115,123,133,143,153,168` | no occurrence |
| `Unschedule` as an asserted effect | used once at `OutboxFault_Specs.cs:166` inside `WhenEnter(Final)` | never asserted; no case checks that the pending schedule token was cancelled |

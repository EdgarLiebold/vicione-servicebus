# T42: Azure Functions receiver pipeline isolation

Base: `10e8568e1736bb2c03d1518dff30a75ed5de9b34`.
Microsoft code-testing-agent focused workflow and run-tests, MTP/xUnit v3.
MessageReceiver and its receiver builders, dispatch pipeline, registration
contexts and neighboring tests were read before changing the implementation.

## Product defects and correction

The unmodified receiver caches by transport path alone. The baseline reproduces
wrong delivery for both queues and subscriptions: requesting consumer B after A
delivers B's message to A. Expanded cases also show that configuring all consumers
requires the optional Saga capability even in consumer-only registrations.

Cache identity now contains path, subscription, explicit queue/subscription
discriminator, dispatch kind and target type. Every receiver builds with a fresh
registration context retaining the original selector, bus identity and scoped
consume-context setter. This prevents already-configured handler state leaking
between pipelines. All-handler dispatch only configures sagas when that capability
is present; invalid registered saga capabilities still follow normal validation.

## Requirements and behavior evidence

All tests are in `MessageReceiverIsolationTests`; seven requirement-manifest entries
map the methods to `REQ-VSB-ASB-FUNCTIONS-TESTING`.

| Product contract | Exact test method | Cases |
| --- | --- | ---: |
| Requested consumer type, exact payload and no duplicate delivery at one path | SamePath_DispatchesOnlyToTheRequestedConsumerAsync | 2 |
| Typed and all-consumer pipelines remain independent in either order | TypedAndAllDispatch_KeepTheirConsumerSetsIndependentAsync | 4 |
| Each different path includes all registered consumers | DifferentPaths_ConfigureAllConsumersForEachReceiverAsync | 2 |
| Invalid subscription cannot reuse an existing queue pipeline | InvalidSubscription_CannotReuseAnExistingQueuePipelineAsync | 1 |
| Saga types retain independent state and exact update counts, including typed-to-all dispatch | SagaDispatch_PreservesSelectedTypesAndIndependentStateAsync | 4 |
| Activity types and consumer/activity roles of one CLR type remain distinct | ActivityDispatch_SeparatesActivityTypesAndConsumerRoleAsync | 1 |
| Default/typed bus ownership and injected ambient context remain correct | TypedBusDispatch_PreservesRegistrationOwnerAndAmbientContextAsync | 1 |

Consumers, deserialization, saga repositories and activity execution are real.
A passive bus-lifetime collaborator prevents automatic broker startup. GUID message
IDs and a non-null assertion make ambient identity comparisons meaningful. All
consumer sets are compared without imposing parallel dispatch order. Saga state
is loaded from its actual in-memory repository. Activity execution uses a real
routing slip; its subscription selects only compensation failure, so normal
completion has no event destination to contact.

No broker startup, settlement or event-delivery claim is made. Custom external
registration contexts resolving an unqualified consume-context setter are not
covered by the standard DI ownership test; no affected production caller was found.

## Verification and counterprobes

- MAIN `artifacts/t42-baseline.log`: original product fails both consumer cases
  with expected B:second versus actual A:second.
- MAIN `artifacts/t42-expanded-baseline.log`: original product fails all eight
  expanded consumer cases; absent Saga capability explains all-first failures.
- MAIN `artifacts/t42-multibus-owner.log`: final focused 15/15, exit 0, no skips.
- MAIN `artifacts/t42-full.log`: 395/395 including requirements, exit 0, no skips.
- MAIN `artifacts/t42-format.log`: verify-only whitespace formatting, exit 0.
- GATE `/private/tmp/servicebus-reply-investigation`, deliberate faults, all exit 2:
  - `t42-mutant-type-retry2.log`: ignore target type, 5 failures/10 controls.
  - `t42-mutant-context.log`: reuse shared registration context, 7 failures/8 controls.
  - `t42-mutant-kind.log`: ignore dispatch kind, 1 failure/14 controls.
  - `t42-mutant-subscription.log`: ignore transport discriminator, 1 failure/14 controls.
  - `t42-mutant-optional-saga.log`: require Saga capability, 7 failures/8 controls.
- All deliberate changes manually restored; product, test and manifest compare
  byte-identically MAIN/GATE. GATE `artifacts/t42-restored.log`: 395/395, exit 0,
  no skips, explicit build with one node followed by tests without rebuilding.

Failed attempts remain preserved, not counted as proof: initial mutant build
MSB4166; retry with `dotnet test -m:1` selected zero tests. Separate build `-m:1`
and `dotnet test --no-build` supplied valid evidence. Earlier test-authoring attempts
had corrected type/import/registration errors. A saga run was terminated after
unexpected credential network activity; subsequent local diagnostic execution
exposed the test's incorrect initiate-only saga contract. Test sagas now allow
initiation and continuation, use local credentials, suppress fault publication
and have a bounded timeout. The final runs above pass.

Read-only adversarial implementation and final evidence reviews found no concrete
blocker within the reviewed scope. The final review confirmed all five detected
faults, restored 395/395 control, byte-identical product/test/manifest files and
the strengthened message identity assertion. Broker/settlement behavior and
external registration-context fallback remain outside this packet's proof.
Exact-commit measurement at `c28e9feb41f8146869e33b0f361ecd3e920f581b` passes
all 33 profiles and 12,923 executions, with all four fixture groups exiting 0.
See [full profile and remaining gaps](product-wide-profile-c28e9feb4.md).
Global A+ stays open.
